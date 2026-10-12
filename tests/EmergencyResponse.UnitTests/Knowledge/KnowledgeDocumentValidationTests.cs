using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.UnitTests.Knowledge;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class KnowledgeDocumentValidationTests
{
    [Theory]
    [InlineData("procedure.pdf", "application/pdf")]
    [InlineData("procedure.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("procedure.txt", "text/plain")]
    [InlineData("procedure.md", "text/markdown")]
    [InlineData("procedure.md", "text/plain")]
    public void Supported_documents_are_accepted(
        string fileName,
        string contentType)
    {
        KnowledgeDocumentValidation.Validate(
            fileName,
            contentType,
            10,
            100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Empty_or_negative_document_length_is_rejected(long length)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeDocumentValidation.Validate(
                "procedure.txt",
                "text/plain",
                length,
                100));

        Assert.Contains("between 1 byte and 100 bytes", exception.Message);
    }

    [Fact]
    public void Document_at_maximum_size_is_accepted()
    {
        KnowledgeDocumentValidation.Validate(
            "procedure.txt",
            "text/plain",
            100,
            100);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Missing_file_name_is_rejected(string? fileName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeDocumentValidation.Validate(
                fileName!,
                "text/plain",
                1,
                100));

        Assert.Equal("A knowledge document file name is required.", exception.Message);
    }

    [Fact]
    public void Content_type_must_match_extension_case_insensitively()
    {
        KnowledgeDocumentValidation.Validate(
            "procedure.PDF",
            "APPLICATION/PDF",
            1,
            100);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeDocumentValidation.Validate(
                "procedure.pdf",
                "text/plain",
                1,
                100));

        Assert.Contains("not valid for '.pdf'", exception.Message);
    }

    [Fact]
    public void Unsupported_extension_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            KnowledgeDocumentValidation.Validate(
                "procedure.exe",
                "application/octet-stream",
                10,
                100));
    }

    [Fact]
    public void Oversized_document_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            KnowledgeDocumentValidation.Validate(
                "procedure.txt",
                "text/plain",
                101,
                100));
    }

    [Theory]
    [InlineData("procedure.PDF", ".pdf")]
    [InlineData("procedure.DOCX", ".docx")]
    [InlineData("procedure.TXT", ".txt")]
    [InlineData("procedure.MD", ".md")]
    public void GetExtension_returns_supported_extension_in_lowercase(
        string fileName,
        string expected)
    {
        Assert.Equal(expected, KnowledgeDocumentValidation.GetExtension(fileName));
    }

    [Theory]
    [InlineData("procedure.exe")]
    [InlineData("procedure")]
    public void GetExtension_rejects_unsupported_or_missing_extensions(string fileName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            KnowledgeDocumentValidation.GetExtension(fileName));

        Assert.Equal("The knowledge document extension is not supported.", exception.Message);
    }
}
