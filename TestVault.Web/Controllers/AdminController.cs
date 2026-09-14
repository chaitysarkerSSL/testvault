using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestVault.Application.DTOs.Admin;
using TestVault.Application.Interfaces;
using TestVault.Application.Security;

namespace TestVault.Web.Controllers;

/// <summary>
/// User administration. New in Phase 6, Admin-only - this is how real
/// accounts get provisioned after the one-time bootstrap admin account
/// (see IdentitySeeder) logs in for the first time.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public AdminController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>Creates a new user with exactly one role.</summary>
    /// <param name="request">Username, email, password, and role (Admin/User/Tester).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the created user.</response>
    /// <response code="400">The role is invalid, or user creation failed (e.g. password policy, duplicate username).</response>
    [HttpPost("users")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSummaryDto>> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManagementService.CreateUserAsync(request, cancellationToken);
        return Ok(user);
    }

    /// <summary>Lists every user and their role(s).</summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns all users.</response>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> ListUsers(CancellationToken cancellationToken)
    {
        var users = await _userManagementService.ListUsersAsync(cancellationToken);
        return Ok(users);
    }
}
