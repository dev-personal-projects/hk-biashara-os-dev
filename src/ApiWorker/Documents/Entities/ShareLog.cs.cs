//who/where/when shared, message ids (WhatsApp)

using System;
using ApiWorker.Documents.Enums;

namespace ApiWorker.Documents.Entities;

/// <summary>
/// Records every share attempt for auditing / retries.
/// </summary>
public sealed class ShareLog
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }     // FK -> Document.Id

    public ShareChannel Channel { get; set; }
    public string Target { get; set; } = string.Empty;   // phone/email/url
    public string? MessageId { get; set; }               // provider msg id
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Document Document { get; set; } = default!;
}
