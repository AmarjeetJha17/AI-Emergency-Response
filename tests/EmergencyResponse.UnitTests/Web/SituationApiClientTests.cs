using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyResponse.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace EmergencyResponse.UnitTests.Web;

public sealed class SituationApiClientTests
{
    [Fact]
    public async Task CommandCenter_ReadsActualArrayOnceAndPreservesManualReviewData()
    {
        using var fixture = Create("api/situations/command-center", """
            [{"id":"11111111-1111-1111-1111-111111111111","situationNumber":"DEMO18-SITUATION-FLOOD",
              "summary":"Manual group; counts may overlap","incidentCount":3,"peopleAffected":6,
              "requiresHumanReview":true,"situationScore":0,"lastAIAnalysisAtUtc":null}]
            """);
        var item = Assert.Single(await fixture.Client.GetCommandCenterAsync());
        Assert.Equal("DEMO18-SITUATION-FLOOD", item.SituationNumber);
        Assert.Equal(3, item.IncidentCount);
        Assert.True(item.RequiresHumanReview);
        Assert.Null(item.LastAIAnalysisAtUtc);
        fixture.VerifySingleRequest();
    }

    [Fact]
    public async Task Map_ReadsActualArrayAndPreservesPrimaryIncidentAndCoordinates()
    {
        using var fixture = Create("api/situations/open/map", """
            [{"situationNumber":"DEMO18-SITUATION-FLOOD","centerLatitude":18.52045,"centerLongitude":73.85675,
              "radiusMeters":250.5,"primaryIncidentId":"22222222-2222-2222-2222-222222222222",
              "incidents":[{"incidentId":"22222222-2222-2222-2222-222222222222",
              "incidentNumber":"DEMO18-FLOOD-01","isPrimaryIncident":true}]}]
            """);
        var item = Assert.Single(await fixture.Client.GetOpenForMapAsync());
        Assert.Equal(18.52045, item.CenterLatitude);
        Assert.Equal(250.5, item.RadiusMeters);
        var incident = Assert.Single(item.Incidents);
        Assert.Equal(item.PrimaryIncidentId, incident.IncidentId);
        Assert.True(incident.IsPrimaryIncident);
        fixture.VerifySingleRequest();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyArray_IsAnEmptySnapshot(bool map)
    {
        using var fixture = Create(Route(map), "[]");
        Assert.Empty(await FetchAsync(fixture.Client, map));
        fixture.VerifySingleRequest();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unauthorized_IsNotSwallowedByDeserializationFallback(bool map)
    {
        using var fixture = Create(Route(map), "", HttpStatusCode.Unauthorized);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => FetchAsync(fixture.Client, map));
        fixture.VerifySingleRequest();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServerFailure_IsNotPresentedAsAnEmptySnapshot(bool map)
    {
        using var fixture = Create(Route(map), "", HttpStatusCode.InternalServerError);
        await Assert.ThrowsAsync<HttpRequestException>(() => FetchAsync(fixture.Client, map));
        fixture.VerifySingleRequest();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidJson_ReportsTheOriginalParseFailure(bool map)
    {
        using var fixture = Create(Route(map), "invalid JSON");
        await Assert.ThrowsAsync<JsonException>(() => FetchAsync(fixture.Client, map));
        fixture.VerifySingleRequest();
    }

    private static string Route(bool map) => map ? "api/situations/open/map" : "api/situations/command-center";

    private static async Task<IReadOnlyList<object>> FetchAsync(SituationApiClient client, bool map) => map
        ? (await client.GetOpenForMapAsync()).Cast<object>().ToArray()
        : (await client.GetCommandCenterAsync()).Cast<object>().ToArray();

    private static Fixture Create(string route, string json, HttpStatusCode status = HttpStatusCode.OK) => new(route, json, status);

    private sealed class Fixture : IDisposable
    {
        private readonly Mock<HttpMessageHandler> handler = new(MockBehavior.Strict);
        private readonly HttpClient httpClient;
        public SituationApiClient Client { get; }

        public Fixture(string route, string json, HttpStatusCode status)
        {
            handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri!.PathAndQuery == "/" + route),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(() => new HttpResponseMessage(status)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            httpClient = new(handler.Object) { BaseAddress = new Uri("https://localhost/") };
            Client = new(httpClient, NullLogger<SituationApiClient>.Instance);
        }

        public void VerifySingleRequest() => handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());

        public void Dispose() => httpClient.Dispose();
    }
}
