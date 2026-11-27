# Graph Report - gimnasio-dotnet  (2026-11-27)

## Corpus Check
- 223 files · ~256,734 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 11 file(s) not represented in the graph (top: (none) 3, .css 2, .xml 2)

## Summary
- 3335 nodes · 8872 edges · 154 communities (102 shown, 52 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 876 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d70d84e0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Dictionary
- GYM_BANK_TRANSACTIONS
- xunit
- system_collections_generic
- GYM_INVOICES
- BulkImportResult
- DatabaseFixture
- GYM_MEMBERS
- GYM_MEMBERSHIP_EVENTS
- GYM_MEMBERSHIPS
- GYM_USERS
- FisioAppointmentsPanel.jsx
- GYM_CLASS_TYPES
- GYM_MEMBERSHIP_PLANS
- GYM_PAYMENTS
- react
- FisioModule
- BusinessProcess
- GYM_INVENTORY
- GYM_ROUTINE_PLAN_EXERCISES
- GYM_SALES
- App.jsx
- GYM_INVENTORY_MOVEMENTS
- IdentityModule
- WebServerService.cs
- RoutinesModule
- GYM_FISIO_PACKAGES
- InventoryModule
- GYM_ROUTINE_PLANS
- GYM_STAFF_SHIFTS
- GYM_ACCESS_CODES
- GYM_ACCESS_LOG
- GYM_ROUTINE_ASSIGNMENTS
- GYM_CLASS_RESERVATIONS
- GYM_CLASS_SESSIONS
- GYM_MAINTENANCE_TICKETS
- GymService.csproj
- GYM_CASH_SESSIONS
- GYM_EQUIPMENT
- GYM_KIOSK_DEVICES
- GYM_OTP_CODES
- Int32
- GYM_SESSIONS
- GYM_SUPPLIERS
- GYM_WORKOUTS
- Int32
- GYM_BANK_ACCOUNTS
- GYM_FISIO_APPOINTMENTS
- GYM_MEASUREMENTS
- GYM_PERMISSIONS
- GYM_PURCHASE_ORDER_ITEMS
- GYM_PUSH_SUBSCRIPTIONS
- GYM_SALE_ITEMS
- GYM_STAFF
- ClassesModule
- GYM_EXERCISES
- IDatabaseObject
- Phase1FlowTests.Kiosk_Token_Rotation_And_Manual_Access_Work
- GymService.Tools
- package.json
- MembersModule
- DateTime
- ServiceImplementation
- DatabaseService
- GYM_MEMBER_CREDITS
- Fact
- steps
- steps
- AccessModule
- SalesModule
- MembersModule.CreateMember
- api.js
- SecurityValidation.Sha256Hash
- GymService.Services
- Boolean
- DateTime
- EmailService
- Phase1FlowTests.Kiosk_Token_Rotation_And_Manual_Access_Work
- StaffShiftFlowTests.Shifts_Attendance_And_Summary_Work
- MaintenanceTests.Cleanup_Removes_Expired_Sessions_And_Otp_But_Keeps_Valid_Ones
- FinanceModule
- MappingObject
- List
- SchemaTests
- StaffShiftsPanel.jsx
- MembershipReceipt
- PermissionCoverageTests.Every_Controller_Route_Has_A_Permission
- ClassSessionsPanel.jsx
- FisioModule
- system_linq
- ObjectToInferredTypesConverter.Read
- AnxoModelGenerator.csproj
- SaleItem
- .oxlintrc.json
- dependencies
- ReportsModule
- register-backup-tasks.ps1
- Input
- Obstáculos y API del Framework GymCore - GymService
- 4. Cola de tareas
- Obstáculos y API del Framework GymCore - GymService
- Despliegue (producción)
- Button
- Estructura del Framework GymCore
- GymService Frontend
- Resumen de Obstáculos Encontrados
- Resumen de Obstáculos Encontrados
- SecurityValidation.Sha256Hash
- GymService - Registro de Cambios y Correcciones
- Respaldo offsite en OneDrive
- HTTPS en red local (acceso por IP)
- AGENTS.md — GymService (.NET)
- EmailServiceTests
- SecurityValidation.ValidateAuthentication
- CurrentUser
- ObjectToInferredTypesConverter.Read
- SKILL.md
- FisioAppointmentsPanel.jsx
- ClassSessionsPanel.jsx
- ImportController
- Monitoreo de respaldos
- README.md
- Select
- Kiosco en este PC
- ResultSet
- RoutinePrintPreview.jsx
- KioskController
- IConfiguration
- Badge
- Test-GymBackupHealth.ps1
- PermissionMatrix
- MaintenanceController

## God Nodes (most connected - your core abstractions)
1. `Backend (.NET)` - 77 edges
2. `AxDatabase` - 75 edges
3. `Button` - 71 edges
4. `GYM_USERS` - 65 edges
5. `IdentityModule` - 60 edges
6. `GYM_MEMBERS` - 56 edges
7. `GYM_BANK_TRANSACTIONS` - 55 edges
8. `GYM_INVENTORY` - 55 edges
9. `GYM_MEMBERSHIPS` - 53 edges
10. `Badge()` - 51 edges

## Surprising Connections (you probably didn't know these)
- `Obstáculo 1: Propiedad mal nombrada en DataCenter.cs` --references--> `BusinessProcess`  [INFERRED]
  OBSTACLES_AND_API.md → GymService/DataCenter.cs
- `Membresías y socios` --references--> `GYM_MEMBER_CREDITS`  [INFERRED]
  docs/reglas-negocio.md → GymService/Models/Generated/GYM_MEMBER_CREDITS.cs
- `P1 — Endurecimiento de producción pendiente` --references--> `WebServerService`  [INFERRED]
  docs/qa-2026-11-21.md → GymService/Services/WebServerService.cs
- `5. Tareas NO delegadas (las asume la IA grande / Daniel)` --references--> `IdentityModule`  [INFERRED]
  TAREAS_DELEGADAS.md → GymService/Modules/Identity/IdentityModule.cs
- `Estado actual del sistema` --references--> `useApiList()`  [INFERRED]
  CHANGELOG.md → GymServiceFront/src/hooks/useApiList.js

## Import Cycles
- None detected.

## Communities (154 total, 52 thin omitted)

### Community 0 - "Dictionary"
Cohesion: 0.09
Nodes (3): AuthController, StaffController, SecurityValidation

### Community 1 - "GYM_BANK_TRANSACTIONS"
Cohesion: 0.07
Nodes (30): GYM_BANK_TRANSACTIONS, ACCOUNT_ID, AMOUNT, CREATED_BY, CREATION_DATETIME, DESCRIPTION, FLAGGED, ID (+22 more)

### Community 2 - "xunit"
Cohesion: 0.10
Nodes (15): GymService.Modules.Inventory, GymService.Modules.Sales, GymService.Tests.Integration, GymService.Modules.Members, GymService.Modules.Classes, GymService.Modules.Billing, GymService.Modules.Access, GymService.Modules.Kiosk (+7 more)

### Community 4 - "GYM_INVOICES"
Cohesion: 0.08
Nodes (24): GYM_INVOICES, AMOUNT, CREATED_BY, CREATION_DATETIME, ID, InDatabase, INVOICE_DATE, INVOICE_NUMBER (+16 more)

### Community 5 - "BulkImportResult"
Cohesion: 0.08
Nodes (22): Importación de socios con correos duplicados (2026-11-20), Sesión 15: Ingreso masivo de datos (CSV/Excel), BulkImportError, ErrorMessage, RowData, RowNumber, BulkImportResult, ErrorRows (+14 more)

### Community 6 - "DatabaseFixture"
Cohesion: 0.09
Nodes (12): DatabaseCollection, DatabaseFixture, Available, ConnectionString, Manager, SkipReason, Tools, EmailNotificationFlowTests (+4 more)

### Community 7 - "GYM_MEMBERS"
Cohesion: 0.08
Nodes (27): GYM_MEMBERS, ADDRESS, CREATED_BY, CREATION_DATETIME, CURP, EMAIL, EMERGENCY_CONTACT, EMERGENCY_PHONE (+19 more)

### Community 8 - "GYM_MEMBERSHIP_EVENTS"
Cohesion: 0.08
Nodes (28): GYM_MEMBERSHIP_EVENTS, AMOUNT, CREATED_BY, CREATION_DATETIME, CURRENCY, DETAILS, EVENT_AT, EVENT_TYPE (+20 more)

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
Cohesion: 0.09
Nodes (23): GYM_PAYMENTS, AMOUNT, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, CURRENCY, ID, InDatabase (+15 more)

### Community 17 - "BusinessProcess"
Cohesion: 0.06
Nodes (18): BusinessProcess, Access, Audit, Billing, Cash, Classes, Dashboard, Finance (+10 more)

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
Cohesion: 0.17
Nodes (18): App(), GlobalSearch(), typeMeta, Layout(), menuSections, routeLabels, ProtectedRoute(), AuthContext (+10 more)

### Community 22 - "GYM_INVENTORY_MOVEMENTS"
Cohesion: 0.09
Nodes (23): GYM_INVENTORY_MOVEMENTS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+15 more)

