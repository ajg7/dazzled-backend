using Dazzled.Domain.Entities;
using Dazzled.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dazzled.Application.EscalationPolicies;

/// <summary>
/// A step together with the targets it pages. A step alone is not actionable — the
/// §9.1 builder and the Phase 4 escalation engine both need to know who it reaches —
/// so reads return the pair rather than making every caller stitch them back together.
/// </summary>
public record EscalationStepDetail(EscalationStep Step, List<EscalationTarget> Targets);

public record EscalationPolicyRequest(
    [Required][MaxLength(200)] string Name,
    [Required] int TeamId);

/// <param name="StepOrder">1-based position in the chain. Must be unique within the policy.</param>
/// <param name="TimeoutMinutes">How long to wait for an ack before escalating to the next step.</param>
/// <param name="Targets">
/// Who this step pages. Replaces the step's existing targets wholesale — the §9.1
/// builder submits the full list, so a target absent from it is removed.
/// </param>
public record EscalationStepRequest(
    [Required][Range(1, int.MaxValue)] int StepOrder,
    [Required][Range(1, 1440)] int TimeoutMinutes,
    [Required][MinLength(1)] List<EscalationTargetRequest> Targets);

/// <param name="TargetId">
/// A user id or schedule id depending on <paramref name="TargetType"/>. Stored as a
/// string because the two reference different tables.
/// </param>
public record EscalationTargetRequest(
    [Required] TargetType TargetType,
    [Required][MaxLength(64)] string TargetId);
