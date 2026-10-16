using System.Text.Json;
using EmergencyResponse.Application.AI.Common;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Situations;

public sealed class OperationalSituationService : IOperationalSituationService, ISituationService
{
    private readonly ApplicationDbContext _db;
    private readonly ISituationNumberGenerator _numberGenerator;
    private readonly ISituationAnalysisService _analysisService;
    private readonly IIncidentNotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly SituationDetectionOptions _options;
    private readonly AIOptions _aiOptions;
    private readonly ILogger<OperationalSituationService> _logger;
    private readonly INotificationRuleEngine? _ruleEngine;
    private readonly INotificationService? _operationalNotificationService;

    public OperationalSituationService(
        ApplicationDbContext db,
        ISituationNumberGenerator numberGenerator,
        ISituationAnalysisService analysisService,
        IIncidentNotificationService notificationService,
        ICurrentUserService currentUserService,
        IOptions<SituationDetectionOptions> options,
        IOptions<AIOptions> aiOptions,
        ILogger<OperationalSituationService> logger,
        INotificationRuleEngine? ruleEngine = null,
        INotificationService? operationalNotificationService = null)
    {
        _db = db;
        _numberGenerator = numberGenerator;
        _analysisService = analysisService;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _options = options.Value;
        _aiOptions = aiOptions.Value;
        _logger = logger;
        _ruleEngine = ruleEngine;
        _operationalNotificationService = operationalNotificationService;
    }

    public async Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var openIncidents = await _db.Incidents
            .AsNoTracking()
            .Include(i => i.Locations)
            .Include(i => i.IncidentType)
            .Where(i => i.Status != IncidentStatus.Closed &&
                        i.Status != IncidentStatus.Rejected &&
                        i.Locations.Any())
            .ToListAsync(cancellationToken);

        if (openIncidents.Count == 0)
        {
            return;
        }

        var candidates = openIncidents.Select(i =>
        {
            var loc = i.Locations.OrderByDescending(l => l.IsPrimary).First();
            return new SituationCandidate
            {
                IncidentId = i.Id,
                IncidentNumber = i.IncidentNumber,
                Title = i.Title,
                IncidentTypeId = i.IncidentTypeId,
                IncidentTypeName = i.IncidentType?.Name,
                Severity = i.Severity,
                Priority = i.Priority,
                Latitude = (double)loc.Latitude,
                Longitude = (double)loc.Longitude,
                PeopleAffected = i.PeopleAffected ?? 0,
                Casualties = i.Casualties ?? 0,
                CreatedAtUtc = i.CreatedAtUtc
            };
        }).ToList();

        var clusters = SituationClusteringEngine.BuildClusters(
            candidates,
            _options.MaxClusterDistanceKm,
            _options.MaxTimeWindowHours,
            _options.MinIncidentCount);

        var existingSituations = await _db.OperationalSituations
            .Include(s => s.Incidents)
            .Include(s => s.Analyses)
            .Where(s => s.Status == EscalationStatus.Open)
            .ToListAsync(cancellationToken);

