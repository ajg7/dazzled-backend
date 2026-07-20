using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Application.Schedules;
using Dazzled.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class SchedulesRepository(DazzledDbContext db) : ISchedulesRepository
{
    public async Task<Result<List<OnCallSchedule>>> GetSchedulesAsync(int? teamId, CancellationToken ct = default)
    {
        try
        {
            var schedules = await db.OnCallSchedules
                .Where(schedule => !teamId.HasValue || schedule.TeamId == teamId.Value)
                .OrderBy(schedule => schedule.Name)
                .ToListAsync(ct);

            return Result.Success(schedules);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<OnCallSchedule>> GetScheduleByIdAsync(int scheduleId, CancellationToken ct = default)
    {
        try
        {
            var schedule = await db.OnCallSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

            return schedule is null
                ? Result.NotFound($"No on-call schedule with id {scheduleId} exists.")
                : Result.Success(schedule);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<OnCallSchedule>> CreateScheduleAsync(OnCallSchedule schedule, CancellationToken ct = default)
    {
        try
        {
            if (!await db.Teams.AnyAsync(team => team.Id == schedule.TeamId, ct))
                return Result.Invalid(new ValidationError
                {
                    Identifier = nameof(ScheduleRequest.TeamId),
                    ErrorMessage = $"No team with id {schedule.TeamId} exists."
                });

            if (await db.OnCallSchedules.AnyAsync(
                    existing => existing.TeamId == schedule.TeamId && existing.Name == schedule.Name, ct))
                return Result.Conflict($"A schedule named '{schedule.Name}' already exists on this team.");

            db.OnCallSchedules.Add(schedule);
            await db.SaveChangesAsync(ct);

            return Result.Created(schedule);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<ScheduleMember>>> GetMembersAsync(int scheduleId, CancellationToken ct = default)
    {
        try
        {
            if (!await db.OnCallSchedules.AnyAsync(schedule => schedule.Id == scheduleId, ct))
                return Result.NotFound($"No on-call schedule with id {scheduleId} exists.");

            var members = await db.ScheduleMembers
                .Where(member => member.ScheduleId == scheduleId)
                .OrderBy(member => member.RotationOrder)
                .ToListAsync(ct);

            return Result.Success(members);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<ScheduleMember>>> SetMembersAsync(int scheduleId, ScheduleMembersRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!await db.OnCallSchedules.AnyAsync(schedule => schedule.Id == scheduleId, ct))
                return Result.NotFound($"No on-call schedule with id {scheduleId} exists.");

            var validation = await ValidateMembersAsync(request.Members, ct);
            if (!validation.IsSuccess)
                return validation;

            var existing = await db.ScheduleMembers
                .Where(member => member.ScheduleId == scheduleId)
                .ToListAsync(ct);

            var replacements = request.Members
                .Select(member => new ScheduleMember
                {
                    ScheduleId = scheduleId,
                    UserId = member.UserId,
                    RotationOrder = member.RotationOrder
                })
                .ToList();

            // The rotation is replaced wholesale. A partial write would leave the
            // round-robin indexing over a half-built list and page the wrong person.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            db.ScheduleMembers.RemoveRange(existing);
            db.ScheduleMembers.AddRange(replacements);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success(replacements.OrderBy(member => member.RotationOrder).ToList());
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<ScheduleOverride>>> GetOverridesAsync(int scheduleId, CancellationToken ct = default)
    {
        try
        {
            if (!await db.OnCallSchedules.AnyAsync(schedule => schedule.Id == scheduleId, ct))
                return Result.NotFound($"No on-call schedule with id {scheduleId} exists.");

            var overrides = await db.ScheduleOverrides
                .Where(scheduleOverride => scheduleOverride.ScheduleId == scheduleId)
                .OrderBy(scheduleOverride => scheduleOverride.StartsAtUtc)
                .ToListAsync(ct);

            return Result.Success(overrides);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<ScheduleOverride>> AddOverrideAsync(int scheduleId, ScheduleOverrideRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!await db.OnCallSchedules.AnyAsync(schedule => schedule.Id == scheduleId, ct))
                return Result.NotFound($"No on-call schedule with id {scheduleId} exists.");

            if (request.EndsAtUtc <= request.StartsAtUtc)
                return Result.Invalid(new ValidationError
                {
                    Identifier = nameof(ScheduleOverrideRequest.EndsAtUtc),
                    ErrorMessage = "The override must end after it starts."
                });

            if (!await db.Users.AnyAsync(user => user.Id == request.UserId, ct))
                return Result.Invalid(new ValidationError
                {
                    Identifier = nameof(ScheduleOverrideRequest.UserId),
                    ErrorMessage = $"No user with id {request.UserId} exists."
                });

            // Two overrides covering the same instant make "who is on call right now"
            // ambiguous, and the resolver would silently pick one.
            var overlaps = await db.ScheduleOverrides.AnyAsync(existing =>
                existing.ScheduleId == scheduleId &&
                existing.StartsAtUtc < request.EndsAtUtc &&
                request.StartsAtUtc < existing.EndsAtUtc, ct);

            if (overlaps)
                return Result.Conflict("An override already covers part of that window.");

            var scheduleOverride = new ScheduleOverride
            {
                ScheduleId = scheduleId,
                UserId = request.UserId,
                StartsAtUtc = request.StartsAtUtc,
                EndsAtUtc = request.EndsAtUtc
            };

            db.ScheduleOverrides.Add(scheduleOverride);
            await db.SaveChangesAsync(ct);

            return Result.Created(scheduleOverride);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result> DeleteOverrideAsync(int scheduleId, int overrideId, CancellationToken ct = default)
    {
        try
        {
            var scheduleOverride = await db.ScheduleOverrides
                .FirstOrDefaultAsync(existing => existing.Id == overrideId && existing.ScheduleId == scheduleId, ct);

            if (scheduleOverride is null)
                return Result.NotFound($"No override with id {overrideId} exists on schedule {scheduleId}.");

            db.ScheduleOverrides.Remove(scheduleOverride);
            await db.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<ScheduleOverride>> GetActiveOverrideAsync(int scheduleId, DateTimeOffset atUtc, CancellationToken ct = default)
    {
        try
        {
            var scheduleOverride = await db.ScheduleOverrides
                .Where(existing =>
                    existing.ScheduleId == scheduleId &&
                    existing.StartsAtUtc <= atUtc &&
                    existing.EndsAtUtc > atUtc)
                .OrderBy(existing => existing.StartsAtUtc)
                .FirstOrDefaultAsync(ct);

            return scheduleOverride is null
                ? Result.NotFound($"No override is active on schedule {scheduleId} at {atUtc:O}.")
                : Result.Success(scheduleOverride);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    private async Task<Result> ValidateMembersAsync(List<ScheduleMemberRequest> members, CancellationToken ct)
    {
        var duplicateUser = members
            .GroupBy(member => member.UserId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateUser is not null)
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(ScheduleMembersRequest.Members),
                ErrorMessage = $"User {duplicateUser.Key} appears more than once in the rotation."
            });

        var duplicateOrder = members
            .GroupBy(member => member.RotationOrder)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateOrder is not null)
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(ScheduleMembersRequest.Members),
                ErrorMessage = $"Rotation order {duplicateOrder.Key} is used more than once."
            });

        var userIds = members.Select(member => member.UserId).ToList();

        var existingUserIds = await db.Users
            .Where(user => userIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync(ct);

        var missing = userIds.Except(existingUserIds).ToList();

        if (missing.Count > 0)
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(ScheduleMembersRequest.Members),
                ErrorMessage = $"No user exists with id(s): {string.Join(", ", missing)}."
            });

        return Result.Success();
    }
}
