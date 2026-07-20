using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Schedules;
using Dazzled.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dazzled.Api.Controllers.Schedules;

[ApiController]
[Authorize]
[Route("api/v1/schedules")]
public class SchedulesController(ISchedulesOrchestrator schedulesOrchestrator) : ControllerBase
{
    [HttpGet]
    [TranslateResultToActionResult]
    public async Task<Result<List<ScheduleResponse>>> GetSchedules([FromQuery] int? teamId)
    {
        var result = await schedulesOrchestrator.GetSchedules(teamId, HttpContext.RequestAborted);
        return result.Map(schedules => schedules.Select(ToResponse).ToList());
    }

    [HttpGet("{scheduleId:int}")]
    [TranslateResultToActionResult]
    public async Task<Result<ScheduleResponse>> GetSchedule([FromRoute] int scheduleId)
    {
        if (scheduleId <= 0)
            return Result<ScheduleResponse>.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        var result = await schedulesOrchestrator.GetScheduleById(scheduleId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<ScheduleResponse>> CreateSchedule([FromBody] ScheduleRequest request)
    {
        var result = await schedulesOrchestrator.CreateSchedule(request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpGet("{scheduleId:int}/members")]
    [TranslateResultToActionResult]
    public async Task<Result<List<ScheduleMemberResponse>>> GetMembers([FromRoute] int scheduleId)
    {
        if (scheduleId <= 0)
            return Result<List<ScheduleMemberResponse>>.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        var result = await schedulesOrchestrator.GetMembers(scheduleId, HttpContext.RequestAborted);
        return result.Map(members => members.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Replaces the rotation wholesale — the posted list becomes the entire ordered
    /// rotation, so omitting a member removes them.
    /// </summary>
    [HttpPut("{scheduleId:int}/members")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<List<ScheduleMemberResponse>>> SetMembers(
        [FromRoute] int scheduleId,
        [FromBody] ScheduleMembersRequest request)
    {
        if (scheduleId <= 0)
            return Result<List<ScheduleMemberResponse>>.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        var result = await schedulesOrchestrator.SetMembers(scheduleId, request, HttpContext.RequestAborted);
        return result.Map(members => members.Select(ToResponse).ToList());
    }

    [HttpGet("{scheduleId:int}/overrides")]
    [TranslateResultToActionResult]
    public async Task<Result<List<ScheduleOverrideResponse>>> GetOverrides([FromRoute] int scheduleId)
    {
        if (scheduleId <= 0)
            return Result<List<ScheduleOverrideResponse>>.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        var result = await schedulesOrchestrator.GetOverrides(scheduleId, HttpContext.RequestAborted);
        return result.Map(overrides => overrides.Select(ToResponse).ToList());
    }

    [HttpPost("{scheduleId:int}/overrides")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<ScheduleOverrideResponse>> AddOverride(
        [FromRoute] int scheduleId,
        [FromBody] ScheduleOverrideRequest request)
    {
        if (scheduleId <= 0)
            return Result<ScheduleOverrideResponse>.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        var result = await schedulesOrchestrator.AddOverride(scheduleId, request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpDelete("{scheduleId:int}/overrides/{overrideId:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result> DeleteOverride([FromRoute] int scheduleId, [FromRoute] int overrideId)
    {
        if (scheduleId <= 0)
            return Result.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        if (overrideId <= 0)
            return Result.Invalid(InvalidId(nameof(overrideId), "Override"));

        return await schedulesOrchestrator.DeleteOverride(scheduleId, overrideId, HttpContext.RequestAborted);
    }

    /// <summary>
    /// Resolves who is on call for this schedule right now: an active override wins,
    /// otherwise the weekly round-robin position. See §2.2.
    /// </summary>
    [HttpGet("{scheduleId:int}/oncall-now")]
    [TranslateResultToActionResult]
    public async Task<Result<OnCallNowResponse>> GetOnCallNow([FromRoute] int scheduleId)
    {
        if (scheduleId <= 0)
            return Result<OnCallNowResponse>.Invalid(InvalidId(nameof(scheduleId), "Schedule"));

        var result = await schedulesOrchestrator.GetOnCallNow(scheduleId, HttpContext.RequestAborted);
        return result.Map(user => new OnCallNowResponse(user.Id, user.Name, user.Email, user.PhoneE164));
    }

    private static List<ValidationError> InvalidId(string identifier, string label) =>
        [new ValidationError(identifier, $"{label} ID must be a positive integer.")];

    private static ScheduleResponse ToResponse(Domain.Entities.OnCallSchedule schedule) =>
        new(schedule.Id, schedule.Name, schedule.TeamId, schedule.RotationType);

    private static ScheduleMemberResponse ToResponse(Domain.Entities.ScheduleMember member) =>
        new(member.Id, member.UserId, member.RotationOrder);

    private static ScheduleOverrideResponse ToResponse(Domain.Entities.ScheduleOverride scheduleOverride) =>
        new(scheduleOverride.Id, scheduleOverride.UserId, scheduleOverride.StartsAtUtc, scheduleOverride.EndsAtUtc);
}
