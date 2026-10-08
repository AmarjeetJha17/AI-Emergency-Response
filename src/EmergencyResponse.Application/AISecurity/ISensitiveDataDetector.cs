namespace EmergencyResponse.Application.AISecurity;

public interface ISensitiveDataDetector
{
    SensitiveDataScanResult Scan(string text);
}

public sealed class SensitiveDataScanResult
{
    public bool Detected { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}
