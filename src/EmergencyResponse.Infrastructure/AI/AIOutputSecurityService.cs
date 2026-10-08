using EmergencyResponse.Application.AISecurity;
using EmergencyResponse.Application.Observability;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class AIOutputSecurityService : IAIOutputSecurityService
{
    private readonly ISensitiveDataDetector _sensitiveData;
    private readonly IAISecurityPolicy _policy;

    public AIOutputSecurityService(
        ISensitiveDataDetector sensitiveData,
        IAISecurityPolicy policy)
    {
        _sensitiveData = sensitiveData;
        _policy = policy;
    }

    public AIOutputSecurityResult Evaluate(
        string output,
        bool schemaValid,
        bool groundingValid)
    {
        var sensitive = _sensitiveData.Scan(output);
        var unsafeInstruction = PromptInjectionPatterns.HighRiskPatterns
            .Any(pattern => output.Contains(
                pattern,
                StringComparison.OrdinalIgnoreCase));
        var signals = sensitive.Signals
            .Select(x => $"sensitive:{x}")
            .Concat(unsafeInstruction ? ["unsafe-instruction"] : [])
            .ToArray();
        var allowed = schemaValid &&
            groundingValid &&
            (!_policy.BlockOnSensitiveOutput || !sensitive.Detected) &&
            (!_policy.BlockOnUnsafeOutput || !unsafeInstruction);
        if (sensitive.Detected)
        {
            EmergencyResponseTelemetry.AISensitiveDataDetections.Add(1);
        }
        if (!groundingValid)
        {
            EmergencyResponseTelemetry.AIGroundingValidationFailures.Add(1);
        }
        if (!allowed)
        {
            EmergencyResponseTelemetry.AIOutputSecurityBlocks.Add(1);
        }

        return new AIOutputSecurityResult
        {
            Allowed = allowed,
            SchemaValid = schemaValid,
            ContainsUnsafeInstruction = unsafeInstruction,
            ContainsSensitiveData = sensitive.Detected,
            GroundingValid = groundingValid,
            Reason = allowed ? null : "The generated response failed AI security validation.",
            Signals = signals
        };
    }
}
