using Microsoft.AspNetCore.SignalR;

namespace Dazzled.Api.Hubs;

public class IncidentHub : Hub
{
    public Task JoinTeam(string teamId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"team-{teamId}");
}
