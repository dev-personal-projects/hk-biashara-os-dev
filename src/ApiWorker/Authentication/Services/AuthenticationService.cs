using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiWorker.Authentication.DTOS;
using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Enum;
using ApiWorker.Authentication.Extensions;
using ApiWorker.Authentication.Interfaces;
using ApiWorker.Authentication.Models.ReadModels;
using ApiWorker.Authentication.Models.ValueObjects;
using ApiWorker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace ApiWorker.Authentication.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _db;
    private readonly IGotrueClient<User, Session> _supabase;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthServiceCollectionExtensions.JwtSettings _jwtSettings;

    public AuthenticationService(
        ApplicationDbContext db,
        IGotrueClient<User, Session> supabase,
        ILogger<AuthenticationService> logger,
        IOptions<AuthServiceCollectionExtensions.JwtSettings> jwtSettings)
    {
        _db = db;
        _supabase = supabase;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<SignupResponse> CreateUserAccountAsync(SignupRequest request, CancellationToken ct)
    {
        try
        {
            if (!CountyCode.IsValid(request.County))
                return new SignupResponse { Success = false, Message = "Please select a valid county" };

            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

            if (existingUser != null)
                return new SignupResponse { Success = false, Message = "This email is already registered. Please login instead." };

            var user = new AppUser
            {
                SupabaseUserId = Guid.NewGuid().ToString(),
                FullName = request.FullName,
                Email = request.Email,
                County = CountyCode.From(request.County).Value
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("User signed up: {Email}", request.Email);

            var accessToken = GenerateJwtToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            return new SignupResponse
            {
                Success = true,
                Message = "Account created successfully! Please register your business to continue.",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserProfile
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    County = user.County,
                    HasBusiness = false,
                    BusinessCount = 0,
                    OnboardingStatus = OnboardingStatus.AccountCreated
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signup failed for {Email}", request.Email);
            return new SignupResponse { Success = false, Message = "Something went wrong. Please try again later." };
        }
    }

    public async Task<LoginResponse> AuthenticateUserAsync(LoginRequest request, CancellationToken ct)
    {
        try
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

            if (user == null)
                return new LoginResponse { Success = false, Message = "Invalid email or password" };

            var businessCount = await _db.Memberships
                .CountAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active, ct);

            var accessToken = GenerateJwtToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserProfile
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    County = user.County,
                    HasBusiness = businessCount > 0,
                    BusinessCount = businessCount,
                    OnboardingStatus = businessCount > 0 ? OnboardingStatus.Completed : OnboardingStatus.AccountCreated
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", request.Email);
            return new LoginResponse { Success = false, Message = "Login failed" };
        }
    }

    public async Task<LoginResponse> AuthenticateWithGoogleAsync(GoogleAuthRequest request, CancellationToken ct)
    {
        try
        {
            var session = await _supabase.SignInWithIdToken(Supabase.Gotrue.Constants.Provider.Google, request.IdToken);

            if (session?.User == null)
                return new LoginResponse { Success = false, Message = "Invalid Google token" };

            if (!CountyCode.IsValid(request.County))
                return new LoginResponse { Success = false, Message = "Please select a valid county" };

            var supabaseUser = session.User;
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUser.Id, ct);

            if (user == null)
            {
                user = new AppUser
                {
                    SupabaseUserId = supabaseUser.Id,
                    FullName = supabaseUser.UserMetadata?.ContainsKey("full_name") == true 
                        ? supabaseUser.UserMetadata["full_name"].ToString() ?? supabaseUser.Email ?? "User"
                        : supabaseUser.Email ?? "User",
                    Email = supabaseUser.Email ?? string.Empty,
                    County = CountyCode.From(request.County).Value
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
                
                _logger.LogInformation("New user created via Supabase OAuth: {Email}", supabaseUser.Email);
            }

            var businessCount = await _db.Memberships
                .CountAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active, ct);

            var accessToken = GenerateJwtToken(user);
            var refreshToken = session.RefreshToken ?? Guid.NewGuid().ToString();

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserProfile
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    County = user.County,
                    HasBusiness = businessCount > 0,
                    BusinessCount = businessCount,
                    OnboardingStatus = businessCount > 0 ? OnboardingStatus.Completed : OnboardingStatus.AccountCreated
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase OAuth failed");
            return new LoginResponse { Success = false, Message = "Authentication failed" };
        }
    }

    public async Task<RegisterBusinessResponse> CreateBusinessProfileAsync(Guid userId, RegisterBusinessRequest request, string? logoUrl, CancellationToken ct)
    {
        try
        {
            if (!CountyCode.IsValid(request.County))
                return new RegisterBusinessResponse { Success = false, Message = "Please select a valid county" };

            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user == null)
                return new RegisterBusinessResponse { Success = false, Message = "User account not found. Please login again." };

            var business = new Business
            {
                Name = request.Name,
                Category = request.Category,
                County = CountyCode.From(request.County).Value,
                Town = request.Town,
                Email = request.Email,
                Phone = request.Phone,
                LogoUrl = logoUrl
            };

            _db.Businesses.Add(business);

            var membership = new Membership
            {
                UserId = userId,
                BusinessId = business.Id,
                Role = MembershipRole.Owner,
                Status = MembershipStatus.Active
            };

            _db.Memberships.Add(membership);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Business registered: {BusinessName} by User {UserId}", request.Name, userId);

            return new RegisterBusinessResponse
            {
                Success = true,
                Message = "Business registered successfully",
                BusinessId = business.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Business registration failed for User {UserId}", userId);
            return new RegisterBusinessResponse { Success = false, Message = "Business registration failed" };
        }
    }

    public async Task<UserSessionResult> InitializeUserSessionAsync(InitializeSessionRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null)
            throw new InvalidOperationException("User not found");

        var memberships = await _db.Memberships
            .Include(m => m.Business)
            .Where(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)
            .ToListAsync(ct);

        var business = memberships.FirstOrDefault()?.Business;

        return new UserSessionResult
        {
            Success = true,
            User = new UserSessionData
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                County = user.County,
                Business = business != null ? new BusinessSessionData
                {
                    BusinessId = business.Id,
                    BusinessName = business.Name,
                    Category = business.Category,
                    County = business.County,
                    LogoUrl = business.LogoUrl,
                    UserRole = memberships.First().Role.ToString()
                } : null,
                HasBusiness = business != null
            }
        };
    }

    public async Task<ListBusinessesResponse> GetUserBusinessesAsync(Guid userId, CancellationToken ct)
    {
        var memberships = await _db.Memberships
            .Include(m => m.Business)
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .ToListAsync(ct);

        var businesses = memberships.Select(m => new BusinessListItem
        {
            Id = m.Business!.Id,
            Name = m.Business.Name,
            Category = m.Business.Category,
            County = m.Business.County,
            LogoUrl = m.Business.LogoUrl,
            UserRole = m.Role.ToString()
        }).ToList();

        return new ListBusinessesResponse { Success = true, Businesses = businesses };
    }

    public async Task<SwitchBusinessResponse> SwitchBusinessAsync(Guid userId, SwitchBusinessRequest request, CancellationToken ct)
    {
        var membership = await _db.Memberships
            .Include(m => m.Business)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.BusinessId == request.BusinessId && m.Status == MembershipStatus.Active, ct);

        if (membership == null)
            return new SwitchBusinessResponse { Success = false, Message = "Business not found or access denied" };

        var business = membership.Business!;
        return new SwitchBusinessResponse
        {
            Success = true,
            Message = "Business switched successfully",
            Business = new BusinessSessionData
            {
                BusinessId = business.Id,
                BusinessName = business.Name,
                Category = business.Category,
                County = business.County,
                LogoUrl = business.LogoUrl,
                UserRole = membership.Role.ToString()
            }
        };
    }

    private string GenerateJwtToken(AppUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("sub", user.SupabaseUserId),
            new("userId", user.Id.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpiryHours),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
