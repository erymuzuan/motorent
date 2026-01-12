using MotoRent.Domain.Helps;
using MotoRent.Domain.Messaging;
using MotoRent.Worker.Infrastructure;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Processes comment mentions to automatically follow entities.
/// When a user is mentioned in a comment or creates a comment, they become a follower.
/// </summary>
public class CommentMentionSubscriber : Subscriber<Comment>
{
    public override string QueueName => nameof(CommentMentionSubscriber);

    public override string[] RoutingKeys => [$"{nameof(Comment)}.#.#"];

    protected override async Task ProcessMessage(Comment item, BrokeredMessage message)
    {
        if (message.Item is not Comment comment) return;

        // Extract mentioned users from comment text
        var recipients = (await comment.ExtractUserMentionsAsync()).ToList();

        // Also add the comment author
        if (!string.IsNullOrEmpty(message.Username))
            recipients.Add(message.Username);

        // Get existing followers for this entity
        var followQuery = DataContext.CreateQuery<Follow>()
            .Where(x => x.EntityId == comment.EntityId && x.Type == comment.Type);
        var followersLoad = await DataContext.LoadAsync(followQuery, size: 1000, includeTotalRows: true);
        var followers = followersLoad.ItemCollection.ToArray();

        using var session = DataContext.OpenSession();

        // Ensure each mentioned user and the author is following the entity
        foreach (var user in recipients.Distinct())
        {
            var tracker = followers.FirstOrDefault(x => x.User == user) ??
                          new Follow
                          {
                              User = user,
                              EntityId = comment.EntityId,
                              Type = comment.Type ?? "",
                              IsActive = true
                          };
            tracker.IsActive = true;
            session.Attach(tracker);
        }

        await session.SubmitChanges("AddFollowerViaCommentMention");
        WriteMessage("Processed mentions for comment {CommentId}, {Count} followers updated",
            item.CommentId, recipients.Distinct().Count());
    }
}
