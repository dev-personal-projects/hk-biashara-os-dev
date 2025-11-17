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
- Create **Invoices** with manual or voice input
- **Voice to Invoice** (English/Swahili): transcript → AI extraction → validate → DOCX/PDF
- **Manual invoice builder** with customer and line items
- Export **DOCX (OpenXML)** and **PDF (QuestPDF)**
- Share via **WhatsApp** or download link
- Audit trail of shares via ShareLog
- Document versioning and status tracking (Draft, Sent, Paid, Overdue, Cancelled)
- Auto-numbering: `INV-{yyyyMM}-{####}` per business
- **Coming Soon**: Receipts, Quotations, Ledgers, Balance Sheets, OCR scan

### 🔊 Voice Processing (✅ Implemented)
- **Azure Speech SDK**: Speech-to-Text for `en-KE` and `sw-KE`
- **Azure OpenAI**: Function-calling to extract structured invoice fields from transcripts
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
| **Integrations** | WhatsApp Business API (document sharing) |
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
```

**Local API**: `http://localhost:5052/api`  
**OpenAPI/Swagger**: `http://localhost:5052/openapi/v1.json` (Development only)

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
       │
       ↓
┌─────────────────┐
│ WhatsApp API    │
│ (Sharing)       │
└─────────────────┘
```

### Core Flows
1. **Voice Invoice**: Mobile transcribes → API extracts (Azure OpenAI) → Validates → Generates DOCX/PDF → Stores in Blob → Shares via WhatsApp
2. **Manual Invoice**: User fills form → API validates → Generates DOCX/PDF → Stores in Blob → Returns URLs
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
- `GET /api/documents` - List all documents with filters
- `POST /api/documents/{id}/share` - Share document

#### Invoices (`/api/invoices`)
- `POST /api/invoices` - Create invoice manually
- `POST /api/invoices/voice` - Create invoice from voice transcript
- `GET /api/invoices/{id}` - Get invoice details
- `PUT /api/invoices/{id}` - Update invoice (draft only)
- `GET /api/invoices` - List invoices with filters
- `POST /api/invoices/{id}/share` - Share invoice via WhatsApp

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
  "Share": {
    "PublicFileBaseUrl": "https://cdn.biasharaos.com",
    "WhatsApp": {
      "PhoneNumberId": "whatsapp-number-id",
      "AccessToken": "meta-access-token",
      "ApiBase": "https://graph.facebook.com/v20.0",
      "SandboxMode": false
    }
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
├── Controllers/          # API endpoints (InvoicesController, DocumentsController)
├── DTOs/                 # Request/Response models (InvoiceDtos, DocumentDtos)
├── Core/Entities/        # Database models (Document, TransactionalDocument, Template)
├── Services/             # Business logic (DocumentService, InvoiceService, VoiceIntentService)
├── Interfaces/           # Service contracts (IDocumentService, IRenderService, IShareService)
├── Validators/           # FluentValidation rules (InvoiceValidators)
├── Mappings/             # AutoMapper profiles (DocumentMappings)
├── Settings/             # Configuration models (DocumentSettings, AzureOpenAISettings)
├── Extensions/           # DI registration (DocumentServiceCollectionExtensions)
└── Templates/            # Default DOCX templates
```

### Supported Document Types
- ✅ **Invoices** - Fully implemented (manual and voice creation)
- 🚧 **Receipts** - Planned
- 🚧 **Quotations** - Planned
- 🚧 **Ledgers** - Planned
- 🚧 **Balance Sheets** - Planned

### Document Flows

#### 1. Voice Invoice
```
Speech → Transcript → Extract fields (AI) → Validate → Render (DOCX/PDF) → WhatsApp
```

#### 2. Manual Document
```
Choose template → Fill fields → Render (DOCX/PDF) → Share
```

### Documents API Endpoints

#### 1. Create Invoice Manually
```http
POST /api/invoices
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "businessId": "45bc8336-f8b7-4bdc-aaf2-1dfbea5dbaa4",
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
  }
}
```

#### 2. Create Invoice from Voice
```http
POST /api/invoices/voice
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "transcriptText": "Create invoice for John Kamau. Three bags of maize flour at 180 shillings each and two bottles of cooking oil at 350 shillings each",
  "locale": "en-KE"
}
```

