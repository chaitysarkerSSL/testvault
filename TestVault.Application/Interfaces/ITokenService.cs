namespace TestVault.Application.Interfaces;

/// <summary>
/// Issues and hashes tokens. Implemented in TestVault.Infrastructure
/// (JwtTokenService) since it reads the signing secret from configuration
/// and does the actual cryptography - AuthService only ever sees the
/// results, never a secret key or signing algorithm.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a short-lived signed JWT access token carrying the user's id, username, and roles.</summary>
    AccessTokenResult CreateAccessToken(string userId, string userName, IEnumerable<string> roles);

    /// <summary>Generates a new cryptographically random refresh token value (returned to the client) together with its lifetime.</summary>
    RefreshTokenResult CreateRefreshToken();

    /// <summary>Hashes a raw refresh token value for storage/lookup - see RefreshToken.TokenHash's own doc comment for why the raw value is never persisted.</summary>
    string HashRefreshToken(string rawToken);
}

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);
