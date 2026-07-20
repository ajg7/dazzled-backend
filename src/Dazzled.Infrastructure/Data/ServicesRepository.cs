using Ardalis.Result;
using Dazzled.Application.Abstractions;
using Dazzled.Application.Services;
using Dazzled.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dazzled.Infrastructure.Data;

public class ServicesRepository(DazzledDbContext db) : IServicesRepository
{
    public async Task<Result<Service>> CreateServiceAsync(Service service, CancellationToken ct = default)
    {
        try
        {
            var references = await ValidateReferencesAsync(service.TeamId, service.EscalationPolicyId, ct);
            if (!references.IsSuccess)
                return references;

            var nameInUse = await NameIsTakenAsync(service.TeamId, service.Name, excludingServiceId: null, ct);
            if (nameInUse)
                return Result.Conflict($"A service named '{service.Name}' already exists on this team.");

            db.Services.Add(service);
            await db.SaveChangesAsync(ct);

            return Result.Created(service);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<List<Service>>> GetServicesAsync(int? teamId, int? escalationPolicyId, CancellationToken ct = default)
    {
        try
        {
            var query = db.Services.AsQueryable();

            if (teamId.HasValue)
                query = query.Where(service => service.TeamId == teamId.Value);

            if (escalationPolicyId.HasValue)
                query = query.Where(service => service.EscalationPolicyId == escalationPolicyId.Value);

            var services = await query.OrderBy(service => service.Name).ToListAsync(ct);

            return Result.Success(services);
        }
        catch (Exception ex)
        {
            return Result.Error(ex.Message);
        }
    }

    public async Task<Result<Service>> GetServicesByIdAsync(int serviceId, CancellationToken ct = default)
    {
        try
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct);
            return service is null ? Result.NotFound($"No service with id {serviceId} exists.") : Result.Success(service);
        }
        catch (Exception ex)
        {
            return Result.Error($"Error retrieving service with id {serviceId}: {ex.Message}");
        }
    }

    public async Task<Result<Service>> UpdateServiceAsync(int serviceId, ServiceRequest request, CancellationToken ct = default)
    {
        try
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct);

            if (service is null)
                return Result.NotFound($"No service with id {serviceId} exists.");

            var references = await ValidateReferencesAsync(request.TeamId, request.EscalationPolicyId, ct);
            if (!references.IsSuccess)
                return references;

            var nameInUse = await NameIsTakenAsync(request.TeamId, request.Name, excludingServiceId: serviceId, ct);
            if (nameInUse)
                return Result.Conflict($"A service named '{request.Name}' already exists on this team.");

            // PUT replaces the resource, so every field is assigned unconditionally.
            // A null EscalationPolicyId detaches the policy rather than being ignored.
            service.Name = request.Name;
            service.TeamId = request.TeamId;
            service.EscalationPolicyId = request.EscalationPolicyId;

            // SaveChanges returns 0 when the submitted values already match what is
            // stored. That is a no-op update, not a failure.
            await db.SaveChangesAsync(ct);

            return Result.Success(service);
        }
        catch (Exception ex)
        {
            return Result.Error($"Error updating service with id {serviceId}: {ex.Message}");
        }
    }

    public async Task<Result<Guid>> GetIntegrationKeyAsync(int serviceId, CancellationToken ct = default)
    {
        try
        {
            var service = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct);
            return service is null
                ? Result.NotFound($"No service with id {serviceId} exists.")
                : Result.Success(service.IntegrationKey);
        }
        catch (Exception ex)
        {
            return Result.Error($"Error retrieving integration key for service with id {serviceId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Shared by create and update so a PUT cannot bypass a guard a POST enforces.
    /// Every config-graph FK is DeleteBehavior.Restrict with no navigation properties,
    /// so an unknown id would otherwise only surface as a DbUpdateException on save —
    /// a 500 for what is plainly bad input.
    /// </summary>
    private async Task<Result> ValidateReferencesAsync(int teamId, int? escalationPolicyId, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(team => team.Id == teamId, ct))
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(ServiceRequest.TeamId),
                ErrorMessage = $"No team with id {teamId} exists."
            });

        if (escalationPolicyId is { } policyId)
        {
            var policy = await db.EscalationPolicies
                .FirstOrDefaultAsync(candidate => candidate.Id == policyId, ct);

            if (policy is null)
                return Result.Invalid(new ValidationError
                {
                    Identifier = nameof(ServiceRequest.EscalationPolicyId),
                    ErrorMessage = $"No escalation policy with id {policyId} exists."
                });

            // A service routed to another team's policy pages the wrong people.
            if (policy.TeamId != teamId)
                return Result.Invalid(new ValidationError
                {
                    Identifier = nameof(ServiceRequest.EscalationPolicyId),
                    ErrorMessage = "The escalation policy belongs to a different team."
                });
        }

        return Result.Success();
    }

    /// <param name="excludingServiceId">The service being updated, so it does not collide with itself.</param>
    private Task<bool> NameIsTakenAsync(int teamId, string name, int? excludingServiceId, CancellationToken ct) =>
        db.Services.AnyAsync(existing =>
            existing.TeamId == teamId &&
            existing.Name == name &&
            (excludingServiceId == null || existing.Id != excludingServiceId), ct);
}
