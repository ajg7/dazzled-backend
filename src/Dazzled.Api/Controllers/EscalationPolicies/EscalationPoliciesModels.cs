using Dazzled.Domain.Enums;

namespace Dazzled.Api.Controllers.EscalationPolicies;

public record EscalationPolicyResponse(
    int Id,
    string Name,
    int TeamId);

/// <param name="Targets">
/// Who this step pages. Always present — a step that reaches nobody is a broken
/// escalation tier, so the §9.1 builder needs this to render the step at all.
/// </param>
public record EscalationStepResponse(
    int Id,
    int PolicyId,
    int StepOrder,
    int TimeoutMinutes,
    List<EscalationTargetResponse> Targets);

/// <param name="TargetId">
/// A user id or schedule id depending on <paramref name="TargetType"/>, as a string
/// because the two reference different tables.
/// </param>
public record EscalationTargetResponse(
    int Id,
    TargetType TargetType,
    string TargetId);
