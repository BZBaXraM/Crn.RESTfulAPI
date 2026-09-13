using Application.DTOs;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Registers a new user with the User role.</summary>
    [HttpPost("register")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await _auth.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Exchanges credentials for an access token and a refresh token.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _auth.LoginAsync(request, cancellationToken));

    /// <summary>Rotates a refresh token: the presented token is revoked and a new pair is issued.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        => Ok(await _auth.RefreshAsync(request, cancellationToken));

    /// <summary>Revokes a refresh token (logout).</summary>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _auth.RevokeAsync(request, cancellationToken);
        return NoContent();
    }
}
