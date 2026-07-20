using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Incidents;
using Dazzled.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dazzled.Api.Controllers.Incidents;

[ApiController]
[Authorize]
[Route("api/v1/incidents")]
public class IncidentsController(IIncidentsOrchestrator incidentsOrchestrator) : ControllerBase
{
    [HttpGet]
    [TranslateResultToActionResult]
    public async Task<Result<List<IncidentResponse>>> GetIncidents(
        [FromQuery] List<IncidentStatuses>? statuses,
        [FromQuery] List<Severity>? severities,
        [FromQuery] int? serviceId,
        [FromQuery] DateTimeOffset? triggeredAfterUtc,
        [FromQuery] DateTimeOffset? triggeredBeforeUtc)
    {
        var filter = new IncidentFilter(
            statuses,
            severities,
            serviceId,
            triggeredAfterUtc,
            triggeredBeforeUtc);

        var result = await incidentsOrchestrator.GetIncidents(filter, HttpContext.RequestAborted);
        return result.Map(incidents => incidents.Select(ToResponse).ToList());
    }

    [HttpGet("{incidentId:int}")]
    [TranslateResultToActionResult]
    public async Task<Result<IncidentResponse>> GetIncident([FromRoute] int incidentId)
    {
        if (incidentId <= 0)
            return Result<IncidentResponse>.Invalid(InvalidIncidentId());

        var result = await incidentsOrchestrator.GetIncidentById(incidentId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpGet("{incidentId:int}/timeline")]
    [TranslateResultToActionResult]
    public async Task<Result<List<IncidentEventResponse>>> GetTimeline([FromRoute] int incidentId)
    {
        if (incidentId <= 0)
            return Result<List<IncidentEventResponse>>.Invalid(InvalidIncidentId());

        var result = await incidentsOrchestrator.GetTimeline(incidentId, HttpContext.RequestAborted);
        return result.Map(events => events.Select(ToResponse).ToList());
    }

    [HttpPost("{incidentId:int}/ack")]
    [TranslateResultToActionResult]
    public async Task<Result<IncidentResponse>> AckIncident([FromRoute] int incidentId)
    {
        if (incidentId <= 0)
            return Result<IncidentResponse>.Invalid(InvalidIncidentId());

        if (!TryGetCallerId(out var actorUserId))
            return Result<IncidentResponse>.Unauthorized();

        var result = await incidentsOrchestrator.AckIncident(incidentId, actorUserId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPost("{incidentId:int}/resolve")]
    [TranslateResultToActionResult]
    public async Task<Result<IncidentResponse>> ResolveIncident([FromRoute] int incidentId)
    {
        if (incidentId <= 0)
            return Result<IncidentResponse>.Invalid(InvalidIncidentId());

        if (!TryGetCallerId(out var actorUserId))
            return Result<IncidentResponse>.Unauthorized();

        var result = await incidentsOrchestrator.ResolveIncident(incidentId, actorUserId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPost("{incidentId:int}/note")]
    [TranslateResultToActionResult]
    public async Task<Result<IncidentEventResponse>> AddNote(
        [FromRoute] int incidentId,
        [FromBody] IncidentNoteRequest request)
    {
        if (incidentId <= 0)
            return Result<IncidentEventResponse>.Invalid(InvalidIncidentId());

        if (!TryGetCallerId(out var actorUserId))
            return Result<IncidentEventResponse>.Unauthorized();

        var result = await incidentsOrchestrator.AddNote(incidentId, actorUserId, request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    /// <summary>
    /// Ack, resolve, and note are all attributed actions — the timeline records who did
    /// them — so a token without a usable subject claim is rejected rather than writing
    /// an event with no actor.
    /// </summary>
    private bool TryGetCallerId(out Guid callerId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out callerId);

    private static List<ValidationError> InvalidIncidentId() =>
        [new ValidationError("incidentId", "Incident ID must be a positive integer.")];

    private static IncidentResponse ToResponse(Domain.Entities.Incident incident) =>
        new(incident.Id,
            incident.ServiceId,
            incident.Title,
            incident.Description,
            incident.Status,
            incident.Severity,
            incident.DedupKey,
            incident.CurrentStepOrder,
            incident.DateTriggered,
            incident.AckedAt,
            incident.AckedByUserId,
            incident.ResolvedAt);

    private static IncidentEventResponse ToResponse(Domain.Entities.IncidentEvent incidentEvent) =>
        new(incidentEvent.Id,
            incidentEvent.IncidentId,
            incidentEvent.EventType,
            incidentEvent.ActorUserId,
            incidentEvent.Note,
            incidentEvent.OccurredAtUtc);
}
