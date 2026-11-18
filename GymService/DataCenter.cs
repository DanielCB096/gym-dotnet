using System;
using Microsoft.Extensions.Configuration;
using AxServiceTools;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Services;

namespace GymService
{
    /// <summary>
    /// Central state holder for the GymService application.
    /// Follows the pattern of PilotTmsService DataCenter.
    /// </summary>
    public static class DataCenter
    {
        // Service definition
        public static readonly ServiceDefinition SERVICE_DATA = new ServiceDefinition
        {
            ServiceName = "anxogymmservice",
            ServiceDisplayName = "Anxo Gym Manager Service",
            StartType = "auto",
            ServiceAccount = "localsystem"
        };

        // Configuration
        public static IConfiguration? Configuration { get; set; }

        // Database
        public static OODatabaseManager? DatabaseManager { get; set; }
        public static ConnectionData? ConnectionData { get; set; }

        // Web server
        public static WebServerService? WebServer { get; set; }

        // Database service for schema management
        public static DatabaseService? DatabaseService { get; set; }

        // Business process modules (lazy initialized)
        public static BusinessProcess BusinessProcess { get; } = new BusinessProcess();

        /// <summary>
        /// Reset all state (used for testing or restart)
        /// </summary>
        public static void Reset()
        {
            Configuration = null;
            DatabaseManager = null;
            ConnectionData = null;
            WebServer = null;
            DatabaseService = null;
        }
    }

    /// <summary>
    /// Container for all business process modules
    /// </summary>
    public class BusinessProcess
    {
        public Modules.Identity.IdentityModule? Identity { get; set; }
        public Modules.Members.MembersModule? Members { get; set; }
        public Modules.Access.AccessModule? Access { get; set; }
        public Modules.Sales.SalesModule? Sales { get; set; }
        public Modules.Inventory.InventoryModule? Inventory { get; set; }
        public Modules.Classes.ClassesModule? Classes { get; set; }
        public Modules.Routines.RoutinesModule? Routines { get; set; }
        public Modules.Fisio.FisioModule? Fisio { get; set; }
        public Modules.Maintenance.MaintenanceModule? Maintenance { get; set; }
        public Modules.Billing.BillingModule? Billing { get; set; }
        public Modules.Finance.FinanceModule? Finance { get; set; }
        public Modules.Reports.ReportsModule? Reports { get; set; }
        public Modules.Kiosk.KioskModule? Kiosk { get; set; }
        public Modules.Staff.StaffModule? Staff { get; set; }
    }
}
