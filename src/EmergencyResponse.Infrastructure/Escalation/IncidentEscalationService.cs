using System.Text.Json;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.Escalation;
using EmergencyResponse.Application.Escalation.DTOs;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Escalation;

public sealed class IncidentEscalationService : IIncidentEscalationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIncidentNotificationService _notificationService;
    private readonly ILogger<IncidentEscalationService> _logger;
    private readonly INotificationRuleEngine? _ruleEngine;
    private readonly INotificationService? _operationalNotificationService;

    public IncidentEscalationService(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationService,
        ILogger<IncidentEscalationService> logger,
        INotificationRuleEngine? ruleEngine = null,
        INotificationService? operationalNotificationService = null)
    {
        _db = db;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
        _ruleEngine = ruleEngine;
        _operationalNotificationService = operationalNotificationService;
    }

    public async Task<IReadOnlyList<IncidentEscalationResponse>> EvaluateIncidentAsync(
        Guid incidentId,
        SeverityLevel? previousSeverity = null,
        CancellationToken cancellationToken = default)
    {
        var incident = await _db.Incidents
            .Include(i => i.IncidentType)
            .Include(i => i.AIAnalyses.OrderByDescending(a => a.CreatedAtUtc))
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null)
        {
            _logger.LogWarning("Cannot evaluate escalations: Incident {IncidentId} not found.", incidentId);
            return [];
        }

        var latestAnalysis = incident.AIAnalyses.FirstOrDefault();
        EmergencyFacts? latestFacts = null;
        if (!string.IsNullOrWhiteSpace(latestAnalysis?.ExtractedFactsJson))
        {
            try
            {
                latestFacts = JsonSerializer.Deserialize<EmergencyFacts>(latestAnalysis.ExtractedFactsJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize extracted facts for incident {IncidentId}", incidentId);
            }
        }

        var relatedIncidentCount = await _db.IncidentDuplicateMatches
            .CountAsync(x => (x.SourceIncidentId == incidentId || x.CandidateIncidentId == incidentId) &&
                             x.DuplicateScore >= 0.5,
                        cancellationToken);

        var duplicateIncidentCount = await _db.IncidentDuplicateMatches
            .CountAsync(x => (x.SourceIncidentId == incidentId || x.CandidateIncidentId == incidentId) &&
                             (x.ConfirmedDuplicate || x.DuplicateScore >= 0.75),
                        cancellationToken);

        var requirements = await _db.IncidentTypeResourceRequirements
            .AsNoTracking()
            .Where(r => r.IncidentTypeId == incident.IncidentTypeId)
            .ToListAsync(cancellationToken);

        var requiredResourceCount = requirements.Sum(r => r.MinimumCount);
        var requiredTypes = requirements.Select(r => r.ResourceType).ToHashSet();

        var availableMatchingResourceCount = requiredTypes.Count > 0
            ? await _db.Resources.CountAsync(
                r => r.Status == ResourceStatus.Available && requiredTypes.Contains(r.Type),
                cancellationToken)
            : 0;

        var input = new EscalationEvaluationInput
        {
            IncidentId = incident.Id,
            CurrentSeverity = incident.Severity,
            PreviousSeverity = previousSeverity,
            PeopleAffected = incident.PeopleAffected ?? 0,
            Casualties = incident.Casualties ?? 0,
            PeopleTrapped = latestFacts?.PeopleTrapped ?? false,
            ImmediateDanger = latestFacts?.ImmediateDanger ?? false,
            CriticalInfrastructureAffected = latestFacts?.CriticalInfrastructureAffected ?? false,
            EscalationRisk = latestFacts?.EscalationRisk ?? false,
            RelatedIncidentCount = relatedIncidentCount,
            DuplicateIncidentCount = duplicateIncidentCount,
            RequiredResourceCount = requiredResourceCount,
            AvailableMatchingResourceCount = availableMatchingResourceCount
        };

        var recommendations = EscalationEngine.Evaluate(input);
        if (recommendations.Count == 0)
        {
            return [];
        }

        var createdEscalations = new List<IncidentEscalation>();
        var now = DateTime.UtcNow;

        foreach (var rec in recommendations)
        {
            var alreadyOpen = await _db.IncidentEscalations.AnyAsync(
                x => x.IncidentId == rec.IncidentId &&
                     x.Type == rec.Type &&
                     x.Status == EscalationStatus.Open,
                cancellationToken);

            if (alreadyOpen)
            {
                continue;
            }

            var escalation = new IncidentEscalation
            {
                Id = Guid.NewGuid(),
                IncidentId = rec.IncidentId,
                Type = rec.Type,
                Level = rec.Level,
                Status = EscalationStatus.Open,
                Reason = rec.Reason,
                EvidenceJson = JsonSerializer.Serialize(rec.Evidence),
                Score = rec.Score,
                RequiresHumanReview = rec.RequiresHumanReview
            };

            _db.IncidentEscalations.Add(escalation);
            createdEscalations.Add(escalation);

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = _currentUserService.UserId,
                Action = "IncidentEscalationCreated",
                EntityName = nameof(IncidentEscalation),
                EntityId = escalation.Id.ToString(),
                NewValues = JsonSerializer.Serialize(new
                {
                    incidentId = rec.IncidentId,
                    incidentNumber = incident.IncidentNumber,
                    type = rec.Type.ToString(),
                    level = rec.Level.ToString(),
                    reason = rec.Reason,
                    score = rec.Score,
                    evidence = rec.Evidence
                }),
                OccurredAtUtc = now
            });
        }

        if (createdEscalations.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);

            if (_ruleEngine != null && _operationalNotificationService != null)
            {
                foreach (var created in createdEscalations)
                {
                    var notifCtx = new NotificationContext
                    {
                        IncidentId = created.IncidentId,
                        IncidentNumber = incident.IncidentNumber,
                        EscalationId = created.Id,
                        EscalationLevel = created.Level,
                        EscalationType = created.Type,
                        Reason = created.Reason,
                        PeopleTrapped = latestFacts?.PeopleTrapped ?? false,
                        ImmediateDanger = latestFacts?.ImmediateDanger ?? false
                    };

                    var requests = _ruleEngine.Evaluate(notifCtx);
                    foreach (var req in requests)
                    {
                        await _operationalNotificationService.CreateAsync(req, cancellationToken);
                    }
                }
            }

            foreach (var created in createdEscalations)
            {
                await _notificationService.NotifyEscalationUpdatedAsync(
                    new EscalationUpdatedEvent
                    {
                        EscalationId = created.Id,
                        IncidentId = created.IncidentId,
                        IncidentNumber = incident.IncidentNumber,
                        Type = created.Type.ToString(),
                        Level = created.Level.ToString(),
                        Status = created.Status.ToString(),
                        Reason = created.Reason,
                        UpdatedAtUtc = now
                    },
                    cancellationToken);
            }

            await _notificationService.NotifyDashboardUpdatedAsync(
                new DashboardUpdatedEvent
                {
                    Reason = EscalationUpdateReasons.Created,
                    IncidentId = incident.Id,
                    UpdatedAtUtc = now
                },
                cancellationToken);
        }

        return createdEscalations.Select(e => MapToResponse(e, incident)).ToList();
    }

    public async Task<IReadOnlyList<IncidentEscalationResponse>> GetOpenEscalationsAsync(
        CancellationToken cancellationToken = default)
    {
        var escalations = await _db.IncidentEscalations
            .AsNoTracking()
            .Include(e => e.Incident)
            .Where(e => e.Status == EscalationStatus.Open)
            .OrderByDescending(e => e.Level)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return escalations.Select(e => MapToResponse(e, e.Incident)).ToList();
    }

    public async Task<IReadOnlyList<IncidentEscalationResponse>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var escalations = await _db.IncidentEscalations
            .AsNoTracking()
            .Include(e => e.Incident)
            .Where(e => e.IncidentId == incidentId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return escalations.Select(e => MapToResponse(e, e.Incident)).ToList();
    }

    public async Task AcknowledgeAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var escalation = await _db.IncidentEscalations
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == escalationId, cancellationToken);

        if (escalation is null)
        {
            throw new KeyNotFoundException($"Escalation {escalationId} not found.");
        }

        if (!EscalationStatusRules.IsAllowed(escalation.Status, EscalationStatus.Acknowledged))
        {
            throw new InvalidOperationException(
                $"Cannot transition escalation from {escalation.Status} to {EscalationStatus.Acknowledged}.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = escalation.Status;

        escalation.Status = EscalationStatus.Acknowledged;
        escalation.AcknowledgedByUserId = _currentUserService.UserId;
        escalation.AcknowledgedAtUtc = now;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            escalation.ResolutionNotes = notes;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "IncidentEscalationAcknowledged",
            EntityName = nameof(IncidentEscalation),
            EntityId = escalation.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { status = previousStatus.ToString() }),
            NewValues = JsonSerializer.Serialize(new
            {
                status = escalation.Status.ToString(),
                notes,
                acknowledgedAtUtc = now
            }),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyEscalationUpdatedAsync(
            new EscalationUpdatedEvent
            {
                EscalationId = escalation.Id,
                IncidentId = escalation.IncidentId,
                IncidentNumber = escalation.Incident?.IncidentNumber,
                Type = escalation.Type.ToString(),
                Level = escalation.Level.ToString(),
                Status = escalation.Status.ToString(),
                Reason = escalation.Reason,
                UpdatedAtUtc = now
            },
            cancellationToken);

        await _notificationService.NotifyDashboardUpdatedAsync(
            new DashboardUpdatedEvent
            {
                Reason = EscalationUpdateReasons.Acknowledged,
                IncidentId = escalation.IncidentId,
                UpdatedAtUtc = now
            },
            cancellationToken);
    }

    public async Task ResolveAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var escalation = await _db.IncidentEscalations
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == escalationId, cancellationToken);

        if (escalation is null)
        {
            throw new KeyNotFoundException($"Escalation {escalationId} not found.");
        }

        if (!EscalationStatusRules.IsAllowed(escalation.Status, EscalationStatus.Resolved))
        {
            throw new InvalidOperationException(
                $"Cannot transition escalation from {escalation.Status} to {EscalationStatus.Resolved}.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = escalation.Status;

        escalation.Status = EscalationStatus.Resolved;
        escalation.ResolvedByUserId = _currentUserService.UserId;
        escalation.ResolvedAtUtc = now;
        escalation.ResolutionNotes = notes;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "IncidentEscalationResolved",
            EntityName = nameof(IncidentEscalation),
            EntityId = escalation.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { status = previousStatus.ToString() }),
            NewValues = JsonSerializer.Serialize(new
            {
                status = escalation.Status.ToString(),
                notes,
                resolvedAtUtc = now
            }),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyEscalationUpdatedAsync(
            new EscalationUpdatedEvent
            {
                EscalationId = escalation.Id,
                IncidentId = escalation.IncidentId,
                IncidentNumber = escalation.Incident?.IncidentNumber,
                Type = escalation.Type.ToString(),
                Level = escalation.Level.ToString(),
                Status = escalation.Status.ToString(),
                Reason = escalation.Reason,
                UpdatedAtUtc = now
            },
            cancellationToken);

        await _notificationService.NotifyDashboardUpdatedAsync(
            new DashboardUpdatedEvent
            {
                Reason = EscalationUpdateReasons.Resolved,
                IncidentId = escalation.IncidentId,
                UpdatedAtUtc = now
            },
            cancellationToken);
    }

    public async Task DismissAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var escalation = await _db.IncidentEscalations
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == escalationId, cancellationToken);

        if (escalation is null)
        {
            throw new KeyNotFoundException($"Escalation {escalationId} not found.");
        }

        if (!EscalationStatusRules.IsAllowed(escalation.Status, EscalationStatus.Dismissed))
        {
            throw new InvalidOperationException(
                $"Cannot transition escalation from {escalation.Status} to {EscalationStatus.Dismissed}.");
        }

        var now = DateTime.UtcNow;
        var previousStatus = escalation.Status;

        escalation.Status = EscalationStatus.Dismissed;
        escalation.ResolvedByUserId = _currentUserService.UserId;
        escalation.ResolvedAtUtc = now;
        escalation.ResolutionNotes = notes;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "IncidentEscalationDismissed",
            EntityName = nameof(IncidentEscalation),
            EntityId = escalation.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new { status = previousStatus.ToString() }),
            NewValues = JsonSerializer.Serialize(new
            {
                status = escalation.Status.ToString(),
                notes,
                dismissedAtUtc = now
            }),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyEscalationUpdatedAsync(
            new EscalationUpdatedEvent
            {
                EscalationId = escalation.Id,
                IncidentId = escalation.IncidentId,
                IncidentNumber = escalation.Incident?.IncidentNumber,
                Type = escalation.Type.ToString(),
                Level = escalation.Level.ToString(),
                Status = escalation.Status.ToString(),
                Reason = escalation.Reason,
                UpdatedAtUtc = now
            },
            cancellationToken);

        await _notificationService.NotifyDashboardUpdatedAsync(
            new DashboardUpdatedEvent
            {
                Reason = EscalationUpdateReasons.Dismissed,
                IncidentId = escalation.IncidentId,
                UpdatedAtUtc = now
            },
            cancellationToken);
    }

    private static IncidentEscalationResponse MapToResponse(
        IncidentEscalation e,
        Incident? incident)
    {
        IReadOnlyList<string> evidence = [];
        if (!string.IsNullOrWhiteSpace(e.EvidenceJson))
        {
            try
            {
                evidence = JsonSerializer.Deserialize<List<string>>(e.EvidenceJson) ?? [];
            }
            catch
            {
                evidence = [e.EvidenceJson];
            }
        }

        return new IncidentEscalationResponse
        {
            Id = e.Id,
            IncidentId = e.IncidentId,
            IncidentNumber = incident?.IncidentNumber,
            IncidentTitle = incident?.Title,
            Type = e.Type,
            TypeName = FormatEnumName(e.Type.ToString()),
            Level = e.Level,
            LevelName = e.Level.ToString(),
            Status = e.Status,
            StatusName = e.Status.ToString(),
            Reason = e.Reason,
            Evidence = evidence,
            Score = e.Score,
            RequiresHumanReview = e.RequiresHumanReview,
            AcknowledgedByUserId = e.AcknowledgedByUserId,
            AcknowledgedByUserName = e.AcknowledgedByUserId,
            AcknowledgedAtUtc = e.AcknowledgedAtUtc,
            ResolvedByUserId = e.ResolvedByUserId,
            ResolvedByUserName = e.ResolvedByUserId,
            ResolvedAtUtc = e.ResolvedAtUtc,
            ResolutionNotes = e.ResolutionNotes,
            CreatedAtUtc = e.CreatedAtUtc
        };
    }

    private static string FormatEnumName(string name)
    {
        // Inserts space before capital letters
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        sb.Append(name[0]);
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                sb.Append(' ');
            }
            sb.Append(name[i]);
        }
        return sb.ToString();
    }
}
