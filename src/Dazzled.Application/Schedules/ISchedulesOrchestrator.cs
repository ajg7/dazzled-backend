using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Schedules;

public interface ISchedulesOrchestrator
{
    Task<Result<List<OnCallSchedule>>> GetSchedules(int? teamId, CancellationToken ct = default);

    Task<Result<OnCallSchedule>> GetScheduleById(int scheduleId, CancellationToken ct = default);

    Task<Result<OnCallSchedule>> CreateSchedule(ScheduleRequest request, CancellationToken ct = default);

    Task<Result<List<ScheduleMember>>> GetMembers(int scheduleId, CancellationToken ct = default);

    Task<Result<List<ScheduleMember>>> SetMembers(int scheduleId, ScheduleMembersRequest request, CancellationToken ct = default);

    Task<Result<List<ScheduleOverride>>> GetOverrides(int scheduleId, CancellationToken ct = default);

    Task<Result<ScheduleOverride>> AddOverride(int scheduleId, ScheduleOverrideRequest request, CancellationToken ct = default);

    Task<Result> DeleteOverride(int scheduleId, int overrideId, CancellationToken ct = default);

    Task<Result<User>> GetOnCallNow(int scheduleId, CancellationToken ct = default);
}