### Community 23 - "IdentityModule"
Cohesion: 0.22
Nodes (14): buildPayload(), CrudPage(), toInputValue(), ExercisesPanel(), MaintenanceEquipmentPanel(), statusOptions, statusVariants, PageHeader() (+6 more)

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

### Community 32 - "GYM_ROUTINE_ASSIGNMENTS"
Cohesion: 0.09
Nodes (19): GYM_ROUTINE_ASSIGNMENTS, ASSIGNED_AT, ASSIGNED_BY, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IS_ACTIVE (+11 more)

### Community 33 - "GYM_CLASS_RESERVATIONS"
Cohesion: 0.09
Nodes (19): GYM_CLASS_RESERVATIONS, ATTENDED, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+11 more)

### Community 34 - "GYM_CLASS_SESSIONS"
Cohesion: 0.09
Nodes (21): GYM_CLASS_SESSIONS, CLASS_TYPE_ID, CREATED_BY, CREATION_DATETIME, END_TIME, ID, InDatabase, INSTRUCTOR_ID (+13 more)

### Community 35 - "GYM_MAINTENANCE_TICKETS"
Cohesion: 0.09
Nodes (21): GYM_MAINTENANCE_TICKETS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, EQUIPMENT_ID, ID, InDatabase, IsChanged (+13 more)

