namespace TestVault.Application.DTOs.Auth;

/// <summary>Request body for POST /api/auth/login.</summary>
public class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
