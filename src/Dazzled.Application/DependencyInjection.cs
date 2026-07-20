using Dazzled.Application.Auth;
using Dazzled.Application.EscalationPolicies;
using Dazzled.Application.Incidents;
using Dazzled.Application.Schedules;
using Dazzled.Application.Services;
using Dazzled.Application.Teams;
using Dazzled.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Dazzled.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
        services.AddScoped<ITeamsOrchestrator, TeamsOrchestrator>();
        services.AddScoped<IUsersOrchestrator, UsersOrchestrator>();
        services.AddScoped<IServicesOrchestrator, ServicesOrchestrator>();
        services.AddScoped<IEscalationPoliciesOrchestrator, EscalationPoliciesOrchestrator>();
        services.AddScoped<ISchedulesOrchestrator, SchedulesOrchestrator>();
        services.AddScoped<IIncidentsOrchestrator, IncidentsOrchestrator>();

        return services;
    }
}
