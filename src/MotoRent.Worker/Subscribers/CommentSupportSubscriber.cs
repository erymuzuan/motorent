using System.Text;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Helps;
using MotoRent.Domain.Messaging;
using MotoRent.Worker.Infrastructure;
using TaskStatus = MotoRent.Domain.Helps.TaskStatus;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Creates support requests when a comment mentions the support email.
/// </summary>
public class CommentSupportSubscriber : Subscriber<Comment>
{
    public override string QueueName => nameof(CommentSupportSubscriber);

    public override string[] RoutingKeys => [$"{nameof(Comment)}.#.#"];

    private static readonly char[] s_separator = [',', ';'];
    private static readonly string[] s_supportEmails = ["support@motorent.com"];

    protected override async Task ProcessMessage(Comment item, BrokeredMessage message)
    {
        if (message.Item is not Comment comment) return;

        // Skip if this was a SetSupportStatus operation (avoid loops)
        if (message is { Operation: "SetSupportStatus" }) return;

        // Extract mentioned users
        var recipients = (await comment.ExtractUserMentionsAsync()).ToList();

        // Check if any support email was mentioned
        if (!recipients.Any(x => s_supportEmails.Contains(x, StringComparer.OrdinalIgnoreCase)))
            return;

        // Build previous thread context
        var query = DataContext.CreateQuery<Comment>()
            .Where(x => x.CommentId != item.CommentId)
            .Where(x => x.Type == item.Type)
            .Where(x => x.EntityId == item.EntityId)
            .OrderBy(x => x.Timestamp);
        var lo = await DataContext.LoadAsync(query, size: 100, includeTotalRows: true);
        var comments = lo.ItemCollection.ToList();

        var previous = new StringBuilder();
        foreach (var cmt in comments)
        {
            var p = $"""
                     <hr/>
                     <div> User : {cmt.User}</div>
                     <div> Date : {cmt.Timestamp}</div>
                     <div>
                        {cmt.Text}
                     </div>
                     """;
            previous.AppendLine(p);
        }

        // Mark the comment as a support comment
        item.SupportComment = true;
        item.SupportStatus = item.SupportStatus switch
        {
            null => TaskStatus.Ready,
            _ => item.SupportStatus
        };

        using var session = DataContext.OpenSession();
        session.Attach(item);
        await session.SubmitChanges("SetSupportStatus");

        // Create or update support request in Core schema
        var acc = string.IsNullOrWhiteSpace(item.AccountNo) ? message.AccountNo : item.AccountNo;
        var sq = CoreDataContext.SupportRequests
            .Where(x => x.CommentId == item.CommentId)
            .Where(x => x.AccountNo == message.AccountNo);
        var srLoad = await CoreDataContext.LoadAsync(sq, size: 1);
        var sr = srLoad.ItemCollection.FirstOrDefault() ?? new SupportRequest(item, acc ?? "");

        // Generate serial number if new
        if (sr is { No: null })
        {
            var generator = ObjectBuilder.GetObject<ISerialNumberGenerator>();
            if (generator != null)
                await generator.SetSerialNumberAsync(sr);
        }

        using var cs = CoreDataContext.OpenSession();
        cs.Attach(sr);
        await cs.SubmitChanges("NewSupportRequest");

        WriteMessage("Created support request {No} for comment {CommentId}",
            sr.No ?? "(pending)", item.CommentId);
    }
}
