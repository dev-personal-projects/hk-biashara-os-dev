# Document Signatures & Theming Plan

## Goals
- Allow users to sign transactional documents after generation from the mobile app.
- Persist signature metadata (who, when, signature image) and re-render DOCX/PDF/preview with signature embedded.
- Introduce template theming (colors, fonts, accents) so templates can be customized without touching code.
- Improve preview image clarity by reusing full QuestPDF layout with theme + signature.

## Tasks
1. **Data Model Updates**
   - `Document`: add `SignatureBlobUrl`, `SignedBy`, `SignedAt`, `SignatureNotes`.
   - `Template`: add `ThemeJson` (serialized colors/fonts) and `PreviewBlobUrl` (optional).
   - `TransactionalDocument`: add `AppliedThemeJson` snapshot to store actual theme used.
   - Create EF migration and update configurations.

2. **DTOs & Requests**
   - Add `DocumentThemeDto` and `TemplateThemeDto`.
   - Extend creation DTO responses to include signature/theme info.
   - New `SignDocumentRequest` (DocumentId, SignerName, SignatureBase64/File, Notes, SignedAt).

3. **Services**
   - Implement `ITemplateService` (list/get/default theme) or extend existing stub.
   - Add `IDocumentSignatureService` or methods on `DocumentService` to handle upload.
   - Update `DocumentService` to:
     - Resolve template + theme when creating documents.
     - Store applied theme snapshot.
     - Render docs with theme + signature context.
     - Provide `SignDocumentAsync`.

4. **Rendering**
   - Update `OpenXmlDocumentGenerator` and `QuestPdfDocumentGenerator` to accept `DocumentRenderOptions` (theme colors, fonts, signature image).
   - Embed signature image near totals.
   - Apply theme colors to headers, tables, totals, fonts.
   - Use same QuestPDF layout for preview to ensure details are visible.

5. **Controllers / Endpoints**
   - `DocumentsController`: add `POST /api/documents/{id}/sign` endpoint.
   - Include theme and signature info in response payloads.
   - Validate base64 signature payloads and file size.

6. **Blob Storage**
   - Store signatures in `document-signatures/{businessId}/` folder.
   - Optionally store template preview images for faster mobile display.

7. **README & Docs**
   - Document signing API usage.
   - Describe theming payloads and testing instructions for all document types.

## Open Questions
- Should template theming be free-form JSON or strongly-typed columns?
- Support for multiple signatures (customer + business) in future? For now single signature.
- Should we lock document after signing? (Assume yes: update status to Signed.)


