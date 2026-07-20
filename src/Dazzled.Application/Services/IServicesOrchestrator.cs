using Ardalis.Result;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Services;

public interface IServicesOrchestrator
{
    /// <summary>
    /// Registers a service and issues its integration key. Returns
    /// <see cref="ResultStatus.Created"/> on success.
    /// </summary>
    Task<Result<Service>> CreateService(ServiceRequest request, CancellationToken ct = default);

    /// <summary>Returns services matching whichever filters are supplied, ordered by name.</summary>
    Task<Result<List<Service>>> GetServices(int? teamId, int? escalationPolicyId, CancellationToken ct = default);

    Task<Result<Service>> GetServicesById(int serviceId, CancellationToken ct = default);

    /// <summary>Replaces every mutable field; the integration key is not one of them.</summary>
    Task<Result<Service>> UpdateService(int serviceId, ServiceRequest request, CancellationToken ct = default);

    Task<Result<Guid>> GetIntegrationKey(int serviceId, CancellationToken ct = default);
}
