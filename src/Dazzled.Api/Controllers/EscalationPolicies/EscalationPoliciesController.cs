using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.EscalationPolicies;
using Dazzled.Domain.Enums;
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
    [TranslateResultToActionResult]
    public async Task<Result<List<EscalationPolicyResponse>>> GetPolicies([FromQuery] int? teamId)
    {
        var result = await escalationPoliciesOrchestrator.GetPolicies(teamId, HttpContext.RequestAborted);
        return result.Map(policies => policies.Select(ToResponse).ToList());
    }

    [HttpGet("{policyId:int}")]
    [TranslateResultToActionResult]
    public async Task<Result<EscalationPolicyResponse>> GetPolicy([FromRoute] int policyId)
    {
        if (policyId <= 0)
            return Result<EscalationPolicyResponse>.Invalid(InvalidId(nameof(policyId), "Policy"));

        var result = await escalationPoliciesOrchestrator.GetPolicyById(policyId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<EscalationPolicyResponse>> CreatePolicy([FromBody] EscalationPolicyRequest request)
    {
        var result = await escalationPoliciesOrchestrator.CreatePolicy(request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPut("{policyId:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<EscalationPolicyResponse>> UpdatePolicy(
        [FromRoute] int policyId,
        [FromBody] EscalationPolicyRequest request)
    {
        if (policyId <= 0)
            return Result<EscalationPolicyResponse>.Invalid(InvalidId(nameof(policyId), "Policy"));

        var result = await escalationPoliciesOrchestrator.UpdatePolicy(policyId, request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpGet("{policyId:int}/steps")]
    [TranslateResultToActionResult]
    public async Task<Result<List<EscalationStepResponse>>> GetSteps([FromRoute] int policyId)
    {
        if (policyId <= 0)
            return Result<List<EscalationStepResponse>>.Invalid(InvalidId(nameof(policyId), "Policy"));

        var result = await escalationPoliciesOrchestrator.GetSteps(policyId, HttpContext.RequestAborted);
        return result.Map(steps => steps.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Appends a step. The posted target list becomes the step's full target set.
    /// </summary>
    [HttpPost("{policyId:int}/steps")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<EscalationStepResponse>> AddStep(
        [FromRoute] int policyId,
        [FromBody] EscalationStepRequest request)
    {
        if (policyId <= 0)
            return Result<EscalationStepResponse>.Invalid(InvalidId(nameof(policyId), "Policy"));

        var result = await escalationPoliciesOrchestrator.AddStep(policyId, request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    /// <summary>
    /// Replaces a step and its targets wholesale — a target absent from the posted
    /// list is removed from the step.
    /// </summary>
    [HttpPut("{policyId:int}/steps/{stepId:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<EscalationStepResponse>> UpdateStep(
        [FromRoute] int policyId,
        [FromRoute] int stepId,
        [FromBody] EscalationStepRequest request)
    {
        if (policyId <= 0)
            return Result<EscalationStepResponse>.Invalid(InvalidId(nameof(policyId), "Policy"));

        if (stepId <= 0)
            return Result<EscalationStepResponse>.Invalid(InvalidId(nameof(stepId), "Step"));

        var result = await escalationPoliciesOrchestrator.UpdateStep(policyId, stepId, request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    private static List<ValidationError> InvalidId(string identifier, string label) =>
        [new ValidationError(identifier, $"{label} ID must be a positive integer.")];

    private static EscalationPolicyResponse ToResponse(Domain.Entities.EscalationPolicy policy) =>
        new(policy.Id, policy.Name, policy.TeamId);

    private static EscalationStepResponse ToResponse(EscalationStepDetail detail) =>
        new(detail.Step.Id,
            detail.Step.PolicyId,
            detail.Step.StepOrder,
            detail.Step.TimeoutMinutes,
            detail.Targets.Select(ToResponse).ToList());

    private static EscalationTargetResponse ToResponse(Domain.Entities.EscalationTarget target) =>
        new(target.Id, target.TargetType, target.TargetId);
}