### Community 36 - "GymService.csproj"
Cohesion: 0.08
Nodes (20): net10.0-windows7.0, Microsoft.NET.Sdk, GymService.Tests, net10.0-windows7.0, Microsoft.NET.Sdk, BouncyCastle.Cryptography (2.4.0), ClosedXML (0.104.2), CsvHelper (33.0.1) (+12 more)

### Community 37 - "GYM_CASH_SESSIONS"
Cohesion: 0.09
Nodes (23): GYM_CASH_SESSIONS, CLOSED_AT, CLOSED_BY, CLOSING_AMOUNT, CREATED_BY, CREATION_DATETIME, EXPECTED_CASH, ID (+15 more)

### Community 38 - "GYM_EQUIPMENT"
Cohesion: 0.09
Nodes (20): GYM_EQUIPMENT, CATEGORY, CREATED_BY, CREATION_DATETIME, EQUIPMENT_NAME, ID, InDatabase, IsChanged (+12 more)

### Community 39 - "GYM_KIOSK_DEVICES"
Cohesion: 0.09
Nodes (19): GYM_KIOSK_DEVICES, CREATED_BY, CREATION_DATETIME, DEVICE_NAME, DEVICE_TOKEN_HASH, ID, InDatabase, IS_ACTIVE (+11 more)

### Community 40 - "GYM_OTP_CODES"
Cohesion: 0.09
Nodes (19): GYM_OTP_CODES, CODE_HASH, CREATED_BY, CREATION_DATETIME, EMAIL, EXPIRES_AT, ID, InDatabase (+11 more)