**Swahili Example:**
```json
{
  "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "transcriptText": "Tengeneza ankara kwa John Kamau. Mifuko mitatu ya unga kwa shilingi 180 kila moja na chupa mbili za mafuta kwa shilingi 350 kila moja",
  "locale": "sw-KE"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Invoice created from voice successfully",
  "documentId": "invoice-uuid",
  "documentNumber": "INV-202501-0002",
  "urls": {
    "docxUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202501-0002.docx",
    "pdfUrl": "https://biasharaos.blob.core.windows.net/invoices/INV-202501-0002.pdf",
    "previewUrl": "https://biasharaos.blob.core.windows.net/doc-previews/INV-202501-0002.png"
  }
}
```

#### 3. Get Invoice Details
```http
GET /api/invoices/{invoiceId}
Authorization: Bearer {jwt-token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Invoice retrieved successfully",
  "invoice": {
    "id": "invoice-uuid",
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

#### 4. List Invoices with Filters
```http
GET /api/invoices?page=1&pageSize=20&status=Draft&searchTerm=John
Authorization: Bearer {jwt-token}
```

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20)
- `type` (Invoice, Receipt, Quotation)
- `status` (Draft, Sent, Paid, Overdue, Cancelled)
- `fromDate` (ISO 8601)
- `toDate` (ISO 8601)
- `searchTerm` (searches number and customer name)

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

#### 5. Update Invoice (Draft Only)
```http
PUT /api/invoices/{invoiceId}
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "invoiceId": "invoice-uuid",
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
  "message": "Invoice updated successfully",
  "documentId": "invoice-uuid",
  "documentNumber": "INV-202501-0001"
}
```

#### 6. Share Invoice
```http
POST /api/invoices/{invoiceId}/share
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "documentId": "invoice-uuid",
  "channel": "WhatsApp",
  "target": "+254712345678"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Document shared via WhatsApp",
  "shareLogId": "share-log-uuid",
  "publicUrl": "https://blob.storage/invoices/INV-202501-0001.pdf"
}
```

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

#### Step 3: Create Invoice
```bash
curl -X POST http://localhost:5052/api/invoices \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
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

#### Step 4: Create Voice Invoice
```bash
curl -X POST http://localhost:5052/api/invoices/voice \
  -H "Authorization: Bearer {your-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "{your-business-id}",
    "transcriptText": "Invoice for Jane with 3 bags of flour at 150 each",
    "locale": "en-KE"
  }'
```

#### Step 5: List Invoices
```bash
curl -X GET "http://localhost:5052/api/invoices?page=1&pageSize=10" \
  -H "Authorization: Bearer {your-token}"
```

### How It Works

#### Voice-to-Invoice Flow
1. **Mobile App**: User speaks invoice details in English or Swahili
2. **Transcription**: Mobile transcribes locally (offline) or sends audio to API
3. **AI Extraction**: Azure OpenAI extracts structured data (customer, items, prices)
4. **Validation**: System validates totals, tax calculations, required fields
5. **Generation**: Creates DOCX using OpenXML (editable)
6. **Storage**: Uploads to Azure Blob Storage
7. **Response**: Returns document URLs to mobile app

#### Manual Invoice Flow
1. **Mobile App**: User fills form with customer and line items
2. **Validation**: FluentValidation checks all business rules
3. **Calculation**: System computes subtotal, tax, total
4. **Numbering**: Auto-generates invoice number (INV-202501-0001)
5. **Generation**: Creates DOCX with business branding
6. **Storage**: Uploads to Azure Blob Storage
7. **Response**: Returns document URLs

#### Document Rendering
- **DOCX**: OpenXML programmatic generation with business branding
- **PDF**: QuestPDF integration (fully implemented)
- **Validation**: FluentValidation for business rules (required fields, tax calculations)
- **Auditing**: ShareLog tracks all shares (WhatsApp, email, download links)
- **Numbering**: Format: `{prefix}{yyyyMM}-{####}` per business (e.g., `INV-202501-0001`)
- **Storage**: Azure Blob Storage with organized paths: `{businessId}/{type}/{yyyy}/{MM}/{fileName}`

### Speech Module

The Speech module provides Azure Speech-to-Text integration and transcription storage.

#### Module Structure
```
src/ApiWorker/Speech/
├── DTOs/                 # TranscriptionResult, TranscriptionRecord
├── Interfaces/           # ISpeechToTextService, ISpeechCaptureService, ITranscriptionStore
├── Services/             # AzureSpeechToTextService, SpeechCaptureService
├── Settings/             # SpeechSettings, VoiceSettings
├── Storage/              # CosmosTranscriptionStore (Cosmos DB)
└── Extensions/           # SpeechServiceCollectionExtensions
```

