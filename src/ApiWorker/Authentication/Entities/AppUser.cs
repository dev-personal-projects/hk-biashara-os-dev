using System;
using System.Collections.Generic; // For List<T> 



namespace ApiWorker.Authentication.Entities;

// Persisted profile mapped to Supabase identity; no passwords here.


public sealed class AppUser : Entity
{
    // Supabase auth.user.id (UUID). Store as string to avoid format drift.
    public string SupabaseUserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    //always store lowe cased
    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set => _email = (value ?? string.Empty).ToLowerInvariant();
    }

    public string County { get; set; } = string.Empty;


    //nav
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();

}