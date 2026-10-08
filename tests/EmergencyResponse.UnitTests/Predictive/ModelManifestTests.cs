using System.Text.Json;
using EmergencyResponse.Application.Predictive.Training;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class ModelManifestTests
{
    [Fact]
    public void ManifestCarriesDatasetAndFeatureSchemaVersions()
    {
        var manifest = new ModelManifest
        {
            ModelName = "EscalationRisk",
            ModelVersion = "v1",
            DatasetVersion = "escalation-risk-dataset-v1",
            FeatureSchemaVersion = FeatureSchemaVersions.EscalationRiskV1
        };

        var json = JsonSerializer.Serialize(manifest);
        var restored = JsonSerializer.Deserialize<ModelManifest>(json);

        Assert.Equal("v1", restored!.ModelVersion);
        Assert.Equal(
            FeatureSchemaVersions.EscalationRiskV1,
            restored.FeatureSchemaVersion);
    }
}
