using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Abstractions;

public interface ITeamsRepository
{
    Task<Result<List<Team>>> GetTeamsAsync(CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no team has the given id.</summary>
    Task<Result<Team>> GetTeamByIdAsync(int teamId, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.Conflict"/> when a team with the same name exists.</summary>
    Task<Result<Team>> CreateTeamAsync(string name, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no team has the given id.</summary>
    Task<Result<List<TeamMember>>> GetTeamMembersAsync(int teamId, CancellationToken ct = default);

    /// <summary>
    /// Adds a user to a team. Returns <see cref="ResultStatus.Created"/> on success,
    /// <see cref="ResultStatus.NotFound"/> when the team does not exist,
    /// <see cref="ResultStatus.Invalid"/> when the user does not exist, and
    /// <see cref="ResultStatus.Conflict"/> when the user is already on the team.
    /// </summary>
    Task<Result<TeamMember>> AddTeamMemberAsync(int teamId, Guid userId, CancellationToken ct = default);
}
