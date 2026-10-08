using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Infrastructure.Notifications;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EmergencyResponse.UnitTests.Notifications;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class NotificationServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NotificationTestDbContext _context;
    private readonly FakeCurrentUserService _currentUserService;
    private readonly FakeIncidentNotificationService _notificationDispatchService;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new NotificationTestDbContext(options);
        _context.Database.EnsureCreated();

        _currentUserService = new FakeCurrentUserService
        {
            UserId = "user-op-1",
            Roles = new List<string> { ApplicationRoles.Operator }
        };

        _notificationDispatchService = new FakeIncidentNotificationService();

        _service = new NotificationService(
            _context,
            _currentUserService,
            _notificationDispatchService,
            NullLogger<NotificationService>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_PersistsNotification_AndDispatchesRealtimeEvent()
    {
        var request = new NotificationRequest
        {
            Type = NotificationType.IncidentCreated,
            Priority = NotificationPriority.Important,
            Title = "New Incident Reported",
            Message = "Structure fire at 4th St",
            TargetRole = ApplicationRoles.Operator,
            DeduplicationKey = "NewIncident:123"
        };

        var result = await _service.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("New Incident Reported", result.Title);
        Assert.Equal(NotificationPriority.Important, result.Priority);
        Assert.False(result.IsRead);

        var dbItem = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == result.Id);
        Assert.NotNull(dbItem);
        Assert.Equal(NotificationStatus.Unread, dbItem.Status);

        Assert.Single(_notificationDispatchService.CreatedNotifications);
        Assert.Equal(result.Id, _notificationDispatchService.CreatedNotifications[0].Id);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUnreadKey_SuppressesDuplicate()
    {
        var request = new NotificationRequest
        {
            Type = NotificationType.EscalationOpened,
            Priority = NotificationPriority.Critical,
            Title = "Escalation Alert",
            Message = "Casualties detected",
            DeduplicationKey = "EscalationOpened:dup-1",
            TargetRole = ApplicationRoles.Operator
        };

        var first = await _service.CreateAsync(request);
        Assert.NotNull(first);

        var second = await _service.CreateAsync(request);
        Assert.Null(second);

        var count = await _context.Notifications.CountAsync(n => n.DeduplicationKey == "EscalationOpened:dup-1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateAsync_ExpiredDuplicateKey_AllowsNewNotification()
    {
        var requestExpired = new NotificationRequest
        {
            Type = NotificationType.ResourceShortage,
            Priority = NotificationPriority.Urgent,
            Title = "Shortage Alert",
            Message = "Need engine",
            DeduplicationKey = "Shortage:exp-1",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
            TargetRole = ApplicationRoles.Operator
        };

        var first = await _service.CreateAsync(requestExpired);
        Assert.NotNull(first);

        var requestNew = new NotificationRequest
        {
            Type = NotificationType.ResourceShortage,
            Priority = NotificationPriority.Urgent,
            Title = "Shortage Alert 2",
            Message = "Need engine urgently",
            DeduplicationKey = "Shortage:exp-1",
            TargetRole = ApplicationRoles.Operator
        };

        var second = await _service.CreateAsync(requestNew);
        Assert.NotNull(second);

        var count = await _context.Notifications.CountAsync(n => n.DeduplicationKey == "Shortage:exp-1");
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetForCurrentUserAsync_ReturnsPermittedNotifications_ScopingCorrectly()
    {
        // Direct to user-op-1
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = "user-op-1",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Unread,
            Title = "Direct To You",
            Message = "Test",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Targeted to Operator role (which user has)
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TargetRole = ApplicationRoles.Operator,
            Type = NotificationType.EscalationOpened,
            Priority = NotificationPriority.Critical,
            Status = NotificationStatus.Unread,
            Title = "Role Alert",
            Message = "Test",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Targeted to different user
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = "user-other-999",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Unread,
            Title = "Other User Alert",
            Message = "Test",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Targeted to different role
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TargetRole = ApplicationRoles.Responder,
            Type = NotificationType.ResourceAssignment,
            Priority = NotificationPriority.Important,
            Status = NotificationStatus.Unread,
            Title = "Responder Alert",
            Message = "Test",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var results = await _service.GetForCurrentUserAsync(10);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Title == "Direct To You");
        Assert.Contains(results, r => r.Title == "Role Alert");
        Assert.DoesNotContain(results, r => r.Title == "Other User Alert");
        Assert.DoesNotContain(results, r => r.Title == "Responder Alert");
    }

    [Fact]
    public async Task GetSummaryAsync_CalculatesCorrectUnreadCounts()
    {
        _context.Notifications.AddRange(
            new Notification
            {
                Id = Guid.NewGuid(),
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.EscalationOpened,
                Priority = NotificationPriority.Critical,
                Status = NotificationStatus.Unread,
                Title = "Critical Alert",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.ResourceShortage,
                Priority = NotificationPriority.Urgent,
                Status = NotificationStatus.Unread,
                Title = "Urgent Alert",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.AIReviewRequired,
                Priority = NotificationPriority.Important,
                Status = NotificationStatus.Unread,
                Title = "Important Alert",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.System,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Read,
                IsRead = true,
                Title = "Read Notification",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            }
        );

        await _context.SaveChangesAsync();

        var summary = await _service.GetSummaryAsync();

        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalUnread);
        Assert.Equal(1, summary.CriticalUnread);
        Assert.Equal(1, summary.UrgentUnread);
        Assert.Equal(1, summary.ImportantUnread);
    }

    [Fact]
    public async Task MarkAsReadAsync_UpdatesStatusAndReadAtUtc()
    {
        var id = Guid.NewGuid();
        _context.Notifications.Add(new Notification
        {
            Id = id,
            UserId = "user-op-1",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Unread,
            IsRead = false,
            Title = "Alert",
            Message = "Msg",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _service.MarkAsReadAsync(id);

        var item = await _context.Notifications.FindAsync(id);
        Assert.NotNull(item);
        Assert.True(item.IsRead);
        Assert.Equal(NotificationStatus.Read, item.Status);
        Assert.NotNull(item.ReadAtUtc);
    }

    [Fact]
    public async Task MarkAsReadAsync_UnpermittedNotification_ThrowsUnauthorizedAccessException()
    {
        var id = Guid.NewGuid();
        _context.Notifications.Add(new Notification
        {
            Id = id,
            UserId = "someone-else",
            TargetRole = ApplicationRoles.Responder,
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Status = NotificationStatus.Unread,
            Title = "Secret",
            Message = "Msg",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.MarkAsReadAsync(id));
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistentNotification_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.MarkAsReadAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllPermittedUnreadNotificationsAsRead()
    {
        var otherUserId = Guid.NewGuid();
        var myNotif1 = Guid.NewGuid();
        var myNotif2 = Guid.NewGuid();

        _context.Notifications.AddRange(
            new Notification
            {
                Id = myNotif1,
                UserId = "user-op-1",
                Type = NotificationType.System,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Unread,
                Title = "Alert 1",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = myNotif2,
                TargetRole = ApplicationRoles.Operator,
                Type = NotificationType.EscalationOpened,
                Priority = NotificationPriority.Critical,
                Status = NotificationStatus.Unread,
                Title = "Alert 2",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = otherUserId,
                UserId = "someone-else",
                TargetRole = ApplicationRoles.Responder,
                Type = NotificationType.System,
                Priority = NotificationPriority.Normal,
                Status = NotificationStatus.Unread,
                Title = "Other Unread",
                Message = "Msg",
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        await _service.MarkAllAsReadAsync();

        var item1 = await _context.Notifications.FindAsync(myNotif1);
        Assert.NotNull(item1);
        Assert.True(item1.IsRead);
        Assert.Equal(NotificationStatus.Read, item1.Status);

        var item2 = await _context.Notifications.FindAsync(myNotif2);
        Assert.NotNull(item2);
        Assert.True(item2.IsRead);
        Assert.Equal(NotificationStatus.Read, item2.Status);

        var otherItem = await _context.Notifications.FindAsync(otherUserId);
        Assert.NotNull(otherItem);
        Assert.False(otherItem.IsRead);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class FakeIncidentNotificationService : IIncidentNotificationService
    {
        public List<Notification> CreatedNotifications { get; } = new();

        public Task NotifyNotificationCreatedAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            CreatedNotifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task NotifyIncidentUpdatedAsync(IncidentUpdatedEvent eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyDashboardUpdatedAsync(DashboardUpdatedEvent eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyResourceUpdatedAsync(ResourceUpdatedEvent eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyEscalationUpdatedAsync(EscalationUpdatedEvent eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifySituationUpdatedAsync(SituationUpdatedEvent eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task NotifyPredictionUpdatedAsync(PredictionUpdatedEvent eventData, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NotificationTestDbContext : ApplicationDbContext
    {
        public NotificationTestDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Incident>().Ignore(x => x.Embedding);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetColumnType() is "nvarchar(max)" or "varchar(max)")
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property(property.Name)
                            .HasColumnType("TEXT");
                    }
                }
            }
        }
    }
}
