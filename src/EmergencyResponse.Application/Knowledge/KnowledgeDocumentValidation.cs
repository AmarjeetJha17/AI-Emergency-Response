namespace EmergencyResponse.Application.Knowledge;

public static class KnowledgeDocumentValidation
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".docx"] =
            [
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            ],
            [".txt"] = ["text/plain"],
            [".md"] = ["text/markdown", "text/plain"]
        };

    public static void Validate(
        string fileName,
        string contentType,
        long length,
        long maxFileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException(
                "A knowledge document file name is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedTypes.TryGetValue(extension, out var contentTypes))
        {
            throw new InvalidOperationException(
                "Only PDF, DOCX, TXT, and Markdown files are supported.");
        }

        if (!contentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The content type '{contentType}' is not valid for '{extension}'.");
        }

        if (length <= 0 || length > maxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"Knowledge documents must be between 1 byte and {maxFileSizeBytes} bytes.");
        }
    }

    public static string GetExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedTypes.ContainsKey(extension))
        {
            throw new InvalidOperationException(
                "The knowledge document extension is not supported.");
        }

        return extension;
    }
}
