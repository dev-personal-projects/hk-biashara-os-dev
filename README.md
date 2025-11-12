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
Local: https://localhost:7000/api
Production: https://your-api.azurewebsites.net/api
```

### 1. User Registration
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
  "message": "Account created successfully. Please verify your email.",
  "userId": "uuid-here",
  "requiresEmailVerification": true
}
```

### 2. Email Verification
```http
POST /auth/verify-email
Content-Type: application/json

{
  "email": "john@example.com",
  "code": "123456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Email verified successfully",
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here"
}
```

### 3. User Login
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
  "message": "Login successful",
  "accessToken": "jwt-token-here",
  "refreshToken": "refresh-token-here",
  "user": {
    "id": "uuid-here",
    "fullName": "John Doe",
    "email": "john@example.com",
    "county": "Nairobi",
    "hasBusiness": false
  }
}
```

### 4. Google OAuth (Mobile-Optimized)
```http
POST /auth/google
Content-Type: application/json

{
  "idToken": "google-id-token-from-mobile",
  "county": "Nairobi"
}
```

**Response:** Same as login response

### 5. Business Registration
```http
POST /auth/business/register
Authorization: Bearer jwt-token-here
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
  "defaultTaxRate": 16.0
}
```

**Response:**
```json
{
  "success": true,
  "message": "Business registered successfully",
  "businessId": "business-uuid-here"
}
```

### 6. Initialize User Session
```http
POST /auth/initialize-session
Authorization: Bearer jwt-token-here
Content-Type: application/json

{
  "supabaseUserId": "supabase-user-id"
}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "userId": "uuid-here",
    "fullName": "John Doe",
    "email": "john@example.com",
    "county": "Nairobi",
    "business": {
      "businessId": "business-uuid",
      "businessName": "Mama Mboga Shop",
      "category": "retail",
      "county": "Nairobi",
      "currency": "KES",
      "usesVat": true,
      "defaultTaxRate": 16.0,
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

### Common HTTP Status Codes
- `200 OK`: Success
- `400 Bad Request`: Invalid input
- `401 Unauthorized`: Missing/invalid token
- `403 Forbidden`: Business profile required
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

## Mobile Integration Guide

### Google OAuth Flow
1. **Mobile App**: Use Google Sign-In SDK to get ID token
2. **API Call**: Send ID token to `/auth/google` endpoint
3. **Response**: Receive JWT tokens and user profile
4. **Storage**: Store JWT tokens securely in mobile app

### Token Management
- Store `accessToken` for API requests
- Store `refreshToken` for token renewal
- Include `Authorization: Bearer {accessToken}` in headers
- Handle token expiration and refresh automatically

## Development

- **Database**: Azure SQL with EF Core migrations
- **Auth**: Supabase Auth + JWT validation
- **PDF**: QuestPDF for document generation
- **Voice**: Azure Speech Services (en/sw)
- **AI**: Azure OpenAI for NLU and insights