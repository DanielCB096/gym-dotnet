# Graph Report - gimnasio-dotnet  (2026-11-20)

## Corpus Check
- 192 files · ~211,328 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .css 2, .xml 2)

## Summary
- 2689 nodes · 7595 edges · 102 communities (70 shown, 32 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 682 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `bce0148f`
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
- Table
- GYM_MEMBERSHIPS
- GYM_USERS
- react
- GYM_CLASS_TYPES
- GYM_MEMBERSHIP_PLANS
- GYM_PAYMENTS
- react
- Badge
- BusinessProcess
- GYM_INVENTORY
- GYM_ROUTINE_PLAN_EXERCISES
- GYM_SALES
- App.jsx
- GYM_INVENTORY_MOVEMENTS
- IdentityModule
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
- GYM_SCHEDULED_REPORTS
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
- StaffModule
- IConfiguration
- GymService.Tools
- package.json
- MembersModule
- DateTime
- ServiceImplementation
- DatabaseService
- FisioModule
- Fact
- steps
- steps
- AccessModule
- SalesModule
- DatabaseFixture.Scalar
- MaintenanceModule
- DashboardFlowTests.cs
- ServiceImplementation.cs
- DatabaseTools
- EmailService
- EmailServiceTests
- FinanceModule
- DataCenter
- List
- StaffShiftsPanel.jsx
- MembershipReceipt
- PermissionCoverageTests.Every_Controller_Route_Has_A_Permission
- ClassSessionsPanel.jsx
- FisioAppointmentsPanel.jsx
- ObjectToInferredTypesConverter.Read
- AnxoModelGenerator.csproj
- dependencies
- IdentityModule.RequestOtpByEmail
- .oxlintrc.json
- MaintenanceTests.Cleanup_Removes_Expired_Sessions_And_Otp_But_Keeps_Valid_Ones
- Finance.jsx

## God Nodes (most connected - your core abstractions)
1. `AxDatabase` - 71 edges
2. `Button` - 57 edges
3. `GYM_USERS` - 56 edges
4. `GYM_BANK_TRANSACTIONS` - 54 edges
5. `GYM_INVENTORY` - 54 edges
6. `GYM_MEMBERS` - 53 edges
7. `GYM_MEMBERSHIPS` - 51 edges
8. `GYM_MEMBERSHIP_PLANS` - 49 edges
9. `GYM_SALES` - 48 edges
10. `GYM_FISIO_PACKAGES` - 48 edges

## Surprising Connections (you probably didn't know these)
- `DataCenter` --references--> `EmailService`  [EXTRACTED]
  GymService/DataCenter.cs → GymService/Services/EmailService.cs
- `IdentityModule` --references--> `EmailService`  [EXTRACTED]
  GymService/Modules/Identity/IdentityModule.cs → GymService/Services/EmailService.cs
- `MembersModule` --references--> `EmailService`  [EXTRACTED]
  GymService/Modules/Members/MembersModule.cs → GymService/Services/EmailService.cs
- `MaintenanceService` --references--> `EmailService`  [EXTRACTED]
  GymService/Services/MaintenanceService.cs → GymService/Services/EmailService.cs
- `DashboardFlowTests` --references--> `DatabaseFixture`  [EXTRACTED]
  GymService.Tests/Integration/DashboardFlowTests.cs → GymService.Tests/Integration/DatabaseFixture.cs

## Import Cycles
- None detected.

## Communities (102 total, 32 thin omitted)

### Community 0 - "Dictionary"
Cohesion: 0.05
Nodes (19): AccessController, AuthController, BillingController, CashController, ClassesController, DashboardController, FinanceController, FisioController (+11 more)

### Community 1 - "GYM_BANK_TRANSACTIONS"
Cohesion: 0.08
Nodes (28): GYM_BANK_TRANSACTIONS, ACCOUNT_ID, AMOUNT, CREATED_BY, CREATION_DATETIME, DESCRIPTION, FLAGGED, ID (+20 more)

### Community 2 - "xunit"
Cohesion: 0.07
Nodes (18): GymService.Modules.Inventory, GymService.Modules.Sales, GymService.Tests.Integration, GymService.Modules.Members, GymService.Tests, GymService.Modules.Classes, GymService.Modules.Billing, GymService.Modules.Access (+10 more)

### Community 4 - "GYM_INVOICES"
Cohesion: 0.08
Nodes (25): GYM_INVOICES, AMOUNT, CREATED_BY, CREATION_DATETIME, ID, InDatabase, INVOICE_DATE, INVOICE_NUMBER (+17 more)

### Community 5 - "BulkImportResult"
Cohesion: 0.09
Nodes (19): BulkImportError, ErrorMessage, RowData, RowNumber, BulkImportResult, ErrorRows, Errors, ImportedRows (+11 more)

### Community 6 - "DatabaseFixture"
Cohesion: 0.08
Nodes (13): DatabaseCollection, DatabaseFixture, Available, ConnectionString, Manager, SkipReason, Tools, EmailNotificationFlowTests (+5 more)

### Community 7 - "GYM_MEMBERS"
Cohesion: 0.08
Nodes (27): GYM_MEMBERS, ADDRESS, CREATED_BY, CREATION_DATETIME, CURP, EMAIL, EMERGENCY_CONTACT, EMERGENCY_PHONE (+19 more)

### Community 8 - "Table"
Cohesion: 0.12
Nodes (31): buildPayload(), CrudPage(), toInputValue(), ExercisesPanel(), errorMessage(), RoutineAssignmentsPanel(), errorMessage(), measurementTypes (+23 more)

### Community 9 - "GYM_MEMBERSHIPS"
Cohesion: 0.08
Nodes (25): GYM_MEMBERSHIPS, AUTO_RENEW, CANCEL_REASON, CANCELLED_AT, CREATED_BY, CREATION_DATETIME, END_DATE, EXPIRY_NOTICE_SENT_AT (+17 more)

### Community 10 - "GYM_USERS"
Cohesion: 0.08
Nodes (27): GYM_USERS, CREATED_BY, CREATION_DATETIME, EMAIL, FAILED_LOGIN_ATTEMPTS, FULL_NAME, ID, InDatabase (+19 more)

### Community 11 - "react"
Cohesion: 0.20
Nodes (20): errorMessage(), MaintenanceTicketsPanel(), priorityLabel, priorityOptions, priorityVariant, statusLabel, statusOptions, statusVariant (+12 more)

### Community 12 - "GYM_CLASS_TYPES"
Cohesion: 0.09
Nodes (21): GYM_CLASS_TYPES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, DURATION_MINUTES, ID, InDatabase, IS_ACTIVE (+13 more)

### Community 13 - "GYM_MEMBERSHIP_PLANS"
Cohesion: 0.09
Nodes (23): GYM_MEMBERSHIP_PLANS, CREATED_BY, CREATION_DATETIME, CURRENCY, DESCRIPTION, DURATION_DAYS, ID, INCLUDES_CLASSES (+15 more)

### Community 14 - "GYM_PAYMENTS"
Cohesion: 0.08
Nodes (23): GYM_PAYMENTS, AMOUNT, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, CURRENCY, ID, InDatabase (+15 more)

### Community 15 - "react"
Cohesion: 0.13
Nodes (27): CancelMembershipModal(), errorMessage(), ENCODINGS, ENTITIES, filenameFromDisposition(), FORMATS, ImportDialog(), saveBlob() (+19 more)

### Community 16 - "Badge"
Cohesion: 0.12
Nodes (26): addDays(), FisioPackagesPanel(), today(), errorMessage(), StaffUsersPanel(), Badge(), dotColors, variants (+18 more)

### Community 17 - "BusinessProcess"
Cohesion: 0.06
Nodes (17): BusinessProcess, Access, Billing, Cash, Classes, Dashboard, Finance, Fisio (+9 more)

### Community 18 - "GYM_INVENTORY"
Cohesion: 0.09
Nodes (23): GYM_INVENTORY, CATEGORY, COST, CREATED_BY, CREATION_DATETIME, DESCRIPTION, ID, InDatabase (+15 more)

### Community 19 - "GYM_ROUTINE_PLAN_EXERCISES"
Cohesion: 0.09
Nodes (21): GYM_ROUTINE_PLAN_EXERCISES, CREATED_BY, CREATION_DATETIME, DAY_NUMBER, EXERCISE_ID, ID, InDatabase, IsChanged (+13 more)

### Community 20 - "GYM_SALES"
Cohesion: 0.09
Nodes (24): GYM_SALES, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IS_OFFLINE, IsChanged (+16 more)

### Community 21 - "App.jsx"
Cohesion: 0.14
Nodes (21): App(), Layout(), menuSections, routeLabels, MaintenanceEquipmentPanel(), statusOptions, statusVariants, ProtectedRoute() (+13 more)

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
Cohesion: 0.10
Nodes (21): GYM_ACCESS_CODES, CODE_HASH, CREATED_BY, CREATION_DATETIME, EXPIRES_AT, GENERATED_AT, HMAC_KEY, ID (+13 more)

### Community 31 - "GYM_ACCESS_LOG"
Cohesion: 0.09
Nodes (21): GYM_ACCESS_LOG, ACCESS_RESULT, ACCESS_TYPE, ACCESSED_AT, AUTHORIZED_BY, CREATED_BY, CREATION_DATETIME, DEVICE_ID (+13 more)

### Community 32 - "GYM_ROUTINE_ASSIGNMENTS"
Cohesion: 0.10
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
Cohesion: 0.08
Nodes (20): GYM_KIOSK_DEVICES, CREATED_BY, CREATION_DATETIME, DEVICE_NAME, DEVICE_TOKEN_HASH, ID, InDatabase, IS_ACTIVE (+12 more)

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

### Community 45 - "Int32"
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
Cohesion: 0.09
Nodes (18): GYM_PERMISSIONS, CREATED_BY, CREATION_DATETIME, DESCRIPTION, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+10 more)

