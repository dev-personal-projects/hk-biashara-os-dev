using ApiWorker.Authentication.DTOS;
using ApiWorker.Authentication.Interfaces;
using ApiWorker.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWorker.Authentication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ICurrentUserService currentUserService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<SignupResponse>> Signup([FromBody] SignupRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new SignupResponse { Success = false, Message = "Invalid request data" });

        var result = await _authenticationService.CreateUserAccountAsync(request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new LoginResponse { Success = false, Message = "Invalid request data" });

        var result = await _authenticationService.AuthenticateUserAsync(request, ct);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("google")]
    public async Task<ActionResult<LoginResponse>> GoogleAuth([FromBody] GoogleAuthRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new LoginResponse { Success = false, Message = "Invalid request data" });

        var result = await _authenticationService.AuthenticateWithGoogleAsync(request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("business/register")]
    public async Task<ActionResult<RegisterBusinessResponse>> RegisterBusiness([FromBody] RegisterBusinessRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new RegisterBusinessResponse { Success = false, Message = "Invalid request data" });

        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new RegisterBusinessResponse { Success = false, Message = "User not authenticated" });

        var userId = _currentUserService.UserId.Value;

        var result = await _authenticationService.CreateBusinessProfileAsync(userId, request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("initialize-session")]
    public async Task<ActionResult> InitializeSession([FromBody] InitializeSessionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request data" });

        try
        {
            var result = await _authenticationService.InitializeUserSessionAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
