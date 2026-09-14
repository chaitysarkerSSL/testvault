namespace TestVault.Application.DTOs.Auth;

/// <summary>
/// Response for POST /api/auth/login and /api/auth/refresh - a fresh access
/// token/refresh token pair plus enough user info for the frontend to
/// render without a separate call.
/// </summary>
public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