### Community 50 - "GYM_PURCHASE_ORDER_ITEMS"
Cohesion: 0.09
Nodes (19): GYM_PURCHASE_ORDER_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 51 - "GYM_PUSH_SUBSCRIPTIONS"
Cohesion: 0.09
Nodes (19): GYM_PUSH_SUBSCRIPTIONS, AUTH, BROWSER, CREATED_BY, CREATION_DATETIME, ENDPOINT, ID, InDatabase (+11 more)

### Community 52 - "GYM_SALE_ITEMS"
Cohesion: 0.09
Nodes (19): GYM_SALE_ITEMS, CREATED_BY, CREATION_DATETIME, ID, InDatabase, IsChanged, LAST_UPDATE_LOG, LAST_UPDATED_BY (+11 more)

### Community 53 - "GYM_STAFF"
Cohesion: 0.09
Nodes (20): GYM_STAFF, CREATED_BY, CREATION_DATETIME, DEPARTMENT, EMPLOYEE_NAME, HIRE_DATE, ID, InDatabase (+12 more)

### Community 55 - "GYM_EXERCISES"
Cohesion: 0.10
Nodes (18): GYM_EXERCISES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, EQUIPMENT, EXERCISE_NAME, ID, InDatabase (+10 more)

### Community 56 - "IDatabaseObject"
Cohesion: 0.09
Nodes (17): GYM_ROLE_PERMISSIONS, CREATED_BY, CREATION_DATETIME, GRANTED, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+9 more)

