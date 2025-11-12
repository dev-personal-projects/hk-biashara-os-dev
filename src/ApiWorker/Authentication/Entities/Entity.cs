using System;
using System.ComponentModel.DataAnnotations; // For [Key] attribute

namespace ApiWorker.Authentication.Entities
{
    public abstract class Entity // Base class for all entities in the authentication module
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? DeletedAt { get; set; } // Soft delete timestamp
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}