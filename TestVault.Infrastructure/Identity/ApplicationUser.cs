using Microsoft.AspNetCore.Identity;

namespace TestVault.Infrastructure.Identity;

/// <summary>
/// TestVault's ASP.NET Core Identity user. Subclassed (rather than using
/// the bare <see cref="IdentityUser"/>) purely so the model can grow
/// without a breaking change later - DisplayName is the one addition today.
///
/// New in Phase 6: the Node app had no authentication at all (no user
/// table, no login route, no frontend auth flow to analyze - confirmed
/// while scanning both frontend/src and backend/routes before writing any
/// of this). Nothing here replaces existing behavior; it's new.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Optional human-friendly name for admin UIs. Not used for login (UserName/Email are).</summary>
    public string? DisplayName { get; set; }
}
