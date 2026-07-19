using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Teams;
using Microsoft.AspNetCore.Mvc;

namespace Dazzled.Api.Controllers.Teams;

[ApiController]
[Route("api/v1/teams")]
public class TeamsController(ITeamsOrchestrator teamsOrchestrator) : ControllerBase
{
    [HttpGet]
    [TranslateResultToActionResult]
    public async Task<Result<List<TeamResponse>>> GetActiveTeams()
    {
        var result = await teamsOrchestrator.GetActiveTeams(HttpContext.RequestAborted);
        return result.Map(teams => teams.Select(t => new TeamResponse(t.Id, t.Name)).ToList());
    }

    [HttpGet("{teamId:int}/members")]
    [TranslateResultToActionResult]
    public async Task<Result<TeamMembersResponse>> GetTeamsMembers([FromRoute] int teamId)
    {
        var result = await teamsOrchestrator.GetTeamMembers(teamId, HttpContext.RequestAborted);
        return result.Map(members => new TeamMembersResponse(members));
    }

    [HttpPost]
    [TranslateResultToActionResult]
    public async Task<Result<TeamResponse>> CreateTeam(TeamCreationRequest request)
    {
        var result = await teamsOrchestrator.CreateTeam(request.Name, HttpContext.RequestAborted);
        return result.Map(team => new TeamResponse(team.Id, team.Name));
    }
}