### Community 58 - "IConfiguration"
Cohesion: 0.11
Nodes (4): CleanupHostedService, GymHealthCheck, GymReadinessCheck, RolePermissionMiddleware

### Community 60 - "package.json"
Cohesion: 0.10
Nodes (24): devDependencies, oxlint@^1.90.0, tailwindcss@^4.3.5, @tailwindcss/vite@^4.3.5, @types/react@^19.3.0, @types/react-dom@^19.3.0, vite@^8.3.6, @vitejs/plugin-react@^6.1.2 (+16 more)

### Community 63 - "ServiceImplementation"
Cohesion: 0.14
Nodes (3): Program, ServiceImplementation, WebServerService

### Community 64 - "DatabaseService"
Cohesion: 0.16
Nodes (3): ModelGym, MappingObjects, DatabaseService

### Community 66 - "Fact"
Cohesion: 0.10
Nodes (6): SchemaTests, MappingSchemaTests, ModelGymTests, ObjectToInferredTypesConverterTests, CurrentUserTests, SecurityValidationTests

### Community 67 - "steps"
Cohesion: 0.10
Nodes (19): runs-on, steps, GYM_TEST_DB_PASSWORD, GYM_TEST_DB_SERVER, GYM_TEST_DB_USER, jobs, build, name (+11 more)

### Community 68 - "steps"
Cohesion: 0.10
Nodes (19): jobs, publish, name, on, push, workflow_dispatch, runs-on, steps (+11 more)

### Community 70 - "SalesModule"
Cohesion: 0.17
Nodes (8): SalesModule, SaleItem, Price, ProductId, ProductName, Quantity, StockOnHand, SalesFlowTests

### Community 71 - "DatabaseFixture.Scalar"
Cohesion: 0.14
Nodes (4): CatalogFlowTests, MembersFlowTests, RoutinesFlowTests, StaffBillingFlowTests

### Community 74 - "ServiceImplementation.cs"
Cohesion: 0.18
Nodes (4): GymService, CurrentUser, ForAudit, Id

