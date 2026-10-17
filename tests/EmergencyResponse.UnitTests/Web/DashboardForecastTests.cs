using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Web.Controllers;
using EmergencyResponse.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EmergencyResponse.UnitTests.Web;

public sealed class DashboardForecastTests
{
    [Fact]
    public async Task UnavailableForecast_IsReturnedWithoutInventingValues()
    {
        var result = new ForecastResult { IsAvailable = false, UnavailableReason = "Insufficient history" };
        var client = new Mock<IDashboardApiClient>();
        client.Setup(c => c.GetIncidentVolumeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(result);
        var response = Assert.IsType<JsonResult>(await new DashboardController(client.Object).IncidentVolume(default));
        Assert.Same(result, response.Value);
        Assert.False(((ForecastResult)response.Value!).IsAvailable);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    [InlineData(429)]
    public async Task UpstreamFailure_DoesNotBecomeSuccessfulEmptyForecast(int status)
    {
        var client = new Mock<IDashboardApiClient>();
        client.Setup(c => c.GetIncidentVolumeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Sensitive upstream detail", null, (HttpStatusCode)status));
        var response = await new DashboardController(client.Object).IncidentVolume(default);
        if (status is 401 or 403) Assert.Equal(status, Assert.IsType<StatusCodeResult>(response).StatusCode);
        else Assert.Equal(503, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.DoesNotContain("Sensitive", JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task InvalidHotspots_AreUnavailableRatherThanAnEmptyArray()
    {
        var client = new Mock<IDashboardApiClient>();
        client.Setup(c => c.GetHotspotsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new JsonException());
        var response = Assert.IsType<ObjectResult>(await new DashboardController(client.Object).Hotspots(default));
        Assert.Equal(503, response.StatusCode);
    }

    [Fact]
    public async Task RequestCancellation_IsNotSwallowed()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = new Mock<IDashboardApiClient>();
        client.Setup(c => c.GetIncidentVolumeAsync(cancellation.Token)).ThrowsAsync(new OperationCanceledException(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new DashboardController(client.Object).IncidentVolume(cancellation.Token));
    }

    [Fact]
    public void ForecastRoutes_RetainDashboardRoleBoundaryAndNoStore()
    {
        var type = typeof(DashboardController);
        Assert.Equal("Operator,EmergencyAdmin,SystemAdmin", type.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single().Roles);
        foreach (var action in new[] { nameof(DashboardController.IncidentVolume), nameof(DashboardController.Hotspots) })
        {
            var cache = type.GetMethod(action)!.GetCustomAttributes(typeof(ResponseCacheAttribute), true)
                .Cast<ResponseCacheAttribute>().Single();
            Assert.True(cache.NoStore);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Client_UsesExistingApiRoutesAndPreservesPayload(bool hotspots)
    {
        var handler = new ForecastHandler(hotspots ? "[]" : """{"isAvailable":false,"points":[],"unavailableReason":"Insufficient history"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var client = new DashboardApiClient(http, NullLogger<DashboardApiClient>.Instance);
        if (hotspots) Assert.Empty(await client.GetHotspotsAsync());
        else Assert.False((await client.GetIncidentVolumeAsync()).IsAvailable);
        Assert.Equal(hotspots ? "/api/predictive/hotspots?hours=24" : "/api/predictive/incident-volume?horizon=24", handler.Path);
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("not JSON")]
    public async Task Client_RejectsMissingOrInvalidForecast(string payload)
    {
        using var http = new HttpClient(new ForecastHandler(payload)) { BaseAddress = new Uri("https://api.test/") };
        var client = new DashboardApiClient(http, NullLogger<DashboardApiClient>.Instance);
        await Assert.ThrowsAsync<JsonException>(() => client.GetIncidentVolumeAsync());
    }

    private sealed class ForecastHandler(string payload) : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public int RequestCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Path = request.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(payload, Encoding.UTF8, "application/json") });
        }
    }
}
