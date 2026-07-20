using System.ComponentModel.DataAnnotations;

namespace Dazzled.Application.Services;

/// <param name="EscalationPolicyId">
/// Optional: a service may be registered before its policy exists. Until one is
/// attached, an incident on this service escalates to nobody.
/// </param>
public record ServiceRequest(
    [Required][MaxLength(200)] string Name,
    [Required] int TeamId,
    int? EscalationPolicyId);
