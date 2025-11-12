using System;

namespace ApiWorker.Authentication.Entities;

// Tracks a user's device for security and "sign out everywhere".
public sealed class DeviceSession : Entity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public string DeviceId { get; set; } = string.Empty;   // stable per-install ID
    public string Platform { get; set; } = "android";      // android / ios
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
