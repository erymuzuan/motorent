using MotoRent.Domain.DataContext;
using MotoRent.Domain.Helps;

namespace MotoRent.Services;

public class CommentService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    /// <summary>
    /// Get all comments for an entity, including replies.
    /// </summary>
    public async Task<List<Comment>> GetCommentsAsync(int entityId, string entityType)
    {
        var query = Context.CreateQuery<Comment>()
            .Where(c => c.EntityId == entityId)
            .Where(c => c.Type == entityType)
            .Where(c => !c.Hidden)
            .OrderByDescending(c => c.Timestamp);

        var result = await Context.LoadAsync(query, 1, 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Get a single comment by ID.
    /// </summary>
    public async Task<Comment?> GetCommentByIdAsync(int commentId)
    {
        return await Context.LoadOneAsync<Comment>(c => c.CommentId == commentId);
    }

    /// <summary>
    /// Add a new comment (root or reply).
    /// </summary>
    public async Task<SubmitOperation> AddCommentAsync(Comment comment, string username)
    {
        comment.Timestamp = DateTimeOffset.Now;
        comment.User = username;
        comment.Hidden = false;
        comment.Flagged = false;

        using var session = Context.OpenSession(username);
        session.Attach(comment);
        return await session.SubmitChanges("Add comment");
    }

    /// <summary>
    /// Update an existing comment's text.
    /// </summary>
    public async Task<SubmitOperation> UpdateCommentAsync(Comment comment, string username)
    {
        using var session = Context.OpenSession(username);
        session.Attach(comment);
        return await session.SubmitChanges("Update comment");
    }

    /// <summary>
    /// Soft delete a comment by setting Hidden = true.
    /// </summary>
    public async Task<SubmitOperation> DeleteCommentAsync(Comment comment, string username)
    {
        comment.Hidden = true;
        using var session = Context.OpenSession(username);
        session.Attach(comment);
        return await session.SubmitChanges("Delete comment");
    }

    /// <summary>
    /// Get comment count for an entity.
    /// </summary>
    public async Task<int> GetCommentCountAsync(int entityId, string entityType)
    {
        var query = Context.CreateQuery<Comment>()
            .Where(c => c.EntityId == entityId)
            .Where(c => c.Type == entityType)
            .Where(c => !c.Hidden);

        return await Context.GetCountAsync(query);
    }
}
