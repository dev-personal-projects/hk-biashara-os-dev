using AutoMapper;
using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Entities;
using ApiWorker.Documents.ValueObjects;

namespace ApiWorker.Documents.Mappings;

/// <summary>
/// AutoMapper profile for mapping between DTOs and Entities.
/// Keeps mapping logic centralized and testable.
/// </summary>
public sealed class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        // ===== ENTITY → DTO MAPPINGS =====
        // Used when reading data from database to send to mobile app

        /// <summary>
        /// Maps TransactionalDocument entities to DocumentDto for API responses.
        /// Includes computed totals and URLs.
        /// </summary>
        CreateMap<TransactionalDocument, DocumentDto>()
            .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => new CustomerDto
            {
                Name = src.CustomerName ?? string.Empty,
                Phone = src.CustomerPhone,
                Email = src.CustomerEmail,
                AddressLine1 = src.BillingAddressLine1,
                AddressLine2 = src.BillingAddressLine2,
                City = src.BillingCity,
                Country = src.BillingCountry
            }))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.Urls, opt => opt.MapFrom(src => new DocumentUrls
            {
                DocxUrl = src.DocxBlobUrl,
                PdfUrl = src.PdfBlobUrl,
                PreviewUrl = src.PreviewBlobUrl
            }))
            .ForMember(dest => dest.Theme, opt => opt.MapFrom(src => DocumentTheme.FromJson(src.AppliedThemeJson).ToDto()))
            .ForMember(dest => dest.Signature, opt => opt.MapFrom(src => MapSignature(src)));

        /// <summary>
        /// Maps TransactionalDocumentLine entity to DocumentLineDto.
        /// Simple 1:1 mapping of properties.
        /// </summary>
        CreateMap<TransactionalDocumentLine, DocumentLineDto>();

        /// <summary>
        /// Maps TransactionalDocument entities to DocumentSummary for list views.
        /// Lightweight DTO with only essential fields.
        /// </summary>
        CreateMap<TransactionalDocument, DocumentSummary>()
            .ForMember(dest => dest.RecipientName, opt => opt.MapFrom(src => src.CustomerName))
            .ForMember(dest => dest.PdfUrl, opt => opt.MapFrom(src => src.PdfBlobUrl));

        // ===== DTO → ENTITY MAPPINGS =====
        // Used when creating/updating entities from API requests

        /// <summary>
        /// Maps CreateDocumentManuallyRequest to TransactionalDocument entities.
        /// Sets initial status to Draft and computes totals.
        /// </summary>
        CreateMap<CreateDocumentManuallyRequest, Invoice>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Number, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => DocumentStatus.Draft))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.BillingAddressLine1, opt => opt.MapFrom(src => src.Customer.AddressLine1))
            .ForMember(dest => dest.BillingAddressLine2, opt => opt.MapFrom(src => src.Customer.AddressLine2))
            .ForMember(dest => dest.BillingCity, opt => opt.MapFrom(src => src.Customer.City))
            .ForMember(dest => dest.BillingCountry, opt => opt.MapFrom(src => src.Customer.Country))
            .ForMember(dest => dest.IssuedAt, opt => opt.MapFrom(src => src.IssuedAt ?? DateTimeOffset.UtcNow))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
            .ForMember(dest => dest.Tax, opt => opt.Ignore())
            .ForMember(dest => dest.Total, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<CreateDocumentManuallyRequest, Receipt>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Number, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => DocumentStatus.Draft))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.BillingAddressLine1, opt => opt.MapFrom(src => src.Customer.AddressLine1))
            .ForMember(dest => dest.BillingAddressLine2, opt => opt.MapFrom(src => src.Customer.AddressLine2))
            .ForMember(dest => dest.BillingCity, opt => opt.MapFrom(src => src.Customer.City))
            .ForMember(dest => dest.BillingCountry, opt => opt.MapFrom(src => src.Customer.Country))
            .ForMember(dest => dest.IssuedAt, opt => opt.MapFrom(src => src.IssuedAt ?? DateTimeOffset.UtcNow))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
            .ForMember(dest => dest.Tax, opt => opt.Ignore())
            .ForMember(dest => dest.Total, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<CreateDocumentManuallyRequest, Quotation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Number, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => DocumentStatus.Draft))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.BillingAddressLine1, opt => opt.MapFrom(src => src.Customer.AddressLine1))
            .ForMember(dest => dest.BillingAddressLine2, opt => opt.MapFrom(src => src.Customer.AddressLine2))
            .ForMember(dest => dest.BillingCity, opt => opt.MapFrom(src => src.Customer.City))
            .ForMember(dest => dest.BillingCountry, opt => opt.MapFrom(src => src.Customer.Country))
            .ForMember(dest => dest.IssuedAt, opt => opt.MapFrom(src => src.IssuedAt ?? DateTimeOffset.UtcNow))
            .ForMember(dest => dest.Lines, opt => opt.MapFrom(src => src.Lines))
            .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
            .ForMember(dest => dest.Tax, opt => opt.Ignore())
            .ForMember(dest => dest.Total, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        /// <summary>
        /// Maps DocumentLineDto to TransactionalDocumentLine entity.
        /// Computes line total from quantity and unit price.
        /// </summary>
        CreateMap<DocumentLineDto, TransactionalDocumentLine>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.TaxRate, opt => opt.MapFrom(src => src.TaxRate ?? 0m))
            .ForMember(dest => dest.LineTotal, opt => opt.MapFrom(src => src.Quantity * src.UnitPrice))
            .ForMember(dest => dest.Document, opt => opt.Ignore());

        /// <summary>
        /// Maps Template entity to TemplateDto.
        /// Includes theme deserialization and blob URL construction.
        /// </summary>
        CreateMap<Template, TemplateDto>()
            .ForMember(dest => dest.BlobUrl, opt => opt.MapFrom(src => src.BlobPath))
            .ForMember(dest => dest.PreviewUrl, opt => opt.MapFrom(src => src.PreviewBlobUrl))
            .ForMember(dest => dest.Theme, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.ThemeJson)
                    ? null
                    : DocumentTheme.FromJson(src.ThemeJson).ToDto()));

        /// <summary>
        /// Maps UpdateDocumentRequest to TransactionalDocument entity.
        /// Only updates provided fields (partial update).
        /// </summary>
        CreateMap<UpdateDocumentRequest, TransactionalDocument>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.DocumentId))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom((src, dest) =>
                src.Customer != null ? src.Customer.Name : dest.CustomerName))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom((src, dest) =>
                src.Customer != null ? src.Customer.Phone : dest.CustomerPhone))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom((src, dest) =>
                src.Customer != null ? src.Customer.Email : dest.CustomerEmail))
            .ForMember(dest => dest.DueAt, opt => opt.MapFrom((src, dest) =>
                src.DueAt ?? dest.DueAt))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom((src, dest) =>
                src.Notes ?? dest.Notes))
            .ForMember(dest => dest.Reference, opt => opt.MapFrom((src, dest) =>
                src.Reference ?? dest.Reference))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static DocumentSignatureDto? MapSignature(TransactionalDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.SignatureBlobUrl) && string.IsNullOrWhiteSpace(document.SignedBy))
            return null;

        return new DocumentSignatureDto
        {
            IsSigned = !string.IsNullOrWhiteSpace(document.SignatureBlobUrl),
            SignedBy = document.SignedBy,
            SignedAt = document.SignedAt,
            SignatureUrl = document.SignatureBlobUrl,
            Notes = document.SignatureNotes
        };
    }
}
