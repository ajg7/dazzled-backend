using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Schedules;

public class SchedulesOrchestrator(ISchedulesRepository schedulesRepository, IUserRepository users) : ISchedulesOrchestrator
{
    /// <summary>
    /// Week 0 of every rotation. Fixed rather than derived from the schedule's creation
    /// date so that adding a schedule never shifts an existing one, and so the same
    /// instant always resolves to the same member.
    /// </summary>
    private static readonly DateTimeOffset RotationEpoch = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public Task<Result<List<OnCallSchedule>>> GetSchedules(int? teamId, CancellationToken ct = default) =>
        schedulesRepository.GetSchedulesAsync(teamId, ct);

    public Task<Result<OnCallSchedule>> GetScheduleById(int scheduleId, CancellationToken ct = default) =>
        schedulesRepository.GetScheduleByIdAsync(scheduleId, ct);

    public Task<Result<OnCallSchedule>> CreateSchedule(ScheduleRequest request, CancellationToken ct = default)
    {
        var schedule = new OnCallSchedule
        {
            Name = request.Name,
            TeamId = request.TeamId,
            RotationType = request.RotationType
        };

        return schedulesRepository.CreateScheduleAsync(schedule, ct);
    }

    public Task<Result<List<ScheduleMember>>> GetMembers(int scheduleId, CancellationToken ct = default) =>
        schedulesRepository.GetMembersAsync(scheduleId, ct);

    public Task<Result<List<ScheduleMember>>> SetMembers(int scheduleId, ScheduleMembersRequest request, CancellationToken ct = default) =>
        schedulesRepository.SetMembersAsync(scheduleId, request, ct);

    public Task<Result<List<ScheduleOverride>>> GetOverrides(int scheduleId, CancellationToken ct = default) =>
        schedulesRepository.GetOverridesAsync(scheduleId, ct);

    public Task<Result<ScheduleOverride>> AddOverride(int scheduleId, ScheduleOverrideRequest request, CancellationToken ct = default) =>
        schedulesRepository.AddOverrideAsync(scheduleId, request, ct);

    public Task<Result> DeleteOverride(int scheduleId, int overrideId, CancellationToken ct = default) =>
        schedulesRepository.DeleteOverrideAsync(scheduleId, overrideId, ct);

    /// <summary>
    /// §2.2 resolution: an active override wins outright, otherwise index into the
    /// ordered rotation by whole weeks elapsed since <see cref="RotationEpoch"/>.
    /// </summary>
    public async Task<Result<User>> GetOnCallNow(int scheduleId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Doubles as the schedule-exists check: NotFound here means no such schedule.
        var members = await schedulesRepository.GetMembersAsync(scheduleId, ct);
        if (!members.IsSuccess)
            return Demote<User>(members);

        var activeOverride = await schedulesRepository.GetActiveOverrideAsync(scheduleId, now, ct);

        if (activeOverride.IsSuccess)
            return await users.GetByIdAsync(activeOverride.Value.UserId, ct);

        if (activeOverride.Status != ResultStatus.NotFound)
            return Demote<User>(activeOverride);

        if (members.Value.Count == 0)
            return Result<User>.Invalid(new ValidationError
            {
                Identifier = nameof(scheduleId),
                ErrorMessage = $"Schedule {scheduleId} has no rotation members and no active override, so nobody is on call."
            });

        var weeksElapsed = (long)Math.Floor((now - RotationEpoch).TotalDays / 7);

        // Guards against a negative index for any instant before the epoch.
        var index = (int)(((weeksElapsed % members.Value.Count) + members.Value.Count) % members.Value.Count);

        return await users.GetByIdAsync(members.Value[index].UserId, ct);
    }

    /// <summary>Carries a failed <see cref="Result{T}"/> over to a different value type.</summary>
    private static Result<TTarget> Demote<TTarget>(IResult failed) => failed.Status switch
    {
        ResultStatus.NotFound => Result<TTarget>.NotFound([.. failed.Errors]),
        ResultStatus.Unauthorized => Result<TTarget>.Unauthorized(),
        ResultStatus.Forbidden => Result<TTarget>.Forbidden(),
        ResultStatus.Conflict => Result<TTarget>.Conflict([.. failed.Errors]),
        ResultStatus.Invalid => Result<TTarget>.Invalid([.. failed.ValidationErrors]),
        _ => Result<TTarget>.Error(new ErrorList([.. failed.Errors], null))
    };
}
