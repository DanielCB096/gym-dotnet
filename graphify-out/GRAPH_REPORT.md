# Graph Report - gimnasio-dotnet  (2026-11-20)

## Corpus Check
- 196 files · ~213,910 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .css 2, .xml 2)

## Summary
- 2703 nodes · 7606 edges · 109 communities (66 shown, 43 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 682 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `97a8aaf1`
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
- Button
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
- DatabaseFixture.Scalar
- DateTime
- ServiceImplementation
- DatabaseService
- WebServerService
- Fact
- steps
- steps
- AccessModule
- SalesModule
- CrudPage
- MaintenanceModule
- Phase1FlowTests.Kiosk_Token_Rotation_And_Manual_Access_Work
- ServiceImplementation.cs
- DatabaseTools
- EmailService
- MappingObject
- EmailServiceTests
- ResultSet
- FinanceModule
- DataCenter
- List
- SchemaTests
- StaffShiftsPanel.jsx
- MembershipReceipt
- PermissionCoverageTests.Every_Controller_Route_Has_A_Permission
- ClassSessionsPanel.jsx
- FisioModule
- ObjectToInferredTypesConverter.Read
- AnxoModelGenerator.csproj
- IdentityModule.RequestOtpByEmail
- .oxlintrc.json
- DatabaseFixture.Execute
- ModelGym
- register-backup-tasks.ps1

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
- `IdentityModule` --references--> `EmailService`  [EXTRACTED]
  GymService/Modules/Identity/IdentityModule.cs → GymService/Services/EmailService.cs
- `ClassSessionsFlowTests` --references--> `DatabaseFixture`  [EXTRACTED]
  GymService.Tests/Integration/ClassSessionsFlowTests.cs → GymService.Tests/Integration/DatabaseFixture.cs
- `MaintenanceService` --references--> `EmailService`  [EXTRACTED]
  GymService/Services/MaintenanceService.cs → GymService/Services/EmailService.cs
- `StaffShiftFlowTests` --references--> `DatabaseFixture`  [EXTRACTED]
  GymService.Tests/Integration/StaffShiftFlowTests.cs → GymService.Tests/Integration/DatabaseFixture.cs
- `DashboardFlowTests` --references--> `DatabaseFixture`  [EXTRACTED]
  GymService.Tests/Integration/DashboardFlowTests.cs → GymService.Tests/Integration/DatabaseFixture.cs

## Import Cycles
- None detected.

## Communities (109 total, 43 thin omitted)

### Community 0 - "Dictionary"
Cohesion: 0.05
Nodes (19): AccessController, AuthController, BillingController, CashController, ClassesController, DashboardController, FinanceController, FisioController (+11 more)

### Community 1 - "GYM_BANK_TRANSACTIONS"
Cohesion: 0.08
Nodes (28): GYM_BANK_TRANSACTIONS, ACCOUNT_ID, AMOUNT, CREATED_BY, CREATION_DATETIME, DESCRIPTION, FLAGGED, ID (+20 more)

### Community 2 - "xunit"
Cohesion: 0.07
Nodes (19): GymService.Modules.Inventory, GymService.Modules.Sales, GymService.Tests.Integration, GymService.Modules.Members, GymService.Tests, GymService.Modules.Classes, GymService.Modules.Billing, GymService.Modules.Access (+11 more)

### Community 4 - "GYM_INVOICES"
Cohesion: 0.07
Nodes (25): GYM_INVOICES, AMOUNT, CREATED_BY, CREATION_DATETIME, ID, InDatabase, INVOICE_DATE, INVOICE_NUMBER (+17 more)

### Community 5 - "BulkImportResult"
Cohesion: 0.09
Nodes (19): BulkImportError, ErrorMessage, RowData, RowNumber, BulkImportResult, ErrorRows, Errors, ImportedRows (+11 more)

### Community 6 - "DatabaseFixture"
Cohesion: 0.06
Nodes (16): CatalogFlowTests, DatabaseCollection, DatabaseFixture, Available, ConnectionString, Manager, SkipReason, Tools (+8 more)

### Community 7 - "GYM_MEMBERS"
Cohesion: 0.08
Nodes (27): GYM_MEMBERS, ADDRESS, CREATED_BY, CREATION_DATETIME, CURP, EMAIL, EMERGENCY_CONTACT, EMERGENCY_PHONE (+19 more)

### Community 8 - "Table"
Cohesion: 0.17
Nodes (21): errorMessage(), RoutineAssignmentsPanel(), errorMessage(), measurementTypes, pad(), RoutineMeasurementsPanel(), toLocalInput(), errorMessage() (+13 more)

### Community 9 - "GYM_MEMBERSHIPS"
Cohesion: 0.08
Nodes (25): GYM_MEMBERSHIPS, AUTO_RENEW, CANCEL_REASON, CANCELLED_AT, CREATED_BY, CREATION_DATETIME, END_DATE, EXPIRY_NOTICE_SENT_AT (+17 more)

### Community 10 - "GYM_USERS"
Cohesion: 0.08
Nodes (27): GYM_USERS, CREATED_BY, CREATION_DATETIME, EMAIL, FAILED_LOGIN_ATTEMPTS, FULL_NAME, ID, InDatabase (+19 more)

### Community 11 - "Button"
Cohesion: 0.14
Nodes (35): defaultAppointmentDate(), errorMessage(), FisioAppointmentsPanel(), pad(), statusLabel, statusOptions, statusVariant, toLocalInput() (+27 more)

### Community 12 - "GYM_CLASS_TYPES"
Cohesion: 0.08
Nodes (21): GYM_CLASS_TYPES, CREATED_BY, CREATION_DATETIME, DESCRIPTION, DURATION_MINUTES, ID, InDatabase, IS_ACTIVE (+13 more)

### Community 13 - "GYM_MEMBERSHIP_PLANS"
Cohesion: 0.08
Nodes (23): GYM_MEMBERSHIP_PLANS, CREATED_BY, CREATION_DATETIME, CURRENCY, DESCRIPTION, DURATION_DAYS, ID, INCLUDES_CLASSES (+15 more)

### Community 14 - "GYM_PAYMENTS"
Cohesion: 0.09
Nodes (23): GYM_PAYMENTS, AMOUNT, CASH_SESSION_ID, CREATED_BY, CREATION_DATETIME, CURRENCY, ID, InDatabase (+15 more)

### Community 15 - "react"
Cohesion: 0.13
Nodes (26): CancelMembershipModal(), errorMessage(), ENCODINGS, ENTITIES, filenameFromDisposition(), FORMATS, ImportDialog(), saveBlob() (+18 more)

### Community 16 - "Badge"
Cohesion: 0.14
Nodes (23): Badge(), dotColors, variants, Card(), Billing(), statusInfo, statusOptions, colorMap (+15 more)

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
Cohesion: 0.19
Nodes (14): App(), Layout(), menuSections, routeLabels, ProtectedRoute(), AuthContext, AuthProvider(), useAuth() (+6 more)

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

### Community 32 - "GYM_ROUTINE_ASSIGNMENTS"
Cohesion: 0.11
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

### Community 41 - "GYM_SCHEDULED_REPORTS"
Cohesion: 0.09
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
Cohesion: 0.10
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
Cohesion: 0.10
Nodes (17): GYM_ROLE_PERMISSIONS, CREATED_BY, CREATION_DATETIME, GRANTED, ID, InDatabase, IsChanged, LAST_UPDATE_LOG (+9 more)

### Community 58 - "IConfiguration"
Cohesion: 0.12
Nodes (6): CleanupHostedService, GymHealthCheck, GymReadinessCheck, MaintenanceService, MaintenanceTests, StaffShiftFlowTests

### Community 60 - "package.json"
Cohesion: 0.08
Nodes (32): dependencies, axios@^1.21.1, lucide-react@^1.75.0, react@^19.3.1, react-dom@^19.3.1, react-router-dom@^7.18.6, devDependencies, oxlint@^1.90.0 (+24 more)

### Community 63 - "ServiceImplementation"
Cohesion: 0.14
Nodes (3): Program, ServiceImplementation, WebServerService

### Community 66 - "Fact"
Cohesion: 0.21
Nodes (3): ModelGymTests, ObjectToInferredTypesConverterTests, CurrentUserTests

### Community 67 - "steps"
Cohesion: 0.10
Nodes (19): runs-on, steps, GYM_TEST_DB_PASSWORD, GYM_TEST_DB_SERVER, GYM_TEST_DB_USER, jobs, build, name (+11 more)

### Community 68 - "steps"
Cohesion: 0.10
Nodes (19): jobs, publish, name, on, push, workflow_dispatch, runs-on, steps (+11 more)

### Community 70 - "SalesModule"
Cohesion: 0.17
Nodes (8): SalesModule, SaleItem, Price, ProductId, ProductName, Quantity, StockOnHand, SalesFlowTests

### Community 71 - "CrudPage"
Cohesion: 0.18
Nodes (17): buildPayload(), CrudPage(), toInputValue(), ExercisesPanel(), addDays(), FisioPackagesPanel(), today(), MaintenanceEquipmentPanel() (+9 more)

### Community 74 - "ServiceImplementation.cs"
Cohesion: 0.14
Nodes (4): GymService, CurrentUser, ForAudit, Id

### Community 77 - "EmailService"
Cohesion: 0.22
Nodes (4): EmailService, HasOutbox, IsConfigured, IsEnabled

### Community 82 - "DataCenter"
Cohesion: 0.18
Nodes (8): DataCenter, BusinessProcess, Configuration, ConnectionData, DatabaseManager, DatabaseService, EmailService, WebServer

### Community 85 - "StaffShiftsPanel.jsx"
Cohesion: 0.25
Nodes (14): addDays(), buildDateTime(), dateValue(), emptyForm(), errorMessage(), formatDuration(), formatTime(), StaffShiftsPanel() (+6 more)

### Community 86 - "MembershipReceipt"
Cohesion: 0.20
Nodes (10): MembershipReceipt, Amount, Currency, EndDate, NoCharge, PaymentDate, PaymentMethod, PlanName (+2 more)

### Community 88 - "ClassSessionsPanel.jsx"
Cohesion: 0.36
Nodes (9): addDaysInput(), ClassSessionsPanel(), errorMessage(), pad(), reservationStatusLabel, reservationStatusVariant, sessionStatusVariant, todayInput() (+1 more)

### Community 95 - ".oxlintrc.json"
Cohesion: 0.33
Nodes (5): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

## Knowledge Gaps
- **523 isolated node(s):** `CREATED_BY`, `CREATION_DATETIME`, `InDatabase`, `IsChanged`, `LAST_UPDATE_LOG` (+518 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 798 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **43 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EmailService` connect `EmailService` to `EmailServiceTests`, `DataCenter`, `IdentityModule`, `WebServerService.cs`, `IConfiguration`, `DatabaseFixture.Scalar`, `IdentityModule.RequestOtpByEmail`, `ServiceImplementation`?**
  _High betweenness centrality (0.036) - this node is a cross-community bridge._
- **What connects `CREATED_BY`, `CREATION_DATETIME`, `InDatabase` to the rest of the system?**
  _523 weakly-connected nodes found - possible missing edges._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.050606568194507894 - nodes in this community are weakly interconnected._
- **Why does `GYM_USERS` connect `GYM_USERS` to `GYM_ROUTINE_ASSIGNMENTS`, `system_collections_generic`, `Boolean`, `GYM_SUPPLIERS`, `MappingObject`, `GYM_PERMISSIONS`, `GYM_PURCHASE_ORDER_ITEMS`, `List`, `GYM_SALE_ITEMS`, `IdentityModule`, `IConfiguration`, `DateTime`?**
  _High betweenness centrality (0.033) - this node is a cross-community bridge._
- **Should `GYM_BANK_TRANSACTIONS` be split into smaller, more focused modules?**
  _Cohesion score 0.07957957957957958 - nodes in this community are weakly interconnected._
- **Why does `GYM_MEMBERS` connect `GYM_MEMBERS` to `GYM_ROUTINE_ASSIGNMENTS`, `system_collections_generic`, `BulkImportResult`, `Boolean`, `GYM_SUPPLIERS`, `MappingObject`, `GYM_PERMISSIONS`, `GYM_PURCHASE_ORDER_ITEMS`, `List`, `GYM_SALE_ITEMS`, `DatabaseFixture.Scalar`, `DateTime`?**
  _High betweenness centrality (0.033) - this node is a cross-community bridge._
- **Should `xunit` be split into smaller, more focused modules?**
  _Cohesion score 0.07467532467532467 - nodes in this community are weakly interconnected._