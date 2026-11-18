using System;
using System.Collections.Generic;
using AxDatabase;

namespace GymService.Models
{
    /// <summary>
    /// Database model definitions for GymManager.
    /// Defines all tables and their structure using SQL DDL.
    /// </summary>
    public static class ModelGym
    {
        /// <summary>
        /// All table definitions in creation order (respecting foreign key dependencies)
        /// </summary>
        public static List<TableDefinition> TableDefinitions = new List<TableDefinition>
        {
            // === Identity & Users ===
            new TableDefinition
            {
                TableName = "gym_users",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "username", DataType = DataType.String, Length = 100, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "email", DataType = DataType.String, Length = 200, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "password_hash", DataType = DataType.String, Length = 255, Nullable = false },
                    new ColumnDefinition { Name = "role", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "full_name", DataType = DataType.String, Length = 200, Nullable = true },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" },
                    new ColumnDefinition { Name = "is_owner", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "mfa_enabled", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "mfa_secret_encrypted", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "mfa_backup_codes", DataType = DataType.String, Length = 2000, Nullable = true },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "updated_at", DataType = DataType.DateTime2, Nullable = true },
                    new ColumnDefinition { Name = "last_login", DataType = DataType.DateTime2, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_permissions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "permission_code", DataType = DataType.String, Length = 100, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "permission_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "module", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_role_permissions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "role", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "permission_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "granted", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                },
                PrimaryKey = new List<string> { "role", "permission_id" }
            },
            new TableDefinition
            {
                TableName = "gym_sessions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "user_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "token_hash", DataType = DataType.String, Length = 128, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "device_info", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "ip_address", DataType = DataType.String, Length = 45, Nullable = true },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "expires_at", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_otp_codes",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "email", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "code_hash", DataType = DataType.String, Length = 128, Nullable = false },
                    new ColumnDefinition { Name = "purpose", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "expires_at", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "used", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" }
                }
            },

            // === Members & Memberships ===
            new TableDefinition
            {
                TableName = "gym_members",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "first_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "last_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "email", DataType = DataType.String, Length = 200, Nullable = true },
                    new ColumnDefinition { Name = "phone", DataType = DataType.String, Length = 20, Nullable = true },
                    new ColumnDefinition { Name = "curp", DataType = DataType.String, Length = 18, Nullable = true },
                    new ColumnDefinition { Name = "rfc", DataType = DataType.String, Length = 13, Nullable = true },
                    new ColumnDefinition { Name = "address", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "emergency_contact", DataType = DataType.String, Length = 200, Nullable = true },
                    new ColumnDefinition { Name = "emergency_phone", DataType = DataType.String, Length = 20, Nullable = true },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "active" },
                    new ColumnDefinition { Name = "photo_url", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "notes", DataType = DataType.String, Length = 2000, Nullable = true },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "updated_at", DataType = DataType.DateTime2, Nullable = true },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_membership_plans",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "plan_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "price", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "currency", DataType = DataType.String, Length = 3, Nullable = false, DefaultValue = "MXN" },
                    new ColumnDefinition { Name = "duration_days", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "max_daily_entries", DataType = DataType.Int32, Nullable = false, DefaultValue = "-1" },
                    new ColumnDefinition { Name = "includes_classes", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "includes_premium_classes", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_memberships",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "plan_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "start_date", DataType = DataType.Date, Nullable = false },
                    new ColumnDefinition { Name = "end_date", DataType = DataType.Date, Nullable = false },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "active" },
                    new ColumnDefinition { Name = "auto_renew", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "freeze_start", DataType = DataType.Date, Nullable = true },
                    new ColumnDefinition { Name = "freeze_end", DataType = DataType.Date, Nullable = true },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_payments",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "membership_id", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "amount", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "currency", DataType = DataType.String, Length = 3, Nullable = false, DefaultValue = "MXN" },
                    new ColumnDefinition { Name = "payment_method", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "payment_date", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "reference", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "notes", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true }
                }
            },

            // === Access & Kiosk ===
            new TableDefinition
            {
                TableName = "gym_access_codes",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "code_hash", DataType = DataType.String, Length = 128, Nullable = false },
                    new ColumnDefinition { Name = "hmac_key", DataType = DataType.String, Length = 128, Nullable = false },
                    new ColumnDefinition { Name = "generated_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "expires_at", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "used", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "used_at", DataType = DataType.DateTime2, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_access_log",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "access_type", DataType = DataType.String, Length = 20, Nullable = false },
                    new ColumnDefinition { Name = "access_result", DataType = DataType.String, Length = 20, Nullable = false },
                    new ColumnDefinition { Name = "device_id", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "reason", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "accessed_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_kiosk_devices",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "device_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "device_token_hash", DataType = DataType.String, Length = 128, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" },
                    new ColumnDefinition { Name = "last_seen", DataType = DataType.DateTime2, Nullable = true },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            },

            // === Sales & Inventory ===
            new TableDefinition
            {
                TableName = "gym_inventory",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "product_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "sku", DataType = DataType.String, Length = 50, Nullable = true, Unique = true },
                    new ColumnDefinition { Name = "category", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "price", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "cost", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = true },
                    new ColumnDefinition { Name = "stock_quantity", DataType = DataType.Int32, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "min_stock", DataType = DataType.Int32, Nullable = false, DefaultValue = "5" },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_sales",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "sale_number", DataType = DataType.String, Length = 20, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "total_amount", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "tax_amount", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "payment_method", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "sale_date", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "is_offline", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_sale_items",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "sale_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "product_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "quantity", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "unit_price", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "total_price", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false }
                }
            },
            new TableDefinition
            {
                TableName = "gym_suppliers",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "supplier_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "contact_name", DataType = DataType.String, Length = 200, Nullable = true },
                    new ColumnDefinition { Name = "email", DataType = DataType.String, Length = 200, Nullable = true },
                    new ColumnDefinition { Name = "phone", DataType = DataType.String, Length = 20, Nullable = true },
                    new ColumnDefinition { Name = "address", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "rfc", DataType = DataType.String, Length = 13, Nullable = true },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_purchase_orders",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "order_number", DataType = DataType.String, Length = 20, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "supplier_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "order_date", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "pending" },
                    new ColumnDefinition { Name = "total_amount", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true }
                }
            },

            // === Classes ===
            new TableDefinition
            {
                TableName = "gym_class_types",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "type_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "max_capacity", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "duration_minutes", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "is_premium", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "premium_price", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = true },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_class_sessions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "class_type_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "instructor_id", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "room", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "start_time", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "end_time", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "scheduled" },
                    new ColumnDefinition { Name = "spots_reserved", DataType = DataType.Int32, Nullable = false, DefaultValue = "0" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_class_reservations",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "session_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "confirmed" },
                    new ColumnDefinition { Name = "attended", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "reserved_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            },

            // === Routines ===
            new TableDefinition
            {
                TableName = "gym_routine_plans",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "plan_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_exercises",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "exercise_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "muscle_group", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "equipment", DataType = DataType.String, Length = 100, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_routine_assignments",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "plan_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "assigned_by", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "assigned_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_workouts",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "exercise_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "sets", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "reps", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "weight", DataType = DataType.Decimal, Precision = 6, Scale = 2, Nullable = true },
                    new ColumnDefinition { Name = "duration_minutes", DataType = DataType.Int32, Nullable = true },
                    new ColumnDefinition { Name = "workout_date", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_measurements",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "measurement_type", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "value", DataType = DataType.Decimal, Precision = 6, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "unit", DataType = DataType.String, Length = 20, Nullable = false },
                    new ColumnDefinition { Name = "measured_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "measured_by", DataType = DataType.Int64, Nullable = true }
                }
            },

            // === Physiotherapy ===
            new TableDefinition
            {
                TableName = "gym_fisio_packages",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "sessions_total", DataType = DataType.Int32, Nullable = false },
                    new ColumnDefinition { Name = "sessions_used", DataType = DataType.Int32, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "price", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "start_date", DataType = DataType.Date, Nullable = false },
                    new ColumnDefinition { Name = "end_date", DataType = DataType.Date, Nullable = false },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_fisio_appointments",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "package_id", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "appointment_date", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "scheduled" },
                    new ColumnDefinition { Name = "notes", DataType = DataType.String, Length = 2000, Nullable = true },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true }
                }
            },

            // === Maintenance ===
            new TableDefinition
            {
                TableName = "gym_equipment",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "equipment_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "category", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "location", DataType = DataType.String, Length = 200, Nullable = true },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "operational" },
                    new ColumnDefinition { Name = "purchase_date", DataType = DataType.Date, Nullable = true },
                    new ColumnDefinition { Name = "warranty_end", DataType = DataType.Date, Nullable = true }
                }
            },
            new TableDefinition
            {
                TableName = "gym_maintenance_tickets",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "equipment_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "ticket_number", DataType = DataType.String, Length = 20, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 2000, Nullable = false },
                    new ColumnDefinition { Name = "priority", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "medium" },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "open" },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" },
                    new ColumnDefinition { Name = "created_by", DataType = DataType.Int64, Nullable = true },
                    new ColumnDefinition { Name = "resolved_at", DataType = DataType.DateTime2, Nullable = true },
                    new ColumnDefinition { Name = "resolved_by", DataType = DataType.Int64, Nullable = true }
                }
            },

            // === Billing ===
            new TableDefinition
            {
                TableName = "gym_invoices",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "invoice_number", DataType = DataType.String, Length = 50, Nullable = false, Unique = true },
                    new ColumnDefinition { Name = "member_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "amount", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "tax_amount", DataType = DataType.Decimal, Precision = 10, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "invoice_date", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "status", DataType = DataType.String, Length = 20, Nullable = false, DefaultValue = "issued" },
                    new ColumnDefinition { Name = "uuid", DataType = DataType.String, Length = 38, Nullable = true },
                    new ColumnDefinition { Name = "pdf_url", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "xml_url", DataType = DataType.String, Length = 500, Nullable = true }
                }
            },

            // === Finance ===
            new TableDefinition
            {
                TableName = "gym_bank_accounts",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "bank_name", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "account_number", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "clabe", DataType = DataType.String, Length = 18, Nullable = true },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_bank_transactions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "account_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "transaction_date", DataType = DataType.DateTime2, Nullable = false },
                    new ColumnDefinition { Name = "description", DataType = DataType.String, Length = 500, Nullable = true },
                    new ColumnDefinition { Name = "amount", DataType = DataType.Decimal, Precision = 12, Scale = 2, Nullable = false },
                    new ColumnDefinition { Name = "reference", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "reconciled", DataType = DataType.Boolean, Nullable = false, DefaultValue = "0" },
                    new ColumnDefinition { Name = "reconciled_with_id", DataType = DataType.Int64, Nullable = true }
                }
            },

            // === Reports ===
            new TableDefinition
            {
                TableName = "gym_scheduled_reports",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "report_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "report_type", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "schedule", DataType = DataType.String, Length = 50, Nullable = false },
                    new ColumnDefinition { Name = "email_recipients", DataType = DataType.String, Length = 1000, Nullable = false },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" },
                    new ColumnDefinition { Name = "last_run", DataType = DataType.DateTime2, Nullable = true },
                    new ColumnDefinition { Name = "next_run", DataType = DataType.DateTime2, Nullable = true }
                }
            },

            // === Staff ===
            new TableDefinition
            {
                TableName = "gym_staff",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "user_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "employee_name", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "position", DataType = DataType.String, Length = 100, Nullable = true },
                    new ColumnDefinition { Name = "hire_date", DataType = DataType.Date, Nullable = false },
                    new ColumnDefinition { Name = "is_active", DataType = DataType.Boolean, Nullable = false, DefaultValue = "1" }
                }
            },
            new TableDefinition
            {
                TableName = "gym_push_subscriptions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "id", DataType = DataType.Int64, PrimaryKey = true, Identity = true, Nullable = false },
                    new ColumnDefinition { Name = "user_id", DataType = DataType.Int64, Nullable = false },
                    new ColumnDefinition { Name = "endpoint", DataType = DataType.String, Length = 1000, Nullable = false },
                    new ColumnDefinition { Name = "p256dh", DataType = DataType.String, Length = 200, Nullable = false },
                    new ColumnDefinition { Name = "auth", DataType = DataType.String, Length = 100, Nullable = false },
                    new ColumnDefinition { Name = "browser", DataType = DataType.String, Length = 50, Nullable = true },
                    new ColumnDefinition { Name = "created_at", DataType = DataType.DateTime2, Nullable = false, DefaultValue = "GETDATE()" }
                }
            }
        };

        /// <summary>
        /// Foreign key definitions
        /// </summary>
        public static List<ForeignKeyDefinition> ForeignKeyDefinitions = new List<ForeignKeyDefinition>
        {
            new ForeignKeyDefinition { Table = "gym_role_permissions", Column = "permission_id", ReferencesTable = "gym_permissions", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_sessions", Column = "user_id", ReferencesTable = "gym_users", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_memberships", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_memberships", Column = "plan_id", ReferencesTable = "gym_membership_plans", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_payments", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_payments", Column = "membership_id", ReferencesTable = "gym_memberships", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_access_codes", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_access_log", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_sale_items", Column = "sale_id", ReferencesTable = "gym_sales", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_sale_items", Column = "product_id", ReferencesTable = "gym_inventory", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_class_sessions", Column = "class_type_id", ReferencesTable = "gym_class_types", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_class_reservations", Column = "session_id", ReferencesTable = "gym_class_sessions", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_class_reservations", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_routine_assignments", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_routine_assignments", Column = "plan_id", ReferencesTable = "gym_routine_plans", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_workouts", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_workouts", Column = "exercise_id", ReferencesTable = "gym_exercises", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_measurements", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_fisio_packages", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_fisio_appointments", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_fisio_appointments", Column = "package_id", ReferencesTable = "gym_fisio_packages", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_maintenance_tickets", Column = "equipment_id", ReferencesTable = "gym_equipment", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_invoices", Column = "member_id", ReferencesTable = "gym_members", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_bank_transactions", Column = "account_id", ReferencesTable = "gym_bank_accounts", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_staff", Column = "user_id", ReferencesTable = "gym_users", ReferencesColumn = "id" },
            new ForeignKeyDefinition { Table = "gym_push_subscriptions", Column = "user_id", ReferencesTable = "gym_users", ReferencesColumn = "id" }
        };
    }

    /// <summary>
    /// Table definition with columns
    /// </summary>
    public class TableDefinition
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
        public List<string>? PrimaryKey { get; set; } // Composite PK override
    }

    /// <summary>
    /// Column definition
    /// </summary>
    public class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public DataType DataType { get; set; }
        public int Length { get; set; } = 0;
        public int Precision { get; set; } = 0;
        public int Scale { get; set; } = 0;
        public bool PrimaryKey { get; set; } = false;
        public bool Identity { get; set; } = false;
        public bool Nullable { get; set; } = true;
        public bool Unique { get; set; } = false;
        public string? DefaultValue { get; set; }
    }

    /// <summary>
    /// Foreign key definition
    /// </summary>
    public class ForeignKeyDefinition
    {
        public string Table { get; set; } = string.Empty;
        public string Column { get; set; } = string.Empty;
        public string ReferencesTable { get; set; } = string.Empty;
        public string ReferencesColumn { get; set; } = string.Empty;
    }
}
