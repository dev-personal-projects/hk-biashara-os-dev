using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiWorker.Documents.Settings;

/// <summary>
/// Global defaults for all business documents (numbering, locale, currency, tax).
/// Keep this class **pure configuration** — put logic in services.
/// </summary>
public sealed class DocumentSettings
{
    /// <summary>ISO-4217 currency code used when a document doesn't provide one.</summary>
    [Required, StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; init; } = "KES";

    /// <summary>Default locale (number/date formatting, language hints).</summary>
    [Required]
    public string DefaultLocale { get; init; } = "en-KE";

    /// <summary>Allowed currencies the UI can show / API can accept.</summary>
    public HashSet<string> AllowedCurrencies { get; init; } =
        new(new[] { "KES", "USD" }, System.StringComparer.OrdinalIgnoreCase);

    /// <summary>Document numbering options (prefixes + pattern).</summary>
    [Required]
    public NumberingOptions Numbering { get; init; } = new();

    public sealed class NumberingOptions
    {
        /// <summary>Invoice number prefix (per business + type uniqueness enforced in DB).</summary>
        [Required, StringLength(12)]
        public string InvoicePrefix { get; init; } = "INV-";

        /// <summary>Receipt number prefix.</summary>
        [Required, StringLength(12)]
        public string ReceiptPrefix { get; init; } = "RCPT-";

        /// <summary>Quotation number prefix.</summary>
        [Required, StringLength(12)]
        public string QuotationPrefix { get; init; } = "QUO-";

        /// <summary>Pattern tokens: yyyy, MM, dd, #### (sequence). Example: yyyyMM-####</summary>
        [Required, StringLength(32)]
        public string Pattern { get; init; } = "yyyyMM-####";
    }
}
