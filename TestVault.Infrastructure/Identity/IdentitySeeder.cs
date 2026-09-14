using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestVault.Application.Security;

namespace TestVault.Infrastructure.Identity;

/// <summary>
/// One-time startup seeding: ensures the three roles exist, and - only if
/// the AspNetUsers table is completely empty - creates a single bootstrap
/// admin account so the system is usable at all on a fresh database
/// (otherwise nobody could ever log in to create the first user).
///
/// Deliberately does NOT use a fixed default password (an "Admin123!"-style
/// constant is a well-known real-world breach pattern when nobody bothers
/// to change it). Instead it generates a random one and logs it once, at
/// Warning level, with an explicit instruction to change it immediately -
/// the password is never persisted anywhere in plaintext (not in
/// configuration, not in the database - only its Identity password hash).
/// Called once from Program.cs via a scoped service provider, not
/// registered as a hosted service - this is one-shot startup work, not an
/// ongoing background process.
/// </summary>
public static class IdentitySeeder
{
    private const string BootstrapAdminUserName = "admin";
    private const string BootstrapAdminEmail = "admin@testvault.local";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                logger.LogInformation("Seeded role '{RoleName}'.", roleName);
            }
        }

        if (userManager.Users.Any())
        {
            // Not a fresh database - never touch existing accounts.
            return;
        }

        var password = GenerateRandomPassword();
        var admin = new ApplicationUser
        {
            UserName = BootstrapAdminUserName,
            Email = BootstrapAdminEmail,
            EmailConfirmed = true,
            DisplayName = "Bootstrap Administrator"
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to create the bootstrap admin account: {Errors}",
                string.Join(" ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);

        logger.LogWarning(
            "======================================================================\n" +
            "No users existed yet - created a bootstrap admin account:\n" +
            "    Username: {UserName}\n" +
            "    Password: {Password}\n" +
            "This password is shown ONLY here, ONCE, and is not stored anywhere in\n" +
            "plaintext. Log in immediately and either change this account's password\n" +
            "or create your real admin accounts via POST /api/admin/users and delete\n" +
            "this one.\n" +
            "======================================================================",
            BootstrapAdminUserName,
            password);
    }

    private static string GenerateRandomPassword()
    {
        // 24 random bytes, base64-encoded, then adjusted to guarantee it
        // satisfies the app's own password policy (DependencyInjection.cs) -
        // random bytes alone aren't guaranteed to contain a digit/symbol.
        var bytes = RandomNumberGenerator.GetBytes(24);
        var candidate = Convert.ToBase64String(bytes).Replace("=", string.Empty);
        return $"{candidate}9!";
    }
}
