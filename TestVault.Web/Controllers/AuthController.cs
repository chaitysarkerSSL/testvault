using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestVault.Application.DTOs.Auth;
using TestVault.Application.Interfaces;

namespace TestVault.Web.Controllers;

/// <summary>
/// Login/refresh/logout. New in Phase 6 - the Node app had no
/// authentication at all (confirmed by scanning both frontend/src and
/// backend/routes before writing any of this phase), so there is no prior
/// route to port here.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Exchanges a username/password for an access token + refresh token pair.</summary>
    /// <param name="request">Username and password.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the token pair.</response>
    /// <response code="401">Invalid credentials, or the account is locked out.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, GetClientIp(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Exchanges a still-valid refresh token for a brand new access token + refresh token pair (rotation - the old refresh token is revoked).</summary>
    /// <param name="request">The refresh token previously issued by /login or /refresh.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the new token pair.</response>
    /// <response code="401">The refresh token is invalid, expired, or already used.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, GetClientIp(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Revokes a refresh token, ending that session. Idempotent - always succeeds, even if the token was already invalid.</summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">The token was revoked (or was already inactive).</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, GetClientIp(), cancellationToken);
        return Ok(new { success = true, message = "Logged out." });
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