### Community 41 - "Int32"
Cohesion: 0.10
Nodes (21): GYM_SCHEDULED_REPORTS, CREATED_BY, CREATION_DATETIME, EMAIL_RECIPIENTS, ID, InDatabase, IS_ACTIVE, IsChanged (+13 more)

### Community 42 - "GYM_SESSIONS"
Cohesion: 0.09
Nodes (20): GYM_SESSIONS, CREATED_BY, CREATION_DATETIME, DEVICE_INFO, EXPIRES_AT, ID, InDatabase, IP_ADDRESS (+12 more)

### Community 43 - "GYM_SUPPLIERS"
Cohesion: 0.09
Nodes (21): GYM_SUPPLIERS, ADDRESS, CONTACT_NAME, CREATED_BY, CREATION_DATETIME, EMAIL, ID, InDatabase (+13 more)

### Community 44 - "GYM_WORKOUTS"
Cohesion: 0.09
Nodes (21): GYM_WORKOUTS, CREATED_BY, CREATION_DATETIME, DURATION_MINUTES, EXERCISE_ID, ID, InDatabase, IsChanged (+13 more)

### Community 45 - "Int32"
Cohesion: 0.09
Nodes (19): GYM_PURCHASE_ORDERS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 46 - "GYM_BANK_ACCOUNTS"
Cohesion: 0.10
Nodes (20): GYM_BANK_ACCOUNTS, ACCOUNT_NUMBER, BALANCE, BANK_NAME, CLABE, CREATED_BY, CREATION_DATETIME, CURRENCY (+12 more)

### Community 47 - "GYM_FISIO_APPOINTMENTS"
Cohesion: 0.09
Nodes (19): GYM_FISIO_APPOINTMENTS, APPOINTMENT_DATE, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+11 more)

### Community 48 - "GYM_MEASUREMENTS"
Cohesion: 0.10
Nodes (20): GYM_MEASUREMENTS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+12 more)

### Community 49 - "GYM_PERMISSIONS"
Cohesion: 0.07
Nodes (18): GYM_PERMISSIONS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+10 more)

### Community 50 - "GYM_PURCHASE_ORDER_ITEMS"
Cohesion: 0.10
Nodes (19): GYM_PURCHASE_ORDER_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 51 - "GYM_PUSH_SUBSCRIPTIONS"
Cohesion: 0.09
Nodes (19): GYM_PUSH_SUBSCRIPTIONS, AUTH, BROWSER, CREATED_BY, CREATION_DATETIME, ENDPOINT, ID, InDatabase (+11 more)

### Community 52 - "GYM_SALE_ITEMS"
Cohesion: 0.10
Nodes (19): GYM_SALE_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 53 - "GYM_STAFF"
Cohesion: 0.09
Nodes (20): GYM_STAFF, CREATED_BY, CREATION_DATETIME, DEPARTMENT, EMPLOYEE_NAME, HIRE_DATE, ID, InDatabase (+12 more)

### Community 55 - "GYM_EXERCISES"
Cohesion: 0.09
Nodes (18): GYM_EXERCISES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, EQUIPMENT, EXERCISE_NAME, ID, InDatabase (+10 more)

### Community 56 - "IDatabaseObject"
Cohesion: 0.10
Nodes (17): GYM_ROLE_PERMISSIONS, CREATED_BY, CREATION_DATETIME, GRANTED, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+9 more)

### Community 60 - "package.json"
Cohesion: 0.07
Nodes (34): dependencies, axios@^1.21.1, lucide-react@^1.75.0, qrcode.react@^4.2.0, react@^19.3.1, react-dom@^19.3.1, react-router-dom@^7.18.6, devDependencies (+26 more)

### Community 61 - "MembersModule"
Cohesion: 0.09
Nodes (6): MembersModule, FisioFlowTests, MembersFlowTests, MembershipCreditTests, Phase1FlowTests, RoutinesFlowTests

### Community 62 - "DateTime"
Cohesion: 0.18
Nodes (11): Auditoría, Búsqueda global y notificaciones, Ficha completa del socio, Foto del socio, Gráficas de progreso, Impresión de facturas, Impresión de rutinas, Monitoreo de respaldos (+3 more)

