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

    public Task<Result<List<EscalationStep>>> GetSteps(int policyId, CancellationToken ct = default) =>
        policiesRepository.GetStepsAsync(policyId, ct);

    public Task<Result<EscalationStep>> AddStep(int policyId, EscalationStepRequest request, CancellationToken ct = default) =>
        policiesRepository.AddStepAsync(policyId, request, ct);

    public Task<Result<EscalationStep>> UpdateStep(int policyId, int stepId, EscalationStepRequest request, CancellationToken ct = default) =>
        policiesRepository.UpdateStepAsync(policyId, stepId, request, ct);
}
