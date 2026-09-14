namespace TestVault.Application.DTOs.Admin;

/// <summary>Request body for POST /api/admin/users.</summary>
public class CreateUserRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    /// <summary>One of Security.Roles.All (Admin/User/Tester).</summary>
    public string Role { get; set; } = string.Empty;
}