### Community 77 - "EmailService"
Cohesion: 0.16
Nodes (5): EmailService, HasOutbox, IsConfigured, IsEnabled, StaffShiftFlowTests

### Community 82 - "DataCenter"
Cohesion: 0.18
Nodes (8): DataCenter, BusinessProcess, Configuration, ConnectionData, DatabaseManager, DatabaseService, EmailService, WebServer

### Community 85 - "StaffShiftsPanel.jsx"
Cohesion: 0.35
Nodes (10): addDays(), buildDateTime(), dateValue(), emptyForm(), errorMessage(), formatDuration(), formatTime(), StaffShiftsPanel() (+2 more)

### Community 86 - "MembershipReceipt"
Cohesion: 0.20
Nodes (10): MembershipReceipt, Amount, Currency, EndDate, NoCharge, PaymentDate, PaymentMethod, PlanName (+2 more)

### Community 88 - "ClassSessionsPanel.jsx"
Cohesion: 0.36
Nodes (9): addDaysInput(), ClassSessionsPanel(), errorMessage(), pad(), reservationStatusLabel, reservationStatusVariant, sessionStatusVariant, todayInput() (+1 more)

### Community 89 - "FisioAppointmentsPanel.jsx"
Cohesion: 0.36
Nodes (8): defaultAppointmentDate(), errorMessage(), FisioAppointmentsPanel(), pad(), statusLabel, statusOptions, statusVariant, toLocalInput()

### Community 93 - "dependencies"
Cohesion: 0.25
Nodes (8): dependencies, axios@^1.21.1, lucide-react@^1.75.0, react@^19.3.1, react-dom@^19.3.1, react-router-dom@^7.18.6, axios, react-dom

### Community 95 - ".oxlintrc.json"
Cohesion: 0.33
Nodes (5): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 103 - "Finance.jsx"
Cohesion: 0.46
Nodes (7): AccountsPanel(), errorMessage(), Finance(), MovementsPanel(), normalize(), parseCsv(), TransactionButton()

## Knowledge Gaps
- **523 isolated node(s):** `PlanName`, `StartDate`, `EndDate`, `Amount`, `Currency` (+518 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 790 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **32 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GYM_PAYMENTS` connect `GYM_PAYMENTS` to `DatabaseService`, `system_collections_generic`, `Boolean`, `Int32`, `GYM_PERMISSIONS`, `GYM_PURCHASE_ORDER_ITEMS`, `GYM_PUSH_SUBSCRIPTIONS`, `GYM_SALE_ITEMS`, `List`, `IDatabaseObject`, `MembersModule`, `DateTime`?**
  _High betweenness centrality (0.039) - this node is a cross-community bridge._
- **What connects `PlanName`, `StartDate`, `EndDate` to the rest of the system?**
  _523 weakly-connected nodes found - possible missing edges._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.050606568194507894 - nodes in this community are weakly interconnected._
- **Why does `GYM_INVENTORY` connect `GYM_INVENTORY` to `DatabaseService`, `system_collections_generic`, `BulkImportResult`, `SalesModule`, `Boolean`, `Int32`, `GYM_PERMISSIONS`, `GYM_PURCHASE_ORDER_ITEMS`, `GYM_PUSH_SUBSCRIPTIONS`, `GYM_SALE_ITEMS`, `List`, `IDatabaseObject`, `InventoryModule`, `DateTime`?**
  _High betweenness centrality (0.030) - this node is a cross-community bridge._
- **Should `GYM_BANK_TRANSACTIONS` be split into smaller, more focused modules?**
  _Cohesion score 0.07957957957957958 - nodes in this community are weakly interconnected._
- **Why does `GYM_MEMBERSHIPS` connect `GYM_MEMBERSHIPS` to `DatabaseService`, `MaintenanceTests.Cleanup_Removes_Expired_Sessions_And_Otp_But_Keeps_Valid_Ones`, `system_collections_generic`, `DatabaseFixture.Scalar`, `Boolean`, `Int32`, `GYM_PERMISSIONS`, `GYM_PURCHASE_ORDER_ITEMS`, `GYM_PUSH_SUBSCRIPTIONS`, `List`, `IDatabaseObject`, `MembersModule`, `DateTime`?**
  _High betweenness centrality (0.029) - this node is a cross-community bridge._
- **Should `xunit` be split into smaller, more focused modules?**
  _Cohesion score 0.07138535995160314 - nodes in this community are weakly interconnected._