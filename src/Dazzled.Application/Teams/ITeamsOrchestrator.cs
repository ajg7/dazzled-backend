using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Teams;

public interface ITeamsOrchestrator
{
    Task<Result<List<Team>>> GetActiveTeams(CancellationToken ct = default);
    Task<Result<Team>> CreateTeam(string name, CancellationToken ct = default);
    Task<Result<List<TeamMember>>> GetTeamMembers(int teamId, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.Created"/> on success.</summary>
    Task<Result<TeamMember>> AddTeamMember(int teamId, Guid userId, CancellationToken ct = default);
}
