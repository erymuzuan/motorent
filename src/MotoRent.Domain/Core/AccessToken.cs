using System.Text.Json.Serialization;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// API access token for authenticated API access.
/// Stores both the token identifier and the JWT payload.
/// </summary>
public class AccessToken : Entity
{
    public int AccessTokenId { get; set; }

    /// <summary>
    /// Client application identifier.
    /// </summary>
    public string Client { get; set; } = "";

    /// <summary>
    /// Optional note/description for this token.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Organization AccountNo this token is scoped to.
    /// </summary>
    public string AccountNo { get; set; } = "";

    /// <summary>
    /// Username this token belongs to.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// Random salt for token validation.
    /// </summary>
    public string Salt { get; set; } = "";

    /// <summary>
    /// Token identifier (public part of the token).
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// JWT payload containing claims. Not serialized to JSON storage.
    /// </summary>
    [JsonIgnore]
    public string Payload { get; set; } = "";

    /// <summary>
    /// Date the token was issued.
    /// </summary>
    public DateOnly Issued { get; set; }

    /// <summary>
    /// Date the token expires.
    /// </summary>
    public DateOnly Expires { get; set; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Whether this is a transient (temporary) token.
    /// </summary>
    public bool IsTransient { get; set; }

    /// <summary>
    /// Checks if the token is valid (not expired and not revoked).
    /// </summary>
    public bool IsValid => !IsRevoked && Expires >= DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// Generates a salted token identifier.
    /// </summary>
    public void GenerateSaltedToken(string encodedId)
    {
        Salt = Guid.NewGuid().ToString("N")[..8];
        Token = encodedId + Salt;
    }

    public override int GetId() => AccessTokenId;
    public override void SetId(int value) => AccessTokenId = value;

    public override string ToString() => $"{UserName}@{AccountNo} ({Client})";
}
