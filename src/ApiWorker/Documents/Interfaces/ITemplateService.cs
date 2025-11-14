using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Manages document templates (upload, list, set default).
/// </summary>
public interface ITemplateService
{
    /// <summary>Gets default template for a business and document type</summary>
    Task<Template?> GetDefaultTemplateAsync(Guid businessId, DocumentType type, CancellationToken ct = default);
    
    /// <summary>Lists all templates for a business</summary>
    Task<List<Template>> ListTemplatesAsync(Guid businessId, DocumentType? type = null, CancellationToken ct = default);
    
    /// <summary>Uploads a new template</summary>
    Task<Template> UploadTemplateAsync(Guid businessId, DocumentType type, string name, Stream templateFile, CancellationToken ct = default);
    
    /// <summary>Sets a template as default for a business</summary>
    Task SetDefaultTemplateAsync(Guid templateId, CancellationToken ct = default);
}
