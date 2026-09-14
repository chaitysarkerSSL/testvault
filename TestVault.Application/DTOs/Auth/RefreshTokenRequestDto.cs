namespace TestVault.Application.DTOs.Auth;

/// <summary>Request body for POST /api/auth/refresh and /api/auth/logout.</summary>
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
