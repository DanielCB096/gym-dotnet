using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using AxLibraries;
using AxServiceTools;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Services;

namespace GymService
{
    /// <summary>
    /// Implementation of the Windows Service lifecycle.
    /// Follows the pattern of PilotTmsService ServiceImplementation.
    /// </summary>
    public class ServiceImplementation : IServiceConsole
    {
        private IConfiguration _configuration;
        private WebServerService? _webServer;
        private DatabaseService? _databaseService;

        public ServiceImplementation(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Called when the service starts
        /// </summary>
        public ProcessResult Start()
        {
            var result = new ProcessResult();
            Logger.AddInfo("========================================");
            Logger.AddInfo("GymService - Iniciar servicio");
            Logger.AddInfo("========================================");

            try
            {
                // Store configuration in DataCenter
                DataCenter.Configuration = _configuration;

                // Initialize database connection
                if (!InitializeDatabase())
                {
                    Logger.AddErrorSubModule("ServiceImplementation", "No se pudo conectar a la base de datos. El servicio no puede iniciar.");
                    result.Code = ProcessResult.GenericError;
                    result.ErrorMessage(AxNLS.Data(DataCenter.NLS_ASSEMBLY_CODE, 2004, "No se pudo conectar a la base de datos"));
                    return result;
                }

                // Initialize database schema
                InitializeDatabaseSchema();

                // Initialize business process modules
                InitializeBusinessModules();

                // Start web server (Kestrel)
                StartWebServer();

                Logger.AddInfo(AxNLS.Data(DataCenter.NLS_ASSEMBLY_CODE, 2002, "GymService iniciado correctamente"));
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("ServiceImplementation", $"Error al iniciar el servicio: {ex.Message}");
                result.Code = ProcessResult.GenericError;
                result.ErrorMessage($"Error al iniciar el servicio: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Called when the service stops
        /// </summary>
        public void Stop()
        {
            Logger.AddInfo("========================================");
            Logger.AddInfo("GymService - Detener servicio");
            Logger.AddInfo("========================================");

            try
            {
                // Stop web server
                if (_webServer != null)
                {
                    _webServer.Stop();
                    Logger.AddInfo("Servidor web detenido");
                }

                // Disconnect database
                if (DataCenter.DatabaseManager != null)
                {
                    DataCenter.DatabaseManager.Disconnect();
                    Logger.AddInfo("Base de datos desconectada");
                }

                // Reset data center
                DataCenter.Reset();

                Logger.AddInfo(AxNLS.Data(DataCenter.NLS_ASSEMBLY_CODE, 2003, "GymService detenido correctamente"));
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("ServiceImplementation", $"Error al detener el servicio: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize database connection using AnxoObjectDatabaseManager
        /// </summary>
        private bool InitializeDatabase()
        {
            try
            {
                var server = _configuration["Database:Server"] ?? "localhost";
                var database = _configuration["Database:Database"] ?? "GymManager";
                var user = _configuration["Database:User"] ?? "sa";
                var password = _configuration["Database:Password"] ?? "";
                var port = int.Parse(_configuration["Database:Port"] ?? "1433");
                var poolSize = int.Parse(_configuration["Database:PoolSize"] ?? "10");

                Logger.AddInfo($"Conectando a SQL Server: {server}\\{port}, Base: {database}");

                // Create connection data
                var connData = new ConnectionData
                {
                    Server = server,
                    Database = database,
                    User = user,
                    Password = password,
                    Port = port,
                    DatabaseType = DatabaseTypes.SQLServer,
                    PoolSize = poolSize
                };

                DataCenter.ConnectionData = connData;

                // Create and connect database manager
                var dbManager = new OODatabaseManager();
                dbManager.Connect(connData);

                DataCenter.DatabaseManager = dbManager;

                Logger.AddInfo("Conexión a base de datos establecida exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("ServiceImplementation", $"Error al conectar a la base de datos: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize database schema using DatabaseService
        /// </summary>
        private void InitializeDatabaseSchema()
        {
            try
            {
                _databaseService = new DatabaseService(DataCenter.DatabaseManager!);
                DataCenter.DatabaseService = _databaseService;

                // Create/validate database model
                _databaseService.InitializeSchema();

                Logger.AddInfo("Esquema de base de datos inicializado");
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("ServiceImplementation", $"Error al inicializar el esquema de BD: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize all business process modules
        /// </summary>
        private void InitializeBusinessModules()
        {
            try
            {
                // Initialize each module with the database manager
                var dbManager = DataCenter.DatabaseManager!;

                var tools = dbManager.Tools;
                DataCenter.EmailService ??= new EmailService(_configuration);
                DataCenter.BusinessProcess.Identity = new Modules.Identity.IdentityModule(tools, _configuration);
                DataCenter.BusinessProcess.Identity.EnsureAuditPermission();
                DataCenter.BusinessProcess.Members = new Modules.Members.MembersModule(tools, DataCenter.EmailService);
                DataCenter.BusinessProcess.Members.EnsureDefaultPlans();
                DataCenter.BusinessProcess.Access = new Modules.Access.AccessModule(tools);
                DataCenter.BusinessProcess.Sales = new Modules.Sales.SalesModule(tools);
                DataCenter.BusinessProcess.Inventory = new Modules.Inventory.InventoryModule(tools);
                DataCenter.BusinessProcess.Classes = new Modules.Classes.ClassesModule(tools);
                DataCenter.BusinessProcess.Routines = new Modules.Routines.RoutinesModule(tools);
                DataCenter.BusinessProcess.Fisio = new Modules.Fisio.FisioModule(tools);
                DataCenter.BusinessProcess.Maintenance = new Modules.Maintenance.MaintenanceModule(tools);
                DataCenter.BusinessProcess.Billing = new Modules.Billing.BillingModule(tools);
                DataCenter.BusinessProcess.Finance = new Modules.Finance.FinanceModule(tools);
                DataCenter.BusinessProcess.Reports = new Modules.Reports.ReportsModule(tools, DataCenter.EmailService, _configuration);
                DataCenter.BusinessProcess.Kiosk = new Modules.Kiosk.KioskModule(tools);
                DataCenter.BusinessProcess.Staff = new Modules.Staff.StaffModule(tools);
                DataCenter.BusinessProcess.Cash = new Modules.Cash.CashModule(tools);
                DataCenter.BusinessProcess.Dashboard = new Modules.Dashboard.DashboardModule(tools);
                DataCenter.BusinessProcess.Audit = new Modules.Audit.AuditModule(tools);

                Logger.AddInfo("Módulos de negocio inicializados");
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("ServiceImplementation", $"Error al inicializar módulos: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Start Kestrel web server
        /// </summary>
        private void StartWebServer()
        {
            try
            {
                var port = int.Parse(_configuration["WebServer:Port"] ?? "5000");
                var urls = _configuration["WebServer:Urls"] ?? $"http://localhost:{port}";

                Logger.AddInfo($"Iniciando servidor Kestrel en: {urls}");

                _webServer = new WebServerService(_configuration);
                DataCenter.WebServer = _webServer;

                _webServer.Start();

                Logger.AddInfo($"Servidor web iniciado en puerto {port}");
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule("ServiceImplementation", $"Error al iniciar el servidor web: {ex.Message}");
                throw;
            }
        }
    }
}
