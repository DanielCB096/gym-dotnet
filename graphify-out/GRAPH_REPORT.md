# Graph Report - gimnasio dotnet  (2026-11-22)

## Corpus Check
- 229 files · ~239,988 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 11 file(s) not represented in the graph (top: (none) 3, .css 2, .xml 2)

## Summary
- 3206 nodes · 8426 edges · 156 communities (101 shown, 55 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 840 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c45f856a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Dictionary
- GYM_BANK_TRANSACTIONS
- xunit
- system_collections_generic
- GYM_INVOICES
- BulkImportService
- DatabaseFixture
- GYM_MEMBERS
- CrudPage
- GYM_MEMBERSHIPS
- GYM_USERS
- StaffModule
- GYM_CLASS_TYPES
- GYM_MEMBERSHIP_PLANS
- GYM_PAYMENTS
- SecurityValidation.ValidateAuthentication
- Badge
- BusinessProcess
- GYM_INVENTORY
- GYM_ROUTINE_PLAN_EXERCISES
- GYM_SALES
- App.jsx
- GYM_INVENTORY_MOVEMENTS
- axlogger
- RoutinesModule
- GYM_FISIO_PACKAGES
- InventoryModule
- GYM_ROUTINE_PLANS
- GYM_STAFF_SHIFTS
- GYM_ACCESS_CODES
- GYM_ACCESS_LOG
- Int32
- GYM_CLASS_RESERVATIONS
- GYM_CLASS_SESSIONS
- GYM_MAINTENANCE_TICKETS
- GymService.csproj
- GYM_CASH_SESSIONS
- GYM_EQUIPMENT
- GYM_KIOSK_DEVICES
- GYM_OTP_CODES
- GYM_SCHEDULED_REPORTS
- GYM_SESSIONS
- GYM_SUPPLIERS
- GYM_WORKOUTS
- GYM_PURCHASE_ORDERS
- GYM_BANK_ACCOUNTS
- GYM_FISIO_APPOINTMENTS
- GYM_MEASUREMENTS
- GYM_PERMISSIONS
- GYM_PURCHASE_ORDER_ITEMS
- IDatabaseObject
- GYM_SALE_ITEMS
- GYM_STAFF
- ClassesModule
- GYM_EXERCISES
- Int64
- FisioModule
- CrudPage
- GymService.Tools
- package.json
- MembersModule
- ReportExportService
- ServiceImplementation
- 11. Historial de Sesiones
- GYM_MEMBER_CREDITS
- Fact
- steps
- steps
- AccessModule
- SalesModule.CreateSale
- Backend (.NET)
- api.js
- 6. Esquema de Base de Datos
- CurrentUser
- MaintenanceService
- EmailService
- DatabaseFixture.Scalar
- GymService - Deployment Guide
- FisioAppointmentsPanel.jsx
- MappingObject
- Modal
- DateTime
- GymService - Centro Fit
- StaffShiftsPanel.jsx
- MembershipReceipt
- Phase1FlowTests.Cash_Session_Tracks_Cash_Sales_And_Closes_With_Difference
- Route
- DataCenter
- ActionResult
- FinanceModule
- GymModelGenerator.csproj
- HttpGet
- Rediseño TailAdmin (Frontend)
- .oxlintrc.json
- Hallazgos
- Frontend (React + Vite)
- register-backup-tasks.ps1
- system_linq
- Obstáculos y API del Framework Anxo - GymService
- 4. Cola de tareas
- Obstáculos y API del Framework GymCore - GymService
- Despliegue (producción)
- Phase1FlowTests.Kiosk_Token_Rotation_And_Manual_Access_Work
- Estructura del Framework GymCore
- GymService Frontend
- Resumen de Obstáculos Encontrados
- Resumen de Obstáculos Encontrados
- IdentityModule.VerifyMfaLogin
- GymService - Registro de Cambios y Correcciones
- Respaldo offsite en OneDrive
- HTTPS en red local (acceso por IP)
- CleanupHostedService
- EmailServiceTests
- ControllerBase
- WebServerService
- ObjectToInferredTypesConverter.Read
- SKILL.md
- SecurityValidation.Sha256Hash
- PermissionMatrix
- ImportController
- PermissionCoverageTests.Every_Controller_Route_Has_A_Permission
- SaleItem
- Button
- Kiosco en este PC
- WebServerService
- README.md
- SalesController
- ClassSessionsFlowTests.Sessions_And_Reservations_Respect_Capacity_And_Waitlist
- AccessController
- Pruebas en vivo (obligatorias)
- Reglas de negocio (portadas del proyecto original)
- Test-GymBackupHealth.ps1
- Base de Datos

## God Nodes (most connected - your core abstractions)
1. `AxDatabase` - 71 edges
2. `Backend (.NET)` - 69 edges
3. `GYM_USERS` - 65 edges
4. `IdentityModule` - 59 edges
5. `Button` - 57 edges
6. `GYM_BANK_TRANSACTIONS` - 55 edges
7. `GYM_INVENTORY` - 55 edges
8. `GYM_MEMBERS` - 55 edges
9. `GYM_MEMBERSHIPS` - 53 edges
10. `GYM_MEMBERSHIP_PLANS` - 50 edges

## Surprising Connections (you probably didn't know these)
- `Operación y seguridad (2026-11-19)` --references--> `DatabaseService`  [INFERRED]
  CHANGELOG.md → GymService/Services/DatabaseService.cs
- `Estado actual del sistema` --references--> `useApiList()`  [INFERRED]
  CHANGELOG.md → GymServiceFront/src/hooks/useApiList.js
- `P1 — Endurecimiento de producción pendiente` --references--> `WebServerService`  [INFERRED]
  docs/qa-2026-11-21.md → GymService/Services/WebServerService.cs
- `5. Tareas NO delegadas (las asume la IA grande / Daniel)` --references--> `IdentityModule`  [INFERRED]
  TAREAS_DELEGADAS.md → GymService/Modules/Identity/IdentityModule.cs
- `Obstáculo 1: Propiedad mal nombrada en DataCenter.cs` --references--> `BusinessProcess`  [INFERRED]
  OBSTACLES_AND_API.md → GymService/DataCenter.cs

## Import Cycles
- None detected.

## Communities (156 total, 55 thin omitted)

### Community 0 - "Dictionary"
Cohesion: 0.10
Nodes (4): AuthController, FinanceController, StaffController, SecurityValidation

### Community 1 - "GYM_BANK_TRANSACTIONS"
Cohesion: 0.08
Nodes (28): GYM_BANK_TRANSACTIONS, ACCOUNT_ID, AMOUNT, CREATED_BY, CREATION_DATETIME, DESCRIPTION, FLAGGED, ID (+20 more)

### Community 2 - "xunit"
Cohesion: 0.07
Nodes (16): GymService.Modules.Inventory, GymService.Modules.Sales, GymService.Tests.Integration, GymService.Modules.Members, GymService.Tests, GymService.Modules.Classes, GymService.Modules.Billing, GymService.Modules.Access (+8 more)

### Community 4 - "GYM_INVOICES"
Cohesion: 0.08
Nodes (24): GYM_INVOICES, AMOUNT, CREATED_BY, CREATION_DATETIME, ID, InDatabase, INVOICE_DATE, INVOICE_NUMBER (+16 more)

### Community 5 - "BulkImportService"
Cohesion: 0.08
Nodes (22): Importación de socios con correos duplicados (2026-11-20), Sesión 15: Ingreso masivo de datos (CSV/Excel), BulkImportError, ErrorMessage, RowData, RowNumber, BulkImportResult, ErrorRows (+14 more)

### Community 6 - "DatabaseFixture"
Cohesion: 0.07
Nodes (16): CatalogFlowTests, DatabaseCollection, DatabaseFixture, Available, ConnectionString, Manager, SkipReason, Tools (+8 more)

### Community 7 - "GYM_MEMBERS"
Cohesion: 0.08
Nodes (27): GYM_MEMBERS, ADDRESS, CREATED_BY, CREATION_DATETIME, CURP, EMAIL, EMERGENCY_CONTACT, EMERGENCY_PHONE (+19 more)

### Community 8 - "CrudPage"
Cohesion: 0.23
Nodes (14): errorMessage(), RoutineAssignmentsPanel(), Table(), Cash(), errorMessage(), GenericPage(), errorMessage(), movementInfo (+6 more)

### Community 9 - "GYM_MEMBERSHIPS"
Cohesion: 0.08
Nodes (26): Correos transaccionales con MailKit (2026-11-20), GYM_MEMBERSHIPS, AUTO_RENEW, CANCEL_REASON, CANCELLED_AT, CREATED_BY, CREATION_DATETIME, END_DATE (+18 more)

### Community 10 - "GYM_USERS"
Cohesion: 0.08
Nodes (27): GYM_USERS, CREATED_BY, CREATION_DATETIME, EMAIL, FAILED_LOGIN_ATTEMPTS, FULL_NAME, ID, InDatabase (+19 more)

### Community 12 - "GYM_CLASS_TYPES"
Cohesion: 0.08
Nodes (21): GYM_CLASS_TYPES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, DURATION_MINUTES, ID, InDatabase, IS_ACTIVE (+13 more)

### Community 13 - "GYM_MEMBERSHIP_PLANS"
Cohesion: 0.09
Nodes (23): GYM_MEMBERSHIP_PLANS, CREATED_BY, CREATION_DATETIME, CURRENCY, DESCRIPTION, DURATION_DAYS, ID, INCLUDES_CLASSES (+15 more)

### Community 14 - "GYM_PAYMENTS"
Cohesion: 0.08
Nodes (23): GYM_PAYMENTS, AMOUNT, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, CURRENCY, ID, InDatabase (+15 more)

### Community 16 - "Badge"
Cohesion: 0.13
Nodes (20): MaintenanceEquipmentPanel(), statusOptions, statusVariants, errorMessage(), MaintenanceTicketsPanel(), priorityLabel, priorityOptions, priorityVariant (+12 more)

### Community 17 - "BusinessProcess"
Cohesion: 0.06
Nodes (17): BusinessProcess, Access, Billing, Cash, Classes, Dashboard, Finance, Fisio (+9 more)

### Community 18 - "GYM_INVENTORY"
Cohesion: 0.09
Nodes (23): GYM_INVENTORY, CATEGORY, COST, CREATED_BY, CREATION_DATETIME, DESCRIPTION, ID, InDatabase (+15 more)

### Community 19 - "GYM_ROUTINE_PLAN_EXERCISES"
Cohesion: 0.08
Nodes (22): Rutinas completas (2026-11-19), GYM_ROUTINE_PLAN_EXERCISES, CREATED_BY, CREATION_DATETIME, DAY_NUMBER, EXERCISE_ID, ID, InDatabase (+14 more)

### Community 20 - "GYM_SALES"
Cohesion: 0.09
Nodes (24): GYM_SALES, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IS_OFFLINE, IsChanged (+16 more)

### Community 21 - "App.jsx"
Cohesion: 0.10
Nodes (33): dependencies, axios@^1.21.1, lucide-react@^1.75.0, qrcode.react@^4.2.0, react@^19.3.1, react-dom@^19.3.1, react-router-dom@^7.18.6, App() (+25 more)

### Community 22 - "GYM_INVENTORY_MOVEMENTS"
Cohesion: 0.09
Nodes (23): GYM_INVENTORY_MOVEMENTS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+15 more)

