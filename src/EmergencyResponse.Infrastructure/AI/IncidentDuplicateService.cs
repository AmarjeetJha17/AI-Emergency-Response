using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Common.Geo;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentDuplicateService
    : IDuplicateIncidentService
{
    private readonly ApplicationDbContext _db;
    private readonly IIncidentEmbeddingService _embeddingService;

    public IncidentDuplicateService(
        ApplicationDbContext db,
        IIncidentEmbeddingService embeddingService)
    {
        _db = db;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<DuplicateIncidentCandidate>>
        FindCandidatesAsync(
            Guid incidentId,
            int maxCandidates = 20,
            CancellationToken cancellationToken = default)
    {
        const double reviewThreshold = 0.75;

        if (maxCandidates <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCandidates),
                "The candidate count must be greater than zero.");
        }

        var source =
            await _db.Incidents
                .AsNoTracking()
                .Include(x => x.Locations)
                .SingleOrDefaultAsync(
                    x => x.Id == incidentId,
                    cancellationToken);

        if (source is null)
        {
            throw new KeyNotFoundException(
                $"Incident '{incidentId}' does not exist.");
        }

        if (source.Embedding is null)
        {
            throw new InvalidOperationException(
                $"Incident '{incidentId}' does not have an embedding.");
        }

        var candidates =
            await _db.Incidents
                .AsNoTracking()
                .Include(x => x.Locations)
                .Where(
                    x =>
                        x.Id != source.Id &&
                        x.Embedding != null)
                .Select(
                    x => new
                    {
                        Incident = x,
                        Distance =
                            EF.Functions.VectorDistance<float>(
                                "cosine",
                                source.Embedding.Value,
                                x.Embedding.Value)
                    })
                .OrderBy(x => x.Distance)
                .Take(maxCandidates)
                .ToListAsync(cancellationToken);

        var sourceLocation =
            source.Locations
                .FirstOrDefault(location => location.IsPrimary)
            ?? source.Locations.FirstOrDefault();

        var results =
            new List<DuplicateIncidentCandidate>(
                candidates.Count);

        foreach (var candidate in candidates)
        {
            var candidateLocation =
                candidate.Incident.Locations
                    .FirstOrDefault(location => location.IsPrimary)
                ?? candidate.Incident.Locations.FirstOrDefault();

            var semanticSimilarity =
                Math.Clamp(
                    1d - candidate.Distance,
                    0d,
                    1d);

            var distanceKm =
                sourceLocation is null || candidateLocation is null
                    ? double.MaxValue
                    : GeoDistance.CalculateKilometers(
                        (double)sourceLocation.Latitude,
                        (double)sourceLocation.Longitude,
                        (double)candidateLocation.Latitude,
                        (double)candidateLocation.Longitude);

            var geographicSimilarity =
                sourceLocation is null || candidateLocation is null
                    ? 0d
                    : GeographicSimilarityCalculator.Calculate(
                        distanceKm);

            var timeDifference =
                source.CreatedAtUtc -
                candidate.Incident.CreatedAtUtc;

            var temporalSimilarity =
                TemporalSimilarityCalculator.Calculate(
                    timeDifference);

            var incidentTypeSimilarity =
                IncidentTypeSimilarityCalculator.Calculate(
                    source.IncidentTypeId,
                    candidate.Incident.IncidentTypeId);

            var duplicateScore =
                DuplicateScoreCalculator.Calculate(
                    semanticSimilarity,
                    geographicSimilarity,
                    temporalSimilarity,
                    incidentTypeSimilarity);

            var sourceId = source.Id;
            var candidateId = candidate.Incident.Id;

            var existingMatch =
                await _db.IncidentDuplicateMatches
                    .SingleOrDefaultAsync(
                        x =>
                            (x.SourceIncidentId == sourceId &&
                             x.CandidateIncidentId == candidateId) ||
                            (x.SourceIncidentId == candidateId &&
                             x.CandidateIncidentId == sourceId),
                        cancellationToken);

            results.Add(
                new DuplicateIncidentCandidate
                {
                    MatchId = existingMatch?.Id ?? Guid.Empty,
                    IncidentId = candidate.Incident.Id,
                    IncidentNumber = candidate.Incident.IncidentNumber,
                    Title = candidate.Incident.Title,
                    SemanticSimilarity = semanticSimilarity,
                    GeographicSimilarity = geographicSimilarity,
                    TemporalSimilarity = temporalSimilarity,
                    IncidentTypeSimilarity = incidentTypeSimilarity,
                    DuplicateScore = duplicateScore,
                    DistanceKm = distanceKm,
                    TimeDifference = timeDifference,
                    RequiresReview =
                        duplicateScore >= reviewThreshold
                });

            if (duplicateScore < reviewThreshold)
            {
                continue;
            }

            if (existingMatch is null)
            {
                existingMatch = new IncidentDuplicateMatch
                {
                    Id = Guid.NewGuid(),
                    SourceIncidentId = sourceId,
                    CandidateIncidentId = candidateId,
                    SemanticSimilarity = semanticSimilarity,
                    GeographicSimilarity = geographicSimilarity,
                    TemporalSimilarity = temporalSimilarity,
                    IncidentTypeSimilarity = incidentTypeSimilarity,
                    DuplicateScore = duplicateScore,
                    Reviewed = false,
                    ConfirmedDuplicate = false
                };

                _db.IncidentDuplicateMatches.Add(existingMatch);

                var resultIndex = results.Count - 1;
                results[resultIndex] = results[resultIndex] with
                {
                    MatchId = existingMatch.Id
                };
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return results;
    }

    public async Task GenerateEmbeddingAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident =
            await _db.Incidents
                .Include(x => x.IncidentType)
                .Include(x => x.Locations)
                .SingleAsync(
                    x => x.Id == incidentId,
                    cancellationToken);

        var text =
            IncidentEmbeddingTextBuilder.Build(incident);

        var vector =
            await _embeddingService.GenerateAsync(
                text,
                cancellationToken);

        incident.Embedding =
            new SqlVector<float>(vector);

        incident.EmbeddingModel =
            "text-embedding-3-small";

        incident.EmbeddingGeneratedAtUtc =
            DateTime.UtcNow;

        await _db.SaveChangesAsync(
            cancellationToken);
    }
}