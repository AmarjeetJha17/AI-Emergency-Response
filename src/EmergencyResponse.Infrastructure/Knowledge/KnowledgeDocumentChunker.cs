using System.Security.Cryptography;
using System.Text;
using EmergencyResponse.Application.Knowledge;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeDocumentChunker : IKnowledgeDocumentChunker
{
    private readonly KnowledgeDocumentOptions _options;

    public KnowledgeDocumentChunker(
        IOptions<KnowledgeDocumentOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<KnowledgeChunk> Chunk(
        IReadOnlyList<KnowledgeTextPage> pages)
    {
        var chunks = new List<KnowledgeChunk>();
        var chunkNumber = 1;

        foreach (var page in pages)
        {
            var paragraphs = page.Text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize)
                .Where(text => text.Length > 0)
                .ToList();

            var buffer = new StringBuilder();
            string? heading = null;

            foreach (var paragraph in paragraphs)
            {
                if (IsHeading(paragraph))
                {
                    heading = paragraph;
                    continue;
                }

                if (buffer.Length > 0 &&
                    buffer.Length + paragraph.Length + 2 >
                    _options.MaximumChunkCharacters)
                {
                    chunks.Add(CreateChunk(
                        chunkNumber++,
                        buffer.ToString(),
                        page.PageNumber,
                        heading));
                    var overlap = GetOverlap(buffer.ToString());
                    buffer.Clear();
                    buffer.Append(overlap);
                }

                if (buffer.Length > 0)
                {
                    buffer.AppendLine();
                    buffer.AppendLine();
                }

                buffer.Append(paragraph);
            }

            if (buffer.Length > 0)
            {
                chunks.Add(CreateChunk(
                    chunkNumber++,
                    buffer.ToString(),
                    page.PageNumber,
                    heading));
            }
        }

        return chunks;
    }

    private KnowledgeChunk CreateChunk(
        int chunkNumber,
        string content,
        int pageNumber,
        string? heading)
    {
        var normalized = Normalize(content);
        return new KnowledgeChunk(
            chunkNumber,
            normalized,
            pageNumber,
            heading,
            Math.Max(1, normalized.Length / 4));
    }

    private string GetOverlap(string content)
    {
        if (_options.ChunkOverlapCharacters <= 0 ||
            content.Length <= _options.ChunkOverlapCharacters)
        {
            return content;
        }

        return content[^_options.ChunkOverlapCharacters..];
    }

    private static string Normalize(string text) =>
        string.Join(
            ' ',
            text.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries));

    private static bool IsHeading(string text) =>
        text.StartsWith('#') ||
        (text.Length <= 120 &&
         text.EndsWith(':') &&
         !text.Contains('.', StringComparison.Ordinal));
}
