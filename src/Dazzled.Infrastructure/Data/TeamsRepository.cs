using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class TeamsRepository(DazzledDbContext db) : ITeamsRepository
{
    public async Task<Result<List<Team>>> GetTeamsAsync(CancellationToken ct = default)
    {
        try
        {
            return Result.Success(await db.Teams.OrderBy(t => t.Name).ToListAsync(ct));
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Team>> GetTeamByIdAsync(int teamId, CancellationToken ct = default)
    {
        try
        {
            var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
            return team is null ? Result.NotFound() : Result.Success(team);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Team>> CreateTeamAsync(string name, CancellationToken ct = default)
    {
        try
        {
            if (await db.Teams.AnyAsync(t => t.Name == name, ct))
                return Result.Conflict($"A team named '{name}' already exists.");

            var team = new Team { Name = name };
            db.Teams.Add(team);
            await db.SaveChangesAsync(ct);
            return Result.Success(team);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<TeamMember>>> GetTeamMembersAsync(int teamId, CancellationToken ct = default)
    {
        try
        {
            if (!await db.Teams.AnyAsync(t => t.Id == teamId, ct))
                return Result.NotFound();

            var members = await db.TeamMembers
                .Where(tm => tm.TeamId == teamId)
                .ToListAsync(ct);

            return Result.Success(members);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<TeamMember>> AddTeamMemberAsync(int teamId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            if (!await db.Teams.AnyAsync(t => t.Id == teamId, ct))
                return Result.NotFound($"No team with id {teamId} exists.");

            if (!await db.Users.AnyAsync(user => user.Id == userId, ct))
                return Result.Invalid(new ValidationError
                {
                    Identifier = nameof(userId),
                    ErrorMessage = $"No user with id {userId} exists."
                });

            if (await db.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId, ct))
                return Result.Conflict($"User {userId} is already a member of team {teamId}.");

            var member = new TeamMember { TeamId = teamId, UserId = userId };
            db.TeamMembers.Add(member);
            await db.SaveChangesAsync(ct);

            return Result.Created(member);
        }
        catch (DbUpdateException)
        {
            // (TeamId, UserId) is the composite primary key, so a concurrent add that
            // slipped past the check above lands here. Same outcome either way — the
            // user is on the team — so report it as the conflict it is, not a 500.
            return Result.Conflict($"User {userId} is already a member of team {teamId}.");
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }
}