### Community 64 - "DatabaseService"
Cohesion: 0.08
Nodes (25): Cierre del frontend: Inventario, Ventas y Membresías (18-nov-2026), Decisiones, Pendiente en frontend, Páginas con CRUD completo, Rediseño del frontend a CRUD real (18-nov-2026), 11. Historial de Sesiones, API de DatabaseAdministrator (hallazgo clave), API de transacciones (hallazgo clave) (+17 more)

### Community 65 - "GYM_MEMBER_CREDITS"
Cohesion: 0.08
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

### Community 70 - "SalesModule"
Cohesion: 0.17
Nodes (8): SalesModule, SaleItem, Price, ProductId, ProductName, Quantity, StockOnHand, SalesFlowTests

### Community 71 - "MembersModule.CreateMember"
Cohesion: 0.03
Nodes (66): appsettings.json, Asignación y renovación de membresía (2026-11-19), Audio de acceso en el kiosco (2026-11-21), Backend (.NET), Búsqueda global y campana de notificaciones (2026-11-25, versión 1.7.12), Ciclo de vida de usuarios y contraseñas (2026-11-22, versión 1.7.3), Clases y reservas (2026-11-19), Compilación portable de ANXO (2026-11-20) (+58 more)

### Community 72 - "api.js"
Cohesion: 0.15
Nodes (20): errorMessage(), MaintenanceTicketsPanel(), priorityLabel, priorityOptions, priorityVariant, statusLabel, statusOptions, statusVariant (+12 more)

### Community 73 - "SecurityValidation.Sha256Hash"
Cohesion: 0.06
Nodes (34): 10. API REST - Endpoints, 1. ¿Qué es GymService?, 2. Framework GymCore - Bibliotecas Usadas, 3.1. Conexión a Base de Datos, 3.2. Consultas SELECT (DatabaseTools), 3.3. INSERT / UPDATE / DELETE (modelo de objetos), 3.4. Lectura de Row (sin indexador []), 3.5. Logger (+26 more)

### Community 74 - "GymService.Services"
Cohesion: 0.10
Nodes (4): GymService, GymService.Tests, GymService.Modules.Reports, GymService.Services

### Community 76 - "DateTime"
Cohesion: 0.20
Nodes (3): Sesión 16: Exportación de reportes a Excel, ReportExportService, ReportsExportTests

### Community 77 - "EmailService"
Cohesion: 0.24
Nodes (4): EmailService, HasOutbox, IsConfigured, IsEnabled

### Community 79 - "StaffShiftFlowTests.Shifts_Attendance_And_Summary_Work"
Cohesion: 0.08
Nodes (25): Build, Built-in Commands, Configuration, First-Time Setup, GymService - Deployment Guide, Health Checks, Installation, Logs (+17 more)

### Community 81 - "FinanceModule"
Cohesion: 0.29
Nodes (3): Dashboard con tendencias (2026-11-20), DashboardModule, DashboardFlowTests

### Community 83 - "List"
Cohesion: 0.22
Nodes (9): Colores y tipografía, Problemas de responsividad, Rediseño TailAdmin (Frontend), Tarea D01 — README desactualizado, Tarea F01 — Colores `primary-*` residuales en Members y Sales, Tarea F03 — Logs de depuración en api.js, Tarea F06 — Página 404 real, Tarea F07 — Persistir sidebar y accesibilidad en Layout (+1 more)

### Community 84 - "SchemaTests"
Cohesion: 0.11
Nodes (19): API REST, Autenticación, Base de Datos, Centro Fit Gym Manager, Compilación, Ejecución (modo consola), Estado del Proyecto, Estructura del Proyecto (+11 more)

### Community 85 - "StaffShiftsPanel.jsx"
Cohesion: 0.15
Nodes (19): addDays(), buildDateTime(), dateValue(), emptyForm(), errorMessage(), formatDuration(), formatTime(), StaffShiftsPanel() (+11 more)

