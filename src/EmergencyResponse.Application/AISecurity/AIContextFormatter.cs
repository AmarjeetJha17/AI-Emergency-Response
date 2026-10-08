namespace EmergencyResponse.Application.AISecurity;

public static class AIContextFormatter
{
    public static string FormatUntrustedDocument(string content) =>
        Format(TrustedContentBoundary.RetrievedKnowledge, content);

    public static string FormatToolOutput(string toolName, string content) =>
        Format(TrustedContentBoundary.ToolOutput, content, toolName);

    public static string Format(
        TrustedContentBoundary boundary,
        string content,
        string? sourceName = null)
    {
        var label = sourceName is null
            ? boundary.ToString()
            : $"{boundary}:{sourceName}";
        return $"<untrusted-data boundary=\"{label}\">{Environment.NewLine}" +
            content.Trim() +
            $"{Environment.NewLine}</untrusted-data>";
    }
}
