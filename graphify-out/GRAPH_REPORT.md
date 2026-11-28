# Graph Report - gimnasio dotnet  (2026-11-28)

## Corpus Check
- 246 files · ~259,429 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 11 file(s) not represented in the graph (top: (none) 3, .css 2, .xml 2)

## Summary
- 3402 nodes · 8931 edges · 169 communities (111 shown, 58 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 879 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c862a5cb`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Dictionary
- GYM_BANK_TRANSACTIONS
- xunit
- system_collections_generic
- GYM_INVOICES
- DatabaseTools
- DatabaseFixture
- GYM_MEMBERS
- GYM_MEMBERSHIPS
- GYM_USERS
- ResultSet
- GYM_CLASS_TYPES
- GYM_MEMBERSHIP_PLANS
- GYM_PAYMENTS
- SecurityValidation.ValidateAuthentication
- ReportExportService
- BusinessProcess
- GYM_INVENTORY
- GYM_ROUTINE_PLAN_EXERCISES
- GYM_SALES
- App.jsx
- GYM_INVENTORY_MOVEMENTS
- CrudPage.jsx
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
- ResultSet
- GYM_EXERCISES
- Int64
- CatalogFlowTests.cs
- DatabaseService
- GymService.Tools
- package.json
- MembersModule
- GYM_MEMBERSHIP_EVENTS
- ServiceImplementation.InitializeBusinessModules
- 11. Historial de Sesiones
- GYM_MEMBER_CREDITS
- Fact
- steps
- steps
- AccessModule
- SalesModule.CreateSale
- Backend (.NET)
- DatabaseFixture.Scalar
- 6. Esquema de Base de Datos
- Billing.jsx
- MaintenanceService
- Input
- EmailService
- DatabaseFixture.Scalar
- GymService - Deployment Guide
- MaintenanceModule
- DateTime
- WebServerService
- Rediseño TailAdmin (Frontend)
- GymService - Centro Fit
- Table
- MembershipReceipt
- Phase1FlowTests.Cash_Session_Tracks_Cash_Sales_And_Closes_With_Difference
- Route
- DataCenter
- ActionResult
- GymModelGenerator.csproj
- HttpGet
- IdentityModule.GetRowValue
- .oxlintrc.json
- Hallazgos
- Frontend (React + Vite)
- register-backup-tasks.ps1
- formatCurrency
- Obstáculos y API del Framework Anxo - GymService
- 4. Cola de tareas
- Obstáculos y API del Framework GymCore - GymService
- Flujo operativo
- Button
- Estructura del Framework GymCore
- GymService Frontend
- Resumen de Obstáculos Encontrados
- Resumen de Obstáculos Encontrados
- SecurityValidation.Sha256Hash
- GymService - Registro de Cambios y Correcciones
- Respaldo offsite en OneDrive
- HTTPS en red local (acceso por IP)
- CleanupHostedService
- EmailServiceTests
- SecurityValidation.ValidateAuthentication
- CurrentUser
- ObjectToInferredTypesConverter.Read
- SKILL.md
- FisioAppointmentsPanel.jsx
- README.md
- ImportController
- FinanceModule
- Monitoreo de respaldos
- Select
- Kiosco en este PC
- Handoff para la PC de casa
- BulkImportService
- Decisión
- KioskController
- Pendientes de entrega
- IConfiguration
- App.jsx
- Test-GymBackupHealth.ps1
- PermissionCoverageTests.Every_Controller_Route_Has_A_Permission
- SecurityValidation.Sha256Hash
- MaintenanceController
- BulkImportEncodingTests.Supported_encodings_are_not_rejected
- Paquete comercial — Membresías y Cobranza
- Runbook — despliegue 1.7.9 → 1.7.13
- SchemaTests
- BulkImportResult
- Finance.jsx
- IdentityModule.RequestOtpByEmail
- Correcciones críticas de backend (19-nov-2026)
- BulkImportService.BuildMember
- AccessController
- MaintenanceFlowTests

## God Nodes (most connected - your core abstractions)
1. `Backend (.NET)` - 79 edges
2. `AxDatabase` - 74 edges
3. `Button` - 70 edges
4. `GYM_USERS` - 65 edges
5. `IdentityModule` - 60 edges
6. `GYM_MEMBERS` - 56 edges
7. `GYM_BANK_TRANSACTIONS` - 55 edges
8. `GYM_INVENTORY` - 55 edges
9. `GYM_MEMBERSHIPS` - 53 edges
10. `GYM_MEMBERSHIP_EVENTS` - 52 edges

## Surprising Connections (you probably didn't know these)
- `1. Estado del repositorio` --references--> `GYM_MEMBERSHIP_EVENTS`  [INFERRED]
  docs/handoff-casa.md → GymService/Models/Generated/GYM_MEMBERSHIP_EVENTS.cs
- `Estado actual del sistema` --references--> `useApiList()`  [INFERRED]
  CHANGELOG.md → GymServiceFront/src/hooks/useApiList.js
- `0. Datos que hay que confirmar antes` --references--> `DatabaseService`  [INFERRED]
  docs/release-1.7.13.md → GymService/Services/DatabaseService.cs
- `P1 — Endurecimiento de producción pendiente` --references--> `WebServerService`  [INFERRED]
  docs/qa-2026-11-21.md → GymService/Services/WebServerService.cs
- `Obstáculo 1: Propiedad mal nombrada en DataCenter.cs` --references--> `BusinessProcess`  [INFERRED]
  OBSTACLES_AND_API.md → GymService/DataCenter.cs

## Import Cycles
- None detected.

## Communities (169 total, 58 thin omitted)

### Community 0 - "Dictionary"
Cohesion: 0.09
Nodes (3): AuthController, StaffController, SecurityValidation

### Community 1 - "GYM_BANK_TRANSACTIONS"
Cohesion: 0.08
Nodes (28): GYM_BANK_TRANSACTIONS, ACCOUNT_ID, AMOUNT, CREATED_BY, CREATION_DATETIME, DESCRIPTION, FLAGGED, ID (+20 more)

### Community 2 - "xunit"
Cohesion: 0.06
Nodes (19): GymService.Modules.Inventory, GymService.Modules.Sales, GymService.Tests.Integration, GymService.Modules.Members, GymService.Tests, GymService.Modules.Classes, GymService.Modules.Billing, GymService.Modules.Access (+11 more)

### Community 4 - "GYM_INVOICES"
Cohesion: 0.08
Nodes (24): GYM_INVOICES, AMOUNT, CREATED_BY, CREATION_DATETIME, ID, InDatabase, INVOICE_DATE, INVOICE_NUMBER (+16 more)

### Community 5 - "DatabaseTools"
Cohesion: 0.19
Nodes (8): BulkImportError, ErrorMessage, RowData, RowNumber, PendingImport, DatabaseObject, RowData, RowNumber

### Community 6 - "DatabaseFixture"
Cohesion: 0.10
Nodes (11): DatabaseCollection, DatabaseFixture, Available, ConnectionString, Manager, SkipReason, Tools, FisioFlowTests (+3 more)

### Community 7 - "GYM_MEMBERS"
Cohesion: 0.08
Nodes (27): GYM_MEMBERS, ADDRESS, CREATED_BY, CREATION_DATETIME, CURP, EMAIL, EMERGENCY_CONTACT, EMERGENCY_PHONE (+19 more)

### Community 9 - "GYM_MEMBERSHIPS"
Cohesion: 0.08
Nodes (26): Correos transaccionales con MailKit (2026-11-20), GYM_MEMBERSHIPS, AUTO_RENEW, CANCEL_REASON, CANCELLED_AT, CREATED_BY, CREATION_DATETIME, END_DATE (+18 more)

### Community 10 - "GYM_USERS"
Cohesion: 0.08
Nodes (27): GYM_USERS, CREATED_BY, CREATION_DATETIME, EMAIL, FAILED_LOGIN_ATTEMPTS, FULL_NAME, ID, InDatabase (+19 more)

### Community 12 - "GYM_CLASS_TYPES"
Cohesion: 0.09
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
Cohesion: 0.09
Nodes (22): Rutinas completas (2026-11-19), GYM_ROUTINE_PLAN_EXERCISES, CREATED_BY, CREATION_DATETIME, DAY_NUMBER, EXERCISE_ID, ID, InDatabase (+14 more)

### Community 20 - "GYM_SALES"
Cohesion: 0.09
Nodes (24): GYM_SALES, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IS_OFFLINE, IsChanged (+16 more)

### Community 21 - "App.jsx"
Cohesion: 0.18
Nodes (22): priorityLabel, priorityOptions, priorityVariant, statusLabel, statusOptions, statusVariant, eventLabel, membershipLabel (+14 more)

### Community 22 - "GYM_INVENTORY_MOVEMENTS"
Cohesion: 0.09
Nodes (23): GYM_INVENTORY_MOVEMENTS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+15 more)

### Community 23 - "CrudPage.jsx"
Cohesion: 0.12
Nodes (28): buildPayload(), CrudPage(), toInputValue(), ExercisesPanel(), MaintenanceEquipmentPanel(), statusOptions, statusVariants, errorMessage() (+20 more)

### Community 26 - "GYM_FISIO_PACKAGES"
Cohesion: 0.09
Nodes (22): GYM_FISIO_PACKAGES, CREATED_BY, CREATION_DATETIME, END_DATE, ID, InDatabase, IS_ACTIVE, IsChanged (+14 more)

### Community 27 - "InventoryModule"
Cohesion: 0.14
Nodes (3): InventoryModule, InventoryFlowTests, ProcurementFlowTests

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
Cohesion: 0.10
Nodes (19): GYM_ROUTINE_ASSIGNMENTS, ASSIGNED_AT, ASSIGNED_BY, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IS_ACTIVE (+11 more)

### Community 33 - "GYM_CLASS_RESERVATIONS"
Cohesion: 0.10
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
Cohesion: 0.10
Nodes (20): GYM_EQUIPMENT, CATEGORY, CREATED_BY, CREATION_DATETIME, EQUIPMENT_NAME, ID, InDatabase, IsChanged (+12 more)

### Community 39 - "GYM_KIOSK_DEVICES"
Cohesion: 0.10
Nodes (19): GYM_KIOSK_DEVICES, CREATED_BY, CREATION_DATETIME, DEVICE_NAME, DEVICE_TOKEN_HASH, ID, InDatabase, IS_ACTIVE (+11 more)

### Community 40 - "GYM_OTP_CODES"
Cohesion: 0.09
Nodes (19): GYM_OTP_CODES, CODE_HASH, CREATED_BY, CREATION_DATETIME, EMAIL, EXPIRES_AT, ID, InDatabase (+11 more)

### Community 41 - "GYM_SCHEDULED_REPORTS"
Cohesion: 0.06
Nodes (26): GYM_SCHEDULED_REPORTS, CREATED_BY, CREATION_DATETIME, EMAIL_RECIPIENTS, ID, InDatabase, IS_ACTIVE, IsChanged (+18 more)

### Community 42 - "GYM_SESSIONS"
Cohesion: 0.10
Nodes (20): GYM_SESSIONS, CREATED_BY, CREATION_DATETIME, DEVICE_INFO, EXPIRES_AT, ID, InDatabase, IP_ADDRESS (+12 more)

### Community 43 - "GYM_SUPPLIERS"
Cohesion: 0.09
Nodes (21): GYM_SUPPLIERS, ADDRESS, CONTACT_NAME, CREATED_BY, CREATION_DATETIME, EMAIL, ID, InDatabase (+13 more)

### Community 44 - "GYM_WORKOUTS"
Cohesion: 0.09
Nodes (21): GYM_WORKOUTS, CREATED_BY, CREATION_DATETIME, DURATION_MINUTES, EXERCISE_ID, ID, InDatabase, IsChanged (+13 more)

### Community 45 - "GYM_PURCHASE_ORDERS"
Cohesion: 0.10
Nodes (19): GYM_PURCHASE_ORDERS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 46 - "GYM_BANK_ACCOUNTS"
Cohesion: 0.10
Nodes (20): GYM_BANK_ACCOUNTS, ACCOUNT_NUMBER, BALANCE, BANK_NAME, CLABE, CREATED_BY, CREATION_DATETIME, CURRENCY (+12 more)

### Community 47 - "GYM_FISIO_APPOINTMENTS"
Cohesion: 0.10
Nodes (19): GYM_FISIO_APPOINTMENTS, APPOINTMENT_DATE, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+11 more)

### Community 48 - "GYM_MEASUREMENTS"
Cohesion: 0.09
Nodes (20): GYM_MEASUREMENTS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+12 more)

### Community 49 - "GYM_PERMISSIONS"
Cohesion: 0.09
Nodes (18): GYM_PERMISSIONS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+10 more)

### Community 50 - "GYM_PURCHASE_ORDER_ITEMS"
Cohesion: 0.10
Nodes (19): GYM_PURCHASE_ORDER_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 51 - "IDatabaseObject"
Cohesion: 0.10
Nodes (19): GYM_PUSH_SUBSCRIPTIONS, AUTH, BROWSER, CREATED_BY, CREATION_DATETIME, ENDPOINT, ID, InDatabase (+11 more)

### Community 52 - "GYM_SALE_ITEMS"
Cohesion: 0.10
Nodes (19): GYM_SALE_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 53 - "GYM_STAFF"
Cohesion: 0.10
Nodes (20): GYM_STAFF, CREATED_BY, CREATION_DATETIME, DEPARTMENT, EMPLOYEE_NAME, HIRE_DATE, ID, InDatabase (+12 more)

### Community 55 - "GYM_EXERCISES"
Cohesion: 0.09
Nodes (18): GYM_EXERCISES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, EQUIPMENT, EXERCISE_NAME, ID, InDatabase (+10 more)

### Community 56 - "Int64"
Cohesion: 0.10
Nodes (17): GYM_ROLE_PERMISSIONS, CREATED_BY, CREATION_DATETIME, GRANTED, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+9 more)

### Community 60 - "package.json"
Cohesion: 0.10
Nodes (24): devDependencies, oxlint@^1.90.0, tailwindcss@^4.3.5, @tailwindcss/vite@^4.3.5, @types/react@^19.3.0, @types/react-dom@^19.3.0, vite@^8.3.6, @vitejs/plugin-react@^6.1.2 (+16 more)

### Community 61 - "MembersModule"
Cohesion: 0.12
Nodes (3): MembersModule, MembershipCreditTests, Phase1FlowTests

### Community 62 - "GYM_MEMBERSHIP_EVENTS"
Cohesion: 0.08
Nodes (29): Historial de membresía (2026-11-27, rama feat/membership-history), GYM_MEMBERSHIP_EVENTS, AMOUNT, CREATED_BY, CREATION_DATETIME, CURRENCY, DETAILS, EVENT_AT (+21 more)

### Community 64 - "11. Historial de Sesiones"
Cohesion: 0.10
Nodes (19): Obstáculo 10: DatabaseAdministrator API inventada → ✓ RESUELTO, 11. Historial de Sesiones, API de DatabaseAdministrator (hallazgo clave), API de transacciones (hallazgo clave), Sesión 10: Paginación y filtros en endpoints GET, Sesión 11: Health checks para monitoreo del servicio, Sesión 12: Validación de roles y permisos por endpoint, Sesión 13: Windows Service deployment (+11 more)

### Community 65 - "GYM_MEMBER_CREDITS"
Cohesion: 0.09
Nodes (23): Cancelación con crédito proporcional (2026-11-20), GYM_MEMBER_CREDITS, AMOUNT, APPLIED_AT, CREATED_BY, CREATION_DATETIME, CURRENCY, ID (+15 more)

### Community 66 - "Fact"
Cohesion: 0.08
Nodes (9): Reportes Excel de caja, membresías y kardex (2026-11-20), ModelGym, MappingObjects, MappingSchemaTests, ModelGymTests, ObjectToInferredTypesConverterTests, PermissionMatrixTests, CurrentUserTests (+1 more)

### Community 67 - "steps"
Cohesion: 0.10
Nodes (19): runs-on, steps, GYM_TEST_DB_PASSWORD, GYM_TEST_DB_SERVER, GYM_TEST_DB_USER, jobs, build, name (+11 more)

### Community 68 - "steps"
Cohesion: 0.10
Nodes (19): jobs, publish, name, on, push, workflow_dispatch, runs-on, steps (+11 more)

### Community 69 - "AccessModule"
Cohesion: 0.23
Nodes (3): Corrección de fechas locales frente a SQL en UTC (2026-11-20), AccessModule, AccessFlowTests

### Community 70 - "SalesModule.CreateSale"
Cohesion: 0.17
Nodes (8): SalesModule, SaleItem, Price, ProductId, ProductName, Quantity, StockOnHand, SalesFlowTests

### Community 71 - "Backend (.NET)"
Cohesion: 0.03
Nodes (67): appsettings.json, Asignación y renovación de membresía (2026-11-19), Audio de acceso en el kiosco (2026-11-21), Backend (.NET), Búsqueda global y campana de notificaciones (2026-11-25, versión 1.7.12), Ciclo de vida de usuarios y contraseñas (2026-11-22, versión 1.7.3), Clases y reservas (2026-11-19), Compilación portable de ANXO (2026-11-20) (+59 more)

### Community 73 - "6. Esquema de Base de Datos"
Cohesion: 0.06
Nodes (34): 10. API REST - Endpoints, 1. ¿Qué es GymService?, 2. Framework GymCore - Bibliotecas Usadas, 3.1. Conexión a Base de Datos, 3.2. Consultas SELECT (DatabaseTools), 3.3. INSERT / UPDATE / DELETE (modelo de objetos), 3.4. Lectura de Row (sin indexador []), 3.5. Logger (+26 more)

### Community 74 - "Billing.jsx"
Cohesion: 0.31
Nodes (8): buildHtml(), escapeHtml(), formatDate(), InvoicePrintPreview(), money(), statusLabels, statusInfo, statusOptions

### Community 76 - "Input"
Cohesion: 0.16
Nodes (17): errorMessage(), MembershipModal(), todayInput(), Input, useApiList(), accessResults, Audit(), Pagination() (+9 more)

### Community 77 - "EmailService"
Cohesion: 0.24
Nodes (4): EmailService, HasOutbox, IsConfigured, IsEnabled

### Community 79 - "GymService - Deployment Guide"
Cohesion: 0.08
Nodes (25): Build, Built-in Commands, Configuration, First-Time Setup, GymService - Deployment Guide, Health Checks, Installation, Logs (+17 more)

### Community 81 - "DateTime"
Cohesion: 0.29
Nodes (3): Dashboard con tendencias (2026-11-20), DashboardModule, DashboardFlowTests

### Community 82 - "WebServerService"
Cohesion: 0.18
Nodes (11): Auditoría, Búsqueda global y notificaciones, Ficha completa del socio, Foto del socio, Gráficas de progreso, Impresión de facturas, Impresión de rutinas, Monitoreo de respaldos (+3 more)

### Community 83 - "Rediseño TailAdmin (Frontend)"
Cohesion: 0.22
Nodes (9): Colores y tipografía, Problemas de responsividad, Rediseño TailAdmin (Frontend), Tarea D01 — README desactualizado, Tarea F01 — Colores `primary-*` residuales en Members y Sales, Tarea F03 — Logs de depuración en api.js, Tarea F06 — Página 404 real, Tarea F07 — Persistir sidebar y accesibilidad en Layout (+1 more)

### Community 84 - "GymService - Centro Fit"
Cohesion: 0.11
Nodes (19): API REST, Autenticación, Base de Datos, Centro Fit Gym Manager, Compilación, Ejecución (modo consola), Estado del Proyecto, Estructura del Proyecto (+11 more)

### Community 85 - "Table"
Cohesion: 0.15
Nodes (22): addDaysInput(), ClassSessionsPanel(), errorMessage(), pad(), reservationStatusLabel, reservationStatusVariant, sessionStatusVariant, todayInput() (+14 more)

### Community 86 - "MembershipReceipt"
Cohesion: 0.18
Nodes (11): MembershipReceipt, Amount, CreditApplied, Currency, EndDate, NoCharge, PaymentDate, PaymentMethod (+3 more)

### Community 88 - "Route"
Cohesion: 0.12
Nodes (3): ClassesController, FisioController, InventoryController

### Community 89 - "DataCenter"
Cohesion: 0.18
Nodes (8): DataCenter, BusinessProcess, Configuration, ConnectionData, DatabaseManager, DatabaseService, EmailService, WebServer

### Community 93 - "HttpGet"
Cohesion: 0.13
Nodes (4): AuditController, CashController, DashboardController, ReportsController

### Community 95 - ".oxlintrc.json"
Cohesion: 0.33
Nodes (5): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 96 - "Hallazgos"
Cohesion: 0.12
Nodes (16): API y permisos, Automatizadas, Hallazgos, P0 — Crítico: la clave MFA no está respaldada, P1 — Endurecimiento de producción pendiente, P1 — Gestión de usuarios incompleta, P2 — Funciones anunciadas pero incompletas, P2 - Operación y monitoreo (+8 more)

### Community 104 - "Frontend (React + Vite)"
Cohesion: 0.25
Nodes (8): 1. Publicar, 2. Instalar, 2b. Instalador MSI (WiX), 3. Configuración mínima de producción, 3b. HTTPS en red local (solo IP), 4. Operación, 5. Checklist antes de entregar, Despliegue (producción)

### Community 109 - "formatCurrency"
Cohesion: 0.16
Nodes (20): formatShortDate(), ProgressChart(), errorMessage(), measurementTypes, pad(), RoutineMeasurementsPanel(), toLocalInput(), errorMessage() (+12 more)

### Community 111 - "Obstáculos y API del Framework Anxo - GymService"
Cohesion: 0.12
Nodes (16): Archivos corregidos en esta sesión, Cómo decompilar DLLs del framework, Estado actual del proyecto (actualizado 18/nov/2026), Exportación de Reportes a Excel (Sesión 16), Ingreso Masivo de Datos (Sesión 15), INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs, Obstáculo 12: Controllers REST — Patrón correcto, Obstáculo 13: Swagger/OpenAPI — Configuración correcta (+8 more)

### Community 112 - "4. Cola de tareas"
Cohesion: 0.12
Nodes (16): 0. Reparto de trabajo (importante), 1. Contexto mínimo, 2. Zonas prohibidas, 3. Reglas de git (dos IAs en el mismo repo), 4. Cola de tareas, 5. Tareas NO delegadas (las asume la IA grande / Daniel), 6. Bitácora, ✅ D01 — Sincronizar README con la realidad (+8 more)

### Community 113 - "Obstáculos y API del Framework GymCore - GymService"
Cohesion: 0.14
Nodes (13): Archivos corregidos en esta sesión, Bibliotecas principales, Cómo decompilar DLLs del framework, Estado actual del proyecto, Estructura del Framework GymCore, INSTRUCCIÓN OBLIGATORIA PARA FUTURAS IAs, Modelo de objetos (OBLIGATORIO para escrituras), Obstáculos y API del Framework GymCore - GymService (+5 more)

### Community 114 - "Flujo operativo"
Cohesion: 0.13
Nodes (14): 1. Preparar el contexto, 2. Abrir los tabs, 3. Enviar la ronda 1 en paralelo, 4. Leer las respuestas, 5. Ronda 2: crítica cruzada, 6. Sintetizar, 7. Guardar y cerrar, Errores que esta skill evita (+6 more)

### Community 115 - "Button"
Cohesion: 0.18
Nodes (19): CancelMembershipModal(), errorMessage(), MemberPhotoModal(), buildHtml(), escapeHtml(), formatDate(), MemberProfilePreview(), membershipLabels (+11 more)

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

### Community 120 - "SecurityValidation.Sha256Hash"
Cohesion: 0.33
Nodes (5): Correcciones posteriores a la migración (2026-11-19), CurrentUser, ForAudit, Id, Pruebas

### Community 121 - "GymService - Registro de Cambios y Correcciones"
Cohesion: 0.12
Nodes (15): api.js, AuthContext.jsx, Cierre del frontend: Inventario, Ventas y Membresías (18-nov-2026), Dashboard.jsx, Decisiones, Estado actual del sistema, Frontend (React + Vite), GenericPage.jsx (+7 more)

### Community 122 - "Respaldo offsite en OneDrive"
Cohesion: 0.22
Nodes (8): Contraseña, Destino, Reinstalar/ajustar, Respaldo de la clave MFA, Respaldo offsite en OneDrive, Restaurar un respaldo cifrado, Sincronización con OneDrive, Tarea programada

### Community 123 - "HTTPS en red local (acceso por IP)"
Cohesion: 0.13
Nodes (14): Android, Arquitectura y archivos, Diagnóstico rápido, Emisión y renovación, Firefox, Firewall y alcance, HTTPS en red local (acceso por IP), Instalar la CA raíz en los clientes (+6 more)

### Community 124 - "CleanupHostedService"
Cohesion: 0.25
Nodes (8): AGENTS.md — GymService (.NET), Antes de escribir código, Convenciones, Documentación obligatoria, Git, graphify, Pruebas y verificación, Stack y estructura

### Community 127 - "CurrentUser"
Cohesion: 0.40
Nodes (5): Control de acceso, Estado en esta implementación, Membresías y socios, Principios generales, Reglas de negocio (portadas del proyecto original)

### Community 130 - "FisioAppointmentsPanel.jsx"
Cohesion: 0.20
Nodes (14): defaultAppointmentDate(), errorMessage(), FisioAppointmentsPanel(), pad(), statusLabel, statusOptions, statusVariant, toLocalInput() (+6 more)

### Community 131 - "README.md"
Cohesion: 0.33
Nodes (5): Monitoreo de respaldos, Qué se revisa, Respaldo completo diario, Tareas, Uso

### Community 137 - "Monitoreo de respaldos"
Cohesion: 0.25
Nodes (5): Evidencia mínima, Excepción, Historial, Pruebas en vivo (obligatorias), Regla

### Community 138 - "Select"
Cohesion: 0.17
Nodes (15): ENCODINGS, ENTITIES, filenameFromDisposition(), FORMATS, ImportDialog(), saveBlob(), Select, Inventory() (+7 more)

### Community 139 - "Kiosco en este PC"
Cohesion: 0.29
Nodes (6): Accesos directos, Dispositivo, Kiosco en este PC, Lanzador, Reconfigurar el token, Requisitos

### Community 143 - "Handoff para la PC de casa"
Cohesion: 0.15
Nodes (8): 1. Estado del repositorio, 2. Entorno, 3. Desplegar 1.7.13 (primero), 4. Probar D (historial), 5. C1 en modo aviso, 6. Cuidados, 7. Archivos tocados por D, Handoff para la PC de casa

### Community 145 - "BulkImportService"
Cohesion: 0.22
Nodes (4): Importación de socios con correos duplicados (2026-11-20), Sesión 15: Ingreso masivo de datos (CSV/Excel), BulkImportService, BulkImportMembersTests

### Community 148 - "Decisión"
Cohesion: 0.17
Nodes (12): API, Backfill (fase 2, no bloqueante), Checklist para la PC de casa, Cuándo se escribe, Decisión, Fuera de alcance, Historial de membresía (D) — diseño, Orden de implementación (+4 more)

### Community 150 - "Pendientes de entrega"
Cohesion: 0.17
Nodes (12): 0. Despliegue 1.7.13, 1. Rebranding: retirar la marca anterior, 2.1 Historial de membresía (D), 2. Otros pendientes de entrega, 3. Hallazgos del QA completo (2026-11-21), Comercial D + C1, Fase C (opcional, white-label completo), Handoff para la PC de casa (+4 more)

### Community 151 - "IConfiguration"
Cohesion: 0.08
Nodes (11): Reglas no negociables, Reportes programados con ejecución real (2026-11-22, versión 1.7.6), Health Checks (Sesión 11), CleanupHostedService, GymHealthCheck, GymReadinessCheck, RolePermissionMiddleware, ScheduledReportHostedService (+3 more)

### Community 152 - "App.jsx"
Cohesion: 0.11
Nodes (29): dependencies, axios@^1.21.1, lucide-react@^1.75.0, qrcode.react@^4.2.0, react@^19.3.1, react-dom@^19.3.1, react-router-dom@^7.18.6, App() (+21 more)

### Community 153 - "Test-GymBackupHealth.ps1"
Cohesion: 0.83
Nodes (3): Add-Issue(), Add-Warning(), Write-Log()

### Community 159 - "Paquete comercial — Membresías y Cobranza"
Cohesion: 0.20
Nodes (10): Contrato — borrador de cláusulas, Demostración, Discurso de venta (30 segundos), Métricas del piloto, Paquete comercial — Membresías y Cobranza, Paquetes, Principio, Qué incluye C1 (+2 more)

### Community 160 - "Runbook — despliegue 1.7.9 → 1.7.13"
Cohesion: 0.20
Nodes (10): 0. Datos que hay que confirmar antes, 1. Ensayo en copia (obligatorio, 1–2 días antes), 2. Preflight de producción (sin tocar nada), 3. Ventana de despliegue (domingo noche o madrugada), 4. Lista de humo, 5. Criterios de aborto y rollback, 6. Después del despliegue, 7. Riesgos conocidos (+2 more)

### Community 162 - "BulkImportResult"
Cohesion: 0.22
Nodes (9): BulkImportResult, ErrorRows, Errors, ImportedRows, Message, SkippedRows, Success, TableName (+1 more)

### Community 163 - "Finance.jsx"
Cohesion: 0.46
Nodes (7): AccountsPanel(), errorMessage(), Finance(), MovementsPanel(), normalize(), parseCsv(), TransactionButton()

### Community 165 - "Correcciones críticas de backend (19-nov-2026)"
Cohesion: 0.33
Nodes (6): Correcciones críticas de backend (19-nov-2026), El arranque del servicio ya no borra la base de datos, Incidente de datos, `membershipType` vacío en el listado de socios, Prueba funcional completa (instancia de prueba en `:5001`), `SCOPE_IDENTITY()` no funciona entre conexiones

### Community 166 - "BulkImportService.BuildMember"
Cohesion: 0.33
Nodes (3): Sesión 17: Frontend con CRUD real, Sesión 18: Cierre de Inventario, Ventas y Membresías, PermissionMatrix

## Knowledge Gaps
- **547 isolated node(s):** `CREATION_DATETIME`, `LAST_UPDATED_DATETIME`, `CREATED_BY`, `LAST_UPDATED_BY`, `LAST_UPDATE_LOG` (+542 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1219 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **58 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GymService - Registro de Cambios y Correcciones` connect `GymService - Registro de Cambios y Correcciones` to `Rediseño TailAdmin (Frontend)`, `Correcciones críticas de backend (19-nov-2026)`, `Backend (.NET)`?**
  _High betweenness centrality (0.163) - this node is a cross-community bridge._
- **What connects `CREATION_DATETIME`, `LAST_UPDATED_DATETIME`, `CREATED_BY` to the rest of the system?**
  _547 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.09487179487179487 - nodes in this community are weakly interconnected._
- **Why does `Estado actual del sistema` connect `GymService - Registro de Cambios y Correcciones` to `Input`?**
  _High betweenness centrality (0.157) - this node is a cross-community bridge._
- **Should `GYM_BANK_TRANSACTIONS` be split into smaller, more focused modules?**
  _Cohesion score 0.08253968253968254 - nodes in this community are weakly interconnected._
- **Why does `useApiList()` connect `Input` to `App.jsx`, `GymService - Registro de Cambios y Correcciones`, `App.jsx`, `CrudPage.jsx`?**
  _High betweenness centrality (0.157) - this node is a cross-community bridge._
- **Should `xunit` be split into smaller, more focused modules?**
  _Cohesion score 0.06453634085213032 - nodes in this community are weakly interconnected._