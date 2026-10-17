using Microsoft.AspNetCore.SignalR;

namespace EBSSTask.API.Hubs;

public class ReportHub : Hub
{
    public const string HubUrl = "/hubs/reports";

    /// <summary>
    /// Optional: Client can join a specific group for a processId to listen to its progress.
    /// </summary>
    public async Task SubscribeToProcess(string processId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, processId);
    }

    public async Task UnsubscribeFromProcess(string processId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, processId);
    }
}