        foreach (var cluster in clusters)
        {
            var clusterIncidentIds = cluster.Select(c => c.IncidentId).ToHashSet();
            OperationalSituation? bestMatch = null;
            var maxShared = 0;

            foreach (var existing in existingSituations)
            {
                var existingIds = existing.Incidents.Select(i => i.IncidentId).ToHashSet();
                var shared = clusterIncidentIds.Count(id => existingIds.Contains(id));
                if (shared > 0)
                {
                    var ratioCluster = (double)shared / cluster.Count;
                    var ratioExisting = (double)shared / Math.Max(1, existing.Incidents.Count);

                    if ((ratioCluster >= _options.OverlapThreshold || ratioExisting >= _options.OverlapThreshold) && shared > maxShared)
                    {
                        maxShared = shared;
                        bestMatch = existing;
                    }
                }
            }

            var hasResourceShortage = await HasResourceShortageAsync(clusterIncidentIds, cancellationToken);

            if (bestMatch != null)
            {
                var situation = bestMatch;
                var prevCount = situation.IncidentCount;
                var prevLevel = situation.Level;
                var prevCasualties = situation.Casualties;
                var hasNewIncidents = false;

                var existingIncidentIds = situation.Incidents.Select(i => i.IncidentId).ToHashSet();
                foreach (var candidate in cluster)
                {
                    if (!existingIncidentIds.Contains(candidate.IncidentId))
                    {
                        situation.Incidents.Add(new OperationalSituationIncident
                        {
                            OperationalSituationId = situation.Id,
                            IncidentId = candidate.IncidentId,
                            IsPrimaryIncident = false,
                            AddedAtUtc = DateTime.UtcNow
                        });
                        hasNewIncidents = true;
                    }
                }

                if (!situation.Incidents.Any(i => i.IsPrimaryIncident))
                {
                    var primary = cluster
                        .OrderByDescending(c => c.Severity)
                        .ThenByDescending(c => c.Priority)
                        .ThenBy(c => c.CreatedAtUtc)
                        .FirstOrDefault();

                    if (primary != null)
                    {
                        var target = situation.Incidents.FirstOrDefault(i => i.IncidentId == primary.IncidentId);
                        if (target != null) target.IsPrimaryIncident = true;
                    }
                }

                situation.IncidentCount = cluster.Count;
                situation.CriticalIncidentCount = cluster.Count(x => x.Severity == SeverityLevel.Critical);
                situation.HighIncidentCount = cluster.Count(x => x.Severity == SeverityLevel.High);
                situation.PeopleAffected = cluster.Sum(x => x.PeopleAffected);
                situation.Casualties = cluster.Sum(x => x.Casualties);
                situation.SituationScore = SituationScoreCalculator.Calculate(
                    situation.IncidentCount,
                    situation.CriticalIncidentCount,
                    situation.HighIncidentCount,
                    situation.PeopleAffected,
                    situation.Casualties);
                situation.Level = SituationLevelCalculator.Calculate(situation.SituationScore.Value);
                situation.ResourceShortageDetected = hasResourceShortage;
                situation.LastEvaluatedAtUtc = DateTime.UtcNow;

                var needsAi = RequiresNewAIAnalysis(situation, prevCount, prevLevel, prevCasualties);
                if (needsAi)
                {
                    await RunAIAnalysisAsync(situation, clusterIncidentIds, cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);

                var reason = needsAi ? SituationUpdateReasons.AIAnalyzed :
                    (situation.Level > prevLevel ? SituationUpdateReasons.LevelEscalated :
                    (hasNewIncidents ? SituationUpdateReasons.IncidentAdded : SituationUpdateReasons.Reviewed));

                await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
                {
                    SituationId = situation.Id,
                    SituationNumber = situation.SituationNumber,
                    Title = situation.Title,
                    Level = situation.Level,
                    Status = situation.Status,
                    IncidentCount = situation.IncidentCount,
                    Reason = reason,
                    UpdatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                var situationNumber = await _numberGenerator.GenerateAsync(cancellationToken);
                var primaryCandidate = cluster
                    .OrderByDescending(c => c.Severity)
                    .ThenByDescending(c => c.Priority)
                    .ThenBy(c => c.CreatedAtUtc)
                    .First();

                var score = SituationScoreCalculator.Calculate(
                    cluster.Count,
                    cluster.Count(x => x.Severity == SeverityLevel.Critical),
                    cluster.Count(x => x.Severity == SeverityLevel.High),
                    cluster.Sum(x => x.PeopleAffected),
                    cluster.Sum(x => x.Casualties));
                var level = SituationLevelCalculator.Calculate(score);

                var situation = new OperationalSituation
                {
                    SituationNumber = situationNumber,
                    Title = $"Situation Cluster ({cluster.Count} incidents)",
                    Level = level,
                    Status = EscalationStatus.Open,
                    SituationScore = score,
                    IncidentCount = cluster.Count,
                    CriticalIncidentCount = cluster.Count(x => x.Severity == SeverityLevel.Critical),
                    HighIncidentCount = cluster.Count(x => x.Severity == SeverityLevel.High),
                    PeopleAffected = cluster.Sum(x => x.PeopleAffected),
                    Casualties = cluster.Sum(x => x.Casualties),
                    ResourceShortageDetected = hasResourceShortage,
                    RequiresHumanReview = true,
                    LastEvaluatedAtUtc = DateTime.UtcNow
                };

                foreach (var candidate in cluster)
                {
                    situation.Incidents.Add(new OperationalSituationIncident
                    {
                        OperationalSituation = situation,
                        IncidentId = candidate.IncidentId,
                        IsPrimaryIncident = candidate.IncidentId == primaryCandidate.IncidentId,
                        AddedAtUtc = DateTime.UtcNow
                    });
                }

                _db.OperationalSituations.Add(situation);
                existingSituations.Add(situation);

                await RunAIAnalysisAsync(situation, clusterIncidentIds, cancellationToken);

                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = _currentUserService.UserId,
                    Action = "OperationalSituationCreated",
                    EntityName = nameof(OperationalSituation),
                    EntityId = situation.Id.ToString(),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        situation.SituationNumber,
                        situation.Title,
                        situation.Level,
                        situation.IncidentCount,
                        situation.SituationScore
                    }),
                    OccurredAtUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(cancellationToken);

                await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
                {
                    SituationId = situation.Id,
                    SituationNumber = situation.SituationNumber,
                    Title = situation.Title,
                    Level = situation.Level,
                    Status = situation.Status,
                    IncidentCount = situation.IncidentCount,
                    Reason = SituationUpdateReasons.Created,
                    UpdatedAtUtc = DateTime.UtcNow
                }, cancellationToken);

                if (_ruleEngine != null && _operationalNotificationService != null)
                {
                    var notifCtx = new NotificationContext
                    {
                        SituationId = situation.Id,
                        SituationNumber = situation.SituationNumber,
                        SituationLevel = situation.Level,
                        ResourceShortage = situation.ResourceShortageDetected,
                        Reason = situation.Title
                    };
                    var notifs = _ruleEngine.Evaluate(notifCtx);
                    foreach (var n in notifs)
                    {
                        await _operationalNotificationService.CreateAsync(n, cancellationToken);
                    }
                }
            }
        }
    }

    public async Task<OperationalSituationResponse?> GetByIdAsync(
        Guid situationId,
        CancellationToken cancellationToken = default)
    {
        var situation = await _db.OperationalSituations
            .AsNoTracking()
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.IncidentType)
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.Locations)
            .Include(s => s.Analyses.OrderByDescending(a => a.GeneratedAtUtc))
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null) return null;

        return MapToResponse(situation);
    }

    public async Task<IReadOnlyList<OperationalSituationResponse>> GetOpenAsync(
        CancellationToken cancellationToken = default)
    {
        var situations = await _db.OperationalSituations
            .AsNoTracking()
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.IncidentType)
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.Locations)
            .Include(s => s.Analyses.OrderByDescending(a => a.GeneratedAtUtc))
            .Where(s => s.Status == EscalationStatus.Open)
            .OrderByDescending(s => s.Level)
            .ThenByDescending(s => s.SituationScore)
            .ThenByDescending(s => s.LastEvaluatedAtUtc)
            .ToListAsync(cancellationToken);

        return situations.Select(MapToResponse).ToList();
    }

    public async Task ReviewAsync(
        Guid situationId,
        ReviewSituationRequest request,
        CancellationToken cancellationToken = default)
    {
        var situation = await _db.OperationalSituations
            .Include(s => s.Analyses.OrderByDescending(a => a.GeneratedAtUtc))
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null)
        {
            throw new KeyNotFoundException($"Situation {situationId} not found.");
        }

        if (situation.Status != EscalationStatus.Open)
        {
            throw new InvalidOperationException($"Cannot review situation with status {situation.Status}.");
        }

        situation.RequiresHumanReview = false;

        var latestAnalysis = situation.Analyses.FirstOrDefault();
        if (latestAnalysis != null)
        {
            latestAnalysis.HumanReviewed = true;
            latestAnalysis.HumanDecision = request.Decision;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "OperationalSituationReviewed",
            EntityName = nameof(OperationalSituation),
            EntityId = situation.Id.ToString(),
            NewValues = JsonSerializer.Serialize(new { request.Decision, request.Notes }),
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
        {
            SituationId = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Level = situation.Level,
            Status = situation.Status,
            IncidentCount = situation.IncidentCount,
            Reason = SituationUpdateReasons.Reviewed,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task ResolveAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var situation = await _db.OperationalSituations
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null)
        {
            throw new KeyNotFoundException($"Situation {situationId} not found.");
        }

        if (situation.Status is EscalationStatus.Resolved or EscalationStatus.Dismissed)
        {
            throw new InvalidOperationException($"Situation {situationId} is already {situation.Status}.");
        }

        var prevStatus = situation.Status;
        situation.Status = EscalationStatus.Resolved;
        situation.RequiresHumanReview = false;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "OperationalSituationResolved",
            EntityName = nameof(OperationalSituation),
            EntityId = situation.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { Status = prevStatus }),
            NewValues = JsonSerializer.Serialize(new { Status = situation.Status, Notes = notes }),
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
        {
            SituationId = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Level = situation.Level,
            Status = situation.Status,
            IncidentCount = situation.IncidentCount,
            Reason = SituationUpdateReasons.Resolved,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task DismissAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var situation = await _db.OperationalSituations
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null)
        {
            throw new KeyNotFoundException($"Situation {situationId} not found.");
        }

        if (situation.Status is EscalationStatus.Resolved or EscalationStatus.Dismissed)
        {
            throw new InvalidOperationException($"Situation {situationId} is already {situation.Status}.");
        }

        var prevStatus = situation.Status;
        situation.Status = EscalationStatus.Dismissed;
        situation.RequiresHumanReview = false;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "OperationalSituationDismissed",
            EntityName = nameof(OperationalSituation),
            EntityId = situation.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { Status = prevStatus }),
            NewValues = JsonSerializer.Serialize(new { Status = situation.Status, Notes = notes }),
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
        {
            SituationId = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Level = situation.Level,
            Status = situation.Status,
            IncidentCount = situation.IncidentCount,
            Reason = SituationUpdateReasons.Dismissed,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task AcknowledgeAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var situation = await _db.OperationalSituations
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null)
        {
            throw new KeyNotFoundException($"Situation {situationId} not found.");
        }

        if (situation.Status != EscalationStatus.Open)
        {
            throw new InvalidOperationException($"Situation {situationId} cannot be acknowledged because its current status is {situation.Status}. Only open situations can be acknowledged.");
        }

        var prevStatus = situation.Status;
        situation.Status = EscalationStatus.Acknowledged;
        situation.RequiresHumanReview = false;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "SituationAcknowledged",
            EntityName = nameof(OperationalSituation),
            EntityId = situation.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { Status = prevStatus }),
            NewValues = JsonSerializer.Serialize(new { Status = situation.Status, Notes = notes }),
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
        {
            SituationId = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Level = situation.Level,
            Status = situation.Status,
            IncidentCount = situation.IncidentCount,
            Reason = SituationUpdateReasons.Acknowledged,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task ReanalyzeAsync(
        Guid situationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = EmergencyResponseTelemetry.ActivitySource.StartActivity("AI.SituationAnalysis");
        activity?.SetTag("situation.id", situationId.ToString());

        var situation = await _db.OperationalSituations
            .Include(s => s.Incidents)
            .Include(s => s.Analyses)
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null)
        {
            throw new KeyNotFoundException($"Situation {situationId} not found.");
        }

        var incidentIds = situation.Incidents.Select(si => si.IncidentId).ToList();
        await RunAIAnalysisAsync(situation, incidentIds, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifySituationUpdatedAsync(new SituationUpdatedEvent
        {
            SituationId = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Level = situation.Level,
            Status = situation.Status,
            IncidentCount = situation.IncidentCount,
            Reason = "Reanalyzed",
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SituationCommandCenterItem>> GetCommandCenterAsync(
        CancellationToken cancellationToken = default)
    {
        var situations = await _db.OperationalSituations
            .AsNoTracking()
            .Where(x => x.Status == EscalationStatus.Open || x.Status == EscalationStatus.Acknowledged)
            .OrderByDescending(x => x.Level)
            .ThenByDescending(x => x.SituationScore)
            .ThenByDescending(x => x.LastEvaluatedAtUtc)
            .Select(x => new SituationCommandCenterItem
            {
                Id = x.Id,
                SituationNumber = x.SituationNumber,
                Title = x.Title,
                Summary = x.Summary ?? string.Empty,
                Level = x.Level,
                Status = x.Status,
                SituationScore = x.SituationScore ?? 0.0,
                IncidentCount = x.IncidentCount,
                CriticalIncidentCount = x.CriticalIncidentCount,
                HighIncidentCount = x.HighIncidentCount,
                PeopleAffected = x.PeopleAffected,
                Casualties = x.Casualties,
                ResourceShortageDetected = x.ResourceShortageDetected,
                RequiresHumanReview = x.RequiresHumanReview,
                LastEvaluatedAtUtc = x.LastEvaluatedAtUtc ?? x.CreatedAtUtc,
                LastAIAnalysisAtUtc = x.LastAIAnalysisAtUtc
            })
            .ToListAsync(cancellationToken);

        return situations;
    }

    public async Task<SituationMapItem> GetMapSituationAsync(
        Guid situationId,
        CancellationToken cancellationToken = default)
    {
        var situation = await _db.OperationalSituations
            .AsNoTracking()
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.Locations)
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.IncidentType)
            .FirstOrDefaultAsync(s => s.Id == situationId, cancellationToken);

        if (situation == null)
        {
            throw new KeyNotFoundException($"Situation {situationId} not found.");
        }

        var mapIncidents = BuildMapIncidents(situation.Incidents);
        if (mapIncidents.Count == 0)
        {
            throw new InvalidOperationException($"Situation {situationId} has no member incidents with geographic coordinates.");
        }

        var envelope = SituationEnvelopeCalculator.CalculateEnvelope(mapIncidents);
        var primaryIncident = mapIncidents.FirstOrDefault(i => i.IsPrimaryIncident) ?? mapIncidents.FirstOrDefault();

        return new SituationMapItem
        {
            Id = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Summary = situation.Summary ?? string.Empty,
            Level = situation.Level,
            Status = situation.Status,
            SituationScore = situation.SituationScore ?? 0.0,
            IncidentCount = situation.IncidentCount,
            CriticalIncidentCount = situation.CriticalIncidentCount,
            HighIncidentCount = situation.HighIncidentCount,
            PeopleAffected = situation.PeopleAffected,
            Casualties = situation.Casualties,
            ResourceShortageDetected = situation.ResourceShortageDetected,
            RequiresHumanReview = situation.RequiresHumanReview,
            CenterLatitude = envelope.CenterLatitude,
            CenterLongitude = envelope.CenterLongitude,
            RadiusMeters = envelope.RadiusMeters,
            PrimaryIncidentId = primaryIncident?.IncidentId,
            PrimaryIncidentNumber = primaryIncident?.IncidentNumber,
            Incidents = mapIncidents
        };
    }

    public async Task<IReadOnlyList<SituationMapItem>> GetOpenForMapAsync(
        CancellationToken cancellationToken = default)
    {
        var situations = await _db.OperationalSituations
            .AsNoTracking()
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.Locations)
            .Include(s => s.Incidents)
                .ThenInclude(si => si.Incident)
                    .ThenInclude(i => i.IncidentType)
            .Where(s => s.Status == EscalationStatus.Open || s.Status == EscalationStatus.Acknowledged)
            .OrderByDescending(s => s.Level)
            .ThenByDescending(s => s.SituationScore)
            .ToListAsync(cancellationToken);

        var result = new List<SituationMapItem>();

        foreach (var situation in situations)
        {
            var mapIncidents = BuildMapIncidents(situation.Incidents);
            if (mapIncidents.Count == 0)
            {
                continue;
            }

            var envelope = SituationEnvelopeCalculator.CalculateEnvelope(mapIncidents);
            var primaryIncident = mapIncidents.FirstOrDefault(i => i.IsPrimaryIncident) ?? mapIncidents.FirstOrDefault();

            result.Add(new SituationMapItem
            {
                Id = situation.Id,
                SituationNumber = situation.SituationNumber,
                Title = situation.Title,
                Summary = situation.Summary ?? string.Empty,
                Level = situation.Level,
                Status = situation.Status,
                SituationScore = situation.SituationScore ?? 0.0,
                IncidentCount = situation.IncidentCount,
                CriticalIncidentCount = situation.CriticalIncidentCount,
                HighIncidentCount = situation.HighIncidentCount,
                PeopleAffected = situation.PeopleAffected,
                Casualties = situation.Casualties,
                ResourceShortageDetected = situation.ResourceShortageDetected,
                RequiresHumanReview = situation.RequiresHumanReview,
                CenterLatitude = envelope.CenterLatitude,
                CenterLongitude = envelope.CenterLongitude,
                RadiusMeters = envelope.RadiusMeters,
                PrimaryIncidentId = primaryIncident?.IncidentId,
                PrimaryIncidentNumber = primaryIncident?.IncidentNumber,
                Incidents = mapIncidents
            });
        }

        return result;
    }

    private static IReadOnlyList<SituationMapIncident> BuildMapIncidents(
        IEnumerable<OperationalSituationIncident> situationIncidents)
    {
        var list = new List<SituationMapIncident>();
        foreach (var si in situationIncidents)
        {
            var incident = si.Incident;
            if (incident == null) continue;

            var primaryLoc = incident.Locations?.OrderByDescending(l => l.IsPrimary).FirstOrDefault();
            if (primaryLoc == null) continue;

            list.Add(new SituationMapIncident
            {
                IncidentId = si.IncidentId,
                IncidentNumber = incident.IncidentNumber ?? string.Empty,
                Title = incident.Title ?? string.Empty,
                IncidentType = incident.IncidentType?.Name ?? "Incident",
                Severity = incident.Severity,
                Priority = incident.Priority,
                Latitude = (double)primaryLoc.Latitude,
                Longitude = (double)primaryLoc.Longitude,
                IsPrimaryIncident = si.IsPrimaryIncident
            });
        }
        return list;
    }

    private async Task<bool> HasResourceShortageAsync(
        IEnumerable<Guid> incidentIds,
        CancellationToken cancellationToken)
    {
        return await _db.IncidentEscalations
            .AnyAsync(e => incidentIds.Contains(e.IncidentId) &&
                           e.Type == EscalationType.ResourceShortage &&
                           e.Status == EscalationStatus.Open, cancellationToken);
    }

    private static bool RequiresNewAIAnalysis(
        OperationalSituation situation,
        int prevCount,
        EscalationLevel prevLevel,
        int prevCasualties)
    {
        if (situation.LastAIAnalysisAtUtc == null) return true;
        if (Math.Abs(situation.IncidentCount - prevCount) >= 2) return true;
        if (situation.Level > prevLevel) return true;
        if (situation.Casualties > prevCasualties) return true;
        if ((DateTime.UtcNow - situation.LastAIAnalysisAtUtc.Value).TotalHours >= 2.0) return true;
        return false;
    }

    private async Task RunAIAnalysisAsync(
        OperationalSituation situation,
        IEnumerable<Guid> incidentIds,
        CancellationToken cancellationToken)
    {
        var targetIds = incidentIds.ToList();
        var snapshotIncidents = await _db.Incidents
            .AsNoTracking()
            .Include(i => i.Locations)
            .Include(i => i.IncidentType)
            .Where(i => targetIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var activeEscalations = await _db.IncidentEscalations
            .AsNoTracking()
            .Where(e => targetIds.Contains(e.IncidentId) && e.Status == EscalationStatus.Open)
            .GroupBy(e => e.IncidentId)
            .Select(g => new { IncidentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.IncidentId, g => g.Count, cancellationToken);

        var snapshotItems = snapshotIncidents.Select(inc =>
        {
            var loc = inc.Locations.OrderByDescending(l => l.IsPrimary).FirstOrDefault();
            return new IncidentSnapshotItem
            {
                IncidentId = inc.Id,
                IncidentNumber = inc.IncidentNumber,
                Title = inc.Title,
                Description = inc.Description,
                IncidentTypeName = inc.IncidentType?.Name,
                Severity = inc.Severity,
                Latitude = loc != null ? (double)loc.Latitude : 0.0,
                Longitude = loc != null ? (double)loc.Longitude : 0.0,
                CreatedAtUtc = inc.CreatedAtUtc,
                PeopleAffected = inc.PeopleAffected,
                Casualties = inc.Casualties,
                ActiveEscalationsCount = activeEscalations.TryGetValue(inc.Id, out var count) ? count : 0
            };
        }).ToList();

        var snapshot = new SituationSnapshot
        {
            SituationNumber = situation.SituationNumber,
            IncidentCount = situation.IncidentCount,
            CriticalIncidentCount = situation.CriticalIncidentCount,
            HighIncidentCount = situation.HighIncidentCount,
            Level = situation.Level,
            SituationScore = situation.SituationScore ?? 0.0,
            PeopleAffected = situation.PeopleAffected,
            Casualties = situation.Casualties,
            ResourceShortageDetected = situation.ResourceShortageDetected,
            Incidents = snapshotItems
        };

        try
        {
            var aiResult = await _analysisService.AnalyzeSituationAsync(snapshot, cancellationToken);
            situation.Title = aiResult.Title;
            situation.Summary = aiResult.Summary;
            situation.LastAIAnalysisAtUtc = DateTime.UtcNow;

            var analysisRecord = new OperationalSituationAnalysis
            {
                OperationalSituation = situation,
                PortfolioDemoOwnershipKey = situation.PortfolioDemoOwnershipKey,
                ModelName = _aiOptions.Model,
                PromptVersion = AIPromptVersions.SituationSummary,
                InputSnapshotJson = JsonSerializer.Serialize(snapshot),
                OutputJson = JsonSerializer.Serialize(aiResult),
                GeneratedAtUtc = DateTime.UtcNow,
                HumanReviewed = false
            };

            situation.Analyses.Add(analysisRecord);
            _db.OperationalSituationAnalyses.Add(analysisRecord);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI situation analysis for {SituationNumber}", situation.SituationNumber);
            if (string.IsNullOrWhiteSpace(situation.Title))
            {
                situation.Title = $"Situation Cluster ({situation.IncidentCount} incidents)";
            }
        }
    }

    private static OperationalSituationResponse MapToResponse(OperationalSituation situation)
    {
        SituationAnalysisResult? latestAnalysisResult = null;
        var latestAnalysis = situation.Analyses.OrderByDescending(a => a.GeneratedAtUtc).FirstOrDefault();
        if (latestAnalysis != null && !string.IsNullOrWhiteSpace(latestAnalysis.OutputJson))
        {
            try
            {
                latestAnalysisResult = JsonSerializer.Deserialize<SituationAnalysisResult>(latestAnalysis.OutputJson);
            }
            catch
            {
                // Fallback to basic summary
            }
        }

        var incidentItems = situation.Incidents.Select(si =>
        {
            var inc = si.Incident;
            var loc = inc?.Locations.OrderByDescending(l => l.IsPrimary).FirstOrDefault();
            return new OperationalSituationIncidentItem
            {
                IncidentId = si.IncidentId,
                IncidentNumber = inc?.IncidentNumber ?? string.Empty,
                Title = inc?.Title ?? string.Empty,
                IncidentTypeName = inc?.IncidentType?.Name,
                Severity = inc?.Severity ?? SeverityLevel.Low,
                Priority = inc?.Priority ?? PriorityLevel.Low,
                Latitude = loc != null ? (double)loc.Latitude : 0.0,
                Longitude = loc != null ? (double)loc.Longitude : 0.0,
                PeopleAffected = inc?.PeopleAffected,
                Casualties = inc?.Casualties,
                IsPrimaryIncident = si.IsPrimaryIncident,
                AddedAtUtc = si.AddedAtUtc
            };
        }).ToList();

        return new OperationalSituationResponse
        {
            Id = situation.Id,
            SituationNumber = situation.SituationNumber,
            Title = situation.Title,
            Summary = situation.Summary,
            Level = situation.Level,
            Status = situation.Status,
            SituationScore = situation.SituationScore,
            IncidentCount = situation.IncidentCount,
            CriticalIncidentCount = situation.CriticalIncidentCount,
            HighIncidentCount = situation.HighIncidentCount,
            PeopleAffected = situation.PeopleAffected,
            Casualties = situation.Casualties,
            ResourceShortageDetected = situation.ResourceShortageDetected,
            RequiresHumanReview = situation.RequiresHumanReview,
            LastEvaluatedAtUtc = situation.LastEvaluatedAtUtc,
            LastAIAnalysisAtUtc = situation.LastAIAnalysisAtUtc,
            LatestAnalysis = latestAnalysisResult,
            Incidents = incidentItems
        };
    }
}
