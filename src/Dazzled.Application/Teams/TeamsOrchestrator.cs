using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Teams;

public class TeamsOrchestrator(ITeamsRepository teamsRepository) : ITeamsOrchestrator
{
    public Task<Result<List<Team>>> GetActiveTeams(CancellationToken ct = default) =>
        teamsRepository.GetTeamsAsync(ct);

    public Task<Result<Team>> CreateTeam(string name, CancellationToken ct = default) =>
        teamsRepository.CreateTeamAsync(name, ct);

    public Task<Result<List<TeamMember>>> GetTeamMembers(int teamId, CancellationToken ct = default) =>
        teamsRepository.GetTeamMembersAsync(teamId, ct);
}
