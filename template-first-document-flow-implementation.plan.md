<!-- 7d3d4ee3-c16b-449b-9ce2-08bc6d34ad20 c1ac2193-39a8-430b-b8bb-20ad8df9a31e -->
# Template-First Document Flow Implementation

## Overview

Implement template-first flow: User selects document type → selects template → fills form → creates document using selected template. Templates are stored in blob storage, metadata in database, and users can own/customize templates.

## Current State

- `ITemplateService` interface exists but no implementation
- `Template` entity exists with BusinessId, Type, Name, Version, BlobPath, FieldsJson, IsDefault
- `TemplateId` field exists in DTOs but not used
- Document generation is programmatic (OpenXmlDocumentGenerator creates from scratch)
- No template management endpoints

## Implementation Plan

### 1. Implement TemplateService

**File**: `src/ApiWorker/Documents/Services/TemplateService.cs` (new file)

- Implement `ITemplateService` interface
- `GetDefaultTemplateAsync`: Query database for default template per business+type
- `ListTemplatesAsync`: Return all templates for business (optionally filtered by type), include global templates (BusinessId = null)
- `UploadTemplateAsync`: 
  - Accept DOCX file stream
  - Upload to blob storage (container: `doc-templates`)
  - Save metadata to database (BusinessId, Type, Name, Version, BlobPath, IsDefault)
  - Extract template fields (placeholders) from DOCX if needed
- `SetDefaultTemplateAsync`: Update IsDefault flag (ensure only one default per business+type)

### 2. Create Template DTOs

**File**: `src/ApiWorker/Documents/DTOs/TemplateDtos.cs` (new file)

- `TemplateDto`: id, businessId, type, name, version, blobUrl, isDefault, createdAt
- `ListTemplatesResponse`: success, message, templates[]
- `UploadTemplateRequest`: type, name, file (multipart/form-data)
- `SetDefaultTemplateRequest`: templateId

### 3. Create TemplatesController

**File**: `src/ApiWorker/Documents/Controllers/TemplatesController.cs` (new file)

- `GET /api/templates?type={Invoice|Receipt|Quotation}`: List templates for current business
- `GET /api/templates/{id}`: Get template details
- `POST /api/templates`: Upload new template (multipart/form-data)
- `PUT /api/templates/{id}/default`: Set template as default
- `DELETE /api/templates/{id}`: Delete template (if owned by business)

### 4. Integrate Templates with Document Generation

**File**: `src/ApiWorker/Documents/Services/DocumentService.cs`

- Inject `ITemplateService` into DocumentService
- Modify `RenderAndUploadAsync`:
  - If `TemplateId` provided: Load template from blob storage, merge document data into template
  - If no `TemplateId`: Use current programmatic generation (fallback)
  - Support template placeholders: {BusinessName}, {DocumentNumber}, {CustomerName}, {LineItems}, {Total}, etc.

**File**: `src/ApiWorker/Documents/Services/TemplateService.cs` (rename/refactor)

- Create `TemplateDocumentGenerator` class
- Method: `GenerateFromTemplate(TransactionalDocument, Business, Template)`
- Load template DOCX from blob storage
- Replace placeholders with actual data
- Return merged document stream

### 5. Update Document Generation Logic

**File**: `src/ApiWorker/Documents/Services/DocumentService.cs`

- In `CreateDocumentManuallyAsync` and `CreateDocumentFromVoiceAsync`:
  - If `request.TemplateId` is provided, use template-based generation
  - If not provided, use default template for business+type
  - If no default exists, fallback to programmatic generation

### 6. Register TemplateService in DI

**File**: `src/ApiWorker/Documents/Extensions/DocumentServiceCollectionExtensions.cs`

- Add `services.AddScoped<ITemplateService, TemplateService>();`

### 7. Create Template Validators

**File**: `src/ApiWorker/Documents/Validators/TemplateValidators.cs` (new file)

- `UploadTemplateRequestValidator`: Validate type, name, file format (DOCX only)
- `SetDefaultTemplateRequestValidator`: Validate templateId

### 8. Add Template Mappings

**File**: `src/ApiWorker/Documents/Mappings/DocumentMappings.cs`

- Add AutoMapper profile for Template → TemplateDto

## Mobile App Flow

### Step 1: User selects document type

```
GET /api/templates?type=Invoice
Response: List of available templates (business templates + global templates)
```

### Step 2: User selects template

```
Mobile app shows template previews/names
User selects template → stores templateId
```

### Step 3: User fills form

```
Mobile app shows form (customer, line items, etc.)
```

### Step 4: Create document

```
POST /api/documents
{
  "type": "Invoice",
  "templateId": "selected-template-id",  // NEW: Include selected template
  "customer": {...},
  "lines": [...]
}
```

## Database Considerations

- Templates table already exists (from migrations)
- Ensure indexes on (BusinessId, Type, IsDefault) for fast queries
- Global templates: BusinessId = null (available to all businesses)

## Template Storage Structure

```
Blob Storage:
  doc-templates/
    {businessId}/
      Invoice/
        template-name-v1.docx
        template-name-v2.docx
      Receipt/
        ...
    global/
      Invoice/
        default-invoice.docx
      Receipt/
        ...
```

## Template Placeholder Format

Templates use placeholders that get replaced:

- `{BusinessName}`, `{BusinessPhone}`, `{BusinessEmail}`
- `{DocumentType}`, `{DocumentNumber}`, `{DocumentDate}`
- `{CustomerName}`, `{CustomerPhone}`, `{CustomerEmail}`
- `{LineItems}` (table with items)
- `{Subtotal}`, `{Tax}`, `{Total}`
- `{Notes}`, `{Reference}`

## Testing

- Upload template for Invoice type
- List templates filtered by type
- Create document with template
- Create document without template (fallback to programmatic)
- Set default template
- Verify template ownership (users can only modify their own templates)

### To-dos

- [ ] Implement TemplateService class with GetDefaultTemplateAsync, ListTemplatesAsync, UploadTemplateAsync, SetDefaultTemplateAsync methods
- [ ] Create TemplateDtos.cs with TemplateDto, ListTemplatesResponse, UploadTemplateRequest, SetDefaultTemplateRequest
- [ ] Create TemplatesController with GET /api/templates, GET /api/templates/{id}, POST /api/templates, PUT /api/templates/{id}/default, DELETE /api/templates/{id} endpoints
- [ ] Create TemplateDocumentGenerator class to merge document data into template DOCX files using OpenXML
- [ ] Modify DocumentService to use ITemplateService and TemplateDocumentGenerator when TemplateId is provided
- [ ] Register ITemplateService in DocumentServiceCollectionExtensions
- [ ] Create TemplateValidators.cs with FluentValidation rules for template upload and default setting
- [ ] Add AutoMapper mappings for Template entity to TemplateDto in DocumentMappings.cs