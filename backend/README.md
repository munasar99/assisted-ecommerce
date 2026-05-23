# Assisted E-commerce — Backend API

ASP.NET Core Web API + MongoDB for the Assisted E-commerce system (Somalia market).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8+ with updated `TargetFramework`)
- [MongoDB](https://www.mongodb.com/try/download/community) running locally **or** MongoDB Atlas connection string

## Quick start

```powershell
cd backend/AssistedEcommerce.Api
dotnet restore
dotnet run
```

- **Swagger:** http://localhost:5298/swagger  
- **Health:** http://localhost:5298/api/health  

## Default admin (seeded on first run)

| Field | Value |
|-------|--------|
| Email | `admin@assisted.local` |
| Password | `Admin@123` |

Change these in `appsettings.json` under `Seed` before production.

## MongoDB configuration

Edit `appsettings.json`:

```json
"MongoDb": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "ubaxsana"
}
```

## Architecture

```
Controllers → Services → MongoDbContext
```

| Layer | Folder |
|-------|--------|
| Controllers | `Controllers/` (7 + Health) |
| Services | `Services/` (8) |
| Models | `Models/` (6 collections + Counter) |
| Infrastructure | `Infrastructure/` |

## API endpoints (19)

| # | Method | Route | Auth |
|---|--------|-------|------|
| 1 | POST | `/api/auth/login` | Public |
| 2 | POST | `/api/auth/logout` | Admin |
| 3 | GET | `/api/auth/profile` | Admin |
| 4 | POST | `/api/orders` | Public |
| 5 | GET | `/api/orders/track` | Public |
| 6 | GET | `/api/orders` | Admin |
| 7 | GET | `/api/orders/{orderId}` | Admin |
| 8 | PATCH | `/api/orders/{orderId}/status` | Admin |
| 9 | POST | `/api/orders/{orderId}/invoice` | Admin |
| 10 | POST | `/api/users` | Admin |
| 11 | GET | `/api/users` | Admin |
| 12 | PATCH | `/api/users/{userId}/status` | Admin |
| 13 | GET | `/api/delivery/zones` | Admin |
| 14 | GET | `/api/delivery/zones/active` | Public |
| 15 | PUT | `/api/delivery/zones/{id}/fee` | Admin |
| 16 | PATCH | `/api/delivery/zones/{id}/toggle` | Admin |
| 17 | POST | `/api/payments/upload` | Public |
| 18 | POST | `/api/uploads/order` | Public |
| 19 | GET | `/api/analytics/dashboard` | Admin |

Use Swagger **Authorize** with: `Bearer <token>` from login.

## Order statuses

`Pending` → `InvoiceSent` → `WaitingPayment` → `PaymentReview` → `Confirmed` → `OrderedFromSupplier` → `Shipping` → `ArrivedMogadishu` → `OutForDelivery` → `Delivered`

## Uploads

- Allowed: JPG, PNG, JPEG, WEBP  
- Max: 5 MB  
- Stored under `uploads/` (served at `/uploads/...`)

## Project structure

```
backend/
└── AssistedEcommerce.Api/
    ├── Controllers/
    ├── Services/
    ├── Models/
    ├── DTOs/
    ├── Infrastructure/
    ├── Middleware/
    ├── Constants/
    └── appsettings.json
```
