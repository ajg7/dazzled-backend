namespace Dazzled.Api.Controllers.Services;

/// <remarks>
/// Deliberately excludes the integration key. That value is a write credential for
/// the alert pipeline — anyone holding it can inject alerts and page the on-call —
/// so it is exposed only by <c>GET /api/v1/services/{id}/integration-key</c>, which
/// can be authorized and audited on its own.
/// </remarks>
public record ServiceResponse(
    int Id,
    string Name,
    int TeamId,
    int? EscalationPolicyId);

/// <param name="WebhookUrl">The full URL to paste into Grafana or another alert source.</param>
public record IntegrationKeyResponse(Guid IntegrationKey, string WebhookUrl);
