using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.EscalationPolicies;

public class EscalationPoliciesOrchestrator(IEscalationPoliciesRepository policiesRepository) : IEscalationPoliciesOrchestrator
{
    public Task<Result<List<EscalationPolicy>>> GetPolicies(int? teamId, CancellationToken ct = default) =>
        policiesRepository.GetPoliciesAsync(teamId, ct);

    public Task<Result<EscalationPolicy>> GetPolicyById(int policyId, CancellationToken ct = default) =>
        policiesRepository.GetPolicyByIdAsync(policyId, ct);

    public Task<Result<EscalationPolicy>> CreatePolicy(EscalationPolicyRequest request, CancellationToken ct = default)
    {
        var policy = new EscalationPolicy
        {
            Name = request.Name,
            TeamId = request.TeamId
        };

        return policiesRepository.CreatePolicyAsync(policy, ct);
    }

    public Task<Result<EscalationPolicy>> UpdatePolicy(int policyId, EscalationPolicyRequest request, CancellationToken ct = default) =>
        policiesRepository.UpdatePolicyAsync(policyId, request, ct);

    public async Task<Result<List<EscalationStepDetail>>> GetSteps(int policyId, CancellationToken ct = default)
    {
        var steps = await policiesRepository.GetStepsAsync(policyId, ct);
        if (!steps.IsSuccess)
            return Demote<List<EscalationStepDetail>>(steps);

        var stepIds = steps.Value.Select(step => step.Id).ToList();

        // One batched lookup for the whole policy rather than a query per step.
        var targets = await policiesRepository.GetTargetsAsync(stepIds, ct);
        if (!targets.IsSuccess)
            return Demote<List<EscalationStepDetail>>(targets);

        var details = steps.Value
            .Select(step => new EscalationStepDetail(step, TargetsFor(targets.Value, step.Id)))
            .ToList();

        return Result.Success(details);
    }

    public async Task<Result<EscalationStepDetail>> AddStep(int policyId, EscalationStepRequest request, CancellationToken ct = default)
    {
        var step = await policiesRepository.AddStepAsync(policyId, request, ct);
        if (!step.IsSuccess)
            return Demote<EscalationStepDetail>(step);

        var detail = await LoadDetail(step.Value, ct);

        // Preserve Created so the controller still answers 201 rather than 200.
        return detail.IsSuccess ? Result.Created(detail.Value) : detail;
    }

    public async Task<Result<EscalationStepDetail>> UpdateStep(int policyId, int stepId, EscalationStepRequest request, CancellationToken ct = default)
    {
        var step = await policiesRepository.UpdateStepAsync(policyId, stepId, request, ct);
        if (!step.IsSuccess)
            return Demote<EscalationStepDetail>(step);

        return await LoadDetail(step.Value, ct);
    }

    /// <summary>
    /// Reads the persisted targets back rather than echoing the request, so the caller
    /// gets real target ids instead of ones it would have to guess at.
    /// </summary>
    private async Task<Result<EscalationStepDetail>> LoadDetail(EscalationStep step, CancellationToken ct)
    {
        var targets = await policiesRepository.GetTargetsAsync([step.Id], ct);

        return targets.IsSuccess
            ? Result.Success(new EscalationStepDetail(step, TargetsFor(targets.Value, step.Id)))
            : Demote<EscalationStepDetail>(targets);
    }

    private static List<EscalationTarget> TargetsFor(Dictionary<int, List<EscalationTarget>> targets, int stepId) =>
        targets.TryGetValue(stepId, out var stepTargets) ? stepTargets : [];

    /// <summary>Carries a failed <see cref="Result{T}"/> over to a different value type.</summary>
    private static Result<TTarget> Demote<TTarget>(IResult failed) => failed.Status switch
    {
        ResultStatus.NotFound => Result<TTarget>.NotFound([.. failed.Errors]),
        ResultStatus.Unauthorized => Result<TTarget>.Unauthorized(),
        ResultStatus.Forbidden => Result<TTarget>.Forbidden(),
        ResultStatus.Conflict => Result<TTarget>.Conflict([.. failed.Errors]),
        ResultStatus.Invalid => Result<TTarget>.Invalid([.. failed.ValidationErrors]),
        _ => Result<TTarget>.Error(new ErrorList([.. failed.Errors], null))
    };
}
