using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Fisio
{
    /// <summary>
    /// Fisio module: paquetes de fisioterapia.
    /// </summary>
    public class FisioModule
    {
        private const string SubModule = "FISIO_MODULE";
        private DatabaseTools _tools;

        public FisioModule(DatabaseTools tools)
        {
            _tools = tools;
        }

        private int GetColumnIndex(ResultSet result, string columnName)
        {
            for (int i = 0; i < result.Columns.Count; i++)
            {
                if (result.Columns[i].ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private object GetRowValue(Row row, ResultSet result, string columnName)
        {
            int idx = GetColumnIndex(result, columnName);
            if (idx < 0) return null;
            return row.GetObject(idx);
        }

        /// <summary>
        /// Create a new physiotherapy package
        /// </summary>
        public bool CreatePackage(Dictionary<string, object> packageData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var name = Convert.ToString(packageData.GetValueOrDefault("name", "")) ?? "";
                var sessions = packageData.ContainsKey("sessions") ? Convert.ToInt32(packageData["sessions"]) : 10;
                var price = packageData.ContainsKey("price") ? Convert.ToDecimal(packageData["price"]) : 0m;
                var startDate = packageData.ContainsKey("startDate") ? Convert.ToDateTime(packageData["startDate"]) : DateTime.Now;

                if (string.IsNullOrEmpty(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del paquete es requerido";
                    return false;
                }

                var session = _tools.CreateSession();
                session.ExecuteTransaction("INSERT INTO gym_fisio_packages (name, sessions, price, start_date, is_active) VALUES (?, ?, ?, ?, 1)", name, sessions, price, startDate);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Paquete de fisioterapia creado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el paquete";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreatePackage: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Create a new physiotherapy appointment
        /// </summary>
        public bool CreateAppointment(Dictionary<string, object> appointmentData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberId = appointmentData.ContainsKey("memberId") ? Convert.ToInt64(appointmentData["memberId"]) : 0L;
                var packageId = appointmentData.ContainsKey("packageId") ? Convert.ToInt64(appointmentData["packageId"]) : 0L;
                var appointmentDate = appointmentData.ContainsKey("appointmentDate") ? Convert.ToDateTime(appointmentData["appointmentDate"]) : DateTime.Now;
                var notes = Convert.ToString(appointmentData.GetValueOrDefault("notes", "")) ?? "";

                var session = _tools.CreateSession();
                session.ExecuteTransaction("INSERT INTO gym_fisio_appointments (member_id, package_id, appointment_date, notes, status) VALUES (?, ?, ?, ?, 'scheduled')", memberId, packageId, appointmentDate, notes);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Cita de fisioterapia creada correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la cita";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateAppointment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a physiotherapy package
        /// </summary>
        public bool DeletePackage(long packageId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession();
                session.ExecuteTransaction("DELETE FROM gym_fisio_packages WHERE id = ?", packageId);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Paquete eliminado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el paquete";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeletePackage: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get active physiotherapy packages
        /// </summary>
        public bool GetPackages(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_fisio_packages WHERE is_active = 1 ORDER BY start_date DESC"
                );

                var packages = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        packages.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["name"] = Convert.ToString(GetRowValue(row, result, "name")) ?? "",
                            ["sessions"] = Convert.ToInt32(GetRowValue(row, result, "sessions")),
                            ["price"] = Convert.ToDecimal(GetRowValue(row, result, "price")),
                            ["startDate"] = GetRowValue(row, result, "start_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "start_date")) : (DateTime?)null
                        });
                    }
                }

                response["success"] = true;
                response["data"] = packages;
                response["count"] = packages.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetPackages: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
