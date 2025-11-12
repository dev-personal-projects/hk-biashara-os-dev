using System.ComponentModel.DataAnnotations;

namespace ApiWorker.Authentication.DTOS;

// ===== SIGNUP =====
public sealed class SignupRequest
{
    [Required, MaxLength(128)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, MaxLength(64)]
    public string County { get; init; } = string.Empty;
}

public sealed class SignupResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UserId { get; init; }
}

// ===== LOGIN =====
public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public UserProfile? User { get; init; }
}

// ===== GOOGLE OAUTH =====
public sealed class GoogleAuthRequest
{
    [Required]
    public string IdToken { get; init; } = string.Empty;

    [Required, MaxLength(64)]
    public string County { get; init; } = string.Empty;
}

// ===== BUSINESS REGISTRATION =====
public sealed class RegisterBusinessRequest
{
    [Required, MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(64)]
    public string Category { get; init; } = string.Empty;

    [Required, MaxLength(64)]
    public string County { get; init; } = string.Empty;

    [MaxLength(96)]
    public string? Town { get; init; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(32)]
    public string? Phone { get; init; }

    [MaxLength(8)]
    public string Currency { get; init; } = "KES";

    public bool UsesVat { get; init; }
    public decimal? DefaultTaxRate { get; init; }
}

public sealed class RegisterBusinessResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? BusinessId { get; init; }
}

// ===== USER SESSION INITIALIZATION =====
public sealed class InitializeSessionRequest
{
    [Required, MaxLength(64)]
    public string SupabaseUserId { get; init; } = string.Empty;
}



public sealed class UserProfile
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public bool HasBusiness { get; init; }
}