### Community 26 - "GYM_FISIO_PACKAGES"
Cohesion: 0.09
Nodes (22): GYM_FISIO_PACKAGES, CREATED_BY, CREATION_DATETIME, END_DATE, ID, InDatabase, IS_ACTIVE, IsChanged (+14 more)

### Community 28 - "GYM_ROUTINE_PLANS"
Cohesion: 0.09
Nodes (19): GYM_ROUTINE_PLANS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, DIFFICULTY, DURATION_MINUTES, ID, InDatabase (+11 more)

### Community 29 - "GYM_STAFF_SHIFTS"
Cohesion: 0.09
Nodes (22): GYM_STAFF_SHIFTS, CHECK_IN, CHECK_OUT, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged (+14 more)

### Community 30 - "GYM_ACCESS_CODES"
Cohesion: 0.09
Nodes (21): GYM_ACCESS_CODES, CODE_HASH, CREATED_BY, CREATION_DATETIME, EXPIRES_AT, GENERATED_AT, HMAC_KEY, ID (+13 more)

### Community 31 - "GYM_ACCESS_LOG"
Cohesion: 0.09
Nodes (21): GYM_ACCESS_LOG, ACCESS_RESULT, ACCESS_TYPE, ACCESSED_AT, AUTHORIZED_BY, CREATED_BY, CREATION_DATETIME, DEVICE_ID (+13 more)

