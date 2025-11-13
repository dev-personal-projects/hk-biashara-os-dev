using ApiWorker.Authentication.DTOS;
using ApiWorker.Authentication.Interfaces;
using ApiWorker.Authentication.Services;
using ApiWorker.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWorker.Authentication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ICurrentUserService currentUserService,
        IBlobStorageService blobStorageService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _currentUserService = currentUserService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<SignupResponse>> Signup([FromBody] SignupRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return BadRequest(new SignupResponse { Success = false, Message = errors ?? "Please check your input and try again" });
        }

        var result = await _authenticationService.CreateUserAccountAsync(request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return BadRequest(new LoginResponse { Success = false, Message = errors ?? "Please check your input and try again" });
        }

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
    public async Task<ActionResult<RegisterBusinessResponse>> RegisterBusiness([FromForm] RegisterBusinessRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return BadRequest(new RegisterBusinessResponse { Success = false, Message = errors ?? "Please check your input and try again" });
        }

        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new RegisterBusinessResponse { Success = false, Message = "Please login to continue" });

        var userId = _currentUserService.UserId.Value;

        string? logoUrl = null;
        if (request.Logo != null)
        {
            logoUrl = await _blobStorageService.UploadImageAsync(request.Logo, "business-logos", ct);
        }

        var result = await _authenticationService.CreateBusinessProfileAsync(userId, request, logoUrl, ct);

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

    [Authorize]
    [HttpGet("businesses")]
    public async Task<ActionResult<ListBusinessesResponse>> GetBusinesses(CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new { success = false, message = "Please login to continue" });

        var result = await _authenticationService.GetUserBusinessesAsync(_currentUserService.UserId.Value, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("businesses/switch")]
    public async Task<ActionResult<SwitchBusinessResponse>> SwitchBusiness([FromBody] SwitchBusinessRequest request, CancellationToken ct)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(new { success = false, message = "Please login to continue" });

        var result = await _authenticationService.SwitchBusinessAsync(_currentUserService.UserId.Value, request, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
