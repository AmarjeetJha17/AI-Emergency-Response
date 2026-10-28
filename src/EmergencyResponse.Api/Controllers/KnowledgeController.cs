using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Application.Security.SecurityAudit;
using EmergencyResponse.Api.Common;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Knowledge;
using EmergencyResponse.Application.Security.Authorization;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
[Authorize]
public sealed class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeDocumentService _documents;
    private readonly IKnowledgeSearchService _search;
    private readonly IKnowledgeAccessContextFactory _access;
    private readonly IKnowledgeDocumentQueue _queue;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public KnowledgeController(
        IKnowledgeDocumentService documents,
        IKnowledgeSearchService search,
        IKnowledgeAccessContextFactory access,
        IKnowledgeDocumentQueue queue,
        ApplicationDbContext db,
        ICurrentUserService currentUser)
    {
        _documents = documents;
        _search = search;
        _access = access;
        _queue = queue;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("documents")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    [Authorize(Roles = $"{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [Authorize(Policy = Permissions.KnowledgeManage)]
    [EnableRateLimiting("admin-api")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<object>> Upload(
        IFormFile file,
        [FromForm] string title,
        [FromForm] KnowledgeDocumentType documentType,
        [FromForm] string version,
        [FromForm] string? description,
        [FromForm] Guid? departmentId,
        [FromForm] bool isGlobal,
        [FromForm] string[]? allowedRoles,
        [FromForm] string[]? allowedDepartments,
        [FromForm] DateTime? effectiveFromUtc,
        [FromForm] DateTime? effectiveUntilUtc,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest("A document file is required.");
        }

        await using var stream = file.OpenReadStream();
        var document = await _documents.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            title,
            documentType,
            version,
            description,
            departmentId,
            isGlobal,
            allowedRoles ?? [],
            allowedDepartments ?? [],
            effectiveFromUtc,
            effectiveUntilUtc,
            cancellationToken);
        await _queue.EnqueueAsync(
            new KnowledgeDocumentJob(document.Id),
            cancellationToken);
        return Ok(new
        {
            document.Id,
            document.DocumentNumber,
            document.Status,
            document.Title,
            document.Version
        });
    }

    [HttpPost("documents/{id:guid}/publish")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    [Authorize(Roles = $"{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [Authorize(Policy = Permissions.KnowledgeManage)]
    [EnableRateLimiting("admin-api")]
    public async Task<IActionResult> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _documents.PublishAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("documents/{id:guid}/archive")]
    [Authorize(Roles = $"{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [Authorize(Policy = Permissions.KnowledgeManage)]
    [EnableRateLimiting("admin-api")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _documents.ArchiveAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("search")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    [Authorize(Roles =
        $"{ApplicationRoles.Responder},{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [Authorize(Policy = Permissions.KnowledgeRead)]
    public async Task<ActionResult<IReadOnlyList<KnowledgeSearchResult>>> Search(
        [FromQuery] string question,
        CancellationToken cancellationToken)
    {
        var access = await _access.CreateAsync(cancellationToken);
        var results = await _search.SearchAsync(
            question,
            access,
            cancellationToken);

        if (results.Count > 0)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.UserId,
                Action = SecurityEventTypes.KnowledgeDocumentAccess,
                EntityName = nameof(KnowledgeDocument),
                EntityId = "search",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    documentNumbers = results
                        .Select(result => result.DocumentNumber)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    resultCount = results.Count
                }),
                OccurredAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(results);
    }
}
