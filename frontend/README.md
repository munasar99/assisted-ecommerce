# Ubaxsana Frontend (React)

Professional React frontend connected to ASP.NET Core API + MongoDB (`ubaxsana`).

## Stack

- React + Vite
- Tailwind CSS 4
- React Router
- Axios + React Query
- Recharts (analytics)

## Run (with backend)

**Terminal 1 — Backend:**
```powershell
cd backend\AssistedEcommerce.Api
dotnet run
```

**Terminal 2 — Frontend:**
```powershell
cd frontend
npm install
npm run dev
```

- Frontend: http://localhost:5173
- API: http://localhost:5298/swagger

## Pages

### Public
| Route | Page |
|-------|------|
| `/order` | Order form (default) |
| `/payment` | Payment + upload |
| `/track` | Track order |
| `/home` | Landing |
| `/success` | Success |

### Admin
| Route | Page |
|-------|------|
| `/admin/login` | JWT login |
| `/admin/dashboard` | Overview |
| `/admin/orders` | Orders + invoice |
| `/admin/users` | Users |
| `/admin/payments` | Payment review |
| `/admin/delivery` | Zones & fees |
| `/admin/analytics` | Charts |
| `/admin/settings` | Settings |

## Environment

`.env`:
```
VITE_API_URL=http://localhost:5298/api
```

## Admin login

- Email: `admin@assisted.local`
- Password: `Admin@123`
