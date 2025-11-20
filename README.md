# BiasharaOS API Worker

**Voice-first, offline-ready business operations platform for SMEs** — Create receipts/invoices by voice, auto-generate orders, discover suppliers, and get AI insights in English and Swahili.

---

## 📋 Table of Contents
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Authentication & Onboarding](#authentication--onboarding)
- [API Documentation](#api-documentation)
- [Mobile Integration](#mobile-integration)
- [Configuration](#configuration)
- [Documents Module](#documents-module)
- [Development](#development)

---

## Features

### 📄 Smart Documents (✅ Implemented)
- Create **Invoices, Receipts, and Quotations** with manual or voice input
- **Voice to Document** (English/Swahili): transcript → AI extraction → validate → DOCX/PDF
- **Manual document builder** with customer and line items
- Export **DOCX (OpenXML)** and **PDF (QuestPDF)**
- Document versioning and status tracking (Draft, Final, Voided)
- **Custom theming & signatures**: mobile users pick colors/fonts, sign on-screen, and get regenerated DOCX/PDF/preview with embedded signature
- Auto-numbering per document type: `INV-{yyyyMM}-{####}`, `RCPT-{yyyyMM}-{####}`, `QUO-{yyyyMM}-{####}`
- **Coming Soon**: Ledgers, Balance Sheets, OCR scan

### 🔊 Voice Processing (✅ Implemented)
- **Azure Speech SDK**: Speech-to-Text for `en-KE` and `sw-KE`
- **Azure OpenAI**: Function-calling to extract structured document fields from transcripts
- **Cosmos DB**: Transcription storage with business-level partitioning
- Phrase-list boosting for product/customer names
- **Offline fallback**: Mobile app handles offline transcription (Whisper/Vosk)

### 🔐 Authentication & Multi-Business (✅ Implemented)
- User signup with auto-login (JWT tokens)
- Email/password and Google OAuth authentication
- Business registration with logo upload
- Multi-business support (users can own/switch between businesses)
- Session initialization and management
- Onboarding status tracking

### 🔄 Auto Reordering (🚧 Planned)
- AI-powered stock forecasting
- Smart reorder suggestions
- One-tap Purchase Orders
- Industry-specific presets

### 🗺️ Supplier Discovery (🚧 Planned)
- Find nearby distributors on map
- Supplier scoring (price + reliability + distance)
- Contact via call/WhatsApp/email

### 🤖 AI Copilot (🚧 Planned)
- Sales forecasting per product
- Smart reminders and follow-ups
- Meeting scheduling assistant
- Explainable business insights

### 📱 Offline-First Architecture
- Mobile app transcribes offline (Whisper/Vosk)
- Background sync when online
- Full Swahili (`sw-KE`) and English (`en-KE`) support

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 9, Clean Architecture, ASP.NET Core Web API |
| **Database** | Azure SQL Database (EF Core), Azure Cosmos DB (transcripts) |
| **AI** | Azure OpenAI (GPT-4o-mini for extraction), Azure Speech to Text (en-KE/sw-KE) |
| **Documents** | OpenXML (DOCX generation), QuestPDF (PDF generation) |
| **Storage** | Azure Blob Storage (documents, templates, logos), Azure Key Vault (secrets) |
| **Authentication** | JWT Bearer tokens, Supabase (user management), Google OAuth |
| **Integrations** | (Sharing handled client-side) |
| **Validation** | FluentValidation, Data Annotations |
| **Mapping** | AutoMapper |
| **Deployment** | Docker, Azure Container Apps, Bicep (IaC) |
| **Mobile** | Flutter (separate repo; offline STT fallback) |

---

## Quick Start

```bash
# Setup database
./migrate.sh add "InitialCreate"
./migrate.sh update

# Run API
dotnet run --project src/ApiWorker

# (Optional) Seed default DOCX templates + previews
./scripts/seed-templates.sh
```

**Local API**: `http://localhost:5052/api`  
**OpenAPI/Swagger**: `http://localhost:5052/openapi/v1.json` (Development only)

> **Why seed templates?**  
> The seeding script loads your Azure Key Vault secret `ConnectionStrings--BlobStorage`, creates the `doc-templates`, `docs`, and `doc-previews` containers (if needed), and uploads six global templates (Invoice, Receipt, Quotation variants). This gives users ready-made layouts they can apply immediately from the mobile app or API.

---

## Architecture

```
┌─────────────┐
│ Flutter App │
└──────┬──────┘
       │ HTTP/REST
       ↓
┌─────────────────────────────────────┐
│      .NET 9 API Worker              │
│  ┌───────────────────────────────┐  │
│  │  Authentication Module        │  │
│  │  - JWT, Supabase, OAuth       │  │
│  └───────────────────────────────┘  │
│  ┌───────────────────────────────┐  │
│  │  Documents Module             │  │
│  │  - Invoices, Templates        │  │
│  └───────────────────────────────┘  │
│  ┌───────────────────────────────┐  │
│  │  Speech Module                │  │
│  │  - Azure Speech SDK           │  │
│  └───────────────────────────────┘  │
└──────┬──────────────────────────┬───┘
       │                          │
       ↓                          ↓
┌──────────────┐          ┌──────────────┐
│ Azure SQL DB │          │ Cosmos DB    │
│ (Documents,  │          │ (Transcripts)│
│  Users,      │          │              │
│  Businesses) │          └──────────────┘
└──────┬───────┘
       │
       ↓
┌─────────────────┐
│ Azure Blob      │
│ Storage         │
│ (DOCX, PDF,     │
│  Logos)         │
└─────────────────┘
       │
       ↓
┌─────────────────┐
│ Azure OpenAI    │
│ (Extraction)    │
└─────────────────┘
```

### Core Flows
1. **Voice Document**: Mobile transcribes → API extracts (Azure OpenAI) → Validates → Generates DOCX/PDF → Stores in Blob → Returns URLs
2. **Manual Document**: User fills form → API validates → Generates DOCX/PDF → Stores in Blob → Returns URLs
3. **Authentication**: Signup → Auto-login → Business registration → Session initialization → Multi-business switching

---

## Authentication & Onboarding

### User Onboarding Flow

```
1. Signup → Auto-login (JWT issued immediately)
2. Business Registration (required, authenticated)
3. Complete Onboarding → Full system access
```

### Onboarding Status
- `AccountCreated (1)`: User signed up, needs to register business
- `BusinessRegistered (2)`: Business created, onboarding in progress
- `OnboardingComplete (3)`: Full access granted

---

## API Documentation

### Base URL
```
Local: http://localhost:5052/api
Production: https://your-api.azurewebsites.net/api
Docker: http://localhost:8000/api
```

### API Endpoints Summary

#### Authentication (`/api/auth`)
- `POST /auth/signup` - Create account (auto-login)
- `POST /auth/login` - Email/password login
- `POST /auth/google` - Google OAuth login
- `POST /auth/business/register` - Register business (authenticated)
- `POST /auth/initialize-session` - Initialize user session (authenticated)
- `GET /auth/businesses` - List user's businesses (authenticated)
- `POST /auth/businesses/switch` - Switch active business (authenticated)
- `GET /auth/health` - Health check

#### Documents (`/api/documents`)
- `POST /api/documents` - Create document manually (Invoice, Receipt, or Quotation)
- `POST /api/documents/voice` - Create document from voice transcript
- `GET /api/documents/{id}` - Get document details
- `PUT /api/documents/{id}` - Update document (draft only)
- `GET /api/documents` - List documents with filters (type, status, date range, search)
- `POST /api/documents/{id}/sign` - Upload mobile signature + regenerate DOCX/PDF/preview

#### Templates (`/api/templates`)
- `GET /api/templates` - List all available templates (optionally filtered by document type)
- `GET /api/templates/{id}` - Get template details by ID

### Authentication Endpoints

#### 1. Signup (with Auto-Login)
```http
POST /auth/signup
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "county": "Nairobi"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Account created successfully! Please register your business to continue.",
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here",
  "user": {
    "id": "uuid-here",
    "fullName": "John Doe",
    "email": "john@example.com",
    "county": "Nairobi",
    "hasBusiness": false,
    "onboardingStatus": 1
  }
}
```

#### 2. Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Welcome back!" | "Please register your business to continue.",
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here",
  "user": {
    "id": "uuid-here",
    "fullName": "John Doe",
    "email": "john@example.com",
    "county": "Nairobi",
    "hasBusiness": true,
    "onboardingStatus": 3
  }
}
```

#### 3. Google OAuth
```http
POST /auth/google
Content-Type: application/json

{
  "idToken": "google-id-token-from-supabase",
  "county": "Nairobi"
}
```

#### 4. Business Registration (Authenticated)
```http
POST /auth/business/register
Authorization: Bearer {jwt-token}
Content-Type: multipart/form-data

{
  "name": "Mama Mboga Shop",
  "category": "retail",
  "county": "Nairobi",
  "town": "Westlands",
  "email": "shop@example.com",
  "phone": "+254712345678",
  "logo": <file> (optional)
}
```

**Response:**
```json
{
  "success": true,
  "message": "Business registered successfully! You can now start using BiasharaOS.",
  "businessId": "business-uuid-here"
}
```

#### 5. Initialize Session (Authenticated)
```http
POST /auth/initialize-session
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "businessId": "business-uuid-here" (optional)
}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "id": "user-uuid",
    "fullName": "John Doe",
    "email": "john@example.com"
  },
  "currentBusiness": {
    "id": "business-uuid",
    "name": "Mama Mboga Shop"
  },
  "businesses": [
    {
      "id": "business-uuid",
      "name": "Mama Mboga Shop",
      "category": "retail"
    }
  ]
}
```

#### 6. List User Businesses (Authenticated)
```http
GET /auth/businesses
Authorization: Bearer {jwt-token}
```

**Response:**
```json
{
  "success": true,
  "businesses": [
    {
      "id": "business-uuid",
      "name": "Mama Mboga Shop",
      "category": "retail",
      "county": "Nairobi",
      "isActive": true
    }
  ]
}
```

#### 7. Switch Business (Authenticated)
```http
POST /auth/businesses/switch
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "businessId": "business-uuid-here"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Business switched successfully",
  "businessId": "business-uuid-here"
}
```

### Error Responses
```json
{
  "success": false,
  "message": "User-friendly error message here"
}
```

### HTTP Status Codes
- `200 OK`: Success
- `400 Bad Request`: Invalid input or validation error
- `401 Unauthorized`: Missing/invalid token or authentication required
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### Valid Counties (Kenya)
Nairobi, Mombasa, Kisumu, Nakuru, Eldoret, Thika, Malindi, Kitale, and 40+ more Kenyan counties

---

## Mobile Integration

### Flutter Integration Guide

#### 1. Signup Flow
```dart
// User signs up
final response = await http.post(
  Uri.parse('$baseUrl/auth/signup'),
  body: jsonEncode({
    'fullName': 'John Doe',
    'email': 'john@example.com',
    'password': 'SecurePass123!',
    'county': 'Nairobi'
  }),
);

// Store tokens immediately (auto-login)
final data = jsonDecode(response.body);
await secureStorage.write(key: 'accessToken', value: data['accessToken']);
await secureStorage.write(key: 'refreshToken', value: data['refreshToken']);

// Check onboarding status
if (data['user']['onboardingStatus'] == 1) {
  // Navigate to business registration screen
}
```

#### 2. Business Registration Flow
```dart
// User registers business (authenticated)
final token = await secureStorage.read(key: 'accessToken');
final response = await http.post(
  Uri.parse('$baseUrl/auth/business/register'),
  headers: {'Authorization': 'Bearer $token'},
  body: jsonEncode({
    'name': 'Mama Mboga Shop',
    'category': 'retail',
    'county': 'Nairobi',
    // ... other fields
  }),
);

// Navigate to main app after successful registration
```

#### 3. Token Management
```dart
// Include token in all authenticated requests
final token = await secureStorage.read(key: 'accessToken');
final response = await http.get(
  Uri.parse('$baseUrl/documents/invoices'),
  headers: {'Authorization': 'Bearer $token'},
);

// Handle 401 errors by refreshing token or re-authenticating
```

#### 4. Template-first + Signature Flow (mobile UX)
1. **Create Document Screen**
   - User taps “Create Document”.
   - Present tabs for `Invoice | Receipt | Quotation`.
   - Optional theme picker (color chips + font dropdown) → map to `theme` object.
2. **Template Selection (optional)**
   - If you maintain template IDs locally (or via future template endpoints), set `templateId` in the payload so the API applies business-approved colors/layout.
3. **Preview & Confirmation**
   - After hitting `POST /api/documents`, render the returned `urls.previewUrl` directly in the app (PNG now mirrors the final PDF, including colors + signature placeholders).
4. **Signature Capture**
   - Show a “Sign Document” CTA that opens a canvas (e.g., `SignatureController` or `scribble` package).
   - Convert the drawing to PNG → `base64Encode`.
   - Call `POST /api/documents/{id}/sign` with `signatureBase64` and `signerName`.
5. **Download / Share**
   - Use `urls.pdfUrl` for sharing.
   - Because the server re-generates the preview, you can refresh the preview in-app without re-creating the document.

```dart
final signature = await controller.toPngBytes();
final payload = {
  'documentId': docId,
  'signerName': currentUser.name,
  'signatureBase64': base64Encode(signature!),
  'notes': 'Signed on delivery'
};
await http.post(
  Uri.parse('$baseUrl/documents/$docId/sign'),
  headers: {'Authorization': 'Bearer $token', 'Content-Type': 'application/json'},
  body: jsonEncode(payload),
);
```

### Supabase OAuth Flow (Optional)
1. Use Supabase Auth SDK with Google provider
2. Extract ID token from Supabase session
3. Send to `/auth/google` endpoint with county
4. Store returned JWT tokens

---

## Configuration

### Local Development (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Default": "Server=tcp:localhost,1433;Initial Catalog=BiasharaOS;Integrated Security=true;",
    "BlobStorage": "UseDevelopmentStorage=true"
  },
  "Cosmos": {
    "Endpoint": "https://localhost:8081",
    "Key": "local-development-key",
    "Database": "biasharaos-db",
    "Containers": {
      "Transcripts": "speech-transcripts"
    },
    "CreateIfNotExists": true
  },
  "Auth": {
    "Supabase": {
      "Url": "https://your-project.supabase.co",
      "Key": "your-supabase-anon-key"
    },
    "Jwt": {
      "SecretKey": "your-jwt-secret-key-min-32-chars-required",
      "Issuer": "BiasharaOS",
      "Audience": "BiasharaOS-Users",
      "ExpiryHours": 1
    }
  },
  "Documents": {
    "DefaultCurrency": "KES",
    "DefaultLocale": "en-KE",
    "AllowedCurrencies": ["KES", "USD", "EUR"],
    "Numbering": {
      "InvoicePrefix": "INV-",
      "ReceiptPrefix": "RCPT-",
      "QuotationPrefix": "QUO-",
      "Pattern": "yyyyMM-####"
    },
    "Storage": {
      "TemplatesContainer": "doc-templates",
      "DocumentsContainer": "docs",
      "PreviewsContainer": "doc-previews",
      "PathFormat": "{businessId}/{type}/{yyyy}/{MM}/{fileName}"
    }
  },
  "Speech": {
    "Provider": "AzureSpeech",
    "Region": "northeurope",
    "Key": "your-azure-speech-key",
    "DefaultLocale": "en-KE",
    "Locales": ["en-KE", "sw-KE"],
    "UseNumeralNormalization": true
  },
  "Voice": {
    "Provider": "AzureSpeech",
    "Region": "northeurope",
    "Key": "your-azure-speech-key",
    "DefaultLocale": "en-KE",
    "Locales": ["en-KE", "sw-KE"],
    "UseNumeralNormalization": true,
    "PhraseList": ["M-Pesa", "Unga", "Mama Mboga", "Sukuma Wiki"]
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-azure-openai.openai.azure.com/",
    "ApiKey": "your-aoai-key",
    "Deployment": "gpt-4o-mini"
  }
}
```

### Production Configuration

For production, use **Azure Key Vault** for secrets. The application automatically loads configuration from Key Vault when `AddKeyVaultConfiguration()` is called in `Program.cs`.

Key Vault secrets should match the configuration keys above (e.g., `ConnectionStrings--Default`, `Auth--Jwt--SecretKey`).

---

## Documents Module

### Module Structure
```
src/ApiWorker/Documents/
├── Controllers/          # API endpoints (DocumentsController)
├── DTOs/                 # Request/Response models (TransactionalDocumentDtos, DocumentDtos, ExtractedDocumentData)
├── Core/Entities/        # Database models (Document, TransactionalDocument, Invoice, Receipt, Quotation)
├── Services/             # Business logic (DocumentService, VoiceIntentService, TemplateService)
├── Interfaces/           # Service contracts (IDocumentService, IVoiceIntentService, ITemplateService)
├── Validators/           # FluentValidation rules (DocumentValidators)
├── Mappings/             # AutoMapper profiles (DocumentMappings)
├── Settings/             # Configuration models (DocumentSettings, AzureOpenAISettings)
├── Extensions/           # DI registration (DocumentServiceCollectionExtensions)
└── Templates/            # Document generators (QuestPdfDocumentGenerator, OpenXmlDocumentGenerator)
```

### Supported Document Types
- ✅ **Invoices** - Fully implemented (manual and voice creation)
- ✅ **Receipts** - Fully implemented (manual and voice creation)
- ✅ **Quotations** - Fully implemented (manual and voice creation)
- 🚧 **Ledgers** - Planned
- 🚧 **Balance Sheets** - Planned

### Document Flows

#### 1. Voice Document
```
Speech → Transcript → Extract fields (AI) → Validate → Render (DOCX/PDF) → Store → Return URLs
```

#### 2. Manual Document
```
Fill form → Validate → Render (DOCX/PDF) → Store → Return URLs
```

### Documents API Endpoints

#### 1. Create Document Manually
```http
POST /api/documents
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "businessId": "45bc8336-f8b7-4bdc-aaf2-1dfbea5dbaa4",
  "type": "Invoice",
  "templateId": "a1aa7f6c-6e8c-4cd0-a2a5-08dd12345678",
  "theme": {
    "primaryColor": "#0F172A",
    "secondaryColor": "#475569",
    "accentColor": "#F97316",
    "fontFamily": "Poppins"
  },
  "customer": {
    "name": "Mega Logistics",
    "phone": "+254712345678",
    "email": "john@example.com",
    "addressLine1": "123 Main Street",
    "addressLine2": "Suite 100",
    "city": "Nairobi",
    "country": "Kenya"
  },
  "lines": [
    {
      "name": "Maize Flour 2kg",
      "description": "Premium quality maize flour",
      "quantity": 90,
      "unitPrice": 180.00,
      "taxRate": 0.16
    },
    {
      "name": "Cooking Oil 1L",
      "description": "Refined vegetable oil",
      "quantity": 90,
      "unitPrice": 350.00,
      "taxRate": 0.16
    }
  ],
  "currency": "KES",
  "issuedAt": "2025-01-15T00:00:00Z",
  "dueAt": "2025-02-15T00:00:00Z",
  "notes": "Payment due within 30 days",
  "reference": "PO-2025-001"
}
```

- `templateId` is optional; when provided the API validates that the business owns/has access to the template.
- `theme` is optional and lets the mobile app override colors/fonts without uploading a new template.

**Minimal Example (Required Fields Only):**
```json
{
  "businessId": "45bc8336-f8b7-4bdc-aaf2-1dfbea5dbaa4",
  "customer": {
    "name": "Mega Logistics",
    "phone": "+254712345678"
  },
  "lines": [
    {
      "name": "Maize Flour 2kg",
      "quantity": 90,
      "unitPrice": 180.00
    },
    {
      "name": "Cooking Oil 1L",
      "quantity": 90,
      "unitPrice": 350.00,
      "taxRate": 0.16
    }
  ],
  "currency": "KES"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Invoice created successfully",
  "documentId": "302a871c-0c8f-4d65-17ed-08de236d7832",
  "documentNumber": "INV-202511-0006",
  "urls": {
    "docxUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202511-0006.docx",
    "pdfUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202511-0006.pdf",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/INV-202511-0006.png"
  },
  "signature": null
}
```

**Create Receipt Example:**
```json
{
  "businessId": "45bc8336-f8b7-4bdc-aaf2-1dfbea5dbaa4",
  "type": "Receipt",
  "theme": {
    "primaryColor": "#065F46",
    "secondaryColor": "#047857",
    "accentColor": "#10B981",
    "fontFamily": "Inter"
  },
  "customer": {
    "name": "Jane Wanjiku",
    "phone": "+254712345678",
    "email": "jane@example.com"
  },
  "lines": [
    {
      "name": "Sugar 2kg",
      "quantity": 5,
      "unitPrice": 250.00,
      "taxRate": 0.16
    },
    {
      "name": "Rice 1kg",
      "quantity": 3,
      "unitPrice": 180.00,
      "taxRate": 0.16
    }
  ],
  "currency": "KES",
  "notes": "Payment received in full"
}
```

**Create Quotation Example:**
```json
{
  "businessId": "45bc8336-f8b7-4bdc-aaf2-1dfbea5dbaa4",
  "type": "Quotation",
  "templateId": "f3d5d0b8-4cd1-4a9a-9910-08dd12349999",
  "customer": {
    "name": "ABC Company Ltd",
    "phone": "+254712345678",
    "email": "contact@abccompany.com",
    "addressLine1": "456 Business Park",
    "city": "Nairobi",
    "country": "Kenya"
  },
  "lines": [
    {
      "name": "Office Supplies Package",
      "description": "Complete office stationery set",
      "quantity": 1,
      "unitPrice": 15000.00,
      "taxRate": 0.16
    }
  ],
  "currency": "KES",
  "issuedAt": "2025-01-15T00:00:00Z",
  "dueAt": "2025-01-30T00:00:00Z",
  "notes": "Valid for 15 days. Prices subject to change.",
  "reference": "QUO-2025-001"
}
```

#### 2. Create Document from Voice
```http
POST /api/documents/voice
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Invoice",
  "transcriptText": "Create invoice for John Kamau. Three bags of maize flour at 180 shillings each and two bottles of cooking oil at 350 shillings each",
  "locale": "en-KE"
}
```

- Optional fields:
  - `templateId`
  - `theme` (same structure as manual request)

**Create Receipt from Voice (English):**
```json
{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Receipt",
  "transcriptText": "Create receipt for Jane Wanjiku. Five bags of sugar at 250 shillings each and three bags of rice at 180 shillings each. Payment received in full",
  "locale": "en-KE"
}
```

**Create Quotation from Voice (English):**
```json
{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Quotation",
  "transcriptText": "Create quotation for ABC Company. One office supplies package at 15000 shillings. Valid for 15 days",
  "locale": "en-KE"
}
```

**Swahili Examples:**
```json
{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Invoice",
  "transcriptText": "Tengeneza ankara kwa John Kamau. Mifuko mitatu ya unga kwa shilingi 180 kila moja na chupa mbili za mafuta kwa shilingi 350 kila moja",
  "locale": "sw-KE"
}
```

```json
{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Receipt",
  "transcriptText": "Tengeneza risiti kwa Jane Wanjiku. Mifuko mitano ya sukari kwa shilingi 250 kila moja",
  "locale": "sw-KE"
}
```

**Response Examples:**

**Invoice Response:**
```json
{
  "success": true,
  "message": "Invoice created from voice successfully",
  "documentId": "document-uuid",
  "documentNumber": "INV-202501-0002",
  "urls": {
    "docxUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202501-0002.docx",
    "pdfUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202501-0002.pdf",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/INV-202501-0002.png"
  }
}
```

**Receipt Response:**
```json
{
  "success": true,
  "message": "Receipt created from voice successfully",
  "documentId": "document-uuid",
  "documentNumber": "RCPT-202501-0001",
  "urls": {
    "docxUrl": "https://biasharaos.blob.core.windows.net/receipts/RCPT-202501-0001.docx",
    "pdfUrl": "https://biasharaos.blob.core.windows.net/receipts/RCPT-202501-0001.pdf",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/RCPT-202501-0001.png"
  }
}
```

**Quotation Response:**
```json
{
  "success": true,
  "message": "Quotation created from voice successfully",
  "documentId": "document-uuid",
  "documentNumber": "QUO-202501-0001",
  "urls": {
    "docxUrl": "https://biasharaos.blob.core.windows.net/quotations/QUO-202501-0001.docx",
    "pdfUrl": "https://biasharaos.blob.core.windows.net/quotations/QUO-202501-0001.pdf",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/QUO-202501-0001.png"
  }
}
```

#### 3. Sign Document (after rendering)
```http
POST /api/documents/{documentId}/sign
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "documentId": "302a871c-0c8f-4d65-17ed-08de236d7832",
  "signerName": "Jane Wanjiku",
  "signatureBase64": "iVBORw0KGgoAAAANSUhEUgAAAUAAAABACAYAAAD+08YQAAAAA...",
  "notes": "Signed on delivery"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Invoice signed successfully",
  "documentId": "302a871c-0c8f-4d65-17ed-08de236d7832",
  "documentNumber": "INV-202511-0006",
  "urls": {
    "docxUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202511-0006.docx",
    "pdfUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202511-0006.pdf",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/INV-202511-0006.png"
  },
  "signature": {
    "isSigned": true,
    "signedBy": "Jane Wanjiku",
    "signedAt": "2025-01-16T08:15:00Z",
    "signatureUrl": "https://biasharaos.blob.core.windows.net/document-signatures/INV-202511-0006.png",
    "notes": "Signed on delivery"
  }
}
```

**Tips**
- Capture the signature on the Flutter canvas (`ui.Image → PNG → base64`) and send as `signatureBase64`.
- `signedAt` is automatically set by the server (UTC) when the document is signed.
- The API regenerates DOCX, PDF, and preview so signatures show up everywhere (mobile preview now matches the PDF layout).
- Signed documents move to the `Signed` status; edit endpoints will block changes until you void or clone the document.

#### 4. Get Document Details
```http
GET /api/documents/{documentId}
Authorization: Bearer {jwt-token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Document retrieved successfully",
  "document": {
    "id": "document-uuid",
    "type": "Invoice",
    "number": "INV-202501-0001",
    "status": "Draft",
    "currency": "KES",
    "customer": {
      "name": "John Kamau",
      "phone": "+254712345678",
      "email": "john@example.com"
    },
    "lines": [
      {
        "name": "Maize Flour 2kg",
        "quantity": 3,
        "unitPrice": 180.00,
        "taxRate": 0.16
      }
    ],
    "subtotal": 540.00,
    "tax": 86.40,
    "total": 626.40,
    "issuedAt": "2025-01-15T10:30:00Z",
    "dueAt": "2025-02-15T00:00:00Z",
    "notes": "Payment due within 30 days",
    "urls": {
      "docxUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202501-0001.docx",
      "pdfUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202501-0001.pdf",
      "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/INV-202501-0001.png"
    },
    "createdAt": "2025-01-15T10:30:00Z",
    "updatedAt": "2025-01-15T10:30:00Z"
  }
}
```

#### 5. List Documents with Filters
```http
GET /api/documents?page=1&pageSize=20&type=Invoice&status=Draft&searchTerm=John
Authorization: Bearer {jwt-token}
```

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20)
- `type` (Invoice, Receipt, Quotation) - optional filter
- `status` (Draft, Final, Voided) - optional filter
- `fromDate` (ISO 8601) - optional filter
- `toDate` (ISO 8601) - optional filter
- `searchTerm` (searches number and customer name) - optional filter

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Found 15 documents",
  "documents": [
    {
      "id": "invoice-uuid",
      "number": "INV-202501-0001",
      "type": "Invoice",
      "status": "Draft",
      "customerName": "John Kamau",
      "total": 626.40,
      "currency": "KES",
      "issuedAt": "2025-01-15T10:30:00Z"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

#### 6. Update Document (Draft Only)
```http
PUT /api/documents/{documentId}
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "documentId": "document-uuid",
  "customer": {
    "name": "John Kamau Updated",
    "phone": "+254712345678"
  },
  "lines": [
    {
      "name": "Maize Flour 2kg",
      "quantity": 5,
      "unitPrice": 180.00,
      "taxRate": 0.16
    }
  ],
  "notes": "Updated payment terms"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Document updated successfully",
  "documentId": "document-uuid",
  "documentNumber": "INV-202501-0001"
}
```

**Note**: Sharing functionality is handled on the client side (Flutter mobile app). The API provides document URLs that can be shared directly.

### Testing the API

#### Step 1: Signup and Get Token
```bash
curl -X POST http://localhost:5052/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Test User",
    "email": "test@example.com",
    "password": "Test123!",
    "county": "Nairobi"
  }'
```

Save the `accessToken` from response.

#### Step 2: Register Business
```bash
curl -X POST http://localhost:5052/api/auth/business/register \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Shop",
    "category": "retail",
    "county": "Nairobi",
    "town": "Westlands",
    "phone": "+254712345678"
  }'
```

Save the `businessId` from response.

#### Step 3: Create Document

**Create Invoice:**
```bash
curl -X POST http://localhost:5052/api/documents \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Invoice",
    "customer": {
      "name": "John Doe",
      "phone": "+254712345678"
    },
    "lines": [
      {
        "name": "Product A",
        "quantity": 2,
        "unitPrice": 100.00
      }
    ],
    "currency": "KES"
  }'
```

**Create Receipt:**
```bash
curl -X POST http://localhost:5052/api/documents \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Receipt",
    "customer": {
      "name": "Jane Wanjiku",
      "phone": "+254712345678"
    },
    "lines": [
      {
        "name": "Sugar 2kg",
        "quantity": 5,
        "unitPrice": 250.00
      }
    ],
    "currency": "KES"
  }'
```

**Create Quotation:**
```bash
curl -X POST http://localhost:5052/api/documents \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Quotation",
    "customer": {
      "name": "ABC Company Ltd",
      "phone": "+254712345678"
    },
    "lines": [
      {
        "name": "Office Supplies",
        "quantity": 1,
        "unitPrice": 15000.00
      }
    ],
    "currency": "KES"
  }'
```

**Use a Seeded Template**

1. List available templates seeded via the script:
   ```bash
   curl -X GET "http://localhost:5052/api/templates" \
     -H "Authorization: Bearer {your-token}"
   ```
2. Pick the `id` for the template you want (e.g., “Modern Blue Invoice”).
3. Pass it as `templateId` when creating or editing a document. The API will merge the DOCX template with your business data and regenerate DOCX/PDF/preview accordingly.

This lets business owners start with polished layouts without designing templates themselves; the default set mirrors the six styles uploaded by `scripts/seed-templates.sh`.

#### Step 4: Create Document from Voice

**Invoice from Voice:**
```bash
curl -X POST http://localhost:5052/api/documents/voice \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Invoice",
    "transcriptText": "Invoice for Jane with 3 bags of flour at 150 each",
    "locale": "en-KE"
  }'
```

**Receipt from Voice:**
```bash
curl -X POST http://localhost:5052/api/documents/voice \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Receipt",
    "transcriptText": "Receipt for John. Five bags of sugar at 250 each. Payment received",
    "locale": "en-KE"
  }'
```

**Quotation from Voice:**
```bash
curl -X POST http://localhost:5052/api/documents/voice \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Quotation",
    "transcriptText": "Quotation for ABC Company. One office package at 15000. Valid for 15 days",
    "locale": "en-KE"
  }'
```

#### Step 5: List Documents
```bash
# List all documents
curl -X GET "http://localhost:5052/api/documents?page=1&pageSize=10" \
  -H "Authorization: Bearer {your-token}"

# Filter by type (Invoice, Receipt, or Quotation)
curl -X GET "http://localhost:5052/api/documents?page=1&pageSize=10&type=Invoice" \
  -H "Authorization: Bearer {your-token}"

curl -X GET "http://localhost:5052/api/documents?page=1&pageSize=10&type=Receipt" \
  -H "Authorization: Bearer {your-token}"

curl -X GET "http://localhost:5052/api/documents?page=1&pageSize=10&type=Quotation" \
  -H "Authorization: Bearer {your-token}"
```

#### Step 6: Get Document Details
```bash
curl -X GET "http://localhost:5052/api/documents/{document-id}" \
  -H "Authorization: Bearer {your-token}"
```

#### Step 7: Update Document
```bash
curl -X PUT http://localhost:5052/api/documents/{document-id} \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "documentId": "{document-id}",
    "notes": "Updated payment terms"
  }'
```

#### Step 8: Sign Document (mobile signature → server)
```bash
curl -X POST http://localhost:5052/api/documents/{document-id}/sign \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "documentId": "{document-id}",
    "signerName": "Delivery Agent",
    "signatureBase64": "{base64-png}",
    "notes": "Signed on delivery"
  }'
```

Expect the response to include an updated `signature` block plus refreshed DOCX/PDF/preview URLs.

#### Step 9: Create a themed document
```bash
curl -X POST http://localhost:5052/api/documents \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Invoice",
    "theme": {
      "primaryColor": "#0F172A",
      "secondaryColor": "#475569",
      "accentColor": "#F97316",
      "fontFamily": "Poppins"
    },
    "customer": { "name": "Theme Test" },
    "lines": [{ "name": "Brand Kit", "quantity": 1, "unitPrice": 2500 }],
    "currency": "KES"
  }'
```

Preview image now mirrors the QuestPDF layout (full header/table/signature styling), so the mobile app can display exactly what the PDF looks like.

#### Step 10: List Available Templates
```bash
# List all templates
curl -X GET "http://localhost:5052/api/templates" \
  -H "Authorization: Bearer {your-token}"

# Filter templates by document type (Invoice, Receipt, or Quotation)
curl -X GET "http://localhost:5052/api/templates?type=Invoice" \
  -H "Authorization: Bearer {your-token}"

curl -X GET "http://localhost:5052/api/templates?type=Receipt" \
  -H "Authorization: Bearer {your-token}"

curl -X GET "http://localhost:5052/api/templates?type=Quotation" \
  -H "Authorization: Bearer {your-token}"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Found 6 template(s)",
  "templates": [
    {
      "id": "1b351696-f113-4c58-a23a-3afbd27114de",
      "type": "Invoice",
      "name": "Modern Blue",
      "version": 1,
      "blobUrl": "global/Invoice/modern-blue-v1.docx",
      "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/1b351696-f113-4c58-a23a-3afbd27114de.png",
      "theme": {
        "primaryColor": "#1E40AF",
        "secondaryColor": "#3B82F6",
        "accentColor": "#60A5FA",
        "fontFamily": "Inter"
      },
      "isDefault": true,
      "createdAt": "2025-01-15T10:00:00Z"
    },
    {
      "id": "3acba4d8-19af-4a7a-a41d-88488bb1676c",
      "type": "Invoice",
      "name": "Classic Green",
      "version": 1,
      "blobUrl": "global/Invoice/classic-green-v1.docx",
      "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/3acba4d8-19af-4a7a-a41d-88488bb1676c.png",
      "theme": {
        "primaryColor": "#065F46",
        "secondaryColor": "#047857",
        "accentColor": "#10B981",
        "fontFamily": "Poppins"
      },
      "isDefault": false,
      "createdAt": "2025-01-15T10:00:00Z"
    }
  ]
}
```

#### Step 11: Get Template Details
```bash
curl -X GET "http://localhost:5052/api/templates/{template-id}" \
  -H "Authorization: Bearer {your-token}"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Template retrieved successfully",
  "template": {
    "id": "1b351696-f113-4c58-a23a-3afbd27114de",
    "type": "Invoice",
    "name": "Modern Blue",
    "version": 1,
    "blobUrl": "global/Invoice/modern-blue-v1.docx",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/1b351696-f113-4c58-a23a-3afbd27114de.png",
    "theme": {
      "primaryColor": "#1E40AF",
      "secondaryColor": "#3B82F6",
      "accentColor": "#60A5FA",
      "fontFamily": "Inter"
    },
    "isDefault": true,
    "createdAt": "2025-01-15T10:00:00Z"
  }
}
```

#### Step 12: Create Document Using a Template
```bash
# Create invoice using a specific template
curl -X POST http://localhost:5052/api/documents \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Invoice",
    "templateId": "1b351696-f113-4c58-a23a-3afbd27114de",
    "customer": {
      "name": "John Doe",
      "phone": "+254712345678"
    },
    "lines": [
      {
        "name": "Product A",
        "quantity": 2,
        "unitPrice": 100.00
      }
    ],
    "currency": "KES"
  }'
```

**Note**: When `templateId` is provided, the document will be generated using the template's layout and theme. You can still override the theme by providing a `theme` object in the request.

#### Step 13: Create Document Without Template (From Scratch)
```bash
# Create invoice without template (uses default programmatic generation)
curl -X POST http://localhost:5052/api/documents \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "type": "Invoice",
    "customer": {
      "name": "John Doe",
      "phone": "+254712345678"
    },
    "lines": [
      {
        "name": "Product A",
        "quantity": 2,
        "unitPrice": 100.00
      }
    ],
    "currency": "KES"
  }'
```

**Note**: When `templateId` is omitted, the system uses programmatic document generation (OpenXmlDocumentGenerator) with default styling.

### Templates API Endpoints

#### 1. List Templates
```http
GET /api/templates?type={Invoice|Receipt|Quotation}
Authorization: Bearer {jwt-token}
```

**Query Parameters:**
- `type` (optional) - Filter by document type: `Invoice`, `Receipt`, or `Quotation`

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Found 6 template(s)",
  "templates": [
    {
      "id": "template-uuid",
      "type": "Invoice",
      "name": "Modern Blue",
      "version": 1,
      "blobUrl": "global/Invoice/modern-blue-v1.docx",
      "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/template-uuid.png",
      "theme": {
        "primaryColor": "#1E40AF",
        "secondaryColor": "#3B82F6",
        "accentColor": "#60A5FA",
        "fontFamily": "Inter"
      },
      "isDefault": true,
      "createdAt": "2025-01-15T10:00:00Z"
    }
  ]
}
```

#### 2. Get Template by ID
```http
GET /api/templates/{templateId}
Authorization: Bearer {jwt-token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Template retrieved successfully",
  "template": {
    "id": "template-uuid",
    "type": "Invoice",
    "name": "Modern Blue",
    "version": 1,
    "blobUrl": "global/Invoice/modern-blue-v1.docx",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/template-uuid.png",
    "theme": {
      "primaryColor": "#1E40AF",
      "secondaryColor": "#3B82F6",
      "accentColor": "#60A5FA",
      "fontFamily": "Inter"
    },
    "isDefault": true,
    "createdAt": "2025-01-15T10:00:00Z"
  }
}
```

### Using Templates When Creating Documents

When creating documents, you have two options:

1. **Use a Template** (Recommended for consistent branding):
   - First, call `GET /api/templates` to see available templates
   - Select a template that matches your document type
   - Include the `templateId` in your document creation request
   - The document will be generated using the template's layout and styling

2. **Create From Scratch** (Custom styling):
   - Omit the `templateId` field
   - Optionally provide a custom `theme` object to override default colors/fonts
   - The system uses programmatic generation with your custom theme

**Example: Create Invoice with Template**
```json
{
  "businessId": "business-uuid",
  "type": "Invoice",
  "templateId": "1b351696-f113-4c58-a23a-3afbd27114de",
  "customer": {
    "name": "John Doe",
    "phone": "+254712345678"
  },
  "lines": [
    {
      "name": "Product A",
      "quantity": 2,
      "unitPrice": 100.00
    }
  ],
  "currency": "KES"
}
```

**Example: Create Invoice Without Template (Custom Theme)**
```json
{
  "businessId": "business-uuid",
  "type": "Invoice",
  "theme": {
    "primaryColor": "#0F172A",
    "secondaryColor": "#475569",
    "accentColor": "#F97316",
    "fontFamily": "Poppins"
  },
  "customer": {
    "name": "John Doe",
    "phone": "+254712345678"
  },
  "lines": [
    {
      "name": "Product A",
      "quantity": 2,
      "unitPrice": 100.00
    }
  ],
  "currency": "KES"
}
```

**Example: Create Invoice Without Template (Default Styling)**
```json
{
  "businessId": "business-uuid",
  "type": "Invoice",
  "customer": {
    "name": "John Doe",
    "phone": "+254712345678"
  },
  "lines": [
    {
      "name": "Product A",
      "quantity": 2,
      "unitPrice": 100.00
    }
  ],
  "currency": "KES"
}
```

### How It Works

#### Voice-to-Document Flow
1. **Mobile App**: User speaks document details in English or Swahili
2. **Transcription**: Mobile transcribes locally (offline) or sends audio to API
3. **AI Extraction**: Azure OpenAI extracts structured data (customer, items, prices)
4. **Validation**: System validates totals, tax calculations, required fields
5. **Generation**: Creates DOCX using OpenXML and PDF using QuestPDF
6. **Storage**: Uploads to Azure Blob Storage (separate containers per document type)
7. **Response**: Returns document URLs (DOCX, PDF, preview) to mobile app

#### Manual Document Flow
1. **Mobile App**: User fills form with document type, customer, and line items
2. **Validation**: FluentValidation checks all business rules
3. **Calculation**: System computes subtotal, tax, total
4. **Numbering**: Auto-generates document number based on type (INV-202501-0001, RCPT-202501-0001, QUO-202501-0001)
5. **Generation**: Creates DOCX and PDF with business branding
6. **Storage**: Uploads to Azure Blob Storage
7. **Response**: Returns document URLs

#### Document Rendering
- **DOCX**: OpenXML programmatic generation with business branding (OpenXmlDocumentGenerator)
- **PDF**: QuestPDF integration (QuestPdfDocumentGenerator)
- **Validation**: FluentValidation for business rules (required fields, tax calculations)
- **Numbering**: Format: `{prefix}{yyyyMM}-{####}` per business and document type
  - Invoices: `INV-202501-0001`
  - Receipts: `RCPT-202501-0001`
  - Quotations: `QUO-202501-0001`
- **Storage**: Azure Blob Storage with organized containers:
  - `invoices/` - Invoice documents
  - `receipts/` - Receipt documents
  - `quotations/` - Quotation documents
  - `doc-previews/` - Preview images for all document types
  - `document-signatures/` - Handwritten signatures uploaded from the mobile app

---

## Development

### Database Migrations
```bash
# Add new migration
./migrate.sh add "MigrationName"

# Apply migrations
./migrate.sh update

# Rollback
./migrate.sh remove
```

### Project Structure
```
src/ApiWorker/
├── Authentication/       # Auth module (signup, login, JWT, OAuth, multi-business)
│   ├── Controllers/      # AuthController
│   ├── Services/         # AuthenticationService, ICurrentUserService
│   ├── Entities/         # AppUser, Business, Membership, Template
│   ├── DTOs/             # AuthDtos (requests/responses)
│   ├── Middleware/       # CurrentUserMiddleware
│   └── Extensions/       # AuthServiceCollectionExtensions
├── Documents/            # Documents module (invoices, receipts, quotations)
│   ├── Controllers/      # DocumentsController
│   ├── Services/         # DocumentService, VoiceIntentService, TemplateService
│   ├── Core/Entities/    # Document, TransactionalDocument, Invoice, Receipt, Quotation
│   ├── DTOs/             # TransactionalDocumentDtos, DocumentDtos, ExtractedDocumentData
│   └── Validators/       # DocumentValidators (FluentValidation)
├── Speech/               # Speech-to-Text module (Azure Speech SDK)
│   ├── Services/         # AzureSpeechToTextService, SpeechCaptureService
│   ├── Storage/          # CosmosTranscriptionStore
│   └── Interfaces/       # ISpeechToTextService, ISpeechCaptureService
├── Data/                 # EF Core DbContext, Migrations
├── Storage/              # BlobStorageService (Azure Blob Storage)
├── Cosmos/               # Cosmos DB configuration
├── Configuration/        # Key Vault, Storage extensions
└── Program.cs            # App entry point, DI registration

scripts/
├── migrate.sh            # EF Core migration helper
├── deploy-azure-container-apps.sh
├── setup-azure-auth.sh
└── setup-keyvault.sh

bicep/                    # Infrastructure as Code (Azure)
└── main.bicep

Dockerfile                # Container image definition
```

### Key Modules

1. **Authentication Module**: User management, JWT tokens, business registration, multi-business support
2. **Documents Module**: Document creation (Invoice/Receipt/Quotation, manual/voice), document rendering (DOCX/PDF)
3. **Speech Module**: Azure Speech-to-Text integration, transcription storage in Cosmos DB
4. **Storage Module**: Azure Blob Storage for documents, templates, and business logos
5. **Cosmos Module**: Cosmos DB for high-volume transcript storage

### Docker & Deployment

#### Build Docker Image
```bash
docker build -t biasharaos-api-worker:latest .
```

#### Run Locally with Docker
```bash
docker run -p 8000:8000 \
  -e ConnectionStrings__Default="Server=..." \
  -e Auth__Jwt__SecretKey="..." \
  biasharaos-api-worker:latest
```

#### Deploy to Azure Container Apps
```bash
# Using provided script
./scripts/deploy-azure-container-apps.sh

# Or using Bicep
az deployment group create \
  --resource-group biasharaos-rg \
  --template-file bicep/main.bicep \
  --parameters @bicep/parameters.dev.json
```

#### Environment Variables (Production)
- `ConnectionStrings--Default`: Azure SQL connection string
- `ConnectionStrings--BlobStorage`: Azure Blob Storage connection string
- `Cosmos--Endpoint`: Cosmos DB endpoint
- `Cosmos--Key`: Cosmos DB key
- `Auth--Jwt--SecretKey`: JWT signing key (min 32 chars)
- `Auth--Supabase--Url`: Supabase project URL
- `Auth--Supabase--Key`: Supabase anon key
- `Speech--Key`: Azure Speech service key
- `AzureOpenAI--Endpoint`: Azure OpenAI endpoint
- `AzureOpenAI--ApiKey`: Azure OpenAI API key

**Note**: In production, these should be stored in Azure Key Vault and loaded automatically.

### Common Issues

**Issue**: "Business not found" when creating document
- **Solution**: Register a business first using `/auth/business/register`

**Issue**: "Document type must be Invoice, Receipt, or Quotation"
- **Solution**: Set `type` to "Invoice", "Receipt", or "Quotation" (case-sensitive)

**Issue**: "Only draft documents can be edited"
- **Solution**: Only documents with status "Draft" can be updated

**Issue**: 401 Unauthorized
- **Solution**: Include `Authorization: Bearer {token}` header in all authenticated requests

**Issue**: "Transcript text is required" for voice document
- **Solution**: Provide `transcriptText` or `audioBlobUrl` in the request

---

## API Response Codes

- **200 OK**: Request successful
- **400 Bad Request**: Validation error or invalid input
- **401 Unauthorized**: Missing or invalid JWT token
- **404 Not Found**: Resource doesn't exist
- **500 Internal Server Error**: Server error (check logs)
