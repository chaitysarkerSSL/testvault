using TestVault.Application.DTOs.Admin;

namespace TestVault.Application.Interfaces;

/// <summary>
/// Admin-only user provisioning, backing AdminController. Separate from
/// IAuthService: this is user *administration* (who exists, what role they
/// have), not login/token orchestration.
/// </summary>
public interface IUserManagementService
{
    /// <exception cref="Exceptions.ValidationException">The role isn't one of Security.Roles.All, or user creation failed (e.g. password policy, duplicate username).</exception>
    Task<UserSummaryDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken cancellationToken = default);
}
