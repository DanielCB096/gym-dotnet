# GymService Frontend

Panel de administración web para el sistema de gestión del gimnasio **Centro Fit**.

## Requisitos

- **Node.js** 20 o superior
- **npm** 10 o superior (incluido con Node.js)
- **Backend GymService** corriendo en `http://localhost:5000`

## Instalación

```powershell
cd GymServiceFront
npm install
```

## Scripts disponibles

| Comando | Descripción |
|---|---|
| `npm run dev` | Inicia el servidor de desarrollo con hot reload en `http://localhost:5173/` |
| `npm run build` | Compila la aplicación para producción en la carpeta `dist/` |
| `npm run lint` | Ejecuta el linter (oxlint) para detectar problemas de código |
| `npm run preview` | Previsualiza el build de producción localmente |

## Proxy de API

Durante el desarrollo, Vite redirige automáticamente las peticiones `/api` al backend:

- **Origen:** `/api/*`
- **Destino:** `http://localhost:5000/api/*`

Esto evita problemas de CORS. El backend debe estar corriendo en el puerto 5000.

Para producción (IIS), se requiere configurar un **reverse proxy** en IIS o ajustar la URL base en `src/services/api.js`.

## Estructura del proyecto

```
src/
├── assets/              # Imágenes y recursos estáticos
├── components/
│   ├── ui/              # Componentes reutilizables (Button, Card, Table, Badge, Input, Modal, Select, PageHeader, ConfirmDialog)
│   ├── Layout.jsx       # Layout principal con sidebar y header
│   ├── ProtectedRoute.jsx # Protección de rutas con JWT
│   ├── ImportDialog.jsx # Importación masiva CSV/Excel (socios e inventario)
│   └── CrudPage.jsx     # Página genérica de CRUD
├── context/
│   └── AuthContext.jsx  # Contexto de autenticación (token, usuario, login, logout)
├── hooks/
│   └── useApiList.js    # Hook para cargar listas de datos desde la API
├── pages/
│   ├── Dashboard.jsx    # Página principal con estadísticas
│   ├── Login.jsx        # Formulario de inicio de sesión
│   ├── NotFound.jsx     # Página 404
│   ├── Members.jsx      # Gestión de miembros (CRUD completo)
│   ├── Sales.jsx        # Ventas
│   ├── Inventory.jsx    # Inventario de productos (CRUD)
│   ├── Classes.jsx      # Clases del gimnasio
│   ├── Staff.jsx        # Personal
│   ├── Billing.jsx      # Facturación
│   ├── Finance.jsx      # Finanzas
│   ├── Maintenance.jsx  # Mantenimiento de equipo
│   ├── Fisio.jsx        # Fisioterapia
│   ├── Routines.jsx     # Rutinas de ejercicio
│   ├── Kiosk.jsx        # Dispositivos kiosco
│   ├── Reports.jsx      # Reportes programados
│   ├── AccessLogs.jsx   # Logs de acceso
│   └── GenericPage.jsx  # Página genérica para módulos sin CRUD
├── services/
│   └── api.js           # Cliente Axios con interceptores JWT
├── utils/
│   └── format.js        # Utilidades de formato (fechas, moneda, etc.)
├── App.jsx              # Rutas y configuración principal
├── main.jsx             # Punto de entrada de React
└── index.css            # Estilos globales y tema Tailwind CSS
```

## Tecnologías

- **React 19** — Biblioteca UI
- **Vite 8** — Bundler y servidor de desarrollo
- **Tailwind CSS 4** — Framework de estilos con tema personalizado (`brand-*`)
- **React Router DOM 7** — Navegación entre páginas
- **Axios** — Cliente HTTP con interceptores para JWT
- **Lucide React** — Iconos

## Autenticación

La aplicación usa **JWT (JSON Web Tokens)** para autenticación:

1. El usuario inicia sesión en `/login`
2. El backend devuelve un token que se guarda en `localStorage`
3. Cada petición incluye el token en el header `Authorization: Bearer <token>`
4. Las rutas protegidas redirigen a `/login` si no hay token válido

## Despliegue en IIS

1. Ejecuta `npm run build` para generar la carpeta `dist/`
2. Copia el contenido de `dist/` al sitio de IIS
3. Agrega el archivo `web.config` (en la raíz del proyecto) para:
   - URL Rewrite para SPA routing (todas las rutas apuntan a `index.html`)
   - Caché de archivos estáticos
4. Asegúrate de que el backend esté corriendo en `localhost:5000`

## Variables de entorno

No se usan variables de entorno. La URL del backend está configurada en `vite.config.js` (proxy) y `src/services/api.js` (producción).

## Créditos

Diseño basado en [TailAdmin](https://demo.tailadmin.com) — Dashboard admin con React + Tailwind CSS.
