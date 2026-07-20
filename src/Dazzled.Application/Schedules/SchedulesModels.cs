using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dazzled.Application.Schedules;

public record ScheduleRequest(
    [Required][MaxLength(200)] string Name,
    [Required] int TeamId,
    [Required] RotationType RotationType);

public record ScheduleMembersRequest(
    [Required][MinLength(1)] List<ScheduleMemberRequest> Members);

public record ScheduleMemberRequest(
    [Required] Guid UserId,
    [Required][Range(1, int.MaxValue)] int RotationOrder);

public record ScheduleOverrideRequest(
    [Required] Guid UserId,
    [Required] DateTimeOffset StartsAtUtc,
    [Required] DateTimeOffset EndsAtUtc);
