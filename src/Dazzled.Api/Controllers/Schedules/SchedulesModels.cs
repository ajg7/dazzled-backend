using Dazzled.Domain.Enums;

namespace Dazzled.Api.Controllers.Schedules;

public record ScheduleResponse(
    int Id,
    string Name,
    int TeamId,
    RotationType RotationType);

public record ScheduleMemberResponse(
    int Id,
    Guid UserId,
    int RotationOrder);

public record ScheduleOverrideResponse(
    int Id,
    Guid UserId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

/// <remarks>
/// Carries the contact fields deliberately: whoever calls <c>oncall-now</c> is usually
/// about to page this person, and making them round-trip to <c>GET /api/v1/users</c>
/// for a phone number adds a hop to the critical path.
/// </remarks>
public record OnCallNowResponse(
    Guid UserId,
    string Name,
    string Email,
    string? PhoneE164);
