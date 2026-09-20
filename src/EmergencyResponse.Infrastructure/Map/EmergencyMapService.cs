using EmergencyResponse.Application.Map;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Map;

public sealed class EmergencyMapService : IEmergencyMapService
{
    private readonly ApplicationDbContext _db;

    public EmergencyMapService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EmergencyMapIncident>> GetIncidentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Incidents
            .AsNoTracking()
            .Include(x => x.IncidentType)
            .Include(x => x.Department)
            .Include(x => x.Locations)
            .Where(x =>
                x.Status != IncidentStatus.Closed &&
                x.Locations.Any())
            .Select(x => new EmergencyMapIncident
            {
                Id = x.Id,
                IncidentNumber = x.IncidentNumber,
                Title = x.Title,
                IncidentType = x.IncidentType != null ? x.IncidentType.Name : null,
                DepartmentName = x.Department != null ? x.Department.Name : null,
                Status = x.Status,
                Severity = x.Severity,
                Priority = x.Priority,
                Latitude = (double)(x.Locations.OrderByDescending(l => l.IsPrimary).First().Latitude),
                Longitude = (double)(x.Locations.OrderByDescending(l => l.IsPrimary).First().Longitude),
                Address = x.Locations.OrderByDescending(l => l.IsPrimary).First().Address,
                CreatedAtUtc = x.CreatedAtUtc,
                HasDuplicateCandidates = _db.IncidentDuplicateMatches.Any(
                    d => d.SourceIncidentId == x.Id &&
                         !d.Reviewed &&
                         d.DuplicateScore >= 0.75),
                HasVisualReview = _db.VisualAnalyses.Any(
                    v => v.IncidentId == x.Id &&
                         v.Status == VisualAnalysisStatus.RequiresReview &&
                         !v.HumanReviewed),
                RequiresAIReview = x.AIAnalyses.Any(
                    a => a.Status == AIAnalysisStatus.RequiresReview)
            })
            .ToListAsync(cancellationToken);
    }
}