### Community 32 - "Int32"
Cohesion: 0.09
Nodes (19): GYM_ROUTINE_ASSIGNMENTS, ASSIGNED_AT, ASSIGNED_BY, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IS_ACTIVE (+11 more)

### Community 33 - "GYM_CLASS_RESERVATIONS"
Cohesion: 0.09
Nodes (19): GYM_CLASS_RESERVATIONS, ATTENDED, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+11 more)

### Community 34 - "GYM_CLASS_SESSIONS"
Cohesion: 0.09
Nodes (21): GYM_CLASS_SESSIONS, CLASS_TYPE_ID, CREATED_BY, CREATION_DATETIME, END_TIME, ID, InDatabase, INSTRUCTOR_ID (+13 more)

### Community 35 - "GYM_MAINTENANCE_TICKETS"
Cohesion: 0.08
Nodes (22): GYM_MAINTENANCE_TICKETS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, EQUIPMENT_ID, ID, InDatabase, IsChanged (+14 more)

### Community 36 - "GymService.csproj"
Cohesion: 0.08
Nodes (20): net10.0-windows7.0, GymService.Tests, net10.0-windows7.0, Microsoft.NET.Sdk, BouncyCastle.Cryptography (2.4.0), ClosedXML (0.104.2), CsvHelper (33.0.1), MailKit (5.0.0) (+12 more)

