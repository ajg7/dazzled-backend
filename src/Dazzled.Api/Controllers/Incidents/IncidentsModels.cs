using Dazzled.Domain.Enums;

namespace Dazzled.Api.Controllers.Incidents;

/// <remarks>
/// Omits <c>HangfireJobId</c>. That is the escalation engine's internal handle for the
/// pending timeout job — exposing it invites a client to reason about (or cancel) the
/// escalation chain out of band, which is what ack/resolve are for.
/// </remarks>
public record IncidentResponse(
    int Id,
    int ServiceId,
    string Title,
    string? Description,
    IncidentStatuses Status,
    Severity Severity,
    string DedupKey,
    int CurrentStepOrder,
    DateTimeOffset DateTriggered,
    DateTimeOffset? AckedAt,
    Guid? AckedByUserId,
    DateTimeOffset? ResolvedAt);

public record IncidentEventResponse(
    int Id,
    int IncidentId,
    string EventType,
    Guid? ActorUserId,
    string? Note,
    DateTimeOffset OccurredAtUtc);
