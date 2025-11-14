//enum (Invoice, Receipt, Quotation, Ledger, BalanceSheet)

namespace ApiWorker.Documents.Entities;

public enum DocumentType
{
    Invoice = 1,
    Receipt = 2,
    Quotation = 3,
    Ledger = 4,
    BalanceSheet = 5
}