### Community 37 - "GYM_CASH_SESSIONS"
Cohesion: 0.09
Nodes (23): GYM_CASH_SESSIONS, CLOSED_AT, CLOSED_BY, CLOSING_AMOUNT, CREATED_BY, CREATION_DATETIME, EXPECTED_CASH, ID (+15 more)

### Community 38 - "GYM_EQUIPMENT"
Cohesion: 0.10
Nodes (20): GYM_EQUIPMENT, CATEGORY, CREATED_BY, CREATION_DATETIME, EQUIPMENT_NAME, ID, InDatabase, IsChanged (+12 more)

### Community 39 - "GYM_KIOSK_DEVICES"
Cohesion: 0.09
Nodes (19): GYM_KIOSK_DEVICES, CREATED_BY, CREATION_DATETIME, DEVICE_NAME, DEVICE_TOKEN_HASH, ID, InDatabase, IS_ACTIVE (+11 more)

### Community 40 - "GYM_OTP_CODES"
Cohesion: 0.09
Nodes (19): GYM_OTP_CODES, CODE_HASH, CREATED_BY, CREATION_DATETIME, EMAIL, EXPIRES_AT, ID, InDatabase (+11 more)

### Community 41 - "GYM_SCHEDULED_REPORTS"
Cohesion: 0.08
Nodes (22): GYM_SCHEDULED_REPORTS, CREATED_BY, CREATION_DATETIME, EMAIL_RECIPIENTS, ID, InDatabase, IS_ACTIVE, IsChanged (+14 more)

### Community 42 - "GYM_SESSIONS"
Cohesion: 0.09
Nodes (20): GYM_SESSIONS, CREATED_BY, CREATION_DATETIME, DEVICE_INFO, EXPIRES_AT, ID, InDatabase, IP_ADDRESS (+12 more)

### Community 43 - "GYM_SUPPLIERS"
Cohesion: 0.09
Nodes (21): GYM_SUPPLIERS, ADDRESS, CONTACT_NAME, CREATED_BY, CREATION_DATETIME, EMAIL, ID, InDatabase (+13 more)

### Community 44 - "GYM_WORKOUTS"
Cohesion: 0.09
Nodes (21): GYM_WORKOUTS, CREATED_BY, CREATION_DATETIME, DURATION_MINUTES, EXERCISE_ID, ID, InDatabase, IsChanged (+13 more)

### Community 45 - "GYM_PURCHASE_ORDERS"
Cohesion: 0.11
Nodes (19): GYM_PURCHASE_ORDERS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 46 - "GYM_BANK_ACCOUNTS"
Cohesion: 0.09
Nodes (20): GYM_BANK_ACCOUNTS, ACCOUNT_NUMBER, BALANCE, BANK_NAME, CLABE, CREATED_BY, CREATION_DATETIME, CURRENCY (+12 more)

### Community 47 - "GYM_FISIO_APPOINTMENTS"
Cohesion: 0.09
Nodes (19): GYM_FISIO_APPOINTMENTS, APPOINTMENT_DATE, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+11 more)

### Community 48 - "GYM_MEASUREMENTS"
Cohesion: 0.10
Nodes (20): GYM_MEASUREMENTS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+12 more)

### Community 49 - "GYM_PERMISSIONS"
Cohesion: 0.10
Nodes (18): GYM_PERMISSIONS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+10 more)