### Community 86 - "MembershipReceipt"
Cohesion: 0.18
Nodes (11): MembershipReceipt, Amount, CreditApplied, Currency, EndDate, NoCharge, PaymentDate, PaymentMethod (+3 more)

### Community 88 - "ClassSessionsPanel.jsx"
Cohesion: 0.12
Nodes (3): ClassesController, FisioController, InventoryController

### Community 89 - "FisioModule"
Cohesion: 0.18
Nodes (8): DataCenter, BusinessProcess, Configuration, ConnectionData, DatabaseManager, DatabaseService, EmailService, WebServer

### Community 93 - "SaleItem"
Cohesion: 0.13
Nodes (4): AuditController, CashController, DashboardController, ReportsController

### Community 95 - ".oxlintrc.json"
Cohesion: 0.33
Nodes (5): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 96 - "dependencies"
Cohesion: 0.08
Nodes (24): 1. Rebranding: retirar la marca anterior, 2. Otros pendientes de entrega, 3. Hallazgos del QA completo (2026-11-21), Fase C (opcional, white-label completo), Hecho (A + B), Pendiente operativo del despliegue, Pendientes de entrega, Restos deliberados (fase C) (+16 more)

### Community 109 - "Input"
Cohesion: 0.17
Nodes (19): formatShortDate(), ProgressChart(), errorMessage(), measurementTypes, pad(), RoutineMeasurementsPanel(), toLocalInput(), errorMessage() (+11 more)

### Community 111 - "Obstáculos y API del Framework GymCore - GymService"
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

### Community 115 - "Button"
Cohesion: 0.12
Nodes (35): CancelMembershipModal(), errorMessage(), buildHtml(), escapeHtml(), formatDate(), InvoicePrintPreview(), money(), statusLabels (+27 more)

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
Cohesion: 0.14
Nodes (13): Obstáculo 10: DatabaseAdministrator API inventada → ✓ RESUELTO, Obstáculo 11: LogLevel ambiguo, Obstáculo 1: Propiedad mal nombrada en DataCenter.cs, Obstáculo 2: Target Framework incompatible, Obstáculo 3: API de Query inventada (~458 errores), Obstáculo 4: Row no soporta indexador [], Obstáculo 5: Session no implementa IDisposable, Obstáculo 6: Logger API incorrecta (+5 more)

### Community 121 - "GymService - Registro de Cambios y Correcciones"
Cohesion: 0.12
Nodes (16): api.js, AuthContext.jsx, Correcciones críticas de backend (19-nov-2026), Dashboard.jsx, El arranque del servicio ya no borra la base de datos, Estado actual del sistema, Frontend (React + Vite), GenericPage.jsx (+8 more)

### Community 122 - "Respaldo offsite en OneDrive"
Cohesion: 0.25
Nodes (8): Contraseña, Destino, Reinstalar/ajustar, Respaldo de la clave MFA, Respaldo offsite en OneDrive, Restaurar un respaldo cifrado, Sincronización con OneDrive, Tarea programada

### Community 123 - "HTTPS en red local (acceso por IP)"
Cohesion: 0.14
Nodes (14): Android, Arquitectura y archivos, Diagnóstico rápido, Emisión y renovación, Firefox, Firewall y alcance, HTTPS en red local (acceso por IP), Instalar la CA raíz en los clientes (+6 more)

### Community 124 - "AGENTS.md — GymService (.NET)"
Cohesion: 0.25
Nodes (8): AGENTS.md — GymService (.NET), Antes de escribir código, Convenciones, Documentación obligatoria, Git, graphify, Pruebas y verificación, Stack y estructura

### Community 126 - "SecurityValidation.ValidateAuthentication"
Cohesion: 0.13
Nodes (3): BillingController, FinanceController, ProcurementController

### Community 127 - "CurrentUser"
Cohesion: 0.33
Nodes (5): Correcciones posteriores a la migración (2026-11-19), CurrentUser, ForAudit, Id, Pruebas

