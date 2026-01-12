using Microsoft.AspNetCore.SignalR;
using MotoRent.Domain.Helps;

namespace MotoRent.Server.Hubs;

/// <summary>
/// SignalR hub for real-time comment notifications.
/// Broadcasts new comments to all connected clients.
/// </summary>
public class CommentHub : Hub
{
    /// <summary>
    /// Broadcasts a new comment to all connected clients.
    /// </summary>
    /// <param name="comment">The comment that was added.</param>
    /// <param name="accountNo">The tenant account number.</param>
    public async Task<bool> CommentAdded(Comment comment, string accountNo)
    {
        if (comment is not { EntityId: > 0, Type: not null })
            return false;

        await Clients.All.SendCoreAsync(nameof(CommentAdded), [comment, accountNo]);
        return true;
    }
}