### Community 50 - "GYM_PURCHASE_ORDER_ITEMS"
Cohesion: 0.10
Nodes (19): GYM_PURCHASE_ORDER_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 51 - "IDatabaseObject"
Cohesion: 0.10
Nodes (19): GYM_PUSH_SUBSCRIPTIONS, AUTH, BROWSER, CREATED_BY, CREATION_DATETIME, ENDPOINT, ID, InDatabase (+11 more)

### Community 52 - "GYM_SALE_ITEMS"
Cohesion: 0.09
Nodes (19): GYM_SALE_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 53 - "GYM_STAFF"
Cohesion: 0.09
Nodes (20): GYM_STAFF, CREATED_BY, CREATION_DATETIME, DEPARTMENT, EMPLOYEE_NAME, HIRE_DATE, ID, InDatabase (+12 more)

### Community 55 - "GYM_EXERCISES"
Cohesion: 0.09
Nodes (18): GYM_EXERCISES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, EQUIPMENT, EXERCISE_NAME, ID, InDatabase (+10 more)

### Community 56 - "Int64"
Cohesion: 0.09
Nodes (17): GYM_ROLE_PERMISSIONS, CREATED_BY, CREATION_DATETIME, GRANTED, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+9 more)

### Community 58 - "CrudPage"
Cohesion: 0.20
Nodes (18): buildPayload(), CrudPage(), toInputValue(), ExercisesPanel(), PageHeader(), Classes(), AccountsPanel(), errorMessage() (+10 more)

### Community 60 - "package.json"
Cohesion: 0.10
Nodes (24): devDependencies, oxlint@^1.90.0, tailwindcss@^4.3.5, @tailwindcss/vite@^4.3.5, @types/react@^19.3.0, @types/react-dom@^19.3.0, vite@^8.3.6, @vitejs/plugin-react@^6.1.2 (+16 more)

### Community 61 - "MembersModule"
Cohesion: 0.11
Nodes (4): MembersModule, MembersFlowTests, MembershipCreditTests, Phase1FlowTests

### Community 62 - "ReportExportService"
Cohesion: 0.19
Nodes (3): Sesión 16: Exportación de reportes a Excel, ReportExportService, ReportsExportTests

### Community 64 - "11. Historial de Sesiones"
Cohesion: 0.10
Nodes (19): Obstáculo 10: DatabaseAdministrator API inventada → ✓ RESUELTO, 11. Historial de Sesiones, API de DatabaseAdministrator (hallazgo clave), API de transacciones (hallazgo clave), Sesión 10: Paginación y filtros en endpoints GET, Sesión 11: Health checks para monitoreo del servicio, Sesión 12: Validación de roles y permisos por endpoint, Sesión 13: Windows Service deployment (+11 more)

### Community 65 - "GYM_MEMBER_CREDITS"
Cohesion: 0.09
Nodes (23): Cancelación con crédito proporcional (2026-11-20), GYM_MEMBER_CREDITS, AMOUNT, APPLIED_AT, CREATED_BY, CREATION_DATETIME, CURRENCY, ID (+15 more)

### Community 66 - "Fact"
Cohesion: 0.07
Nodes (10): Reportes Excel de caja, membresías y kardex (2026-11-20), ModelGym, MappingObjects, SchemaTests, MappingSchemaTests, ModelGymTests, ObjectToInferredTypesConverterTests, PermissionMatrixTests (+2 more)

### Community 67 - "steps"
Cohesion: 0.10
Nodes (19): runs-on, steps, GYM_TEST_DB_PASSWORD, GYM_TEST_DB_SERVER, GYM_TEST_DB_USER, jobs, build, name (+11 more)

### Community 68 - "steps"
Cohesion: 0.10
Nodes (19): jobs, publish, name, on, push, workflow_dispatch, runs-on, steps (+11 more)

### Community 69 - "AccessModule"
Cohesion: 0.23
Nodes (3): Corrección de fechas locales frente a SQL en UTC (2026-11-20), AccessModule, AccessFlowTests

### Community 71 - "Backend (.NET)"
Cohesion: 0.03
Nodes (60): appsettings.json, Asignación y renovación de membresía (2026-11-19), Audio de acceso en el kiosco (2026-11-21), Backend (.NET), Ciclo de vida de usuarios y contraseñas (2026-11-22, versión 1.7.3), Clases y reservas (2026-11-19), Compilación portable de ANXO (2026-11-20), Contención: cuenta virtual del servicio (2026-11-21, versión 1.7.1) (+52 more)

