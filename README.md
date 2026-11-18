# GymService - Centro Fit

Nueva versión del sistema de gestión de gimnasios **Centro Fit**, reescrita en **.NET 8** que corre como **servicio de Windows**, reutilizando las bibliotecas ANXO de PILOT TMS.

## Stack Técnico

- **Backend**: .NET 8 con ASP.NET Core Kestrel
- **Servicio Windows**: AnxoServiceTools
- **Base de datos**: SQL Server con AnxoDatabase y AnxoObjectDatabaseManager
- **Bibliotecas**: AnxoLibrary, AnxoServiceTools, AnxoDatabase, AnxoObjectDatabaseManager
- **API REST**: Controllers con DTOs, Swagger/OpenAPI
- **Autenticación**: Sesiones Bearer opacas, MFA TOTP, OTP por correo
- **Frontend**: Panel web Next.js y app móvil React Native (reutilizados)

## Estructura del Proyecto

```
gimnasio-dotnet/
├── GymService/
│   ├── GymService.csproj          # Proyecto .NET 8
│   ├── Program.cs                 # Entry point
│   ├── DataCenter.cs              # Estado global
│   ├── ServiceImplementation.cs   # IServiceConsole
│   ├── appsettings.json           # Configuración
│   ├── nls.xml                    # Localización
│   ├── Controllers/               # API REST
│   │   └── AuthController.cs
│   ├── DTOs/                      # Request/Response DTOs
│   ├── Modules/                   # Lógica de negocio
│   │   ├── Identity/              # Autenticación, MFA, OTP, RBAC
│   │   ├── Members/               # Socios y membresías
│   │   ├── Access/                # Códigos rotativos, check-in
│   │   ├── Sales/                 # Punto de venta
│   │   ├── Inventory/             # Productos y stock
│   │   ├── Classes/               # Clases y reservas
│   │   ├── Routines/              # Rutinas y ejercicios
│   │   ├── Fisio/                 # Fisioterapia
│   │   ├── Maintenance/           # Equipos y tickets
│   │   ├── Billing/               # Facturación CFDI
│   │   ├── Finance/               # Cuentas bancarias
│   │   ├── Reports/               # Reportes
│   │   ├── Kiosk/                 # Dispositivos kiosco
│   │   └── Staff/                 # Personal y push
│   ├── Models/
│   │   └── ModelGym.cs            # 35 tablas SQL Server
│   ├── Services/
│   │   ├── WebServerService.cs    # Kestrel
│   │   └── DatabaseService.cs     # Esquema de BD
│   └── Tools/
│       └── SecurityValidation.cs  # Validación JWT/sesiones
└── GymServiceSetup/               # Instalador WiX (pendiente)
```

## Módulos

| Módulo | Descripción |
|--------|-------------|
| **Identity** | Autenticación, sesiones Bearer, MFA TOTP, OTP por correo, RBAC, bootstrap de dueño |
| **Members** | Socios, membresías, ciclo de vida (freeze/resume/cancel), pagos manuales |
| **Access** | Códigos rotativos HMAC, check-in, bitácora, override manual |
| **Sales** | Punto de venta, devoluciones, ventas offline, tickets PDF |
| **Inventory** | Productos, stock, proveedores, órdenes de compra |
| **Classes** | Tipos, salones, sesiones, reservas, lista de espera, asistencia |
| **Routines** | Planes, ejercicios, asignaciones, entrenamientos, medidas, metas |
| **Fisio** | Paquetes, citas, historial |
| **Maintenance** | Equipos, tickets, bitácora técnica, rondas |
| **Billing** | Facturas CFDI 4.0 (diferido), complementos de pago PPD |
| **Finance** | Cuentas bancarias, importación CSV, conciliación |
| **Reports** | Reportes financieros y operativos, CSV programados |
| **Kiosk** | Dispositivos, tokens de 90 días, validación de códigos |
| **Staff** | Personal, roles, permisos, push notifications |

## Base de Datos

35 tablas SQL Server definidas en `Models/ModelGym.cs`:

