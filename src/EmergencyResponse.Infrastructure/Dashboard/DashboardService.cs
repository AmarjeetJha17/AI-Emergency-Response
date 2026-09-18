using EmergencyResponse.Application.Dashboard;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var critical = await _db.Incidents.CountAsync(
            x => x.Severity == SeverityLevel.Critical,
            cancellationToken);

        var high = await _db.Incidents.CountAsync(
            x => x.Severity == SeverityLevel.High,
            cancellationToken);

        var moderate = await _db.Incidents.CountAsync(
            x => x.Severity == SeverityLevel.Moderate,
            cancellationToken);

        var low = await _db.Incidents.CountAsync(
            x => x.Severity == SeverityLevel.Low,
            cancellationToken);

        // Closed and Rejected are the terminal states in this domain.
        var active = await _db.Incidents.CountAsync(
            x => x.Status != IncidentStatus.Closed &&
                 x.Status != IncidentStatus.Rejected,
            cancellationToken);

        var unassigned = await _db.Incidents.CountAsync(
            x => !x.Assignments.Any(),
            cancellationToken);

        var aiReview = await _db.AIAnalyses.CountAsync(
            x => x.Status == AIAnalysisStatus.RequiresReview,
            cancellationToken);

        var duplicateReview = await _db.IncidentDuplicateMatches.CountAsync(
            x => !x.Reviewed && x.DuplicateScore >= 0.75,
            cancellationToken);

        var visualReview = await _db.VisualAnalyses.CountAsync(
            x => x.Status == VisualAnalysisStatus.RequiresReview &&
                 !x.HumanReviewed,
            cancellationToken);

        return new DashboardSummary
        {
            CriticalIncidents = critical,
            HighIncidents = high,
            ModerateIncidents = moderate,
            LowIncidents = low,
            ActiveIncidents = active,
            UnassignedIncidents = unassigned,
            AwaitingAIReview = aiReview,
            AwaitingDuplicateReview = duplicateReview,
            AwaitingVisualReview = visualReview
        };
    }

    public async Task<IReadOnlyList<DashboardIncidentItem>> GetRecentIncidentsAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);

        return await _db.Incidents
            .AsNoTracking()
            .Include(x => x.IncidentType)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new DashboardIncidentItem
            {
                Id = x.Id,
                IncidentNumber = x.IncidentNumber,
                Title = x.Title,

                IncidentType =
                    x.IncidentType != null
                        ? x.IncidentType.Name
                        : null,

                Status = x.Status,
                Severity = x.Severity,
                Priority = x.Priority,
                CreatedAtUtc = x.CreatedAtUtc,

                // Incident has an Assignments navigation property.
                IsAssigned = x.Assignments.Any(),

                // Incident has an AIAnalyses navigation property.
                RequiresAIReview = x.AIAnalyses.Any(
                    a => a.Status == AIAnalysisStatus.RequiresReview),

                // No nav prop for duplicates on Incident — use a correlated
                // subquery via the DbContext DbSet.
                HasDuplicateCandidates = _db.IncidentDuplicateMatches.Any(
                    d => d.SourceIncidentId == x.Id &&
                         !d.Reviewed &&
                         d.DuplicateScore >= 0.75),

                // No nav prop for visual analyses on Incident — correlated subquery.
                HasVisualReview = _db.VisualAnalyses.Any(
                    v => v.IncidentId == x.Id &&
                         v.Status == VisualAnalysisStatus.RequiresReview &&
                         !v.HumanReviewed)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardChartPoint>> GetSeverityDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Incidents
            .AsNoTracking()
            .GroupBy(x => x.Severity)
            .Select(group => new DashboardChartPoint
            {
                Label = group.Key.ToString(),
                Value = group.Count()
            })
            .OrderBy(x => x.Label)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardTimeSeriesPoint>> GetIncidentTrendAsync(
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 31);

        var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var rows = await _db.Incidents
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= startDate)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        var lookup = rows.ToDictionary(x => x.Date, x => x.Count);

        // Fill gaps so the chart always has a point for every day in the range.
        return Enumerable
            .Range(0, days)
            .Select(offset =>
            {
                var date = startDate.AddDays(offset);

                return new DashboardTimeSeriesPoint
                {
                    Date = date,
                    Count = lookup.TryGetValue(date, out var count) ? count : 0
                };
            })
            .ToList();
    }
}