### Community 72 - "api.js"
Cohesion: 0.15
Nodes (22): CancelMembershipModal(), errorMessage(), addDays(), FisioPackagesPanel(), today(), errorMessage(), membershipLabel, MembershipModal() (+14 more)

### Community 73 - "6. Esquema de Base de Datos"
Cohesion: 0.06
Nodes (34): 10. API REST - Endpoints, 1. ¿Qué es GymService?, 2. Framework GymCore - Bibliotecas Usadas, 3.1. Conexión a Base de Datos, 3.2. Consultas SELECT (DatabaseTools), 3.3. INSERT / UPDATE / DELETE (modelo de objetos), 3.4. Lectura de Row (sin indexador []), 3.5. Logger (+26 more)

### Community 74 - "CurrentUser"
Cohesion: 0.33
Nodes (5): Correcciones posteriores a la migración (2026-11-19), CurrentUser, ForAudit, Id, Pruebas

### Community 77 - "EmailService"
Cohesion: 0.24
Nodes (4): EmailService, HasOutbox, IsConfigured, IsEnabled

### Community 79 - "GymService - Deployment Guide"
Cohesion: 0.08
Nodes (25): Build, Built-in Commands, Configuration, First-Time Setup, GymService - Deployment Guide, Health Checks, Installation, Logs (+17 more)

### Community 80 - "FisioAppointmentsPanel.jsx"
Cohesion: 0.36
Nodes (8): defaultAppointmentDate(), errorMessage(), FisioAppointmentsPanel(), pad(), statusLabel, statusOptions, statusVariant, toLocalInput()

### Community 82 - "Modal"
Cohesion: 0.22
Nodes (15): addDaysInput(), ClassSessionsPanel(), errorMessage(), pad(), reservationStatusLabel, reservationStatusVariant, sessionStatusVariant, todayInput() (+7 more)

### Community 83 - "DateTime"
Cohesion: 0.29
Nodes (3): Dashboard con tendencias (2026-11-20), DashboardModule, DashboardFlowTests

### Community 84 - "GymService - Centro Fit"
Cohesion: 0.11
Nodes (19): API REST, Autenticación, Centro Fit Gym Manager, Compilación, Ejecución (modo consola), Estado del Proyecto, Estructura del Proyecto, Grafo del código (graphify) (+11 more)

### Community 85 - "StaffShiftsPanel.jsx"
Cohesion: 0.35
Nodes (10): addDays(), buildDateTime(), dateValue(), emptyForm(), errorMessage(), formatDuration(), formatTime(), StaffShiftsPanel() (+2 more)

### Community 86 - "MembershipReceipt"
Cohesion: 0.18
Nodes (11): MembershipReceipt, Amount, CreditApplied, Currency, EndDate, NoCharge, PaymentDate, PaymentMethod (+3 more)

### Community 88 - "Route"
Cohesion: 0.11
Nodes (3): ClassesController, FisioController, InventoryController

### Community 89 - "DataCenter"
Cohesion: 0.18
Nodes (8): DataCenter, BusinessProcess, Configuration, ConnectionData, DatabaseManager, DatabaseService, EmailService, WebServer

### Community 90 - "ActionResult"
Cohesion: 0.12
Nodes (3): KioskController, MembersController, ProcurementController

### Community 94 - "Rediseño TailAdmin (Frontend)"
Cohesion: 0.22
Nodes (9): Colores y tipografía, Problemas de responsividad, Rediseño TailAdmin (Frontend), Tarea D01 — README desactualizado, Tarea F01 — Colores `primary-*` residuales en Members y Sales, Tarea F03 — Logs de depuración en api.js, Tarea F06 — Página 404 real, Tarea F07 — Persistir sidebar y accesibilidad en Layout (+1 more)

### Community 95 - ".oxlintrc.json"
Cohesion: 0.33
Nodes (5): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 96 - "Hallazgos"
Cohesion: 0.08
Nodes (24): 1. Rebranding: retirar la marca anterior, 2. Otros pendientes de entrega, 3. Hallazgos del QA completo (2026-11-21), Fase C (opcional, white-label completo), Hecho (A + B), Pendiente operativo del despliegue, Pendientes de entrega, Restos deliberados (fase C) (+16 more)

### Community 104 - "Frontend (React + Vite)"
Cohesion: 0.29
Nodes (7): api.js, AuthContext.jsx, Dashboard.jsx, Frontend (React + Vite), GenericPage.jsx, Login.jsx, vite.config.js

