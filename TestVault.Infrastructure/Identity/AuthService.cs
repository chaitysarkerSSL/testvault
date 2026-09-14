using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestVault.Application.DTOs.Auth;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;

namespace TestVault.Infrastructure.Identity;

/// <summary>
/// Login/refresh/logout orchestration - see IAuthService's doc comment for
/// why this isn't split into a further "framework-free" abstraction.
///
/// Refresh-token rotation with reuse detection: every successful refresh
/// issues a brand new token and revokes the one just used, recording
/// ReplacedByTokenHash. If an already-revoked token is presented again
/// (only possible if it leaked and was stolen, since a legitimate client
/// always uses its newest token), that's treated as a compromise signal -
/// every other active token for that user is revoked too, forcing
/// re-authentication everywhere.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TestVaultIdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TestVaultIdentityDbContext dbContext,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        // Same generic message whether the username doesn't exist or the
        // password is wrong - never reveal which, to avoid letting a caller
        // enumerate valid usernames.
        const string invalidCredentialsMessage = "Invalid username or password.";

        if (user is null)
        {
            throw new AuthenticationException(invalidCredentialsMessage);
        }

        // lockoutOnFailure: true - increments/checks Identity's own
        // AccessFailedCount/LockoutEnd (configured in DependencyInjection.cs)
        // so repeated bad attempts lock the account automatically.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                throw new AuthenticationException("This account is locked due to repeated failed login attempts. Try again later.");
            }

            throw new AuthenticationException(invalidCredentialsMessage);
        }

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        if (stored.IsRevoked)
        {
            // Reuse of an already-rotated-out token - possible theft.
            // Revoke every other active token for this user as a precaution.
            await RevokeAllActiveTokensForUserAsync(stored.UserId, ipAddress, cancellationToken);
            throw new AuthenticationException("This refresh token has already been used. All sessions for this account have been revoked as a precaution.");
        }

        if (stored.IsExpired)
        {
            throw new AuthenticationException("Refresh token has expired. Please log in again.");
        }

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null)
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        var newRefreshToken = _tokenService.CreateRefreshToken();
        var newHash = _tokenService.HashRefreshToken(newRefreshToken.Token);

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ipAddress;
        stored.ReplacedByTokenHash = newHash;

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            ExpiresAt = newRefreshToken.ExpiresAtUtc,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user.Id, user.UserName!, roles);

        return new AuthResultDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAtUtc,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAtUtc,
            UserName = user.UserName!,
            Roles = roles.ToList()
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        // Logout is idempotent/forgiving: an unknown or already-inactive
        // token is simply a no-op, not an error - the caller's goal (this
        // token no longer works) is already satisfied either way.
        if (stored is null || !stored.IsActive)
        {
            return;
        }

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ipAddress;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResultDto> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user.Id, user.UserName!, roles);
        var refreshToken = _tokenService.CreateRefreshToken();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken.Token),
            ExpiresAt = refreshToken.ExpiresAtUtc,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResultDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAtUtc,
            UserName = user.UserName!,
            Roles = roles.ToList()
        };
    }

    private async Task RevokeAllActiveTokensForUserAsync(string userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
