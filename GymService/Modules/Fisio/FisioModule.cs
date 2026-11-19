using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Modules.Fisio
{
    /// <summary>
    /// Fisio module: paquetes de fisioterapia y citas.
    /// </summary>
    public class FisioModule
    {
        private const string SubModule = "FISIO_MODULE";

        private static readonly HashSet<string> AppointmentStatuses = new()
        {
            "scheduled", "completed", "cancelled", "no_show"
        };

        // Serializa los cambios de uso de sesiones por paquete dentro de la instancia.
        private static readonly ConcurrentDictionary<long, object> PackageLocks = new();

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

        private static object LockFor(long packageId)
        {
            return PackageLocks.GetOrAdd(packageId, _ => new object());
        }

        // =====================================================================
        // Paquetes
        // =====================================================================

        /// <summary>
        /// Lista los paquetes de fisioterapia.
        /// </summary>
        public bool GetPackages(out Dictionary<string, object> response, long? memberId = null, bool includeInactive = false)
        {
            response = new Dictionary<string, object>();

            try
            {
                var whereParts = new List<string>();
                var parameters = new List<object>();

                if (!includeInactive)
                    whereParts.Add("p.IS_ACTIVE = 1");

                if (memberId.HasValue)
                {
                    whereParts.Add("p.MEMBER_ID = ?");
                    parameters.Add(memberId.Value);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT p.*, m.FIRST_NAME, m.LAST_NAME " +
                    "FROM GYM_FISIO_PACKAGES p " +
                    "LEFT JOIN GYM_MEMBERS m ON m.ID = p.MEMBER_ID " +
                    $"{whereClause} ORDER BY p.IS_ACTIVE DESC, p.START_DATE DESC",
                    parameters.ToArray());

                var packages = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        int total = Convert.ToInt32(GetRowValue(row, result, "sessions_total"));
                        int used = Convert.ToInt32(GetRowValue(row, result, "sessions_used"));
                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        packages.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["name"] = Convert.ToString(GetRowValue(row, result, "name")) ?? "",
                            ["memberId"] = GetRowValue(row, result, "member_id") != null ? Convert.ToInt64(GetRowValue(row, result, "member_id")) : (long?)null,
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["sessions"] = total,
                            ["sessionsTotal"] = total,
                            ["sessionsUsed"] = used,
                            ["sessionsRemaining"] = Math.Max(0, total - used),
                            ["price"] = Convert.ToDecimal(GetRowValue(row, result, "price")),
                            ["startDate"] = GetRowValue(row, result, "start_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "start_date")) : (DateTime?)null,
                            ["endDate"] = GetRowValue(row, result, "end_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "end_date")) : (DateTime?)null,
                            ["isActive"] = GetRowValue(row, result, "is_active") != null && Convert.ToBoolean(GetRowValue(row, result, "is_active"))
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

        /// <summary>
        /// Crea un paquete de fisioterapia (general o asociado a un socio).
        /// </summary>
        public bool CreatePackage(Dictionary<string, object> packageData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var name = Convert.ToString(packageData.GetValueOrDefault("name", "")) ?? "";
                var sessions = packageData.ContainsKey("sessions") ? Convert.ToInt32(packageData["sessions"]) : 10;
                var price = packageData.ContainsKey("price") ? Convert.ToDecimal(packageData["price"]) : 0m;
                var memberId = packageData.ContainsKey("memberId") ? Convert.ToInt64(packageData["memberId"]) : 0L;

                DateTime startDate = DateTime.TryParse(Convert.ToString(packageData.GetValueOrDefault("startDate", "")), out var parsedStart)
                    ? parsedStart.Date
                    : DateTime.Now.Date;

                DateTime endDate = DateTime.TryParse(Convert.ToString(packageData.GetValueOrDefault("endDate", "")), out var parsedEnd)
                    ? parsedEnd.Date
                    : startDate.AddMonths(1);

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del paquete es requerido";
                    return false;
                }

                if (sessions <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El número de sesiones debe ser mayor a cero";
                    return false;
                }

                if (price < 0)
                {
                    response["success"] = false;
                    response["message"] = "El precio no puede ser negativo";
                    return false;
                }

                if (endDate < startDate)
                {
                    response["success"] = false;
                    response["message"] = "La fecha de fin no puede ser anterior a la de inicio";
                    return false;
                }

                if (memberId > 0 && !MemberExists(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                long newPackageId = _tools.GenerateUniqueID(GYM_FISIO_PACKAGES.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_FISIO_PACKAGES
                {
                    ID = newPackageId,
                    NAME = name.Trim(),
                    // El paquete puede ser general (sin socio asignado).
                    MEMBER_ID = memberId > 0 ? memberId : (long?)null,
                    SESSIONS_TOTAL = sessions,
                    SESSIONS_USED = 0,
                    PRICE = price,
                    START_DATE = startDate,
                    END_DATE = endDate,
                    IS_ACTIVE = true
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear el paquete";
                    return false;
                }

                response["success"] = true;
                response["packageId"] = newPackageId;
                response["message"] = "Paquete de fisioterapia creado correctamente";
                return true;
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
        /// Actualiza un paquete de fisioterapia.
        /// </summary>
        public bool UpdatePackage(long packageId, Dictionary<string, object> packageData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var current = GetPackageState(packageId);
                if (current == null)
                {
                    response["success"] = false;
                    response["message"] = "El paquete no existe";
                    return false;
                }

                var name = packageData.ContainsKey("name")
                    ? Convert.ToString(packageData["name"]) ?? ""
                    : current.Value.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    response["success"] = false;
                    response["message"] = "El nombre del paquete es requerido";
                    return false;
                }

                int sessions = packageData.ContainsKey("sessions")
                    ? Convert.ToInt32(packageData["sessions"])
                    : current.Value.Total;

                if (sessions <= 0)
                {
                    response["success"] = false;
                    response["message"] = "El número de sesiones debe ser mayor a cero";
                    return false;
                }

                if (sessions < current.Value.Used)
                {
                    response["success"] = false;
                    response["message"] = $"El paquete ya tiene {current.Value.Used} sesiones usadas; no puede tener menos";
                    return false;
                }

                decimal price = packageData.ContainsKey("price")
                    ? Convert.ToDecimal(packageData["price"])
                    : current.Value.Price;

                if (price < 0)
                {
                    response["success"] = false;
                    response["message"] = "El precio no puede ser negativo";
                    return false;
                }

                long? memberId = current.Value.MemberId;
                if (packageData.ContainsKey("memberId"))
                {
                    long requested = Convert.ToInt64(packageData["memberId"]);
                    memberId = requested > 0 ? requested : (long?)null;

                    if (memberId.HasValue && !MemberExists(memberId.Value))
                    {
                        response["success"] = false;
                        response["message"] = "El socio no existe";
                        return false;
                    }
                }

                DateTime startDate = packageData.ContainsKey("startDate") &&
                    DateTime.TryParse(Convert.ToString(packageData["startDate"]), out var parsedStart)
                    ? parsedStart.Date
                    : current.Value.StartDate;

                DateTime endDate = packageData.ContainsKey("endDate") &&
                    DateTime.TryParse(Convert.ToString(packageData["endDate"]), out var parsedEnd)
                    ? parsedEnd.Date
                    : current.Value.EndDate;

                if (endDate < startDate)
                {
                    response["success"] = false;
                    response["message"] = "La fecha de fin no puede ser anterior a la de inicio";
                    return false;
                }

                bool isActive = packageData.ContainsKey("isActive")
                    ? Convert.ToBoolean(packageData["isActive"])
                    : current.Value.IsActive;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_FISIO_PACKAGES
                {
                    ID = packageId,
                    NAME = name.Trim(),
                    MEMBER_ID = memberId,
                    SESSIONS_TOTAL = sessions,
                    SESSIONS_USED = current.Value.Used,
                    PRICE = price,
                    START_DATE = startDate,
                    END_DATE = endDate,
                    IS_ACTIVE = isActive
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar el paquete";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Paquete actualizado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdatePackage: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Elimina un paquete si no tiene citas ni sesiones usadas.
        /// </summary>
        public bool DeletePackage(long packageId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var state = GetPackageState(packageId);
                if (state == null)
                {
                    response["success"] = false;
                    response["message"] = "El paquete no existe";
                    return false;
                }

                long appointments = _tools.ExecuteQueryObjectCount(
                    "SELECT COUNT(*) FROM GYM_FISIO_APPOINTMENTS WHERE PACKAGE_ID = ?", packageId);

                if (appointments > 0 || state.Value.Used > 0)
                {
                    response["success"] = false;
                    response["message"] = "El paquete tiene sesiones usadas o citas asociadas; no se puede eliminar para no perder el historial. Desactívalo en su lugar.";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_FISIO_PACKAGES { ID = packageId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar el paquete";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Paquete eliminado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeletePackage: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Citas
        // =====================================================================

        /// <summary>
        /// Lista las citas de fisioterapia con filtros.
        /// </summary>
        public bool GetAppointments(
            out Dictionary<string, object> response,
            long? memberId = null,
            DateTime? from = null,
            DateTime? to = null,
            string? status = null,
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

                if (memberId.HasValue)
                {
                    whereParts.Add("a.MEMBER_ID = ?");
                    parameters.Add(memberId.Value);
                }

                if (from.HasValue)
                {
                    whereParts.Add("a.APPOINTMENT_DATE >= ?");
                    parameters.Add(from.Value);
                }

                if (to.HasValue)
                {
                    whereParts.Add("a.APPOINTMENT_DATE <= ?");
                    parameters.Add(to.Value);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    whereParts.Add("a.STATUS = ?");
                    parameters.Add(status);
                }

                string whereClause = whereParts.Count > 0 ? "WHERE " + string.Join(" AND ", whereParts) : "";

                long totalCount = _tools.ExecuteQueryObjectCount(
                    $"SELECT COUNT(*) FROM GYM_FISIO_APPOINTMENTS a {whereClause}", parameters.ToArray());

                int offset = (page - 1) * limit;
                var queryParameters = new List<object>(parameters) { offset, limit };

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT a.*, m.FIRST_NAME, m.LAST_NAME, p.NAME AS PACKAGE_NAME " +
                    "FROM GYM_FISIO_APPOINTMENTS a " +
                    "INNER JOIN GYM_MEMBERS m ON m.ID = a.MEMBER_ID " +
                    "LEFT JOIN GYM_FISIO_PACKAGES p ON p.ID = a.PACKAGE_ID " +
                    $"{whereClause} ORDER BY a.APPOINTMENT_DATE DESC, a.ID DESC " +
                    "OFFSET ? ROWS FETCH NEXT ? ROWS ONLY",
                    queryParameters.ToArray());

                var items = new List<Dictionary<string, object>>();
                if (result != null)
                {
                    foreach (Row row in result.Rows)
                    {
                        string firstName = Convert.ToString(GetRowValue(row, result, "first_name")) ?? "";
                        string lastName = Convert.ToString(GetRowValue(row, result, "last_name")) ?? "";

                        items.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["memberId"] = Convert.ToInt64(GetRowValue(row, result, "member_id")),
                            ["memberName"] = (firstName + " " + lastName).Trim(),
                            ["packageId"] = GetRowValue(row, result, "package_id") != null ? Convert.ToInt64(GetRowValue(row, result, "package_id")) : (long?)null,
                            ["packageName"] = Convert.ToString(GetRowValue(row, result, "package_name")) ?? "",
                            ["appointmentDate"] = GetRowValue(row, result, "appointment_date") != null ? Convert.ToDateTime(GetRowValue(row, result, "appointment_date")) : (DateTime?)null,
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                            ["notes"] = Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetAppointments: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Agenda una cita de fisioterapia.
        /// </summary>
        public bool CreateAppointment(Dictionary<string, object> appointmentData, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var memberId = appointmentData.ContainsKey("memberId") ? Convert.ToInt64(appointmentData["memberId"]) : 0L;
                var packageId = appointmentData.ContainsKey("packageId") ? Convert.ToInt64(appointmentData["packageId"]) : 0L;
                var notes = Convert.ToString(appointmentData.GetValueOrDefault("notes", "")) ?? "";

                DateTime appointmentDate = DateTime.TryParse(
                    Convert.ToString(appointmentData.GetValueOrDefault("appointmentDate", "")), out var parsedDate)
                    ? parsedDate
                    : DateTime.Now;

                if (memberId <= 0 || !MemberExists(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no existe";
                    return false;
                }

                if (!MemberIsActive(memberId))
                {
                    response["success"] = false;
                    response["message"] = "El socio no está activo";
                    return false;
                }

                if (appointmentDate < DateTime.Now.AddMinutes(-5))
                {
                    response["success"] = false;
                    response["message"] = "La fecha de la cita no puede estar en el pasado";
                    return false;
                }

                long? assignedPackageId = null;
                bool assignPackage = false;
                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                if (packageId > 0)
                {
                    var package = GetPackageState(packageId);
                    if (package == null)
                    {
                        response["success"] = false;
                        response["message"] = "El paquete no existe";
                        return false;
                    }

                    if (package.Value.Used >= package.Value.Total)
                    {
                        response["success"] = false;
                        response["message"] = "El paquete no tiene sesiones disponibles";
                        return false;
                    }

                    if (!package.Value.IsActive)
                    {
                        response["success"] = false;
                        response["message"] = "El paquete está inactivo";
                        return false;
                    }

                    if (package.Value.MemberId.HasValue && package.Value.MemberId.Value != memberId)
                    {
                        response["success"] = false;
                        response["message"] = "El paquete pertenece a otro socio";
                        return false;
                    }

                    if (appointmentDate.Date < package.Value.StartDate || appointmentDate.Date > package.Value.EndDate)
                    {
                        response["success"] = false;
                        response["message"] = "La cita está fuera del periodo de vigencia del paquete";
                        return false;
                    }

                    assignedPackageId = packageId;

                    // Un paquete general queda asignado al socio la primera vez que se usa.
                    if (!package.Value.MemberId.HasValue)
                    {
                        assignPackage = true;
                        session.UpdateObjectChanges(new GYM_FISIO_PACKAGES
                        {
                            ID = packageId,
                            MEMBER_ID = memberId
                        });
                    }
                }

                long newAppointmentId = _tools.GenerateUniqueID(GYM_FISIO_APPOINTMENTS.DatabaseMappingObject, 1).GetInt64UniqueId();

                session.InsertObjectWithID(new GYM_FISIO_APPOINTMENTS
                {
                    ID = newAppointmentId,
                    MEMBER_ID = memberId,
                    PACKAGE_ID = assignedPackageId,
                    APPOINTMENT_DATE = appointmentDate,
                    STATUS = "scheduled",
                    NOTES = notes.Trim()
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al crear la cita";
                    return false;
                }

                response["success"] = true;
                response["appointmentId"] = newAppointmentId;
                response["packageAssigned"] = assignPackage;
                response["message"] = "Cita de fisioterapia creada correctamente";
                return true;
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
        /// Reprograma o edita las notas de una cita programada.
        /// </summary>
        public bool UpdateAppointment(long appointmentId, Dictionary<string, object> data, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var appointment = GetAppointmentState(appointmentId);
                if (appointment == null)
                {
                    response["success"] = false;
                    response["message"] = "La cita no existe";
                    return false;
                }

                if (appointment.Value.Status != "scheduled")
                {
                    response["success"] = false;
                    response["message"] = "Solo se pueden modificar citas programadas";
                    return false;
                }

                DateTime appointmentDate = data.ContainsKey("appointmentDate") &&
                    DateTime.TryParse(Convert.ToString(data["appointmentDate"]), out var parsedDate)
                    ? parsedDate
                    : appointment.Value.AppointmentDate;

                if (appointmentDate < DateTime.Now.AddMinutes(-5))
                {
                    response["success"] = false;
                    response["message"] = "La fecha de la cita no puede estar en el pasado";
                    return false;
                }

                string notes = data.ContainsKey("notes")
                    ? Convert.ToString(data["notes"]) ?? ""
                    : appointment.Value.Notes;

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                session.UpdateObjectChanges(new GYM_FISIO_APPOINTMENTS
                {
                    ID = appointmentId,
                    MEMBER_ID = appointment.Value.MemberId,
                    PACKAGE_ID = appointment.Value.PackageId,
                    APPOINTMENT_DATE = appointmentDate,
                    STATUS = appointment.Value.Status,
                    NOTES = notes.Trim()
                });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al actualizar la cita";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Cita actualizada correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en UpdateAppointment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Cambia el estado de una cita. Al completarla consume una sesión del paquete.
        /// </summary>
        public bool SetAppointmentStatus(long appointmentId, string status, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                status = (status ?? "").Trim().ToLowerInvariant();

                if (!AppointmentStatuses.Contains(status))
                {
                    response["success"] = false;
                    response["message"] = "Estado de cita no válido";
                    return false;
                }

                var appointment = GetAppointmentState(appointmentId);
                if (appointment == null)
                {
                    response["success"] = false;
                    response["message"] = "La cita no existe";
                    return false;
                }

                string current = appointment.Value.Status;

                if (current == status)
                {
                    response["success"] = true;
                    response["message"] = "La cita ya tiene ese estado";
                    return true;
                }

                if (current == "completed")
                {
                    response["success"] = false;
                    response["message"] = "Una cita completada no se puede modificar";
                    return false;
                }

                if (current != "scheduled" && status == "completed")
                {
                    response["success"] = false;
                    response["message"] = "Solo una cita programada se puede completar";
                    return false;
                }

                bool consumeSession = status == "completed" && appointment.Value.PackageId.HasValue;
                bool releaseSession = current == "completed" && status != "completed" && appointment.Value.PackageId.HasValue;

                if (consumeSession || releaseSession)
                {
                    long packageId = appointment.Value.PackageId!.Value;

                    lock (LockFor(packageId))
                    {
                        var package = GetPackageState(packageId);
                        if (package == null)
                        {
                            response["success"] = false;
                            response["message"] = "El paquete de la cita no existe";
                            return false;
                        }

                        int used = package.Value.Used;

                        if (consumeSession)
                        {
                            if (used >= package.Value.Total)
                            {
                                response["success"] = false;
                                response["message"] = "El paquete no tiene sesiones disponibles";
                                return false;
                            }
                            used++;
                        }
                        else
                        {
                            used = Math.Max(0, used - 1);
                        }

                        var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                        session.UpdateObjectChanges(new GYM_FISIO_PACKAGES
                        {
                            ID = packageId,
                            SESSIONS_USED = used,
                            IS_ACTIVE = used < package.Value.Total && package.Value.IsActive
                        });

                        session.UpdateObjectChanges(new GYM_FISIO_APPOINTMENTS
                        {
                            ID = appointmentId,
                            MEMBER_ID = appointment.Value.MemberId,
                            PACKAGE_ID = appointment.Value.PackageId,
                            APPOINTMENT_DATE = appointment.Value.AppointmentDate,
                            STATUS = status,
                            NOTES = appointment.Value.Notes
                        });

                        if (session.TryExecute().IsError)
                        {
                            response["success"] = false;
                            response["message"] = "Error al actualizar la cita";
                            return false;
                        }
                    }
                }
                else
                {
                    var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);

                    session.UpdateObjectChanges(new GYM_FISIO_APPOINTMENTS
                    {
                        ID = appointmentId,
                        MEMBER_ID = appointment.Value.MemberId,
                        PACKAGE_ID = appointment.Value.PackageId,
                        APPOINTMENT_DATE = appointment.Value.AppointmentDate,
                        STATUS = status,
                        NOTES = appointment.Value.Notes
                    });

                    if (session.TryExecute().IsError)
                    {
                        response["success"] = false;
                        response["message"] = "Error al actualizar la cita";
                        return false;
                    }
                }

                response["success"] = true;
                response["status"] = status;
                response["message"] = status switch
                {
                    "completed" => "Cita completada y sesión descontada",
                    "cancelled" => "Cita cancelada",
                    "no_show" => "Cita marcada como no asistida",
                    _ => "Cita reprogramada"
                };
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en SetAppointmentStatus: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Elimina una cita que no haya sido completada.
        /// </summary>
        public bool DeleteAppointment(long appointmentId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var appointment = GetAppointmentState(appointmentId);
                if (appointment == null)
                {
                    response["success"] = false;
                    response["message"] = "La cita no existe";
                    return false;
                }

                if (appointment.Value.Status == "completed")
                {
                    response["success"] = false;
                    response["message"] = "Una cita completada no se puede eliminar porque ya consumió una sesión";
                    return false;
                }

                var session = _tools.CreateSession(SubModule, CurrentUser.ForAudit);
                session.DeleteObject(new GYM_FISIO_APPOINTMENTS { ID = appointmentId });

                if (session.TryExecute().IsError)
                {
                    response["success"] = false;
                    response["message"] = "Error al eliminar la cita";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Cita eliminada";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeleteAppointment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private bool MemberExists(long memberId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_MEMBERS WHERE ID = ?", memberId) > 0;
        }

        private bool MemberIsActive(long memberId)
        {
            return _tools.ExecuteQueryObjectCount(
                "SELECT COUNT(*) FROM GYM_MEMBERS WHERE ID = ? AND STATUS = 'active'", memberId) > 0;
        }

        private (string Name, long? MemberId, int Total, int Used, decimal Price, DateTime StartDate, DateTime EndDate, bool IsActive)? GetPackageState(long packageId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM GYM_FISIO_PACKAGES WHERE ID = ?", packageId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToString(GetRowValue(row, result, "name")) ?? "",
                GetRowValue(row, result, "member_id") != null ? Convert.ToInt64(GetRowValue(row, result, "member_id")) : (long?)null,
                Convert.ToInt32(GetRowValue(row, result, "sessions_total")),
                Convert.ToInt32(GetRowValue(row, result, "sessions_used")),
                Convert.ToDecimal(GetRowValue(row, result, "price")),
                Convert.ToDateTime(GetRowValue(row, result, "start_date")),
                Convert.ToDateTime(GetRowValue(row, result, "end_date")),
                Convert.ToBoolean(GetRowValue(row, result, "is_active"))
            );
        }

        private (long MemberId, long? PackageId, DateTime AppointmentDate, string Status, string Notes)? GetAppointmentState(long appointmentId)
        {
            var result = _tools.ExecuteQueryResultSet(
                "SELECT * FROM GYM_FISIO_APPOINTMENTS WHERE ID = ?", appointmentId);

            if (result == null || result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];

            return (
                Convert.ToInt64(GetRowValue(row, result, "member_id")),
                GetRowValue(row, result, "package_id") != null ? Convert.ToInt64(GetRowValue(row, result, "package_id")) : (long?)null,
                Convert.ToDateTime(GetRowValue(row, result, "appointment_date")),
                Convert.ToString(GetRowValue(row, result, "status")) ?? "",
                Convert.ToString(GetRowValue(row, result, "notes")) ?? ""
            );
        }
    }
}
