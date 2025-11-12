using System;
using ApiWorker.Authentication.Enum;

namespace ApiWorker.Authentication.Entities;

// Links a user to a business; MVP role is Owner.
public sealed class Membership : Entity
{
    public Guid UserId { get; set; }
    public Guid BusinessId { get; set; }

    public MembershipRole Role { get; set; } = MembershipRole.Owner;
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Nav
    public AppUser? User { get; set; }
    public Business? Business { get; set; }
}
