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
            var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == teamId, ct);
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
}
