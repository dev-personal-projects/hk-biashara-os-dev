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

### 📄 Smart Documents
- Create **Invoices, Receipts, Quotations, Ledgers, Balance Sheets**
- **Voice to Document** (English/Swahili): speak → transcript → validate → DOCX/PDF
- **Manual builder** with branded templates (logo, colors, fields)
- Export **DOCX (OpenXML)** and **PDF (QuestPDF)**
- Share via **WhatsApp**, PDF link, or **QR code**
- Audit trail of views/sends; versioned templates
- OCR scan supplier invoices (coming soon)

### 🔊 Voice Processing
- **Azure Speech SDK**: Streaming STT for `en-KE` and `sw-KE`
- Phrase-list boosting for product/customer names
- **Azure OpenAI** function-calling to extract structured fields
- **Offline fallback**: Whisper/Vosk transcription on mobile

### 🔄 Auto Reordering
- AI-powered stock forecasting
- Smart reorder suggestions
- One-tap Purchase Orders
- Industry-specific presets

### 🗺️ Supplier Discovery
- Find nearby distributors on map
- Supplier scoring (price + reliability + distance)
- Contact via call/WhatsApp/email

### 🤖 AI Copilot
- Sales forecasting per product
- Smart reminders and follow-ups
- Meeting scheduling assistant
- Explainable business insights

### 📱 Offline-First
- Works without internet (mobile transcribes offline)
- Background sync when online
- Full Swahili and English support

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 9, Clean Architecture, Azure SQL Database |
| **AI** | Azure OpenAI (extraction), Azure Speech to Text (en/sw) |
| **Documents** | OpenXML (DOCX), QuestPDF (PDF) |
| **Storage** | Azure Blob Storage, Redis Cache |
| **Integrations** | WhatsApp Business API, Google Places API |
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

---

## Architecture

```
Flutter App → .NET API → Azure SQL
     ↓           ↓         ↓
  Voice/OCR → Azure AI → Blob Storage
     ↓           ↓         ↓
 WhatsApp ← Notifications ← Redis Cache
```

### Core Flows
1. **Voice Invoice**: Speak → Parse → Validate → PDF → WhatsApp
2. **Auto Reorder**: Low stock → Forecast → Draft PO → Send to supplier
3. **Supplier Discovery**: Search nearby → Rank by score → Contact

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
```

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
Content-Type: application/json

{
  "name": "Mama Mboga Shop",
  "category": "retail",
  "county": "Nairobi",
  "town": "Westlands",
  "email": "shop@example.com",
  "phone": "+254712345678",
  "currency": "KES",
  "usesVat": true,
  "defaultTaxRate": 0.16
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

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=your-server.database.windows.net;Database=BiasharaOS;User Id=admin;Password=***;",
    "BlobStorage": "DefaultEndpointsProtocol=https;AccountName=***;AccountKey=***;"
  },
  "Auth": {
    "Supabase": {
      "Url": "https://your-project.supabase.co",
      "Key": "your-supabase-anon-key"
    },
    "Jwt": {
      "SecretKey": "your-jwt-secret-key-min-32-chars",
      "Issuer": "BiasharaOS",
      "Audience": "BiasharaOS-Users",
      "ExpiryHours": 24
    }
  },
  "Documents": {
    "DefaultCurrency": "KES",
    "Numbering": {
      "InvoicePrefix": "INV-",
      "ReceiptPrefix": "RCPT-"
    },
    "BlobContainers": {
      "Documents": "docs",
      "Templates": "doc-templates"
    }
  },
  "Voice": {
    "Provider": "AzureSpeech",
    "Region": "eastus",
    "Key": "your-azure-speech-key",
    "Locales": ["en-KE", "sw-KE"],
    "PhraseList": ["Unga", "M-Pesa", "Karatasi"]
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-azure-openai.openai.azure.com/",
    "ApiKey": "your-aoai-key",
    "Deployment": "gpt-4o-mini"
  },
  "Share": {
    "WhatsApp": {
      "PhoneNumberId": "whatsapp-number-id",
      "AccessToken": "meta-access-token",
      "ApiBase": "https://graph.facebook.com/v20.0"
    }
  }
}
```

---

## Documents Module

### Module Structure
```
src/ApiWorker/Documents/
├── Controllers/          # API endpoints
├── DTOs/                 # Request/Response models
├── Entities/             # Database models
├── Services/             # Business logic
├── Interfaces/           # Service contracts
├── Validators/           # FluentValidation rules
├── Mappings/             # AutoMapper profiles
├── Settings/             # Configuration models
├── Extensions/           # DI registration
└── Templates/            # Default DOCX templates
```

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
- **DOCX**: OpenXML programmatic generation (no templates needed)
- **PDF**: Coming soon (QuestPDF integration)
- **Validation**: FluentValidation for business rules
- **Auditing**: ShareLog tracks all shares
- **Numbering**: Format: {prefix}{yyyyMM}-{sequence} per business

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
├── Authentication/       # Auth module (signup, login, JWT)
├── Documents/            # Documents module (invoices, receipts)
├── Data/                 # EF Core DbContext
├── Middleware/           # Custom middleware
└── Program.cs            # App entry point

tests/
└── ApiWorker.UnitTests/  # Unit tests
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"
```

### Common Issues

**Issue**: "Business not found" when creating invoice
- **Solution**: Ensure you've registered a business first using `/auth/business/register`

**Issue**: "Transcript text is required" for voice invoice
- **Solution**: Provide `transcriptText` field with the spoken invoice details

**Issue**: "Only draft invoices can be edited"
- **Solution**: You can only update invoices with status "Draft". Once sent/paid, they're locked.

**Issue**: 401 Unauthorized
- **Solution**: Include `Authorization: Bearer {token}` header in all authenticated requests

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
