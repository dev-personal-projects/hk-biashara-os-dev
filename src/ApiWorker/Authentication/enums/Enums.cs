namespace ApiWorker.Authentication.Enum;

// MVP role; extend later (Manager/Cashier/Accountant).
public enum MembershipRole { Owner = 0 }

// Lifecycle for memberships (future proof).
public enum MembershipStatus { Active = 0, Invited = 1, Disabled = 2 }

// Supported document types for templates/rendering.
public enum DocType { Invoice = 0, Receipt = 1, PO = 2, CreditNote = 3 }
