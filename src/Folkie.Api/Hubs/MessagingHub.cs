using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Folkie.Api.Hubs;

/// <summary>
/// SignalR hub for real-time message delivery.
/// Frontend connects to /hubs/messaging with Clerk JWT.
/// On message send, server pushes "messageReceived" to recipient's user group.
/// </summary>
[Authorize]
public sealed class MessagingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Identity claim "sub" = Clerk user id; we group by that for now.
        // Real-time push uses Clerk user id, frontend resolves to local UI.
        var clerkUserId = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(clerkUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{clerkUserId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var clerkUserId = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(clerkUserId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{clerkUserId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
