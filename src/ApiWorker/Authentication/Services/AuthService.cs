using ApiWorker.Authentication.DTOS;
using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Enum;
using ApiWorker.Authentication.Interfaces;
using ApiWorker.Authentication.Models.ReadModels;
using ApiWorker.Authentication.Models.ValueObjects;
using ApiWorker.Data;
using Microsoft.EntityFrameworkCore;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace ApiWorker.Authentication.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _db;
    private readonly IGotrueClient<User, Session> _supabase;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ApplicationDbContext db,
        IGotrueClient<User, Session> supabase,
        ILogger<AuthenticationService> logger)
    {
        _db = db;
        _supabase = supabase;
        _logger = logger;
    }

    public async Task<SignupResponse> CreateUserAccountAsync(SignupRequest request, CancellationToken ct)
    {
        try
        {
            // Validate county
            if (!CountyCode.IsValid(request.County))
                return new SignupResponse { Success = false, Message = "Invalid county selected" };

            // Check if email exists in our DB
            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

            if (existingUser != null)
                return new SignupResponse { Success = false, Message = "Email already registered" };

            // Create user in Supabase
            var session = await _supabase.SignUp(request.Email, request.Password);

            if (session?.User == null)
                return new SignupResponse { Success = false, Message = "Failed to create account" };

            // Create user in our DB
            var user = new AppUser
            {
                SupabaseUserId = session.User.Id,
                FullName = request.FullName,
                Email = request.Email,
                County = CountyCode.From(request.County).Value
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("User signed up: {Email}", request.Email);

            return new SignupResponse
            {
                Success = true,
                Message = "Account created successfully. Please verify your email.",
                UserId = session.User.Id,
                RequiresEmailVerification = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signup failed for {Email}", request.Email);
            return new SignupResponse { Success = false, Message = "An error occurred during signup" };
        }
    }

    public async Task<VerifyEmailResponse> VerifyUserEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        try
        {
            // Verify OTP with Supabase
            var session = await _supabase.VerifyOTP(request.Email, request.Code, Supabase.Gotrue.Constants.EmailOtpType.Signup);

            if (session?.AccessToken == null)
                return new VerifyEmailResponse { Success = false, Message = "Invalid or expired verification code" };

            _logger.LogInformation("Email verified: {Email}", request.Email);

            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email verified successfully",
                AccessToken = session.AccessToken!,
                RefreshToken = session.RefreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for {Email}", request.Email);
            return new VerifyEmailResponse { Success = false, Message = "Verification failed" };
        }
    }

    public async Task<LoginResponse> AuthenticateUserAsync(LoginRequest request, CancellationToken ct)
    {
        try
        {
            // Authenticate with Supabase
            var session = await _supabase.SignIn(request.Email, request.Password);

            if (session?.User == null)
                return new LoginResponse { Success = false, Message = "Invalid email or password" };

            // Get user from our DB
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.SupabaseUserId == session.User.Id, ct);

            if (user == null)
                return new LoginResponse { Success = false, Message = "User profile not found" };

            // Check if user has business
            var hasBusiness = await _db.Memberships
                .AnyAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active, ct);

            _logger.LogInformation("User logged in: {Email}", request.Email);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = session.AccessToken!,
                RefreshToken = session.RefreshToken,
                User = new UserProfile
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    County = user.County,
                    HasBusiness = hasBusiness
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
            // Validate county
            if (!CountyCode.IsValid(request.County))
                return new LoginResponse { Success = false, Message = "Invalid county selected" };

            // TODO: Implement Google ID token validation
            var email = ExtractEmailFromGoogleToken(request.IdToken);
            if (string.IsNullOrEmpty(email))
                return new LoginResponse { Success = false, Message = "Invalid Google token" };

            // Check if user exists
            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

            if (existingUser != null)
            {
                // Existing user - sign them in
                var session = await CreateSupabaseSessionForUser(existingUser);
                return await CreateLoginResponse(existingUser, session, ct);
            }
            else
            {
                // New user - create account
                var fullName = ExtractNameFromGoogleToken(request.IdToken) ?? email.Split('@')[0];
                
                var signupRequest = new SignupRequest
                {
                    FullName = fullName,
                    Email = email,
                    Password = GenerateTemporaryPassword(),
                    County = request.County
                };

                var signupResult = await CreateUserAccountAsync(signupRequest, ct);
                
                if (!signupResult.Success)
                    return new LoginResponse { Success = false, Message = signupResult.Message };

                // Auto-verify Google users
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
                
                var session = await CreateSupabaseSessionForUser(user!);
                return await CreateLoginResponse(user!, session, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google auth failed");
            return new LoginResponse { Success = false, Message = "Google authentication failed" };
        }
    }

    public async Task<RegisterBusinessResponse> CreateBusinessProfileAsync(Guid userId, RegisterBusinessRequest request, CancellationToken ct)
    {
        try
        {
            // Validate county
            if (!CountyCode.IsValid(request.County))
                return new RegisterBusinessResponse { Success = false, Message = "Invalid county selected" };

            // Check if user exists
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user == null)
                return new RegisterBusinessResponse { Success = false, Message = "User not found" };

            // Check if user already has a business
            var existingMembership = await _db.Memberships
                .AnyAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active, ct);

            if (existingMembership)
                return new RegisterBusinessResponse { Success = false, Message = "User already has a business" };

            // Create business
            var business = new Business
            {
                Name = request.Name,
                Category = request.Category,
                County = CountyCode.From(request.County).Value,
                Town = request.Town,
                Email = request.Email,
                Phone = request.Phone,
                Currency = request.Currency,
                UsesVat = request.UsesVat,
                DefaultTaxRate = request.DefaultTaxRate
            };

            _db.Businesses.Add(business);

            // Create membership
            var membership = new Membership
            {
                UserId = userId,
                BusinessId = business.Id,
                Role = MembershipRole.Owner,
                Status = MembershipStatus.Active
            };

            _db.Memberships.Add(membership);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Business registered: {BusinessName} for user {UserId}", request.Name, userId);

            return new RegisterBusinessResponse
            {
                Success = true,
                Message = "Business registered successfully",
                BusinessId = business.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Business registration failed for user {UserId}", userId);
            return new RegisterBusinessResponse { Success = false, Message = "Business registration failed" };
        }
    }

    public async Task<UserSessionResult> InitializeUserSessionAsync(InitializeSessionRequest request, CancellationToken ct)
    {
        try
        {
            var user = await _db.Users
                .Include(u => u.Memeberships.Where(m => m.Status == MembershipStatus.Active))
                .ThenInclude(m => m.Business)
                .FirstOrDefaultAsync(u => u.SupabaseUserId == request.SupabaseUserId, ct);

            if (user == null)
                return new UserSessionResult { Success = false, Message = "User not found" };

            var activeMembership = user.Memeberships.FirstOrDefault();
            var userSession = Models.BusinessLogic.UserSession.FromUser(user, activeMembership);

            return new UserSessionResult
            {
                Success = true,
                User = userSession
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session initialization failed for {SupabaseUserId}", request.SupabaseUserId);
            return new UserSessionResult { Success = false, Message = "Session initialization failed" };
        }
    }

    private string ExtractEmailFromGoogleToken(string idToken)
    {
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3) return string.Empty;
            
            var payload = parts[1];
            while (payload.Length % 4 != 0)
                payload += "=";
            
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            return tokenData?.GetValueOrDefault("email")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string? ExtractNameFromGoogleToken(string idToken)
    {
        try
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3) return null;
            
            var payload = parts[1];
            while (payload.Length % 4 != 0)
                payload += "=";
            
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            return tokenData?.GetValueOrDefault("name")?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private string GenerateTemporaryPassword() => Guid.NewGuid().ToString("N")[..16] + "!A1";

    private Task<Session> CreateSupabaseSessionForUser(AppUser user)
    {
        return Task.FromResult(new Session
        {
            AccessToken = GenerateJwtToken(user),
            RefreshToken = Guid.NewGuid().ToString(),
            User = new User { Id = user.SupabaseUserId, Email = user.Email }
        });
    }

    private string GenerateJwtToken(AppUser user)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"sub\":\"{user.SupabaseUserId}\",\"email\":\"{user.Email}\"}}"));
    }

    private async Task<LoginResponse> CreateLoginResponse(AppUser user, Session session, CancellationToken ct)
    {
        var hasBusiness = await _db.Memberships
            .AnyAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active, ct);

        return new LoginResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = session.AccessToken!,
            RefreshToken = session.RefreshToken,
            User = new UserProfile
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                County = user.County,
                HasBusiness = hasBusiness
            }
        };
    }
}