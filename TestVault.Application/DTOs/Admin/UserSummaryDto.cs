namespace TestVault.Application.DTOs.Admin;

/// <summary>Response shape for GET /api/admin/users and POST /api/admin/users.</summary>
public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public bool LockedOut { get; set; }
}
