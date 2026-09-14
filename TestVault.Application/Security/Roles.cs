namespace TestVault.Application.Security;

/// <summary>
/// The three roles this app authorizes against (the exact set named in
/// Phase 6's own instructions). Defined as compile-time constants - both
/// so [Authorize(Roles = Roles.Admin)] attributes can reference them
/// (attribute arguments must be constants) and so IdentitySeeder/UserManagementService
/// have one shared source of truth for the role names, instead of magic
/// strings scattered across Infrastructure and Web.
/// </summary>
public static class Roles
{
    /// <summary>Full access: user administration, manual runs, AI analysis, dashboards.</summary>
    public const string Admin = "Admin";

    /// <summary>Read-only dashboard access (run history, test results, existing AI analysis).</summary>
    public const string User = "User";

    /// <summary>Can trigger/stop manual runs and request AI analysis, in addition to User's read access.</summary>
    public const string Tester = "Tester";

    /// <summary>All roles, in the order they're seeded.</summary>
    public static readonly IReadOnlyList<string> All = new[] { Admin, User, Tester };

    /// <summary>Admin + Tester - the operational roles allowed to trigger manual runs or AI analysis.</summary>
    public const string AdminOrTester = Admin + "," + Tester;
}
