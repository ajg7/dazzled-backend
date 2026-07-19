using Dazzled.Application.Auth;
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

        return services;
    }
}
