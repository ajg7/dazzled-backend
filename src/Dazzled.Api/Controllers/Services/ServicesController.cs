using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Dazzled.Application.Configuration;
using Dazzled.Application.Services;
using Dazzled.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dazzled.Api.Controllers.Services;

[ApiController]
[Authorize]
[Route("api/v1/services")]
public class ServicesController(
    IServicesOrchestrator servicesOrchestrator,
    IOptions<PublicUrlOptions> publicUrlOptions) : ControllerBase
{
    /// <summary>
    /// Registers a new alertable thing (services) and issues its integration key.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<ServiceResponse>> CreateService([FromBody] ServiceRequest request)
    {
        var result = await servicesOrchestrator.CreateService(request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<List<ServiceResponse>>> GetServices([FromQuery] int? teamId, [FromQuery] int? escalationPolicyId)
    {
        var result = await servicesOrchestrator.GetServices(teamId, escalationPolicyId, HttpContext.RequestAborted);
        return result.Map(services => services.Select(ToResponse).ToList());
    }

    [HttpGet("{serviceId:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<ServiceResponse>> GetService([FromRoute] int serviceId)
    {
        if (serviceId <= 0)
        {
            return Result<ServiceResponse>.Invalid(new List<ValidationError>
            {
                new ValidationError(nameof(serviceId), "Service ID must be a positive integer.")
            });
        }
        var result = await servicesOrchestrator.GetServicesById(serviceId, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpPut("{serviceId:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<ServiceResponse>> UpdateService([FromRoute] int serviceId, [FromBody] ServiceRequest request)
    {
        if (serviceId <= 0)
        {
            return Result<ServiceResponse>.Invalid(new List<ValidationError>
            {
                new ValidationError(nameof(serviceId), "Service ID must be a positive integer.")
            });
        }
        
        var result = await servicesOrchestrator.UpdateService(serviceId, request, HttpContext.RequestAborted);
        return result.Map(ToResponse);
    }

    [HttpGet("{serviceId:int}/integration-key")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [TranslateResultToActionResult]
    public async Task<Result<IntegrationKeyResponse>> GetIntegrationKey([FromRoute] int serviceId)
    {
        if (serviceId <= 0)
        {
            return Result<IntegrationKeyResponse>.Invalid(new List<ValidationError>
            {
                new ValidationError(nameof(serviceId), "Service ID must be a positive integer.")
            });
        }

        var result = await servicesOrchestrator.GetIntegrationKey(serviceId, HttpContext.RequestAborted);
        return result.Map(key => new IntegrationKeyResponse(key, BuildWebhookUrl(key)));
    }

    /// <summary>
    /// Alert sources call this from outside the container network, so the configured
    /// public origin wins. It falls back to the current request's origin, which keeps
    /// the URL usable in local dev before PublicBaseUrl is set.
    /// </summary>
    private string BuildWebhookUrl(Guid integrationKey)
    {
        var configured = publicUrlOptions.Value.PublicBaseUrl;

        var origin = string.IsNullOrWhiteSpace(configured)
            ? $"{Request.Scheme}://{Request.Host}"
            : configured.TrimEnd('/');

        return $"{origin}/api/v1/ingest/{integrationKey}";
    }

    private static ServiceResponse ToResponse(Domain.Entities.Service service) =>
        new(service.Id, service.Name, service.TeamId, service.EscalationPolicyId);
}