### Community 111 - "Obstáculos y API del Framework Anxo - GymService"
Cohesion: 0.12
Nodes (16): Archivos corregidos en esta sesión, Cómo decompilar DLLs del framework, Estado actual del proyecto (actualizado 18/nov/2026), Exportación de Reportes a Excel (Sesión 16), Ingreso Masivo de Datos (Sesión 15), INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs, Obstáculo 12: Controllers REST — Patrón correcto, Obstáculo 13: Swagger/OpenAPI — Configuración correcta (+8 more)

### Community 112 - "4. Cola de tareas"
Cohesion: 0.12
Nodes (16): 0. Reparto de trabajo (importante), 1. Contexto mínimo, 2. Zonas prohibidas, 3. Reglas de git (dos IAs en el mismo repo), 4. Cola de tareas, 5. Tareas NO delegadas (las asume la IA grande / Daniel), 6. Bitácora, ✅ D01 — Sincronizar README con la realidad (+8 more)

### Community 113 - "Obstáculos y API del Framework GymCore - GymService"
Cohesion: 0.14
Nodes (13): Archivos corregidos en esta sesión, Bibliotecas principales, Cómo decompilar DLLs del framework, Estado actual del proyecto, Estructura del Framework GymCore, INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs, Modelo de objetos (OBLIGATORIO para escrituras), Obstáculos y API del Framework GymCore - GymService (+5 more)

### Community 114 - "Despliegue (producción)"
Cohesion: 0.25
Nodes (8): 1. Publicar, 2. Instalar, 2b. Instalador MSI (WiX), 3. Configuración mínima de producción, 3b. HTTPS en red local (solo IP), 4. Operación, 5. Checklist antes de entregar, Despliegue (producción)

### Community 116 - "Estructura del Framework GymCore"
Cohesion: 0.40
Nodes (5): Bibliotecas principales, Estructura del Framework GymCore, Patrón de acceso a base de datos, Patrón de módulos de negocio (referencia: CombuExpressBusinessProcess), Patrón de servicio (Windows Service)

### Community 117 - "GymService Frontend"
Cohesion: 0.17
Nodes (11): Autenticación, Créditos, Despliegue en IIS, Estructura del proyecto, GymService Frontend, Instalación, Proxy de API, Requisitos (+3 more)

### Community 118 - "Resumen de Obstáculos Encontrados"
Cohesion: 0.17
Nodes (12): Obstáculo 10: API de DatabaseAdministrator, Obstáculo 11: LogLevel ambiguo, Obstáculo 1: Propiedad mal nombrada en DataCenter.cs, Obstáculo 2: Target Framework incompatible, Obstáculo 3: API de Query inventada (~458 errores), Obstáculo 4: Row no soporta indexador [], Obstáculo 5: Session no implementa IDisposable, Obstáculo 6: Logger API incorrecta (+4 more)

### Community 119 - "Resumen de Obstáculos Encontrados"
Cohesion: 0.18
Nodes (11): Obstáculo 11: LogLevel ambiguo, Obstáculo 1: Propiedad mal nombrada en DataCenter.cs, Obstáculo 2: Target Framework incompatible, Obstáculo 3: API de Query inventada (~458 errores), Obstáculo 4: Row no soporta indexador [], Obstáculo 5: Session no implementa IDisposable, Obstáculo 6: Logger API incorrecta, Obstáculo 7: ServiceDefinition API incorrecta (+3 more)

### Community 121 - "GymService - Registro de Cambios y Correcciones"
Cohesion: 0.20
Nodes (9): Correcciones críticas de backend (19-nov-2026), El arranque del servicio ya no borra la base de datos, Estado actual del sistema, GymService - Registro de Cambios y Correcciones, Incidente de datos, Lecciones aprendidas, `membershipType` vacío en el listado de socios, Prueba funcional completa (instancia de prueba en `:5001`) (+1 more)

### Community 122 - "Respaldo offsite en OneDrive"
Cohesion: 0.25
Nodes (8): Contraseña, Destino, Reinstalar/ajustar, Respaldo de la clave MFA, Respaldo offsite en OneDrive, Restaurar un respaldo cifrado, Sincronización con OneDrive, Tarea programada

### Community 123 - "HTTPS en red local (acceso por IP)"
Cohesion: 0.14
Nodes (14): Android, Arquitectura y archivos, Diagnóstico rápido, Emisión y renovación, Firefox, Firewall y alcance, HTTPS en red local (acceso por IP), Instalar la CA raíz en los clientes (+6 more)

