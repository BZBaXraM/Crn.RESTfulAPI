namespace Domain.Entities;

/// <summary>
/// A refresh token. Only the SHA-256 hash of the raw token is persisted; the raw value is returned
/// to the client once and never stored.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? RevokedOn { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresOn;
    public bool IsRevoked => RevokedOn.HasValue;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
}
