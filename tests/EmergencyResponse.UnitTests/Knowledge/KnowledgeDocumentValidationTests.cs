using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.UnitTests.Knowledge;

public sealed class KnowledgeDocumentValidationTests
{
    [Theory]
    [InlineData("procedure.pdf", "application/pdf")]
    [InlineData("procedure.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("procedure.txt", "text/plain")]
    [InlineData("procedure.md", "text/markdown")]
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
}