### Community 130 - "FisioAppointmentsPanel.jsx"
Cohesion: 0.23
Nodes (13): defaultAppointmentDate(), errorMessage(), FisioAppointmentsPanel(), pad(), statusLabel, statusOptions, statusVariant, toLocalInput() (+5 more)

### Community 131 - "ClassSessionsPanel.jsx"
Cohesion: 0.36
Nodes (9): addDaysInput(), ClassSessionsPanel(), errorMessage(), pad(), reservationStatusLabel, reservationStatusVariant, sessionStatusVariant, todayInput() (+1 more)

### Community 136 - "Monitoreo de respaldos"
Cohesion: 0.33
Nodes (5): Monitoreo de respaldos, Qué se revisa, Respaldo completo diario, Tareas, Uso

### Community 137 - "README.md"
Cohesion: 0.13
Nodes (10): Evidencia mínima, Excepción, Historial, Pruebas en vivo (obligatorias), Regla, Control de acceso, Estado en esta implementación, Membresías y socios (+2 more)

### Community 138 - "Select"
Cohesion: 0.21
Nodes (15): ENCODINGS, ENTITIES, filenameFromDisposition(), FORMATS, ImportDialog(), saveBlob(), Select, AccountsPanel() (+7 more)

### Community 139 - "Kiosco en este PC"
Cohesion: 0.29
Nodes (6): Accesos directos, Dispositivo, Kiosco en este PC, Lanzador, Reconfigurar el token, Requisitos

### Community 148 - "RoutinePrintPreview.jsx"
Cohesion: 0.42
Nodes (7): errorMessage(), RoutineAssignmentsPanel(), buildHtml(), difficultyLabel(), escapeHtml(), formatDate(), RoutinePrintPreview()

### Community 151 - "IConfiguration"
Cohesion: 0.08
Nodes (11): Reglas no negociables, Reportes programados con ejecución real (2026-11-22, versión 1.7.6), Health Checks (Sesión 11), CleanupHostedService, GymHealthCheck, GymReadinessCheck, RolePermissionMiddleware, ScheduledReportHostedService (+3 more)

### Community 152 - "Badge"
Cohesion: 0.17
Nodes (22): Badge(), Cash(), errorMessage(), booleanOptions, Plans(), toBooleanString(), errorMessage(), movementInfo (+14 more)

### Community 153 - "Test-GymBackupHealth.ps1"
Cohesion: 0.83
Nodes (3): Add-Issue(), Add-Warning(), Write-Log()

## Knowledge Gaps
- **547 isolated node(s):** `CREATION_DATETIME`, `LAST_UPDATED_DATETIME`, `CREATED_BY`, `LAST_UPDATED_BY`, `LAST_UPDATE_LOG` (+542 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1167 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **52 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Backend (.NET)` connect `MembersModule.CreateMember` to `GYM_MEMBER_CREDITS`, `Fact`, `AccessModule`, `BulkImportResult`, `GYM_MEMBERSHIPS`, `FinanceModule`, `MappingObject`, `GYM_ROUTINE_PLAN_EXERCISES`, `IConfiguration`, `GymService - Registro de Cambios y Correcciones`, `PermissionMatrix`, `CurrentUser`?**
  _High betweenness centrality (0.149) - this node is a cross-community bridge._
- **What connects `CREATION_DATETIME`, `LAST_UPDATED_DATETIME`, `CREATED_BY` to the rest of the system?**
  _547 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.09268292682926829 - nodes in this community are weakly interconnected._
- **Why does `GymService - Registro de Cambios y Correcciones` connect `GymService - Registro de Cambios y Correcciones` to `DatabaseService`, `List`, `MembersModule.CreateMember`?**
  _High betweenness centrality (0.122) - this node is a cross-community bridge._
- **Should `GYM_BANK_TRANSACTIONS` be split into smaller, more focused modules?**
  _Cohesion score 0.06662770309760374 - nodes in this community are weakly interconnected._
- **Why does `Estado actual del sistema` connect `GymService - Registro de Cambios y Correcciones` to `api.js`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Should `xunit` be split into smaller, more focused modules?**
  _Cohesion score 0.0975609756097561 - nodes in this community are weakly interconnected._