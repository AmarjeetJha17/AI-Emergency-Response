namespace EmergencyResponse.Application.AISecurity;

public sealed record AIUntrustedContent(
    TrustedContentBoundary Boundary,
    string Content,
    string? SourceName = null);
