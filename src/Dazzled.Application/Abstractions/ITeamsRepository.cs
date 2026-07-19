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
}
