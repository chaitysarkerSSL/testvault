using Microsoft.AspNetCore.Identity;
using TestVault.Application.DTOs.Admin;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Application.Security;

namespace TestVault.Infrastructure.Identity;

/// <summary>Admin-only user provisioning, backing AdminController.</summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!Roles.All.Contains(request.Role))
        {
            throw new ValidationException($"'{request.Role}' is not a valid role. Must be one of: {string.Join(", ", Roles.All)}.");
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.DisplayName,
            // Admin-created accounts are pre-confirmed - there's no email
            // sending/confirmation flow in this app (see the Phase 6
            // summary's noted gaps).
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            // Roll back the just-created account rather than leave a
            // roleless user behind - every user in this app is expected to
            // have exactly one of the three roles.
            await _userManager.DeleteAsync(user);
            throw new ValidationException(string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        return await ToDto(user);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.ToList();
        var summaries = new List<UserSummaryDto>(users.Count);

        foreach (var user in users)
        {
            summaries.Add(await ToDto(user));
        }

        return summaries;
    }

    private async Task<UserSummaryDto> ToDto(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var lockedOut = await _userManager.IsLockedOutAsync(user);

        return new UserSummaryDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToList(),
            LockedOut = lockedOut
        };
    }
}
