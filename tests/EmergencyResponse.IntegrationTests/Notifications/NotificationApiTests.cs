using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EmergencyResponse.IntegrationTests.Notifications;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class NotificationApiTests : IClassFixture<EmergencyResponseWebApplicationFactory>
{
    private readonly EmergencyResponseWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationApiTests(EmergencyResponseWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAdminAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest
            {
                Email = "admin@test.local",
                Password = "Admin@Test1!"
            });
        loginResp.EnsureSuccessStatusCode();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<string> RegisterAndLoginCitizenAsync()
    {
        var email = $"citizen.{Guid.NewGuid():N}@test.local";
        const string password = "Test@123!";

        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest
            {
                FirstName = "Citizen",
                LastName = "User",
                Email = email,
                Password = password
            });
        registerResp.EnsureSuccessStatusCode();

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    [Fact]
    public async Task GetNotifications_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/notifications/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_AuthenticatedAdmin_ReturnsOkWithSummary()
    {
        var token = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/notifications/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<NotificationSummary>(JsonOptions);
        Assert.NotNull(summary);
    }

    [Fact]
    public async Task MarkRead_NonExistentNotification_ReturnsNotFound()
    {
        var token = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_ExistingNotification_ReturnsNoContent()
    {
        var token = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var notificationId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Notifications.Add(new Notification
            {
                Id = notificationId,
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.EscalationOpened,
                Priority = NotificationPriority.Critical,
                Status = NotificationStatus.Unread,
                Title = "Integration Test Escalation",
                Message = "Testing mark read endpoint",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/notifications/{notificationId}/read", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = await db.Notifications.FindAsync(notificationId);
            Assert.NotNull(item);
            Assert.True(item.IsRead);
            Assert.Equal(NotificationStatus.Read, item.Status);
        }
    }

    [Fact]
    public async Task MarkRead_CitizenAccessingOperatorNotification_ReturnsForbidden()
    {
        var citizenToken = await RegisterAndLoginCitizenAsync();
        var notificationId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Notifications.Add(new Notification
            {
                Id = notificationId,
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.EscalationOpened,
                Priority = NotificationPriority.Critical,
                Status = NotificationStatus.Unread,
                Title = "Operator Only Alert",
                Message = "Sensitive incident escalation",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", citizenToken);

        var response = await _client.PostAsync($"/api/notifications/{notificationId}/read", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Authenticated_ReturnsNoContent()
    {
        var token = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var notif1 = Guid.NewGuid();
        var notif2 = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Notifications.AddRange(
                new Notification
                {
                    Id = notif1,
                    TargetRole = ApplicationRoles.Operator,
                    Type = NotificationType.IncidentCreated,
                    Priority = NotificationPriority.Important,
                    Status = NotificationStatus.Unread,
                    Title = "Notif 1",
                    Message = "Msg",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Notification
                {
                    Id = notif2,
                    TargetRole = ApplicationRoles.EmergencyAdmin,
                    Type = NotificationType.ResourceShortage,
                    Priority = NotificationPriority.Urgent,
                    Status = NotificationStatus.Unread,
                    Title = "Notif 2",
                    Message = "Msg",
                    CreatedAtUtc = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync("/api/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RoleIsolation_CitizenDoesNotSeeOperatorAlerts()
    {
        var adminToken = await LoginAdminAsync();
        var citizenToken = await RegisterAndLoginCitizenAsync();

        var opNotifId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Notifications.Add(new Notification
            {
                Id = opNotifId,
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.EscalationOpened,
                Priority = NotificationPriority.Critical,
                Status = NotificationStatus.Unread,
                Title = "Top Secret Operational Incident Escalation",
                Message = "Only operators should receive this alert.",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Citizen check
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", citizenToken);
        var citizenResp = await _client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, citizenResp.StatusCode);
        var citizenItems = await citizenResp.Content.ReadFromJsonAsync<List<NotificationItem>>(JsonOptions);
        Assert.NotNull(citizenItems);
        Assert.DoesNotContain(citizenItems, n => n.Id == opNotifId);

        // Admin check
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminResp = await _client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, adminResp.StatusCode);
        var adminItems = await adminResp.Content.ReadFromJsonAsync<List<NotificationItem>>(JsonOptions);
        Assert.NotNull(adminItems);
        Assert.Contains(adminItems, n => n.Id == opNotifId);
    }
}
