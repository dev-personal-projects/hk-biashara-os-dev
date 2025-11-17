namespace ApiWorker.Documents.Entities;

/// <summary>
/// Invoice document - inherits all fields from TransactionalDocument.
/// Can add invoice-specific fields here if needed in the future.
/// </summary>
public sealed class Invoice : TransactionalDocument
{
    // Invoice-specific fields can be added here
    // For now, all fields are inherited from TransactionalDocument
}
