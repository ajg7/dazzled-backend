using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.EscalationPolicies;
using Dazzled.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dazzled.Api.Controllers.EscalationPolicies;

[ApiController]
[Authorize]
[Route("api/v1/escalation-policies")]
public class EscalationPoliciesController(
    IEscalationPoliciesOrchestrator escalationPoliciesOrchestrator) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [TranslateResultToActionResult]
    public async Task<Result<List<EscalationPolicy>>> GetPolicies([FromQuery] int? teamId)
    {
        return await escalationPoliciesOrchestrator.GetPolicies(teamId, HttpContext.RequestAborted);
    }
}