#### Features
- **Azure Speech SDK**: Streaming and file-based transcription
- **Locale Support**: `en-KE` (English Kenya), `sw-KE` (Swahili Kenya)
- **Numeral Normalization**: Converts spoken numbers to digits
- **Phrase Lists**: Boosts recognition of business-specific terms
- **Cosmos DB Storage**: High-volume transcript storage with business-level partitioning
- **Offline Support**: Mobile app handles offline transcription, API processes transcripts

#### Usage
The Speech module is used internally by the Documents module for voice invoice creation. Mobile apps can:
1. Transcribe audio offline (Whisper/Vosk)
2. Send transcript text to `/api/invoices/voice`
3. Or upload audio blob URL for server-side transcription

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
├── Documents/            # Documents module (invoices, templates, sharing)
│   ├── Controllers/      # InvoicesController, DocumentsController
│   ├── Services/         # DocumentService, InvoiceService, VoiceIntentService
│   ├── Core/Entities/    # Document, TransactionalDocument, Template, ShareLog
│   ├── DTOs/             # InvoiceDtos, DocumentDtos
│   └── Validators/       # InvoiceValidators (FluentValidation)
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
2. **Documents Module**: Invoice creation (manual/voice), document rendering (DOCX/PDF), sharing (WhatsApp)
3. **Speech Module**: Azure Speech-to-Text integration, transcription storage in Cosmos DB
4. **Storage Module**: Azure Blob Storage for documents, templates, and business logos
5. **Cosmos Module**: Cosmos DB for high-volume transcript storage

### Testing
```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"
```

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
- `Share--WhatsApp--AccessToken`: WhatsApp Business API token

**Note**: In production, these should be stored in Azure Key Vault and loaded automatically.

### Common Issues

**Issue**: "Business not found" when creating invoice
- **Solution**: Ensure you've registered a business first using `/auth/business/register`

**Issue**: "Transcript text is required" for voice invoice
- **Solution**: Provide `transcriptText` field with the spoken invoice details

**Issue**: "Only draft invoices can be edited"
- **Solution**: You can only update invoices with status "Draft". Once sent/paid, they're locked.

**Issue**: 401 Unauthorized
- **Solution**: Include `Authorization: Bearer {token}` header in all authenticated requests

**Issue**: "Cosmos DB connection failed"
- **Solution**: Ensure Cosmos DB emulator is running locally, or provide valid Cosmos DB endpoint/key in production

**Issue**: "Blob Storage upload failed"
- **Solution**: Check Azure Storage connection string or use `UseDevelopmentStorage=true` for local development

**Issue**: "Azure OpenAI extraction failed"
- **Solution**: Verify Azure OpenAI endpoint, API key, and deployment name in configuration

---

## Deployment

### Prerequisites
- Azure subscription
- Azure SQL Database
- Azure Blob Storage account
- Azure Cosmos DB account
- Azure Key Vault
- Azure Speech service
- Azure OpenAI resource
- WhatsApp Business API credentials (optional)

### Deployment Steps

1. **Setup Azure Resources**
   ```bash
   # Use Bicep templates
   az deployment group create \
     --resource-group biasharaos-rg \
     --template-file bicep/main.bicep \
     --parameters @bicep/parameters.dev.json
   ```

2. **Configure Key Vault**
   ```bash
   ./scripts/setup-keyvault.sh
   ```

3. **Run Database Migrations**
   ```bash
   ./migrate.sh update
   ```

4. **Deploy Container App**
   ```bash
   ./scripts/deploy-azure-container-apps.sh
   ```

### Environment-Specific Configuration

- **Development**: Uses `appsettings.json` and `appsettings.Development.json`
- **Production**: Uses Azure Key Vault (configured via `AddKeyVaultConfiguration()`)
- **Docker**: Environment variables override configuration

---

## License

Proprietary - BiasharaOS © 2024

---

## API Response Codes

- **200 OK**: Request successful
- **400 Bad Request**: Validation error or invalid input
- **401 Unauthorized**: Missing or invalid JWT token
- **404 Not Found**: Resource doesn't exist
- **500 Internal Server Error**: Server error (check logs)

## Support

For issues or questions, contact: support@biasharaos.com
