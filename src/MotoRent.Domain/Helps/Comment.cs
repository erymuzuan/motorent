using System.Text;
using System.Text.RegularExpressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Helps;

public class Comment : Entity
{
    public int CommentId { get; set; }
    public string? AccountNo { get; set; }
    public string? User { get; set; }
    public string? UserDisplayName { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Title { get; set; }
    public string? ObjectWebId { get; set; }
    public string? Text { get; set; }
    public int EntityId { get; set; }
    public string? Type { get; set; }
    public int? Root { get; set; }
    public bool Flagged { get; set; }
    public bool Hidden { get; set; }
    public string? ObjectUri { get; set; }
    public List<UserExpression> Expressions { get; set; } = [];
    public int? ReplyTo { get; set; }
    public bool SupportComment { get; set; }
    public bool SupportResponse { get; set; }
    public int? SupportRequestId { get; set; }
    public TaskStatus? SupportStatus { get; set; }

    public override int GetId() => CommentId;
    public override void SetId(int value) => CommentId = value;

    public Task<IEnumerable<string>> ExtractUserMentionsAsync(string? text = null)
    {
        text ??= this.Text;
        const string PATTERN = @"data-id=""(?<user>[A-Za-z0-9@.]{6,50})""";

        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(Array.Empty<string>().AsEnumerable());

        const RegexOptions OPTIONS = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        var matches = Regex.Matches(text, PATTERN, OPTIONS);
        var names = matches.Select(x => x.Groups[1].Value).ToArray();
        return Task.FromResult(names.AsEnumerable());
    }
}

public class UserExpression
{
    public CommentExpression Expression { get; set; }
    public string? User { get; set; }
    public string? Name { get; set; }
}

public enum CommentExpression
{
    None,
    Kiss,
    Like,
    Laugh,
    Sad,
    Angry
}

public class Follow : Entity
{
    public int FollowId { get; set; }
    public int EntityId { get; set; }
    public string? Type { get; set; }
    public string? User { get; set; }
    public bool IsActive { get; set; }

    public override int GetId() => FollowId;
    public override void SetId(int value) => FollowId = value;
}
