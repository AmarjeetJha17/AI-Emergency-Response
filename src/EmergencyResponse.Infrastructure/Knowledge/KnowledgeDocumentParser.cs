using System.Text;
using DocumentFormat.OpenXml.Packaging;
using EmergencyResponse.Application.Knowledge;
using UglyToad.PdfPig;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeDocumentParser : IKnowledgeDocumentParser
{
    public async Task<IReadOnlyList<KnowledgeTextPage>> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var extension = KnowledgeDocumentValidation.GetExtension(fileName);
        return extension switch
        {
            ".txt" or ".md" => await ParseTextAsync(
                content,
                cancellationToken),
            ".pdf" => ParsePdf(content),
            ".docx" => ParseDocx(content),
            _ => throw new InvalidOperationException(
                "The knowledge document format is not supported.")
        };
    }

    private static async Task<IReadOnlyList<KnowledgeTextPage>> ParseTextAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            content,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return [new KnowledgeTextPage(1, text)];
    }

    private static IReadOnlyList<KnowledgeTextPage> ParsePdf(Stream content)
    {
        using var document = PdfDocument.Open(content);
        return document.GetPages()
            .Select(page => new KnowledgeTextPage(
                page.Number,
                page.Text))
            .ToList();
    }

    private static IReadOnlyList<KnowledgeTextPage> ParseDocx(Stream content)
    {
        using var document = WordprocessingDocument.Open(
            content,
            isEditable: false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return [];
        }

        var paragraphs = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .Select(paragraph => string.Concat(
                paragraph.Descendants<
                    DocumentFormat.OpenXml.Wordprocessing.Text>()
                    .Select(text => text.Text)))
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return [new KnowledgeTextPage(1, string.Join(
            Environment.NewLine + Environment.NewLine,
            paragraphs))];
    }
}
