using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Application.EscalationPolicies;
using Dazzled.Domain.Entities;
using Dazzled.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class EscalationPoliciesRepository(DazzledDbContext db) : IEscalationPoliciesRepository
{
    public async Task<Result<List<EscalationPolicy>>> GetPoliciesAsync(int? teamId, CancellationToken ct = default)
    {
        try
        {
            var policies = await db.EscalationPolicies
                .Where(policy => !teamId.HasValue || policy.TeamId == teamId.Value)
                .OrderBy(policy => policy.Name)
                .ToListAsync(ct);

            return Result.Success(policies);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<EscalationPolicy>> GetPolicyByIdAsync(int policyId, CancellationToken ct = default)
    {
        try
        {
            var policy = await db.EscalationPolicies.FirstOrDefaultAsync(p => p.Id == policyId, ct);

            return policy is null
                ? Result.NotFound($"No escalation policy with id {policyId} exists.")
                : Result.Success(policy);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<EscalationPolicy>> CreatePolicyAsync(EscalationPolicy policy, CancellationToken ct = default)
    {
        try
        {
            var team = await ValidateTeamAsync(policy.TeamId, ct);
            if (!team.IsSuccess)
                return team;

            if (await NameIsTakenAsync(policy.TeamId, policy.Name, excludingPolicyId: null, ct))
                return Result.Conflict($"An escalation policy named '{policy.Name}' already exists on this team.");

            db.EscalationPolicies.Add(policy);
            await db.SaveChangesAsync(ct);

            return Result.Created(policy);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<EscalationPolicy>> UpdatePolicyAsync(int policyId, EscalationPolicyRequest request, CancellationToken ct = default)
    {
        try
        {
            var policy = await db.EscalationPolicies.FirstOrDefaultAsync(p => p.Id == policyId, ct);

            if (policy is null)
                return Result.NotFound($"No escalation policy with id {policyId} exists.");

            var team = await ValidateTeamAsync(request.TeamId, ct);
            if (!team.IsSuccess)
                return team;

            if (await NameIsTakenAsync(request.TeamId, request.Name, excludingPolicyId: policyId, ct))
                return Result.Conflict($"An escalation policy named '{request.Name}' already exists on this team.");

            // Services validate that their policy shares their team. Moving a policy
            // out from under an attached service would leave a pairing that no write
            // path would accept, so block the move instead of creating it.
            if (request.TeamId != policy.TeamId)
            {
                var attachedServices = await db.Services.CountAsync(service => service.EscalationPolicyId == policyId, ct);

                if (attachedServices > 0)
                    return Result.Invalid(new ValidationError
                    {
                        Identifier = nameof(EscalationPolicyRequest.TeamId),
                        ErrorMessage = $"{attachedServices} service(s) still reference this policy. Detach them before moving it to another team."
                    });
            }

            policy.Name = request.Name;
            policy.TeamId = request.TeamId;

            await db.SaveChangesAsync(ct);

            return Result.Success(policy);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<EscalationStep>>> GetStepsAsync(int policyId, CancellationToken ct = default)
    {
        try
        {
            if (!await db.EscalationPolicies.AnyAsync(policy => policy.Id == policyId, ct))
                return Result.NotFound($"No escalation policy with id {policyId} exists.");

            var steps = await db.EscalationSteps
                .Where(step => step.PolicyId == policyId)
                .OrderBy(step => step.StepOrder)
                .ToListAsync(ct);

            return Result.Success(steps);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<EscalationStep>> AddStepAsync(int policyId, EscalationStepRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!await db.EscalationPolicies.AnyAsync(policy => policy.Id == policyId, ct))
                return Result.NotFound($"No escalation policy with id {policyId} exists.");

            if (await StepOrderIsTakenAsync(policyId, request.StepOrder, excludingStepId: null, ct))
                return Result.Conflict($"Step {request.StepOrder} already exists on this policy.");

            var targets = await ValidateTargetsAsync(request.Targets, ct);
            if (!targets.IsSuccess)
                return targets;

            var step = new EscalationStep
            {
                PolicyId = policyId,
                StepOrder = request.StepOrder,
                TimeoutMinutes = request.TimeoutMinutes
            };

            // The step's identity is needed before its targets can reference it, so
            // this is two saves. A step persisted without targets is an escalation
            // tier that pages nobody, so both must land or neither.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            db.EscalationSteps.Add(step);
            await db.SaveChangesAsync(ct);

            db.EscalationTargets.AddRange(BuildTargets(step.Id, request.Targets));
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return Result.Created(step);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<EscalationStep>> UpdateStepAsync(int policyId, int stepId, EscalationStepRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!await db.EscalationPolicies.AnyAsync(policy => policy.Id == policyId, ct))
                return Result.NotFound($"No escalation policy with id {policyId} exists.");

            var step = await db.EscalationSteps
                .FirstOrDefaultAsync(candidate => candidate.Id == stepId && candidate.PolicyId == policyId, ct);

            if (step is null)
                return Result.NotFound($"No step with id {stepId} exists on policy {policyId}.");

            if (await StepOrderIsTakenAsync(policyId, request.StepOrder, excludingStepId: stepId, ct))
                return Result.Conflict($"Step {request.StepOrder} already exists on this policy.");

            var targets = await ValidateTargetsAsync(request.Targets, ct);
            if (!targets.IsSuccess)
                return targets;

            var existingTargets = await db.EscalationTargets
                .Where(target => target.StepId == stepId)
                .ToListAsync(ct);

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            step.StepOrder = request.StepOrder;
            step.TimeoutMinutes = request.TimeoutMinutes;

            // Targets are replaced wholesale — the builder submits the full list, so
            // anything absent from it was removed.
            db.EscalationTargets.RemoveRange(existingTargets);
            db.EscalationTargets.AddRange(BuildTargets(stepId, request.Targets));

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success(step);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Dictionary<int, List<EscalationTarget>>>> GetTargetsAsync(IReadOnlyCollection<int> stepIds, CancellationToken ct = default)
    {
        try
        {
            if (stepIds.Count == 0)
                return Result.Success(new Dictionary<int, List<EscalationTarget>>());

            var targets = await db.EscalationTargets
                .Where(target => stepIds.Contains(target.StepId))
                .ToListAsync(ct);

            var grouped = targets
                .GroupBy(target => target.StepId)
                .ToDictionary(group => group.Key, group => group.ToList());

            return Result.Success(grouped);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    private async Task<Result> ValidateTeamAsync(int teamId, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(team => team.Id == teamId, ct))
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(EscalationPolicyRequest.TeamId),
                ErrorMessage = $"No team with id {teamId} exists."
            });

        return Result.Success();
    }

    /// <summary>
    /// TargetId is a string because the two target types reference different tables —
    /// a user Guid or a schedule int. Nothing in the schema enforces that pairing, so
    /// a mistyped id would otherwise become a step that silently pages nobody.
    /// </summary>
    private async Task<Result> ValidateTargetsAsync(List<EscalationTargetRequest> targets, CancellationToken ct)
    {
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var identifier = $"{nameof(EscalationStepRequest.Targets)}[{index}].{nameof(EscalationTargetRequest.TargetId)}";

            switch (target.TargetType)
            {
                case TargetType.User:
                    if (!Guid.TryParse(target.TargetId, out var userId))
                        return Invalid(identifier, $"'{target.TargetId}' is not a valid user id.");

                    if (!await db.Users.AnyAsync(user => user.Id == userId, ct))
                        return Invalid(identifier, $"No user with id {userId} exists.");

                    break;

                case TargetType.Schedule:
                    if (!int.TryParse(target.TargetId, out var scheduleId))
                        return Invalid(identifier, $"'{target.TargetId}' is not a valid schedule id.");

                    if (!await db.OnCallSchedules.AnyAsync(schedule => schedule.Id == scheduleId, ct))
                        return Invalid(identifier, $"No on-call schedule with id {scheduleId} exists.");

                    break;

                default:
                    return Invalid(
                        $"{nameof(EscalationStepRequest.Targets)}[{index}].{nameof(EscalationTargetRequest.TargetType)}",
                        $"'{target.TargetType}' is not a supported target type.");
            }
        }

        return Result.Success();

        static Result Invalid(string identifier, string message) =>
            Result.Invalid(new ValidationError { Identifier = identifier, ErrorMessage = message });
    }

    private static IEnumerable<EscalationTarget> BuildTargets(int stepId, List<EscalationTargetRequest> targets) =>
        targets.Select(target => new EscalationTarget
        {
            StepId = stepId,
            TargetType = target.TargetType,
            TargetId = target.TargetId
        });

    private Task<bool> NameIsTakenAsync(int teamId, string name, int? excludingPolicyId, CancellationToken ct) =>
        db.EscalationPolicies.AnyAsync(existing =>
            existing.TeamId == teamId &&
            existing.Name == name &&
            (excludingPolicyId == null || existing.Id != excludingPolicyId), ct);

    private Task<bool> StepOrderIsTakenAsync(int policyId, int stepOrder, int? excludingStepId, CancellationToken ct) =>
        db.EscalationSteps.AnyAsync(existing =>
            existing.PolicyId == policyId &&
            existing.StepOrder == stepOrder &&
            (excludingStepId == null || existing.Id != excludingStepId), ct);
}
