using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.RegisterAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
}