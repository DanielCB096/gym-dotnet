using System;
using System.Collections.Generic;
using System.Linq;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;
using GymService.Tools;

namespace GymService.Modules.Kiosk
{
    /// <summary>
    /// Kiosk module: dispositivos de kiosko y autoservicio.
    /// </summary>
    public class KioskModule
    {
        private const string SubModule = "KIOSK_MODULE";
        private DatabaseTools _tools;

        public KioskModule(DatabaseTools tools)
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
        /// Register a new kiosk device
        /// </summary>
        public bool RegisterDevice(string deviceName, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var token = SecurityValidation.GenerateToken(64);
                var tokenHash = SecurityValidation.Sha256Hash(token);

                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "INSERT INTO gym_kiosk_devices (device_name, device_token_hash, is_active, created_at) VALUES (?, ?, 1, GETDATE())",
                    deviceName, tokenHash
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al registrar dispositivo kiosko");
                    response["success"] = false;
                    response["message"] = "Error al registrar dispositivo";
                    return false;
                }

                response["success"] = true;
                response["token"] = token;
                response["message"] = "Dispositivo registrado exitosamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en RegisterDevice: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Validate a kiosk device token
        /// </summary>
        public bool ValidateDeviceToken(string token, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var tokenHash = SecurityValidation.Sha256Hash(token);

                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id, device_name FROM gym_kiosk_devices WHERE device_token_hash = ? AND is_active = 1",
                    tokenHash
                );

                if (result == null || result.Rows.Count == 0)
                {
                    response["success"] = false;
                    response["message"] = "Token de dispositivo inválido";
                    return false;
                }

                var row = result.Rows[0];
                response["success"] = true;
                response["deviceId"] = Convert.ToInt64(GetRowValue(row, result, "id"));
                response["deviceName"] = Convert.ToString(GetRowValue(row, result, "device_name")) ?? "";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en ValidateDeviceToken: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Get unavailable equipment from maintenance records
        /// </summary>
        public bool GetUnavailableEquipment(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT e.* FROM gym_equipment e WHERE e.status = 'maintenance' ORDER BY e.name"
                );

                var equipment = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        equipment.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["name"] = Convert.ToString(GetRowValue(row, result, "name")) ?? "",
                            ["status"] = Convert.ToString(GetRowValue(row, result, "status")) ?? "maintenance"
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
                Logger.AddErrorSubModule(SubModule, $"Error en GetUnavailableEquipment: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
        /// <summary>
        /// Get all registered kiosk devices
        /// </summary>
        public bool GetDevices(out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var result = _tools.ExecuteQueryResultSet(
                    "SELECT id, device_name, is_active, created_at FROM gym_kiosk_devices ORDER BY device_name"
                );

                var devices = new List<Dictionary<string, object>>();
                if (result != null && result.Rows.Count > 0)
                {
                    foreach (Row row in result.Rows)
                    {
                        devices.Add(new Dictionary<string, object>
                        {
                            ["id"] = Convert.ToInt64(GetRowValue(row, result, "id")),
                            ["deviceName"] = Convert.ToString(GetRowValue(row, result, "device_name")) ?? "",
                            ["isActive"] = Convert.ToBoolean(GetRowValue(row, result, "is_active")),
                            ["createdAt"] = Convert.ToString(GetRowValue(row, result, "created_at")) ?? ""
                        });
                    }
                }

                response["success"] = true;
                response["data"] = devices;
                response["count"] = devices.Count;
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en GetDevices: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }

        /// <summary>
        /// Deactivate a kiosk device
        /// </summary>
        public bool DeactivateDevice(long deviceId, out Dictionary<string, object> response)
        {
            response = new Dictionary<string, object>();

            try
            {
                var session = _tools.CreateSession(SubModule);
                session.ExecuteTransaction(
                    "UPDATE gym_kiosk_devices SET is_active = 0 WHERE id = ?",
                    deviceId
                );

                if (session.TryExecute().IsError)
                {
                    Logger.AddErrorSubModule(SubModule, "Error al desactivar dispositivo kiosko");
                    response["success"] = false;
                    response["message"] = "Error al desactivar dispositivo";
                    return false;
                }

                response["success"] = true;
                response["message"] = "Dispositivo desactivado correctamente";
                return true;
            }
            catch (Exception ex)
            {
                Logger.AddErrorSubModule(SubModule, $"Error en DeactivateDevice: {ex.Message}");
                response["success"] = false;
                response["message"] = "Error interno del servidor";
                return false;
            }
        }
    }
}
