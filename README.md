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

### Documents API

#### Create Invoice (Voice)
```http
POST /api/documents/invoices/voice
Authorization: Bearer {jwt}
Content-Type: application/json

{
  "audioUrl": "https://blob/audio.wav",
  "transcriptText": "invoice to John for 3 bags of maize flour at 180 shillings each",
  "locale": "sw-KE",
  "businessId": "uuid"
}
```

#### Create Invoice (Manual)
```http
POST /api/documents/invoices
Authorization: Bearer {jwt}
Content-Type: application/json

{
  "businessId": "uuid",
  "currency": "KES",
  "customer": {
    "name": "John Kamau",
    "phone": "+254712345678"
  },
  "lines": [
    {
      "name": "Maize Flour 2kg",
      "quantity": 3,
      "unitPrice": 180
    }
  ],
  "taxRate": 0.16,
  "notes": "Thank you for your business"
}
```

#### Share Document
```http
POST /api/documents/{documentId}/share
Authorization: Bearer {jwt}
Content-Type: application/json

{
  "channel": "WhatsApp",
  "phone": "+254712345678",
  "message": "Here is your invoice from Mama Mboga Shop."
}
```

### Rendering
- **DOCX**: OpenXML with content controls (editable by users)
- **PDF**: QuestPDF for fast server-side generation
- **Validation**: FluentValidation for totals/taxes/required fields
- **Auditing**: ShareLog tracks all document shares

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

---

## License

Proprietary - BiasharaOS © 2024

---

## Support

For issues or questions, contact: support@biasharaos.com
