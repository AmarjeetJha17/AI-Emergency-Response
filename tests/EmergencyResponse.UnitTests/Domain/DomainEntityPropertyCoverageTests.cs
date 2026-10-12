using System.Collections;
using System.Reflection;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Knowledge;

namespace EmergencyResponse.UnitTests.EntityCoverage;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class DomainEntityPropertyCoverageTests
{
    [Fact]
    public void IncidentEnvironmentalContext_exposes_defaults_and_round_trips_all_properties()
    {
        var entity = new IncidentEnvironmentalContext();

        Assert.Equal(DataClassification.Confidential, entity.DataClassification);
        AssertAllPropertiesRoundTrip(entity);
    }

    [Fact]
    public void KnowledgeDocument_exposes_defaults_and_round_trips_all_properties()
    {
        var entity = new KnowledgeDocument();

        Assert.Equal(DataClassification.Internal, entity.DataClassification);
        Assert.Equal(SecurityScanStatus.Pending, entity.SecurityScanStatus);
        AssertAllPropertiesRoundTrip(entity);
    }

    [Fact]
    public void VisualAnalysis_exposes_defaults_and_round_trips_all_properties()
    {
        var entity = new VisualAnalysis();

        Assert.Equal(1, entity.AttemptNumber);
        Assert.Equal(0, entity.RetryCount);
        AssertAllPropertiesRoundTrip(entity);
    }

    [Fact]
    public void Incident_exposes_defaults_and_round_trips_all_properties()
    {
        var entity = new Incident();

        Assert.Equal(DataClassification.Confidential, entity.DataClassification);
        Assert.Equal(DuplicateDetectionStatus.NotStarted, entity.DuplicateDetectionStatus);
        Assert.True(entity.IsCanonical);
        AssertAllPropertiesRoundTrip(entity);
    }

    [Fact]
    public void CopilotActionProposal_exposes_defaults_and_round_trips_all_properties()
    {
        var entity = new CopilotActionProposal();

        Assert.Equal(DataClassification.Confidential, entity.DataClassification);
        Assert.Equal("{}", entity.ArgumentsJson);
        AssertAllPropertiesRoundTrip(entity);
    }

    [Fact]
    public void AIAnalysis_exposes_defaults_and_round_trips_all_properties()
    {
        var entity = new AIAnalysis();

        Assert.Equal(DataClassification.Confidential, entity.DataClassification);
        Assert.Equal(1, entity.AttemptNumber);
        Assert.Equal(0, entity.RetryCount);
        AssertAllPropertiesRoundTrip(entity);
    }

    private static void AssertAllPropertiesRoundTrip(object entity)
    {
        var properties = entity.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            .ToArray();

        Assert.NotEmpty(properties);

        foreach (var property in properties)
        {
            var expected = CreatePropertyValue(property.PropertyType);
            property.SetValue(entity, expected);

            var actual = property.GetValue(entity);
            if (expected is null)
            {
                Assert.Null(actual);
            }
            else if (property.PropertyType.IsValueType)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                Assert.Same(expected, actual);
            }
        }
    }

    private static object? CreatePropertyValue(Type propertyType)
    {
        var valueType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (valueType == typeof(string))
        {
            return "coverage-value";
        }

        if (valueType == typeof(Guid))
        {
            return Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        }

        if (valueType == typeof(DateTime))
        {
            return new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        }

        if (valueType.IsEnum)
        {
            return Enum.GetValues(valueType).GetValue(0);
        }

        if (valueType == typeof(bool))
        {
            return true;
        }

        if (valueType.IsGenericType &&
            typeof(IEnumerable).IsAssignableFrom(valueType) &&
            valueType.GetGenericTypeDefinition() is var genericDefinition &&
            (genericDefinition == typeof(ICollection<>) ||
             genericDefinition == typeof(IReadOnlyCollection<>) ||
             genericDefinition == typeof(IEnumerable<>)))
        {
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType.GetGenericArguments()[0]));
        }

        if (valueType.IsValueType)
        {
            return Activator.CreateInstance(valueType);
        }

        return Activator.CreateInstance(valueType);
    }
}
