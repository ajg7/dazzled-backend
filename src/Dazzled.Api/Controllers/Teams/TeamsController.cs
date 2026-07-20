using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Teams;
using Dazzled.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dazzled.Api.Controllers.Teams;

[ApiController]
[Authorize]
[Route("api/v1/teams")]
public class TeamsController(ITeamsOrchestrator teamsOrchestrator) : ControllerBase
{
    [HttpGet]
    [TranslateResultToActionResult]
    public async Task<Result<List<TeamResponse>>> GetActiveTeams()
    {
        var result = await teamsOrchestrator.GetActiveTeams(HttpContext.RequestAborted);
        return result.Map(teams => teams.Select(team => new TeamResponse(team.Id, team.Name)).ToList());
    }

    [HttpGet("{teamId:int}/members")]
    [TranslateResultToActionResult]
    public async Task<Result<TeamMembersResponse>> GetTeamsMembers([FromRoute] int teamId)
    {
        if (teamId <= 0)
            return Result<TeamMembersResponse>.Invalid(InvalidTeamId());

        var result = await teamsOrchestrator.GetTeamMembers(teamId, HttpContext.RequestAborted);
        return result.Map(members => new TeamMembersResponse(members.Select(ToResponse).ToList()));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<TeamResponse>> CreateTeam([FromBody] TeamCreationRequest request)
    {
        var result = await teamsOrchestrator.CreateTeam(request.Name, HttpContext.RequestAborted);
        return result.Map(team => new TeamResponse(team.Id, team.Name));
    }

    [HttpPost("{teamId:int}/members")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<TeamMemberResponse>> AddTeamMember(
        [FromRoute] int teamId,
        [FromBody] TeamMemberRequest request)
    {
        if (teamId <= 0)
            return Result<TeamMemberResponse>.Invalid(InvalidTeamId());

        var result = await teamsOrchestrator.AddTeamMember(teamId, request.UserId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    private static List<ValidationError> InvalidTeamId() =>
        [new ValidationError("teamId", "Team ID must be a positive integer.")];

    private static TeamMemberResponse ToResponse(Domain.Entities.TeamMember member) =>
        new(member.TeamId, member.UserId);
}
