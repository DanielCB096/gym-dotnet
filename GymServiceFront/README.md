# GymService Frontend

Panel de administración web para GymService, construido con **React + Vite + Tailwind CSS**.

## Arquitectura

```
IIS (puerto 80)          Backend .NET (puerto 5000)
┌────────────────┐       ┌──────────────────┐
│  Frontend SPA  │ ──▶   │  API REST        │
│  (React/Vite)  │  API  │  (GymService)    │
│  web.config    │       │  SQL Server      │
└────────────────┘       └──────────────────┘
```

- **Backend** corre como Windows Service en `http://localhost:5000`
- **Frontend** se sirve desde IIS como archivos estáticos
- El frontend consume la API en `localhost:5000/api`

## Desarrollo local

```powershell
cd GymServiceFront
npm install
npm run dev
```

El dev server corre en `http://localhost:5173`.

## Producción (IIS)

```powershell
cd GymServiceFront
npm run build
```

Los archivos compilados están en `dist/`. Para desplegar en IIS:

1. Copia la carpeta `dist/` a una ubicación, ej: `C:\inetpub\wwwroot\gym`
2. En IIS Manager, agrega un sitio nuevo apuntando a esa carpeta
3. Asegúrate de tener el módulo **URL Rewrite** instalado
4. El `web.config` ya incluye las reglas de SPA routing

## Estructura

```
src/
├── services/
│   └── api.js              # Axios configurado con JWT
├── context/
│   └── AuthContext.jsx     # Autenticación JWT
├── components/
│   ├── Layout.jsx          # Sidebar + navegación
│   └── ProtectedRoute.jsx  # Rutas protegidas
├── pages/
│   ├── Login.jsx           # Pantalla de login
│   ├── Dashboard.jsx       # Estadísticas generales
│   ├── Members.jsx         # Gestión de miembros
│   ├── Sales.jsx           # Ventas
│   ├── Inventory.jsx       # Inventario
│   ├── Classes.jsx         # Clases
│   ├── Staff.jsx           # Personal
│   ├── Billing.jsx         # Facturación
│   ├── Finance.jsx         # Finanzas
│   ├── Maintenance.jsx     # Mantenimiento
│   ├── Fisio.jsx           # Fisioterapia
│   ├── Routines.jsx        # Rutinas
│   ├── Kiosk.jsx           # Kioscos
│   ├── Reports.jsx         # Reportes
│   ├── AccessLogs.jsx      # Logs de acceso
│   └── GenericPage.jsx     # Componente reutilizable
├── App.jsx                 # Rutas principales
└── main.jsx                # Entry point
```

## Credenciales de prueba

- **Usuario:** `admin`
- **Contraseña:** `Admin123!`
