using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Maintenance
{
    /// <summary>
    /// Maintenance module: equipos y registro de mantenimiento.
    /// </summary>
    public class MaintenanceModule
    {
        private const string SubModule = "MAINTENANCE_MODULE";
        private DatabaseTools _tools;

        public MaintenanceModule(DatabaseTools tools)
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
        /// Create a maintenance ticket
        /// </summary>
        public bool CreateTicket(Dictionary<string, object> ticketData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var equipmentId = ticketData.ContainsKey("equipmentId") ? Convert.ToInt64(ticketData["equipmentId"]) : 0L;
                var description = Convert.ToString(ticketData.GetValueOrDefault("description", "")) ?? "";
                var priority = Convert.ToString(ticketData.GetValueOrDefault("priority", "normal")) ?? "normal";
                var assignedTo = ticketData.ContainsKey("assignedTo") ? Convert.ToInt64(ticketData["assignedTo"]) : (long?)null;

                if (string.IsNullOrEmpty(description))
                {
                    response["success"] = false;
                    response["message"] = "La descripción es requerida";
                    return false;
                }

                var session = _tools.CreateSession();
                session.ExecuteTransaction("INSERT INTO gym_maintenance_tickets (equipment_id, description, priority, status, assigned_to, created_date) VALUES (?, ?, ?, 'open', ?, ?)", equipmentId, description, priority, assignedTo ?? (object)DBNull.Value, DateTime.Now);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Ticket de mantenimiento creado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el ticket";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateTicket: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Update maintenance ticket status
        /// </summary>
        public bool UpdateTicketStatus(long ticketId, string status, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession();
                session.ExecuteTransaction("UPDATE gym_maintenance_tickets SET status = ? WHERE id = ?", status, ticketId);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Estado del ticket actualizado";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el ticket";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateTicketStatus: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Delete a maintenance ticket
        /// </summary>
        public bool DeleteTicket(long ticketId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession();
                session.ExecuteTransaction("DELETE FROM gym_maintenance_tickets WHERE id = ?", ticketId);

                if (!session.TryExecute().IsError)
                {
                    response["success"] = true;
                    response["message"] = "Ticket eliminado correctamente";
                }
                else
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el ticket";
                }

                return (bool)response["success"];
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteTicket: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get all equipment with status
        /// </summary>
        public bool GetEquipment(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT * FROM gym_equipment ORDER BY equipment_name"
                );

                var equipment = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        equipment.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["name"] = Convert.ToString(GetRowValue(row, result, "equipment_name")) ?? "",
                            ["category"] = Convert.ToString(GetRowValue(row, result, "category")) ?? "",
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "operational",
                            ["location"] = Convert.ToString(GetRowValue(row, result, "location")) ?? ""
                        });
                    }
                }

                response["success"] = true;
                response["data"] = equipment;
                response["count"] = equipment.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetEquipment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
