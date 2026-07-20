using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Domain.Entities;

namespace Dazzled.Application.Services;

public class ServicesOrchestrator(IServicesRepository servicesRepository) : IServicesOrchestrator
{
    public async Task<Result<Service>> CreateService(ServiceRequest request, CancellationToken ct = default)
    {
        var service = new Service
        {
            Name = request.Name,
            TeamId = request.TeamId,
            EscalationPolicyId = request.EscalationPolicyId,
            IntegrationKey = Guid.NewGuid()
        };

        return await servicesRepository.CreateServiceAsync(service, ct);
    }

    public Task<Result<List<Service>>> GetServices(int? teamId, int? escalationPolicyId, CancellationToken ct = default) =>
        servicesRepository.GetServicesAsync(teamId, escalationPolicyId, ct);

    public Task<Result<Service>> GetServicesById(int serviceId, CancellationToken ct = default) =>
        servicesRepository.GetServicesByIdAsync(serviceId, ct);

    public Task<Result<Service>> UpdateService(int serviceId, ServiceRequest request, CancellationToken ct = default) => 
        servicesRepository.UpdateServiceAsync(serviceId, request, ct);
    public Task<Result<Guid>> GetIntegrationKey(int serviceId, CancellationToken ct = default) =>
        servicesRepository.GetIntegrationKeyAsync(serviceId, ct);
}
