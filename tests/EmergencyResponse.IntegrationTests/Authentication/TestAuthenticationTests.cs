using System.Net;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.IntegrationTests.Infrastructure;

namespace EmergencyResponse.IntegrationTests.Authentication;

[Collection(IntegrationTestCollection.Name)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
public sealed class TestAuthenticationTests : IntegrationTestBase
{
    public TestAuthenticationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ProtectedEndpoint_WithoutIdentity_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/incidents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OperatorIdentity_CanReadIncidentList()
    {
        AuthenticateAs(TestUsers.OperatorId.ToString(), ApplicationRoles.Operator);

        var response = await Client.GetAsync("/api/incidents?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
