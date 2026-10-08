namespace EmergencyResponse.Application.AISecurity;

public interface IAIOutputSecurityService
{
    AIOutputSecurityResult Evaluate(
        string output,
        bool schemaValid,
        bool groundingValid);
}
