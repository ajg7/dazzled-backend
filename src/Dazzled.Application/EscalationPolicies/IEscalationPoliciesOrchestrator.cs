using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.EscalationPolicies;

public interface IEscalationPoliciesOrchestrator
{
    /// <summary>Returns policies for the given team, or all policies when null.</summary>
    Task<Result<List<EscalationPolicy>>> GetPolicies(int? teamId, CancellationToken ct = default);

    Task<Result<EscalationPolicy>> GetPolicyById(int policyId, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.Created"/> on success.</summary>
    Task<Result<EscalationPolicy>> CreatePolicy(EscalationPolicyRequest request, CancellationToken ct = default);

    Task<Result<EscalationPolicy>> UpdatePolicy(int policyId, EscalationPolicyRequest request, CancellationToken ct = default);

    Task<Result<List<EscalationStep>>> GetSteps(int policyId, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.Created"/> on success.</summary>
    Task<Result<EscalationStep>> AddStep(int policyId, EscalationStepRequest request, CancellationToken ct = default);

    Task<Result<EscalationStep>> UpdateStep(int policyId, int stepId, EscalationStepRequest request, CancellationToken ct = default);
}
