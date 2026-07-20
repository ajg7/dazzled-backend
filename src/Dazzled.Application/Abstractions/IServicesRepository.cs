using Ardalis.Result;
using Dazzled.Application.Services;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Abstractions;

public interface IServicesRepository
{
    /// <summary>
    /// Persists a new service. Returns <see cref="ResultStatus.Created"/> on success,
    /// <see cref="ResultStatus.Invalid"/> when the team or escalation policy does not
    /// exist (or the policy belongs to a different team), and
    /// <see cref="ResultStatus.Conflict"/> when the team already has a service by that name.
    /// </summary>
    Task<Result<Service>> CreateServiceAsync(Service service, CancellationToken ct = default);

    /// <summary>
    /// Returns services matching whichever filters are supplied, ordered by name.
    /// Filters that match nothing yield an empty list rather than
    /// <see cref="ResultStatus.NotFound"/> — an unknown id is an empty result set,
    /// not a missing resource.
    /// </summary>
    Task<Result<List<Service>>> GetServicesAsync(int? teamId, int? escalationPolicyId, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no service has the given id.</summary>
    Task<Result<Service>> GetServicesByIdAsync(int serviceId, CancellationToken ct = default);

    /// <summary>
    /// Replaces every mutable field on the service — a null
    /// <see cref="ServiceRequest.EscalationPolicyId"/> detaches the policy. Enforces the
    /// same reference and name-uniqueness guards as
    /// <see cref="CreateServiceAsync"/>, and additionally returns
    /// <see cref="ResultStatus.NotFound"/> when no service has the given id.
    /// The integration key is never modified here.
    /// </summary>
    Task<Result<Service>> UpdateServiceAsync(int serviceId, ServiceRequest request, CancellationToken ct = default);

    /// <summary>Returns <see cref="ResultStatus.NotFound"/> when no service has the given id.</summary>
    Task<Result<Guid>> GetIntegrationKeyAsync(int serviceId, CancellationToken ct = default);
}
