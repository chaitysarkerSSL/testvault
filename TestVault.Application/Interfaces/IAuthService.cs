using TestVault.Application.DTOs.Auth;

namespace TestVault.Application.Interfaces;

/// <summary>
/// Login/refresh/logout orchestration. Implemented in
/// TestVault.Infrastructure (AuthService), where UserManager&lt;ApplicationUser&gt;/
/// SignInManager&lt;ApplicationUser&gt; and the RefreshTokens table actually
/// live - see that class for why this isn't split further into its own
/// "Infrastructure-free" abstraction: ASP.NET Core Identity's manager
/// classes already *are* the abstraction over user storage, so wrapping
/// them again here would be redundant indirection, not real decoupling.
/// </summary>
public interface IAuthService
{
    /// <exception cref="Exceptions.AuthenticationException">Invalid username or password.</exception>
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <exception cref="Exceptions.AuthenticationException">The refresh token is unknown, expired, or already revoked.</exception>
    Task<AuthResultDto> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Revokes one refresh token (logout). A no-op if it's already inactive.</summary>
    Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
}
