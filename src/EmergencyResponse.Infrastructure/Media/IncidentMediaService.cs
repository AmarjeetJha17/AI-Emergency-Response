using EmergencyResponse.Application.Media;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Media;

public sealed class IncidentMediaService : IIncidentMediaService
{
    private readonly ApplicationDbContext _db;
    private readonly IIncidentMediaStorage _storage;

    public IncidentMediaService(
        ApplicationDbContext db,
        IIncidentMediaStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Guid> SaveImageAsync(
        Guid incidentId,
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default)
    {
        IncidentMediaValidation.ValidateImage(length, contentType);

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException(
                "The uploaded image has no extension.");
        }

        return await SaveMediaAsync(
            incidentId,
            content,
            extension,
            contentType,
            length,
            IncidentMediaType.Image,
            cancellationToken);
    }

    public async Task<Guid> SaveVideoAsync(
        Guid incidentId,
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default)
    {
        IncidentVideoValidation.Validate(length, contentType);

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException(
                "The uploaded video has no extension.");
        }

        return await SaveMediaAsync(
            incidentId,
            content,
            extension,
            contentType,
            length,
            IncidentMediaType.Video,
            cancellationToken);
    }

    private async Task<Guid> SaveMediaAsync(
        Guid incidentId,
        Stream content,
        string extension,
        string contentType,
        long length,
        IncidentMediaType mediaType,
        CancellationToken cancellationToken)
    {
        var hash = await MediaHashService.ComputeSha256Async(
            content,
            cancellationToken);

        var existing = await _db.IncidentMedia
            .FirstOrDefaultAsync(
                x => x.IncidentId == incidentId && x.ContentHash == hash,
                cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var storageKey = await _storage.SaveAsync(
            content,
            extension,
            cancellationToken);

        var media = new IncidentMedia
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            StorageKey = storageKey,
            ContentType = contentType,
            SizeBytes = length,
            // FileSizeBytes / FileName / StoragePath are legacy columns that remain
            // NOT NULL in the current schema. Populate them from the new values so
            // the INSERT does not violate the NOT NULL constraints.
            FileSizeBytes = length,
            FileName = storageKey,
            StoragePath = storageKey,
            ContentHash = hash,
            AIAnalysisRequested = true,
            MediaType = mediaType
        };

        _db.IncidentMedia.Add(media);

        await _db.SaveChangesAsync(cancellationToken);

        return media.Id;
    }
}
