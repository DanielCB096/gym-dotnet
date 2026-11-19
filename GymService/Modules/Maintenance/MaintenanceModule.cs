using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Maintenance
{
    /// <summary>
    /// Maintenance module: equipos, tickets de mantenimiento y resolución.
    /// </summary>
    public class MaintenanceModule
    {
        private const string SubModule = "MAINTENANCE_MODULE";

        private static readonly HashSet<string> EquipmentStatuses = new()
        {
            "operational", "maintenance", "out_of_service"
        };

        private static readonly HashSet<string> TicketStatuses = new()
        {
            "open", "in_progress", "resolved", "cancelled"
        };

        private static readonly HashSet<string> TicketPriorities = new()
        {
            "low", "normal", "high", "urgent"
        };

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

        // =====================================================================
        // Equipos
        // =====================================================================

        public bool GetEquipment(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT e.*, " +
                    "(SELECT COUNT(*) FROM GYM_MAINTENANCE_TICKETS t " +
                    " WHERE t.EQUIPMENT_ID = e.ID AND t.STATUS IN ('open','in_progress')) AS OPEN_TICKETS " +
                    "FROM gym_equipment e ORDER BY e.equipment_name");

                var equipment = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        equipment.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["name"] = Convert.ToString(GetRowValue(row, result, "equipment_name")) ?? "",
                            ["category"] = Convert.ToString(GetRowValue(row, result, "category")) ?? "",
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "operational",
                            ["location"] = Convert.ToString(GetRowValue(row, result, "location")) ?? "",
                            ["purchaseDate"] = GetRowValue(row, result, "purchase_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "purchase_date")) : (DateTime?)null,
                            ["warrantyEnd"] = GetRowValue(row, result, "warranty_end") != null ? Convert.ToDateTime(GetRowValue(row, result, "warranty_end")) : (DateTime?)null,
                            ["openTickets"] = GetRowValue(row, result, "open_tickets") != null ? Convert.ToInt32(GetRowValue(row, result, "open_tickets")) : 0
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

        public bool CreateEquipment(Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                string name = Convert.ToString(data.GetValueOrDefault("name", "")) ?? "";
                string category = Convert.ToString(data.GetValueOrDefault("category", "")) ?? "";
                string location = Convert.ToString(data.GetValueOrDefault("location", "")) ?? "";
                string status = (Convert.ToString(data.GetValueOrDefault("status", "operational")) ?? "operational").Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del equipo es requerido";
                    return false;
                }

                if (!EquipmentStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de equipo no válido";
                    return false;
                }

                DateTime? purchaseDate = TryParseDate(data.GetValueOrDefault("purchaseDate"));
                DateTime? warrantyEnd = TryParseDate(data.GetValueOrDefault("warrantyEnd"));

                if (purchaseDate.HasValue && warrantyEnd.HasValue && warrantyEnd.Value < purchaseDate.Value)
                {
                    response["success"] = false;
                    response["message"] = "El fin de garantía no puede ser anterior a la compra";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long equipmentId = _tools.GenerateUniqueID(GYM_EQUIPMENT.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_EQUIPMENT
                {
                    ID = equipmentId,
                    EQUIPMENT_NAME = name.Trim(),
                    CATEGORY = category.Trim(),
                    LOCATION = location.Trim(),
                    STATUS = status,
                    PURCHASE_DATE = purchaseDate,
                    WARRANTY_END = warrantyEnd
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el equipo";
                    return false;
                }

                response["success"] = true;
                response["equipmentId"] = equipmentId;
                response["message"] = "Equipo creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateEquipment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool UpdateEquipment(long equipmentId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetEquipmentState(equipmentId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El equipo no existe";
                    return false;
                }

                string name = data.ContainsKey("name")
                    ? Convert.ToString(data["name"]) ?? ""
                    : current.Value.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del equipo es requerido";
                    return false;
                }

                string status = data.ContainsKey("status")
                    ? (Convert.ToString(data["status"]) ?? "").Trim().ToLowerInvariant()
                    : current.Value.Status;

                if (!EquipmentStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de equipo no válido";
                    return false;
                }

                DateTime? purchaseDate = data.ContainsKey("purchaseDate")
                    ? TryParseDate(data["purchaseDate"])
                    : current.Value.PurchaseDate;

                DateTime? warrantyEnd = data.ContainsKey("warrantyEnd")
                    ? TryParseDate(data["warrantyEnd"])
                    : current.Value.WarrantyEnd;

                if (purchaseDate.HasValue && warrantyEnd.HasValue && warrantyEnd.Value < purchaseDate.Value)
                {
                    response["success"] = false;
                    response["message"] = "El fin de garantía no puede ser anterior a la compra";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_EQUIPMENT
                {
                    ID = equipmentId,
                    EQUIPMENT_NAME = name.Trim(),
                    CATEGORY = data.ContainsKey("category") ? Convert.ToString(data["category"]) ?? "" : current.Value.Category,
                    LOCATION = data.ContainsKey("location") ? Convert.ToString(data["location"]) ?? "" : current.Value.Location,
                    STATUS = status,
                    PURCHASE_DATE = purchaseDate,
                    WARRANTY_END = warrantyEnd
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el equipo";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Equipo actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateEquipment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeleteEquipment(long equipmentId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (GetEquipmentState(equipmentId) == null)
                {
                    response["success"] = false;
                    response["message"] = "El equipo no existe";
                    return false;
                }

                long tickets = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_MAINTENANCE_TICKETS WHERE EQUIPMENT_ID = ?", equipmentId);

                if (tickets > 0)
                {
                    response["success"] = false;
                    response["message"] = "El equipo tiene tickets de mantenimiento; no se puede eliminar para no perder el historial. Márcalo como fuera de servicio.";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_EQUIPMENT { ID = equipmentId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el equipo";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Equipo eliminado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteEquipment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Tickets
        // =====================================================================

        public bool GetTickets(
            out Dictionary<string, object> response,
            string? status = null,
            string? priority = null,
            long? equipmentId = null,
            int page = 1,
            int limit = 100)
        {
            response = new Dictionary<string, object>();

            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 500) limit = 100;

                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    whereParts.Add("t.STATUS = ?");
                    parameters.Add(status);
                }

                if (!string.IsNullOrWhiteSpace(priority))
                {
                    whereParts.Add("t.PRIORITY = ?");
                    parameters.Add(priority);
                }

                if (equipmentId.HasValue)
                {
                    whereParts.Add("t.EQUIPMENT_ID = ?");
                    parameters.Add(equipmentId.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM GYM_MAINTENANCE_TICKETS t {whereClause}", parameters.ToArray());

                int offset = (page - 1) * limit;
                var queryParameters = new List<object>(parameters) { offset, limit };

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT t.*, e.EQUIPMENT_NAME, u.FULL_NAME AS RESOLVED_BY_NAME " +
                    "FROM GYM_MAINTENANCE_TICKETS t " +
                    "INNER JOIN GYM_EQUIPMENT e ON e.ID = t.EQUIPMENT_ID " +
                    "LEFT JOIN GYM_USERS u ON u.ID = t.RESOLVED_BY " +
                    $"{whereClause} ORDER BY " +
                    "CASE t.STATUS WHEN 'open' THEN 0 WHEN 'in_progress' THEN 1 ELSE 2 END, " +
                    "CASE t.PRIORITY WHEN 'urgent' THEN 0 WHEN 'high' THEN 1 WHEN 'normal' THEN 2 ELSE 3 END, " +
                    "t.ID DESC OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                    queryParameters.ToArray());

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["ticketNumber"] = Convert.ToString(GetRowValue(row, result, "ticket_number")) ?? "",
                            ["equipmentId"] = Convert.ToInt64(GetRowValue(row, result, "equipment_id")),
                            ["equipmentName"] = Convert.ToString(GetRowValue(row, result, "equipment_name")) ?? "",
                            ["description"] = Convert.ToString(GetRowValue(row, result, "description")) ?? "",
                            ["priority"] = Convert.ToString(GetRowValue(row, result, "priority")) ?? "",
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                            ["resolvedAt"] = GetRowValue(row, result, "resolved_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "resolved_at")) : (DateTime?)null,
                            ["resolvedBy"] = GetRowValue(row, result, "resolved_by") != null ? Convert.ToInt64(GetRowValue(row, result, "resolved_by")) : (long?)null,
                            ["resolvedByName"] = Convert.ToString(GetRowValue(row, result, "resolved_by_name")) ?? "",
                            ["createdAt"] = GetRowValue(row, result, "created_at") != null ? Convert.ToDateTime(GetRowValue(row, result, "created_at")) : (DateTime?)null
                        });
                    }
                }

                int totalPages = limit > 0 ? (int)((totalCount + limit - 1) / limit) : 0;

                response["success"] = true;
                response["data"] = items;
                response["pagination"] = new Dictionary<string, object>
                {
                    ["page"] = page,
                    ["limit"] = limit,
                    ["totalCount"] = totalCount,
                    ["totalPages"] = totalPages,
                    ["hasNext"] = page < totalPages,
                    ["hasPrev"] = page > 1
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetTickets: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool CreateTicket(Dictionary<string, object> ticketData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var equipmentId = ticketData.ContainsKey("equipmentId") ? Convert.ToInt64(ticketData["equipmentId"]) : 0L;
                var description = Convert.ToString(ticketData.GetValueOrDefault("description", "")) ?? "";
                var priority = (Convert.ToString(ticketData.GetValueOrDefault("priority", "normal")) ?? "normal").Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(description))
                {
                    response["success"] = false;
                    response["message"] = "La descripción es requerida";
                    return false;
                }

                if (!TicketPriorities.Contains(priority))
                {
                    response["success"] = false;
                    response["message"] = "Prioridad no válida";
                    return false;
                }

                var equipment = GetEquipmentState(equipmentId);
                if (equipment == null)
                {
                    response["success"] = false;
                    response["message"] = "El equipo no existe";
                    return false;
                }

                long openTickets = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_MAINTENANCE_TICKETS WHERE EQUIPMENT_ID = ? AND STATUS IN ('open','in_progress')",
                    equipmentId);

                if (openTickets > 0)
                {
                    response["success"] = false;
                    response["message"] = "El equipo ya tiene un ticket de mantenimiento abierto";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long newTicketId = _tools.GenerateUniqueID(GYM_MAINTENANCE_TICKETS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_MAINTENANCE_TICKETS
                {
                    ID = newTicketId,
                    EQUIPMENT_ID = equipmentId,
                    TICKET_NUMBER = $"TKT-{newTicketId:D10}",
                    DESCRIPTION = description.Trim(),
                    PRIORITY = priority,
                    STATUS = "open"
                });

                if (equipment.Value.Status == "operational")
                {
                    session.UpdateObjectChanges(new GYM_EQUIPMENT
                    {
                        ID = equipmentId,
                        STATUS = "maintenance"
                    });
                }

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el ticket";
                    return false;
                }

                response["success"] = true;
                response["ticketId"] = newTicketId;
                response["ticketNumber"] = $"TKT-{newTicketId:D10}";
                response["message"] = "Ticket de mantenimiento creado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en CreateTicket: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool UpdateTicketStatus(long ticketId, string status, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                status = (status ?? "").Trim().ToLowerInvariant();

                if (!TicketStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de ticket no válido";
                    return false;
                }

                var ticket = GetTicketState(ticketId);
                if (ticket == null)
                {
                    response["success"] = false;
                    response["message"] = "El ticket no existe";
                    return false;
                }

                if (ticket.Value.Status == "resolved" || ticket.Value.Status == "cancelled")
                {
                    response["success"] = false;
                    response["message"] = "Un ticket resuelto o cancelado es final y no se puede modificar";
                    return false;
                }

                if (ticket.Value.Status == status)
                {
                    response["success"] = true;
                    response["message"] = "El ticket ya tiene ese estado";
                    return true;
                }

                bool isClosing = status == "resolved" || status == "cancelled";
                long auditUser = CurrentUser.ForAudit;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_MAINTENANCE_TICKETS
                {
                    ID = ticketId,
                    STATUS = status,
                    RESOLVED_AT = isClosing ? DateTime.Now : (DateTime?)null,
                    RESOLVED_BY = isClosing && auditUser > 0 ? auditUser : (long?)null
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el ticket";
                    return false;
                }

                if (isClosing)
                    RefreshEquipmentStatus(ticket.Value.EquipmentId);

                response["success"] = true;
                response["status"] = status;
                response["message"] = status switch
                {
                    "in_progress" => "Ticket marcado en progreso",
                    "resolved" => "Ticket resuelto correctamente",
                    "cancelled" => "Ticket cancelado",
                    _ => "Ticket reabierto"
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateTicketStatus: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        public bool DeleteTicket(long ticketId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var ticket = GetTicketState(ticketId);
                if (ticket == null)
                {
                    response["success"] = false;
                    response["message"] = "El ticket no existe";
                    return false;
                }

                if (ticket.Value.Status == "resolved")
                {
                    response["success"] = false;
                    response["message"] = "Un ticket resuelto no se puede eliminar porque forma parte del historial del equipo";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_MAINTENANCE_TICKETS { ID = ticketId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el ticket";
                    return false;
                }

                RefreshEquipmentStatus(ticket.Value.EquipmentId);

                response["success"] = true;
                response["message"] = "Ticket eliminado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteTicket: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static DateTime? TryParseDate(object? value)
        {
            var raw = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return DateTime.TryParse(raw, out var parsed) ? parsed.Date : (DateTime?)null;
        }

        /// <summary>
        /// Si el equipo no tiene tickets abiertos, vuelve a operativo.
        /// </summary>
        private void RefreshEquipmentStatus(long equipmentId)
        {
            try
            {
                var equipment = GetEquipmentState(equipmentId);
                if (equipment == null || equipment.Value.Status != "maintenance") return;

                long openTickets = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_MAINTENANCE_TICKETS WHERE EQUIPMENT_ID = ? AND STATUS IN ('open','in_progress')",
                    equipmentId);

                if (openTickets > 0) return;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.UpdateObjectChanges(new GYM_EQUIPMENT
                {
                    ID = equipmentId,
                    STATUS = "operational"
                });
                session.TryExecute();
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en RefreshEquipmentStatus: {ex.Message}");
            }
        }

        private (string Name, string Category, string Location, string Status, DateTime? PurchaseDate, DateTime? WarrantyEnd)? GetEquipmentState(long equipmentId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM GYM_EQUIPMENT WHERE ID = ?", equipmentId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToString(GetRowValue(row, result, "equipment_name")) ?? "",
                Convert.ToString(GetRowValue(row, result, "category")) ?? "",
                Convert.ToString(GetRowValue(row, result, "location")) ?? "",
                Convert.ToString(GetRowValue(row, result, "status")) ?? "operational",
                GetRowValue(row, result, "purchase_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "purchase_date")) : (DateTime?)null,
                GetRowValue(row, result, "warranty_end") != null ? Convert.ToDateTime(GetRowValue(row, result, "warranty_end")) : (DateTime?)null
            );
        }

        private (long EquipmentId, string Status)? GetTicketState(long ticketId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT EQUIPMENT_ID, STATUS FROM GYM_MAINTENANCE_TICKETS WHERE ID = ?", ticketId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "equipment_id")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? ""
            );
        }
    }
}
