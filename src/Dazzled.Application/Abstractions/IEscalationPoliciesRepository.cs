using Ardalis.Result;
using Dazzled.Application.EscalationPolicies;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Abstractions;

public interface IEscalationPoliciesRepository
{
    /// <summary>
    /// Returns policies for the given team, or all policies when
    /// <paramref name="teamId"/> is null. A filter matching nothing yields an empty
    /// list rather than <see cref="ResultStatus.NotFound"/>.
    /// </summary>
    Task<Result<List<EscalationPolicy>>> GetPoliciesAsync(int? teamId, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no policy has the given id.</summary>
    Task<Result<EscalationPolicy>> GetPolicyByIdAsync(int policyId, CancellationToken ct = default);

    /// <summary>
    /// Persists a new policy. Returns <see cref="ResultStatus.Created"/> on success,
    /// <see cref="ResultStatus.Invalid"/> when the team does not exist, and
    /// <see cref="ResultStatus.Conflict"/> when the team already has a policy by that name.
    /// </summary>
    Task<Result<EscalationPolicy>> CreatePolicyAsync(EscalationPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Replaces every mutable field on the policy, enforcing the same guards as
    /// <see cref="CreatePolicyAsync"/> plus <see cref="ResultStatus.NotFound"/> when no
    /// policy has the given id.
    /// </summary>
    /// <remarks>
    /// Moving a policy to a different team orphans any service still pointing at it
    /// from the old team — the cross-team guard in
    /// <see cref="IServicesRepository.UpdateServiceAsync"/> rejects that pairing, so
    /// decide here whether to block the move or cascade it.
    /// </remarks>
    Task<Result<EscalationPolicy>> UpdatePolicyAsync(int policyId, EscalationPolicyRequest request, CancellationToken ct = default);

    /// <summary>Returns the policy's steps ordered by <see cref="EscalationStep.StepOrder"/>.</summary>
    Task<Result<List<EscalationStep>>> GetStepsAsync(int policyId, CancellationToken ct = default);

    /// <summary>
    /// Appends a step and its targets. Returns <see cref="ResultStatus.Created"/> on
    /// success, <see cref="ResultStatus.NotFound"/> when the policy does not exist,
    /// and <see cref="ResultStatus.Invalid"/> when the step order is already taken or
    /// a target references a missing user or schedule.
    /// </summary>
    Task<Result<EscalationStep>> AddStepAsync(int policyId, EscalationStepRequest request, CancellationToken ct = default);

    /// <summary>
    /// Replaces a step and its full target list. Returns
    /// <see cref="ResultStatus.NotFound"/> when the policy or step does not exist, and
    /// <see cref="ResultStatus.Invalid"/> under the same conditions as
    /// <see cref="AddStepAsync"/>.
    /// </summary>
    Task<Result<EscalationStep>> UpdateStepAsync(int policyId, int stepId, EscalationStepRequest request, CancellationToken ct = default);

    /// <summary>Returns the targets for each of the given steps, keyed by step id.</summary>
    Task<Result<Dictionary<int, List<EscalationTarget>>>> GetTargetsAsync(IReadOnlyCollection<int> stepIds, CancellationToken ct = default);
}
