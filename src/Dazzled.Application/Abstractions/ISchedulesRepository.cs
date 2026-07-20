using Ardalis.Result;
using Dazzled.Application.Schedules;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Abstractions;

public interface ISchedulesRepository
{
    Task<Result<List<OnCallSchedule>>> GetSchedulesAsync(int? teamId, CancellationToken ct = default);

    Task<Result<OnCallSchedule>> GetScheduleByIdAsync(int scheduleId, CancellationToken ct = default);

    Task<Result<OnCallSchedule>> CreateScheduleAsync(OnCallSchedule schedule, CancellationToken ct = default);

    Task<Result<List<ScheduleMember>>> GetMembersAsync(int scheduleId, CancellationToken ct = default);

    Task<Result<List<ScheduleMember>>> SetMembersAsync(int scheduleId, ScheduleMembersRequest request, CancellationToken ct = default);

    Task<Result<List<ScheduleOverride>>> GetOverridesAsync(int scheduleId, CancellationToken ct = default);

    Task<Result<ScheduleOverride>> AddOverrideAsync(int scheduleId, ScheduleOverrideRequest request, CancellationToken ct = default);

    Task<Result> DeleteOverrideAsync(int scheduleId, int overrideId, CancellationToken ct = default);

    Task<Result<ScheduleOverride>> GetActiveOverrideAsync(int scheduleId, DateTimeOffset atUtc, CancellationToken ct = default);
}
