using System;
using Microsoft.Extensions.Configuration;
using AxLibraries;
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
        /// <summary>
        /// Nombre de assembly usado por el mecanismo NLS de Anxo.
        /// El codigo NO es arbitrario: NlsTools lo calcula como hash del nombre del assembly.
        /// </summary>
        public const string NLS_ASSEMBLY_NAME = "GymService";
        public static readonly int NLS_ASSEMBLY_CODE = NlsTools.GetPersistenceHashCode(NLS_ASSEMBLY_NAME);

        // Service definition
        public static readonly ServiceDefinition SERVICE_DATA = new ServiceDefinition
        {
            ServiceName = "anxogymmservice",
            ServiceDisplayName = "Anxo Gym Manager Service",
            // Sin AssemblyName, ServiceConsole usa Assembly.GetExecutingAssembly() (la DLL de
            // AnxoServiceTools) al hacer -install y el servicio quedaria mal registrado.
            AssemblyName = "GymService.exe",
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

        // Correo (SMTP u outbox de desarrollo)
        public static EmailService? EmailService { get; set; }

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
            EmailService = null;

            // Los modulos guardan un DatabaseTools ligado al manager desconectado:
            // hay que soltarlos para que no se reutilicen tras un Stop().
            BusinessProcess.Identity = null;
            BusinessProcess.Members = null;
            BusinessProcess.Access = null;
            BusinessProcess.Sales = null;
            BusinessProcess.Inventory = null;
            BusinessProcess.Classes = null;
            BusinessProcess.Routines = null;
            BusinessProcess.Fisio = null;
            BusinessProcess.Maintenance = null;
            BusinessProcess.Billing = null;
            BusinessProcess.Finance = null;
            BusinessProcess.Reports = null;
            BusinessProcess.Kiosk = null;
            BusinessProcess.Staff = null;
            BusinessProcess.Cash = null;
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
        public Modules.Cash.CashModule? Cash { get; set; }    }
}
