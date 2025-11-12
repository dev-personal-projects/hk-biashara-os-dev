# Models (MVP)

src/
└─ ApiWorker/
   ├─ ApiWorker.csproj
   ├─ Program.cs
   ├─ Controllers/                      # non-auth controllers (e.g., Health, Ping)
   │  └─ HealthController.cs
   │
   ├─ Authentication/                   # all auth-specific pieces live here
   │  ├─ Controllers/                   # Auth endpoints (Signup/Login/Bootstrap, Google OAuth)
   │  ├─ DTOs/                          # RegisterRequest, LoginRequest, AuthResponse, VerifyEmailCodeRequest
   │  ├─ Models/                        # domain shapes not 1:1 with tables
   │  │  ├─ ValueObjects/
   │  │  │  ├─ GeoPoint.cs             # lat/long VO (shared usage from here)
   │  │  │  └─ CountyCode.cs           # KE county code (string/enum-backed)
   │  │  └─ ReadModels/
   │  │     └─ AuthUserSummary.cs      # minimal user profile returned to client
   │  ├─ Interfaces/                    # IAuthService, ITokenService (if needed later)
   │  ├─ Services/                      # AuthService, TokenService (supabase + jwt glue)
   │  ├─ Settings/                      # JwtOptions, GoogleOAuthOptions
   │  └─ Extensions/                    # AddAuthenticationInfrastructure(this IServiceCollection …)
   │
   ├─ Entities/                         # persisted tables in Azure SQL (MVP core)
   │  ├─ AppUser.cs                     # users table (maps Supabase user -> our system)
   │  ├─ Business.cs                    # businesses table
   │  ├─ Membership.cs                  # memberships table (MVP: role = Owner only)
   │  └─ Template.cs                    # templates table (doc_type, json_definition, is_default)
   │
   ├─ Data/                             # EF Core data access (minimal)
   │  ├─ ApplicationDbContext.cs        # DbSets: AppUser, Business, Membership, Template
   │  └─ Migrations/                    # ef migrations land here
   │
   ├─ Config/                           # appsettings + typed binding
   │  ├─ appsettings.json
   │  └─ appsettings.Development.json
   │
   └─ README.md                         # quick how-to run, env vars, and module map

These POCOs mirror the minimal SQL schema used during auth/bootstrap and onboarding.

- **AppUser** — local profile mirroring Supabase identity (keeps county + optional lat/long).
- **Business** — tenant record (single-outlet for MVP) with optional geolocation.
- **Membership** — links user↔business; role is **Owner** only for MVP.
- **Template** — per-business (or global) document layout stored as JSON.
- **Enums** — Role, MembershipStatus, DocumentType.

No persistence attributes or navigation properties yet. EF Core configuration will live in `Data/` later.


# Models (Authentication)

- **ValueObjects/**
  - `GeoPoint` – immutable lat/long with range checks.
  - `CountyCode` – Kenya counties whitelist; normalizes for display.

- **ReadModels/**
  - `AuthUserSummary` – minimal user profile for the client cache.
  - `BusinessSummary` – tenant picker / header display.
  - `AuthBootstrapResult` – unified payload after login/refresh.

Notes:
- No EF attributes here; these are framework-agnostic shapes.
- Controllers/Services can map from Entities → ReadModels.
- Keep them small and stable to avoid breaking the mobile app.
