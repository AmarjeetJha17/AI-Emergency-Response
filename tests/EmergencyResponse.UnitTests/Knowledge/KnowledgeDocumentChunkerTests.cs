using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Infrastructure.Knowledge;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.UnitTests.Knowledge;

public sealed class KnowledgeDocumentChunkerTests
{
    [Fact]
    public void Chunking_preserves_page_and_heading_metadata()
    {
        var chunker = new KnowledgeDocumentChunker(
            Options.Create(new KnowledgeDocumentOptions
            {
                MaximumChunkCharacters = 40,
                ChunkOverlapCharacters = 5
            }));

        var result = chunker.Chunk(
        [
            new KnowledgeTextPage(
                3,
                "Road closure:\n\nClose the road immediately.\n\n" +
                "Notify the transport coordinator.")
        ]);

        Assert.NotEmpty(result);
        Assert.All(result, chunk => Assert.Equal(3, chunk.PageNumber));
        Assert.Equal("Road closure:", result[0].SectionHeading);
        Assert.All(result, chunk => Assert.False(
            string.IsNullOrWhiteSpace(chunk.Content)));
    }
}
