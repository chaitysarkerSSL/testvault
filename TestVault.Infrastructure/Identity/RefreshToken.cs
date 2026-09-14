namespace TestVault.Infrastructure.Identity;

/// <summary>
/// A single refresh token, persisted so it can be validated, rotated, and
/// revoked - ASP.NET Core Identity itself doesn't include refresh token
/// storage (only password/lockout/2FA state), so this is a small custom
/// addition alongside it, in its own table (see IdentityDbContext).
///
/// The token value itself is never stored in plain text - only its SHA-256
/// hash (<see cref="TokenHash"/>), the same defense-in-depth principle
/// Identity already applies to passwords: a leaked database row can't be
/// replayed as a live token. Unlike a password, a refresh token is already
/// high-entropy random data, so a fast hash (SHA-256) is sufficient here -
/// it doesn't need bcrypt/Argon2's deliberate slowness, which exists to
/// resist brute-forcing a *low*-entropy human-chosen secret.
///
/// Rotation: TokenService/AuthService issue a brand new refresh token on
/// every successful /api/auth/refresh call and revoke this one, recording
/// ReplacedByTokenHash. If a token is ever presented again after being
/// revoked, that's a strong signal of theft (a legitimate client only ever
/// uses the newest token) - AuthService treats that as a breach and revokes
/// every other active token for the same user.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>Foreign key to <see cref="ApplicationUser.Id"/>.</summary>
    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }

    /// <summary>Set when this token was rotated out in favor of a new one - null if revoked for any other reason (logout, breach response).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;
}