- `gym_users` - Usuarios del sistema
- `gym_permissions` - Permisos RBAC
- `gym_role_permissions` - Asignación de permisos por rol
- `gym_sessions` - Sesiones Bearer
- `gym_otp_codes` - Códigos OTP por correo
- `gym_members` - Socios
- `gym_membership_plans` - Planes de membresía
- `gym_memberships` - Membresías activas
- `gym_payments` - Pagos manuales
- `gym_access_codes` - Códigos rotativos
- `gym_access_log` - Bitácora de accesos
- `gym_kiosk_devices` - Dispositivos kiosco
- `gym_inventory` - Productos
- `gym_sales` - Ventas
- `gym_sale_items` - Items de venta
- `gym_suppliers` - Proveedores
- `gym_purchase_orders` - Órdenes de compra
- `gym_class_types` - Tipos de clase
- `gym_class_sessions` - Sesiones de clase
- `gym_class_reservations` - Reservas
- `gym_routine_plans` - Planes de rutina
- `gym_exercises` - Ejercicios
- `gym_routine_assignments` - Asignaciones
- `gym_workouts` - Entrenamientos
- `gym_measurements` - Medidas corporales
- `gym_fisio_packages` - Paquetes de fisioterapia
- `gym_fisio_appointments` - Citas de fisioterapia
- `gym_equipment` - Equipos
- `gym_maintenance_tickets` - Tickets de mantenimiento
- `gym_invoices` - Facturas
- `gym_bank_accounts` - Cuentas bancarias
- `gym_bank_transactions` - Transacciones bancarias
- `gym_scheduled_reports` - Reportes programados
- `gym_staff` - Personal
- `gym_push_subscriptions` - Suscripciones Web Push

## API REST

### Autenticación

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/auth/login` | POST | Login con username/password |
| `/api/auth/logout` | POST | Cerrar sesión |
| `/api/auth/mfa/setup` | POST | Configurar MFA TOTP |
| `/api/auth/mfa/verify` | POST | Verificar código MFA |
| `/api/auth/otp/request` | POST | Solicitar OTP por correo |
| `/api/auth/otp/verify` | POST | Verificar OTP |
| `/api/auth/bootstrap` | POST | Crear primer usuario dueño |

## Configuración

Edita `appsettings.json`:

```json
{
  "Database": {
    "Server": "localhost",
    "Database": "GymManager",
    "User": "sa",
    "Password": "tu_password",
    "Port": 1433,
    "PoolSize": 10
  },
  "WebServer": {
    "Port": 5000,
    "Urls": "http://localhost:5000"
  }
}
```

## Compilación

```powershell
cd C:\proyectos\gimnasio-dotnet\GymService
dotnet build
```

## Ejecución (modo consola)

```powershell
cd C:\proyectos\gimnasio-dotnet\GymService
dotnet run
```

## Instalación como servicio Windows

```powershell
# Instalar
.\GymService.exe -install

# Desinstalar
.\GymService.exe -uninstall
```

## Roles

| Rol | Descripción |
|-----|-------------|
| **OWNER** | Dueño, acceso total, MFA TOTP opcional |
| **MANAGER** | Gerente, gestión operativa |
| **RECEPTION** | Recepción, acceso, ventas, clases |
| **ACCOUNTING** | Contabilidad, facturación y finanzas |
| **MEMBER** | Socio, app móvil con OTP |
| **EMPLOYEE** | Personal del gimnasio |

## Estado del Proyecto

- ✅ Estructura del proyecto .NET 8
- ✅ Servicio Windows con AnxoServiceTools
- ✅ Conexión a SQL Server con AnxoDatabase
- ✅ Servidor Kestrel con ASP.NET Core
- ✅ 35 tablas SQL Server definidas
- ✅ 14 módulos de negocio (stubs)
- ✅ Módulo Identity completo (login, MFA, OTP, RBAC)
- ✅ AuthController con endpoints REST
- ⏳ Controllers restantes (Step 2-8)
- ⏳ Instalador WiX (Step 9)
- ⏳ Documentación completa

## Migración desde PostgreSQL

El gimnasio actual usa PostgreSQL. Para migrar a SQL Server:

1. Exportar datos desde PostgreSQL con `pg_dump`
2. Transformar el schema a SQL Server
3. Importar datos con scripts de migración
4. Validar integridad de datos

## Licencia

Propietario - ANXO / Centro Fit
