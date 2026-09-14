using Microsoft.AspNetCore.Identity;

namespace TestVault.Infrastructure.Identity;

/// <summary>
/// TestVault's ASP.NET Core Identity role. Subclassed (rather than the bare
/// <see cref="IdentityRole"/>) for the same future-proofing reason as
/// <see cref="ApplicationUser"/> - no extra fields today. The three roles
/// this app defines (Admin, User, Tester - see IdentitySeeder) are seeded
/// data, not separate C# types.
/// </summary>
public class ApplicationRole : IdentityRole
{
}
