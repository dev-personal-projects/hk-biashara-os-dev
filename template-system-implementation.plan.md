<!-- 87d2835d-4df4-4405-a935-49fa2cfe0f07 d303a6a0-1b64-40e6-aa6a-77755f6a33a5 -->
# Template System Implementation

## Overview

Implement a complete template system where users can select from 6 pre-uploaded global DOCX templates (2 Invoice, 2 Receipt, 2 Quotation) with different themes. Templates are stored in Azure Blob Storage, metadata in database, and include preview images for mobile efficiency.

## Implementation Steps

### 1. Implement TemplateService

**File**: `src/ApiWorker/Documents/Services/TemplateService.cs`

- Implement `ITemplateService` interface
- `GetDefaultTemplateAsync`: Query for default template per document type (global templates only)
- `ListTemplatesAsync`: Return all global templates (BusinessId = null), optionally filtered by type
- `GetTemplateAsync`: Get single template by ID
- `UploadTemplateAsync`: Upload DOCX to blob storage, save metadata (for seeding script use)
- `SetDefaultTemplateAsync`: Mark template as default (ensure only one default per type globally)

### 2. Create Template DTOs

**File**: `src/ApiWorker/Documents/DTOs/TemplateDtos.cs` (new file)

- `TemplateDto`: id, type, name, version, blobUrl, previewUrl, themeJson, isDefault, createdAt
- `ListTemplatesResponse`: success, message, templates[]
- `GetTemplateResponse`: success, message, template

### 3. Create TemplatesController

**File**: `src/ApiWorker/Documents/Controllers/TemplatesController.cs` (new file)

- `GET /api/templates?type={Invoice|Receipt|Quotation}`: List all global templates (optionally filtered by type)
- `GET /api/templates/{id}`: Get template details including preview URL
- All endpoints require authentication (Authorize attribute)

### 4. Implement DOCX Template Merging

**File**: `src/ApiWorker/Documents/Services/TemplateDocumentGenerator.cs` (new file)

- Create `TemplateDocumentGenerator` class
- Method: `MergeTemplateAsync(Stream templateDocx, TransactionalDocument document, Business business, DocumentTheme theme, DocumentSignatureRender signature)`
- Load DOCX from blob storage using template.BlobPath
- Replace placeholders:
  - `{BusinessName}`, `{BusinessPhone}`, `{BusinessEmail}`, `{BusinessAddress}`
  - `{DocumentType}`, `{DocumentNumber}`, `{DocumentDate}`, `{DueDate}`
  - `{CustomerName}`, `{CustomerPhone}`, `{CustomerEmail}`, `{CustomerAddress}`
  - `{LineItems}` → Generate table with items
  - `{Subtotal}`, `{Tax}`, `{Total}`, `{Currency}`
  - `{Notes}`, `{Reference}`
  - `{Signature}` → Embed signature image if available
- Return merged DOCX stream
- Use DocumentFormat.OpenXml for placeholder replacement

### 5. Update DocumentService to Use Templates

**File**: `src/ApiWorker/Documents/Services/DocumentService.cs`

- Inject `ITemplateService` and `TemplateDocumentGenerator`
- Modify `RenderAndUploadAsync`:
  - If `document.TemplateId` is provided:
    - Load template from database
    - Download DOCX from blob storage
    - Merge using `TemplateDocumentGenerator.MergeTemplateAsync`
    - Generate PDF from merged DOCX (or use QuestPDF with template theme)
    - Generate preview image
  - If no `TemplateId`: Use existing programmatic generation (OpenXmlDocumentGenerator, QuestPdfDocumentGenerator)
- Update `ApplyThemeAsync` to load theme from template.ThemeJson when template is selected

### 6. Create Template Seeding Script

**File**: `scripts/seed-templates.sh` (new file)

- Create 6 DOCX template files programmatically or use pre-made templates
- Templates:
  - Invoice: "Modern Blue" (blue theme), "Classic Green" (green theme)
  - Receipt: "Elegant Purple" (purple theme), "Bold Orange" (orange theme)
  - Quotation: "Professional Gray" (gray theme), "Vibrant Teal" (teal theme)
- For each template:
  - Generate DOCX with placeholders using OpenXML
  - Upload to blob storage using BlobStorageService
  - Create database record (BusinessId = null, IsDefault = false for first of each type)
  - Generate preview image (PNG) using QuestPDF with sample data
  - Upload preview to blob storage
  - Update template record with PreviewBlobUrl
- Use appsettings.json for connection strings and blob storage config

