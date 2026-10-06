using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeDocumentNumberGenerator
    : IKnowledgeDocumentNumberGenerator
{
    private readonly ApplicationDbContext _db;

    public KnowledgeDocumentNumberGenerator(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"DOC-{year}-";
        var lastNumber = await _db.KnowledgeDocuments
            .Where(x => x.DocumentNumber.StartsWith(prefix))
            .OrderByDescending(x => x.DocumentNumber)
            .Select(x => x.DocumentNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber) &&
            int.TryParse(lastNumber[prefix.Length..], out var current))
        {
            next = current + 1;
        }

        return $"{prefix}{next:00000}";
    }
}
