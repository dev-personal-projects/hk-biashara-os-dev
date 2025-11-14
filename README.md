# BiasharaOS API Worker

**Voice-first, offline-ready business ops for SMEs** — create receipts/invoices by voice, auto-generate orders, discover suppliers, and get AI insights in English and Swahili.

## Features

### 📄 Smart Documents
- Create invoices/receipts by voice (English/Swahili)
- Custom templates with logo, colors, fields
- Share via WhatsApp, PDF, or QR code
- OCR scan supplier invoices

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
- Works without internet
- Background sync when online
- Full Swahili and English support

## Tech Stack

**Backend**: .NET 9, Clean Architecture, Azure SQL Database  
**AI**: Azure OpenAI, Speech-to-Text, Document Intelligence  
**Storage**: Azure Blob Storage, Redis Cache  
**Integrations**: WhatsApp Business API, Google Places API  
**Mobile**: Flutter (separate repo)

## Quick Start

```bash
# Setup database
./migrate.sh add "InitialCreate"
./migrate.sh update

# Run API
dotnet run --project src/ApiWorker
```

## Architecture

```
Flutter App → .NET API → Azure SQL
     ↓           ↓         ↓
  Voice/OCR → Azure AI → Blob Storage
     ↓           ↓         ↓
 WhatsApp ← Notifications ← Redis Cache
```

## Core Flows

1. **Voice Invoice**: Speak → Parse → Validate → PDF → WhatsApp
2. **Auto Reorder**: Low stock → Forecast → Draft PO → Send to supplier
3. **Supplier Discovery**: Search nearby → Rank by score → Contact

## API Testing with Postman

### Base URL
```
Local: http://localhost:5052/api
Production: https://your-api.azurewebsites.net/api
```

### 1. Google OAuth (Recommended)
```http
POST /auth/google
Content-Type: application/json

{
  "idToken": "google-id-token-from-supabase",
  "county": "Nairobi"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here",
  "user": {
    "id": "uuid-here",
    "fullName": "John Doe",
    "email": "john@gmail.com",
    "county": "Nairobi",
    "hasBusiness": false,
    "businessCount": 0,
    "onboardingStatus": 1
  }
}
```

### 2. Business Registration (with Logo Upload)
```http
POST /auth/business/register
Authorization: Bearer jwt-token-here
Content-Type: multipart/form-data

name: Mama Mboga Shop
category: retail
county: Nairobi
town: Westlands
email: shop@example.com
phone: +254712345678
logo: [file upload - optional]
```

**Response:**
```json
{
  "success": true,
  "message": "Business registered successfully",
  "businessId": "business-uuid-here"
}
```

### 3. List User Businesses
```http
GET /auth/businesses
Authorization: Bearer jwt-token-here
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
      "logoUrl": "https://storage.blob.url/logo.jpg",
      "userRole": "Owner"
    }
  ]
}
```

### 4. Switch Business
```http
POST /auth/businesses/switch
Authorization: Bearer jwt-token-here
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
  "business": {
    "businessId": "business-uuid",
    "businessName": "Mama Mboga Shop",
    "category": "retail",
    "county": "Nairobi",
    "logoUrl": "https://storage.blob.url/logo.jpg",
    "userRole": "Owner"
  }
}
```

### 5. Initialize User Session
```http
POST /auth/initialize-session
Authorization: Bearer jwt-token-here
Content-Type: application/json

{
  "userId": "user-uuid-here"
}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "userId": "uuid-here",
    "fullName": "John Doe",
    "email": "john@gmail.com",
    "county": "Nairobi",
    "business": {
      "businessId": "business-uuid",
      "businessName": "Mama Mboga Shop",
      "category": "retail",
      "county": "Nairobi",
      "logoUrl": "https://storage.blob.url/logo.jpg",
      "userRole": "Owner"
    },
    "hasBusiness": true
  }
}
```

### Error Responses
```json
{
  "success": false,
  "message": "Error description here"
}
```

### HTTP Status Codes
- `200 OK`: Success
- `400 Bad Request`: Invalid input
- `401 Unauthorized`: Missing/invalid token
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### Valid Counties (Kenya)
Nairobi, Mombasa, Kisumu, Nakuru, Eldoret, Thika, Malindi, Kitale, and 40+ more Kenyan counties

## Mobile Integration Guide

### Supabase OAuth Flow
1. **Mobile App**: Use Supabase Auth SDK with Google provider
2. **Get ID Token**: Extract ID token from Supabase session
3. **API Call**: Send ID token to `/auth/google` endpoint with county
4. **Response**: Receive custom JWT tokens and user profile
5. **Storage**: Store JWT tokens securely in Flutter Secure Storage

### Multi-Business Support
- Users can create multiple businesses
- Use `/auth/businesses` to list all businesses
- Use `/auth/businesses/switch` to change active business
- Business context is maintained in JWT claims

### Token Management
- Store `accessToken` for API requests (expires in 1 hour)
- Store `refreshToken` for token renewal
- Include `Authorization: Bearer {accessToken}` in headers
- Handle token expiration and refresh automatically

## Development

- **Database**: Azure SQL with EF Core migrations
- **Auth**: Supabase OAuth + Custom JWT tokens
- **Storage**: Azure Blob Storage for images/documents
- **PDF**: QuestPDF for document generation
- **Voice**: Azure Speech Services (en/sw)
- **AI**: Azure OpenAI for NLU and insights

## Configuration

Update `appsettings.json` with your credentials:

```json
{
  "ConnectionStrings": {
    "Default": "Azure SQL connection string",
    "BlobStorage": "Azure Blob Storage connection string"
  },
  "Auth": {
    "Supabase": {
      "Url": "https://your-project.supabase.co",
      "Key": "your-supabase-anon-key"
    },
    "Jwt": {
      "SecretKey": "your-jwt-secret-key",
      "Issuer": "BiasharaOS",
      "Audience": "BiasharaOS-Users",
      "ExpiryHours": 1
    }
  }
}
```