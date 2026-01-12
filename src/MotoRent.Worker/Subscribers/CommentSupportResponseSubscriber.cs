using MotoRent.Domain.Helps;
using MotoRent.Domain.Messaging;
using MotoRent.Worker.Infrastructure;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Tracks support response times when support staff responds to a support request.
/// </summary>
public class CommentSupportResponseSubscriber : Subscriber<Comment>
{
    public override string QueueName => nameof(CommentSupportResponseSubscriber);

    public override string[] RoutingKeys => [$"{nameof(Comment)}.#.#"];

    private static readonly string[] s_supportUserNames = ["MotoRent Support", "Support"];

    protected override async Task ProcessMessage(Comment item, BrokeredMessage message)
    {
        if (message.Item is not Comment comment) return;

        // Only process responses from support staff
        if (!s_supportUserNames.Contains(comment.UserDisplayName, StringComparer.OrdinalIgnoreCase))
            return;

        // Find the associated support request
        var sq = CoreDataContext.SupportRequests
            .Where(x => x.Type == item.Type)
            .Where(x => x.EntityId == item.EntityId)
            .Where(x => x.AccountNo == message.AccountNo);
        var srLoad = await CoreDataContext.LoadAsync(sq, size: 1);
        var sr = srLoad.ItemCollection.FirstOrDefault();

        if (sr is null) return;

        // Only update if first response hasn't been recorded
        if (sr is not { TotalMinutesResponded: null }) return;

        sr.TotalMinutesResponded = (DateTimeOffset.Now - sr.Timestamp).TotalMinutes;

        using var cs = CoreDataContext.OpenSession();
        cs.Attach(sr);
        await cs.SubmitChanges("SupportResponse");

        WriteMessage("Recorded first response time for support request {No}: {Minutes:F1} minutes",
            sr.No ?? sr.SupportRequestId.ToString(), sr.TotalMinutesResponded);
    }
}