### Community 124 - "CleanupHostedService"
Cohesion: 0.25
Nodes (8): AGENTS.md — GymService (.NET), Antes de escribir código, Convenciones, Documentación obligatoria, Git, graphify, Pruebas y verificación, Stack y estructura

### Community 126 - "ControllerBase"
Cohesion: 0.15
Nodes (3): BillingController, CashController, DashboardController

### Community 127 - "WebServerService"
Cohesion: 0.10
Nodes (8): Reglas no negociables, Health Checks (Sesión 11), CleanupHostedService, GymHealthCheck, GymReadinessCheck, RolePermissionMiddleware, Configuración, Seguridad y operación

### Community 131 - "PermissionMatrix"
Cohesion: 0.25
Nodes (8): Cierre del frontend: Inventario, Ventas y Membresías (18-nov-2026), Decisiones, Pendiente en frontend, Páginas con CRUD completo, Rediseño del frontend a CRUD real (18-nov-2026), Sesión 17: Frontend con CRUD real, Sesión 18: Cierre de Inventario, Ventas y Membresías, PermissionMatrix

### Community 137 - "SaleItem"
Cohesion: 0.33
Nodes (6): SaleItem, Price, ProductId, ProductName, Quantity, StockOnHand

### Community 138 - "Button"
Cohesion: 0.13
Nodes (30): ENCODINGS, ENTITIES, filenameFromDisposition(), FORMATS, ImportDialog(), saveBlob(), errorMessage(), measurementTypes (+22 more)

### Community 139 - "Kiosco en este PC"
Cohesion: 0.29
Nodes (6): Accesos directos, Dispositivo, Kiosco en este PC, Lanzador, Reconfigurar el token, Requisitos

### Community 143 - "README.md"
Cohesion: 0.18
Nodes (5): Monitoreo de respaldos, Qué se revisa, Respaldo completo diario, Tareas, Uso

### Community 151 - "Pruebas en vivo (obligatorias)"
Cohesion: 0.40
Nodes (5): Evidencia mínima, Excepción, Historial, Pruebas en vivo (obligatorias), Regla

### Community 152 - "Reglas de negocio (portadas del proyecto original)"
Cohesion: 0.40
Nodes (5): Control de acceso, Estado en esta implementación, Membresías y socios, Principios generales, Reglas de negocio (portadas del proyecto original)

### Community 153 - "Test-GymBackupHealth.ps1"
Cohesion: 0.83
Nodes (3): Add-Issue(), Add-Warning(), Write-Log()

### Community 154 - "Base de Datos"
Cohesion: 0.67
Nodes (3): Base de Datos, Regenerar el modelo y las entidades, SQL Server Express (instalación local)

## Knowledge Gaps
- **535 isolated node(s):** `net10.0-windows7.0`, `ClosedXML (0.104.2)`, `CsvHelper (33.0.1)`, `Microsoft.Extensions.Configuration.Json (10.0.0)`, `Microsoft.Extensions.Configuration.Binder (10.0.0)` (+530 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1129 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **55 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GymService - Registro de Cambios y Correcciones` connect `GymService - Registro de Cambios y Correcciones` to `Frontend (React + Vite)`, `PermissionMatrix`, `Rediseño TailAdmin (Frontend)`, `Backend (.NET)`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **What connects `net10.0-windows7.0`, `ClosedXML (0.104.2)`, `CsvHelper (33.0.1)` to the rest of the system?**
  _535 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.0957372466806429 - nodes in this community are weakly interconnected._
- **Why does `Backend (.NET)` connect `Backend (.NET)` to `GYM_MEMBER_CREDITS`, `Fact`, `AccessModule`, `BulkImportService`, `PermissionCoverageTests.Every_Controller_Route_Has_A_Permission`, `GYM_MEMBERSHIPS`, `CurrentUser`, `DateTime`, `GYM_ROUTINE_PLAN_EXERCISES`, `GymService - Registro de Cambios y Correcciones`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Should `GYM_BANK_TRANSACTIONS` be split into smaller, more focused modules?**
  _Cohesion score 0.07957957957957958 - nodes in this community are weakly interconnected._
- **Why does `Estado actual del sistema` connect `GymService - Registro de Cambios y Correcciones` to `Modal`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **Should `xunit` be split into smaller, more focused modules?**
  _Cohesion score 0.07390648567119155 - nodes in this community are weakly interconnected._