### 7. Generate Preview Images

**File**: `src/ApiWorker/Documents/Services/TemplatePreviewGenerator.cs` (new file)

- Create `TemplatePreviewGenerator` class
- Method: `GeneratePreviewAsync(Template template, CancellationToken ct)`
- Create sample document data (mock business, customer, line items)
- Render using QuestPDF with template's theme
- Export as PNG image
- Return image bytes for upload to blob storage

### 8. Register Services in DI

**File**: `src/ApiWorker/Documents/Extensions/DocumentServiceCollectionExtensions.cs`

- Add `services.AddScoped<ITemplateService, TemplateService>();`
- Add `services.AddScoped<TemplateDocumentGenerator>();`
- Add `services.AddScoped<TemplatePreviewGenerator>();`

### 9. Create Template Validators

**File**: `src/ApiWorker/Documents/Validators/TemplateValidators.cs` (new file)

- `ListTemplatesRequestValidator`: Validate type filter (optional, must be valid DocumentType)

### 10. Add Template Mappings

**File**: `src/ApiWorker/Documents/Mappings/DocumentMappings.cs`

- Add AutoMapper profile: `Template` → `TemplateDto`
- Map ThemeJson to DocumentThemeDto

### 11. Update Database Context

**File**: `src/ApiWorker/Data/ApplicationDbContext.cs`

- Ensure `DocumentTemplates` DbSet is properly configured (already exists)

## Template Structure

### DOCX Placeholder Format

Templates use placeholders that get replaced:

- Business: `{BusinessName}`, `{BusinessPhone}`, `{BusinessEmail}`, `{BusinessAddress}`
- Document: `{DocumentType}`, `{DocumentNumber}`, `{DocumentDate}`, `{DueDate}`
- Customer: `{CustomerName}`, `{CustomerPhone}`, `{CustomerEmail}`, `{CustomerAddress}`
- Items: `{LineItems}` (replaced with formatted table)
- Totals: `{Subtotal}`, `{Tax}`, `{Total}`, `{Currency}`
- Other: `{Notes}`, `{Reference}`, `{Signature}`

### Blob Storage Structure

```
doc-templates/
  global/
    Invoice/
      modern-blue-v1.docx
      classic-green-v1.docx
    Receipt/
      elegant-purple-v1.docx
      bold-orange-v1.docx
    Quotation/
      professional-gray-v1.docx
      vibrant-teal-v1.docx
doc-previews/
  templates/
    {template-id}.png
```

## API Endpoints

### List Templates

```
GET /api/templates?type=Invoice
Authorization: Bearer {token}
Response: { success: true, templates: [...] }
```

### Get Template Details

```
GET /api/templates/{id}
Authorization: Bearer {token}
Response: { success: true, template: { id, name, type, previewUrl, themeJson, ... } }
```

## Mobile Integration

- Mobile app calls `GET /api/templates?type={type}` to fetch available templates
- Displays template preview images (previewUrl) in selection UI
- User selects template → stores templateId
- When creating document, includes `templateId` in request
- API merges document data into selected template DOCX

## Testing

- Run seeding script to create 6 templates
- Verify templates appear in `/api/templates` endpoint
- Create document with templateId and verify merged output
- Create document without templateId (should use programmatic generation)
- Verify preview images are generated and accessible

### To-dos

- [ ] Implement TemplateService class with GetDefaultTemplateAsync, ListTemplatesAsync, GetTemplateAsync, UploadTemplateAsync, and SetDefaultTemplateAsync methods
- [ ] Create TemplateDtos.cs with TemplateDto, ListTemplatesResponse, and GetTemplateResponse
- [ ] Create TemplatesController with GET /api/templates and GET /api/templates/{id} endpoints
- [ ] Create TemplateDocumentGenerator class to merge document data into DOCX templates using OpenXML placeholder replacement
- [ ] Modify DocumentService.RenderAndUploadAsync to use TemplateDocumentGenerator when TemplateId is provided, fallback to programmatic generation otherwise
- [ ] Create TemplatePreviewGenerator class to generate PNG preview images for templates using QuestPDF with sample data
- [ ] Create scripts/seed-templates.sh that generates 6 DOCX templates, uploads to blob storage, creates database records, and generates preview images
- [ ] Register ITemplateService, TemplateDocumentGenerator, and TemplatePreviewGenerator in DocumentServiceCollectionExtensions
- [ ] Create TemplateValidators.cs and add AutoMapper mappings for Template entity to TemplateDto