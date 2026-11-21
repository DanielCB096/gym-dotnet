using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using AxLogger;
using AxDatabase;
using AxObjectOrientedDatabase;

namespace GymService.Services
{
    /// <summary>
    /// Result of a bulk import operation, containing success/failure counts and details.
    /// </summary>
    public class BulkImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ImportedRows { get; set; }
        public int SkippedRows { get; set; }
        public int ErrorRows { get; set; }
        public List<BulkImportError> Errors { get; set; } = new List<BulkImportError>();
        public string TableName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A single row error during bulk import.
    /// </summary>
    public class BulkImportError
    {
        public int RowNumber { get; set; }
        public string RowData { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service for bulk importing data from CSV and Excel files into the database.
    /// Supports members, inventory, and other entities with validation and error reporting.
    /// </summary>
    public class BulkImportService
    {
        private const string SubModule = "BULK_IMPORT";

        /// <summary>
        /// Filas por sesión. Mantiene transacciones pequeñas sin llegar a una por fila.
        /// </summary>
        private const int BatchSize = 200;

        private readonly DatabaseTools? _tools;

        public BulkImportService()
        {
        }

        /// <summary>
        /// Constructor para pruebas o ejecución fuera del arranque global del servicio.
        /// </summary>
        public BulkImportService(DatabaseTools tools)
        {
            _tools = tools;
        }

        private DatabaseTools? GetTools() => _tools ?? DataCenter.DatabaseManager?.Tools;

        private sealed class PendingImport<T> where T : IDatabaseObject
        {
            public PendingImport(int rowNumber, string rowData, T databaseObject)
            {
                RowNumber = rowNumber;
                RowData = rowData;
                DatabaseObject = databaseObject;
            }

            public int RowNumber { get; }
            public string RowData { get; }
            public T DatabaseObject { get; }
        }

        /// <summary>
        /// Inserta el lote en una sola sesión. Si la transacción falla se reintenta
        /// fila por fila para conservar el detalle de errores por fila.
        /// </summary>
        private void ExecuteBatch<T>(List<PendingImport<T>> batch, DatabaseTools tools, BulkImportResult result) where T : IDatabaseObject
        {
            if (batch.Count == 0)
                return;

            var session = tools.CreateSession(SubModule, CurrentUser.ForAudit);
            foreach (var pending in batch)
                session.InsertObject(pending.DatabaseObject);

            var rc = session.TryExecute();
            if (!rc.IsError)
            {
                result.ImportedRows += batch.Count;
                batch.Clear();
                return;
            }

            Logger.AddWarningSubModule(SubModule,
                $"Lote de {batch.Count} filas rechazado ({rc.Message}); reintentando fila por fila");

            foreach (var pending in batch)
            {
                var rowSession = tools.CreateSession(SubModule, CurrentUser.ForAudit);
                rowSession.InsertObject(pending.DatabaseObject);
                var rowResult = rowSession.TryExecute();

                if (!rowResult.IsError)
                {
                    result.ImportedRows++;
                }
                else
                {
                    result.ErrorRows++;
                    result.Errors.Add(new BulkImportError
                    {
                        RowNumber = pending.RowNumber,
                        RowData = pending.RowData,
                        ErrorMessage = FriendlyErrorMessage(rowResult.Message)
                    });
                }
            }

            batch.Clear();
        }

        private static string ReadColumn(Dictionary<string, string> row, string column)
        {
            return row.GetValueOrDefault(column, "").Trim();
        }

        /// <summary>
        /// Traduce los errores de SQL Server más frecuentes a mensajes entendibles
        /// para la tabla de errores de la importación (sin volcar el SQL completo).
        /// </summary>
        private static string FriendlyErrorMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Error al insertar la fila";

            if (message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(message, @"duplicate key value is \((?<value>.*?)\)", RegexOptions.IgnoreCase);
                return match.Success
                    ? $"El valor '{match.Groups["value"].Value}' ya existe; usa uno distinto."
                    : "Ya existe un registro con ese valor único.";
            }

            if (message.Contains("would be truncated", StringComparison.OrdinalIgnoreCase))
                return "Uno de los valores supera la longitud permitida.";

            if (message.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase))
                return "El registro hace referencia a un dato que no existe.";

            var firstLine = message.Split('\n')[0].Trim();
            return firstLine.Length > 300 ? firstLine[..300] : firstLine;
        }

        /// <summary>
        /// Codificaciones admitidas sin depender del proveedor de code pages:
        /// UTF-8 y Latin-1 (los CSV exportados por Excel en español suelen ser Latin-1).
        /// </summary>
        private static bool TryResolveEncoding(string? encoding, out Encoding resolved, out string error)
        {
            resolved = Encoding.UTF8;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(encoding))
                return true;

            switch (encoding.Trim().ToLowerInvariant().Replace("_", "-"))
            {
                case "utf-8":
                case "utf8":
                    return true;
                case "latin1":
                case "latin-1":
                case "iso-8859-1":
                case "windows-1252":
                case "cp1252":
                case "1252":
                    resolved = Encoding.Latin1;
                    return true;
                default:
                    error = $"Codificación no admitida: {encoding}. Usa UTF-8 o Latin-1.";
                    return false;
            }
        }

        private static HashSet<string> LoadExistingMemberEmails(DatabaseTools tools)
        {
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = tools.ExecuteQueryResultSet(
                "SELECT EMAIL FROM GYM_MEMBERS WHERE EMAIL IS NOT NULL AND EMAIL <> ''");

            if (result == null)
                return emails;

            foreach (Row row in result.Rows)
            {
                var email = Convert.ToString(row.GetObject(0))?.Trim();
                if (!string.IsNullOrWhiteSpace(email))
                    emails.Add(email);
            }

            return emails;
        }

        private static GYM_MEMBERS? BuildMember(
            Dictionary<string, string> row,
            int rowNum,
            BulkImportResult result,
            HashSet<string> existingEmails,
            HashSet<string> fileEmails)
        {
            var firstName = ReadColumn(row, "FirstName");
            var lastName = ReadColumn(row, "LastName");

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                result.ErrorRows++;
                result.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNum,
                    RowData = $"FirstName='{firstName}', LastName='{lastName}'",
                    ErrorMessage = "Nombre y apellido son obligatorios"
                });
                return null;
            }

            var member = new GYM_MEMBERS
            {
                FIRST_NAME = firstName,
                LAST_NAME = lastName,
                STATUS = "active"
            };

            var email = ReadColumn(row, "Email");
            if (email.Length > 0)
            {
                if (existingEmails.Contains(email))
                {
                    result.ErrorRows++;
                    result.Errors.Add(new BulkImportError
                    {
                        RowNumber = rowNum,
                        RowData = $"Nombre: {firstName} {lastName}",
                        ErrorMessage = $"Ya existe un socio con el correo '{email}'"
                    });
                    return null;
                }

                if (!fileEmails.Add(email))
                {
                    result.ErrorRows++;
                    result.Errors.Add(new BulkImportError
                    {
                        RowNumber = rowNum,
                        RowData = $"Nombre: {firstName} {lastName}",
                        ErrorMessage = $"El correo '{email}' está repetido en el archivo"
                    });
                    return null;
                }

                member.EMAIL = email;
            }

            var phone = ReadColumn(row, "Phone");
            if (phone.Length > 0) member.PHONE = phone;

            var curp = ReadColumn(row, "CURP");
            if (curp.Length > 0) member.CURP = curp;

            var rfc = ReadColumn(row, "RFC");
            if (rfc.Length > 0) member.RFC = rfc;

            var address = ReadColumn(row, "Address");
            if (address.Length > 0) member.ADDRESS = address;

            var emergencyContact = ReadColumn(row, "EmergencyContact");
            if (emergencyContact.Length > 0) member.EMERGENCY_CONTACT = emergencyContact;

            var emergencyPhone = ReadColumn(row, "EmergencyPhone");
            if (emergencyPhone.Length > 0) member.EMERGENCY_PHONE = emergencyPhone;

            return member;
        }

        private static GYM_INVENTORY? BuildInventoryItem(Dictionary<string, string> row, int rowNum, string rowData, BulkImportResult result)
        {
            var productName = ReadColumn(row, "ProductName");
            if (string.IsNullOrWhiteSpace(productName))
            {
                result.ErrorRows++;
                result.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNum,
                    RowData = rowData,
                    ErrorMessage = "Nombre del producto es obligatorio"
                });
                return null;
            }

            var priceText = ReadColumn(row, "Price");
            if (priceText.Length == 0)
                priceText = "0";

            if (!decimal.TryParse(priceText, out var price))
            {
                result.ErrorRows++;
                result.Errors.Add(new BulkImportError
                {
                    RowNumber = rowNum,
                    RowData = $"ProductName='{productName}', Price='{priceText}'",
                    ErrorMessage = "Precio no es un número válido"
                });
                return null;
            }

            int.TryParse(ReadColumn(row, "StockQuantity"), out var stockQty);

            var minStock = 5;
            int.TryParse(ReadColumn(row, "MinStock"), out minStock);

            decimal.TryParse(ReadColumn(row, "Cost"), out var cost);

            var item = new GYM_INVENTORY
            {
                PRODUCT_NAME = productName,
                PRICE = price,
                COST = cost > 0 ? cost : (decimal?)null,
                STOCK_QUANTITY = stockQty,
                MIN_STOCK = minStock,
                IS_ACTIVE = true
            };

            var description = ReadColumn(row, "Description");
            if (description.Length > 0) item.DESCRIPTION = description;

            var sku = ReadColumn(row, "SKU");
            if (sku.Length > 0) item.SKU = sku;

            var category = ReadColumn(row, "Category");
            if (category.Length > 0) item.CATEGORY = category;

            return item;
        }

        /// <summary>
        /// Import members from a CSV file.
        /// Expected columns: FirstName, LastName, Email, Phone, CURP, RFC, Address, EmergencyContact, EmergencyPhone
        /// </summary>
        public BulkImportResult ImportMembersFromCsv(Stream fileStream, string? encoding = null)
        {
            var result = new BulkImportResult { TableName = "gym_members" };
            if (!TryResolveEncoding(encoding, out var enc, out var encodingError))
            {
                result.Message = encodingError;
                result.Success = false;
                return result;
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null
                };

                using var reader = new StreamReader(fileStream, enc);
                using var csv = new CsvReader(reader, config);

                csv.Read();
                csv.ReadHeader();

                var rows = new List<Dictionary<string, string>>();
                while (csv.Read())
                {
                    var row = new Dictionary<string, string>();
                    foreach (var header in csv.HeaderRecord)
                    {
                        row[header] = csv.GetField(header) ?? string.Empty;
                    }
                    rows.Add(row);
                }

                result.TotalRows = rows.Count;
                if (rows.Count == 0)
                {
                    result.Message = "El archivo CSV está vacío";
                    result.Success = false;
                    return result;
                }

                var tools = GetTools();
                if (tools == null)
                {
                    result.Message = "Servicio de base de datos no disponible";
                    result.Success = false;
                    return result;
                }

                var pending = new List<PendingImport<GYM_MEMBERS>>();
                var existingEmails = LoadExistingMemberEmails(tools);
                var fileEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    var rowNum = r + 2; // +2: 1-based + header row
                    var shouldFlush = false;
                    try
                    {
                        var member = BuildMember(row, rowNum, result, existingEmails, fileEmails);
                        if (member == null)
                            continue;

                        pending.Add(new PendingImport<GYM_MEMBERS>(
                            rowNum,
                            $"Nombre: {member.FIRST_NAME} {member.LAST_NAME}",
                            member));

                        shouldFlush = pending.Count >= BatchSize;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRows++;
                        result.Errors.Add(new BulkImportError
                        {
                            RowNumber = rowNum,
                            RowData = SerializeRow(row),
                            ErrorMessage = ex.Message
                        });
                        Logger.AddErrorDevExceptionSubModule(SubModule, $"Error en fila {rowNum}: {ex.Message}");
                    }

                    if (shouldFlush)
                        ExecuteBatch(pending, tools, result);
                }

                ExecuteBatch(pending, tools, result);

                result.Success = result.ErrorRows == 0;
                result.Message = $"Importación completada: {result.ImportedRows} importados, {result.ErrorRows} errores, {result.SkippedRows} omitidos";
                Logger.AddInfoSubModule(SubModule, result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error al leer el archivo CSV: {ex.Message}";
                Logger.AddErrorDevExceptionSubModule(SubModule, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Import members from an Excel (.xlsx) file.
        /// Expected columns in first row: FirstName, LastName, Email, Phone, CURP, RFC, Address, EmergencyContact, EmergencyPhone
        /// </summary>
        public BulkImportResult ImportMembersFromExcel(Stream fileStream)
        {
            var result = new BulkImportResult { TableName = "gym_members" };

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);
                var rowsUsed = worksheet.RowsUsed().ToList();

                if (rowsUsed.Count <= 1)
                {
                    result.Message = "El archivo Excel está vacío o solo tiene encabezados";
                    result.Success = false;
                    return result;
                }

                // Read headers from first row
                var headerRow = rowsUsed[0];
                var headers = new List<string>();
                foreach (var cell in headerRow.CellsUsed())
                {
                    headers.Add(cell.GetString().Trim());
                }

                result.TotalRows = rowsUsed.Count - 1; // Exclude header

                var tools = GetTools();
                if (tools == null)
                {
                    result.Message = "Servicio de base de datos no disponible";
                    result.Success = false;
                    return result;
                }

                var pending = new List<PendingImport<GYM_MEMBERS>>();
                var existingEmails = LoadExistingMemberEmails(tools);
                var fileEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Process data rows (skip header)
                for (int i = 1; i < rowsUsed.Count; i++)
                {
                    var rowNum = i + 1; // 1-based
                    var row = rowsUsed[i];
                    var cells = row.Cells().ToList();
                    var shouldFlush = false;

                    try
                    {
                        var rowDict = new Dictionary<string, string>();
                        for (int h = 0; h < headers.Count && h < cells.Count; h++)
                        {
                            rowDict[headers[h]] = cells[h].GetString().Trim();
                        }

                        var member = BuildMember(rowDict, rowNum, result, existingEmails, fileEmails);
                        if (member == null)
                            continue;

                        pending.Add(new PendingImport<GYM_MEMBERS>(
                            rowNum,
                            $"Nombre: {member.FIRST_NAME} {member.LAST_NAME}",
                            member));

                        shouldFlush = pending.Count >= BatchSize;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRows++;
                        result.Errors.Add(new BulkImportError
                        {
                            RowNumber = rowNum,
                            RowData = $"Fila {rowNum}",
                            ErrorMessage = ex.Message
                        });
                        Logger.AddErrorDevExceptionSubModule(SubModule, $"Error en fila {rowNum}: {ex.Message}");
                    }

                    if (shouldFlush)
                        ExecuteBatch(pending, tools, result);
                }

                ExecuteBatch(pending, tools, result);

                result.Success = result.ErrorRows == 0;
                result.Message = $"Importación completada: {result.ImportedRows} importados, {result.ErrorRows} errores, {result.SkippedRows} omitidos";
                Logger.AddInfoSubModule(SubModule, result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error al leer el archivo Excel: {ex.Message}";
                Logger.AddErrorDevExceptionSubModule(SubModule, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Import inventory items from a CSV file.
        /// Expected columns: ProductName, Description, SKU, Category, Price, Cost, StockQuantity, MinStock
        /// </summary>
        public BulkImportResult ImportInventoryFromCsv(Stream fileStream, string? encoding = null)
        {
            var result = new BulkImportResult { TableName = "gym_inventory" };
            if (!TryResolveEncoding(encoding, out var enc, out var encodingError))
            {
                result.Message = encodingError;
                result.Success = false;
                return result;
            }

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null
                };

                using var reader = new StreamReader(fileStream, enc);
                using var csv = new CsvReader(reader, config);

                csv.Read();
                csv.ReadHeader();

                var rows = new List<Dictionary<string, string>>();
                while (csv.Read())
                {
                    var row = new Dictionary<string, string>();
                    foreach (var header in csv.HeaderRecord)
                    {
                        row[header] = csv.GetField(header) ?? string.Empty;
                    }
                    rows.Add(row);
                }

                result.TotalRows = rows.Count;
                if (rows.Count == 0)
                {
                    result.Message = "El archivo CSV está vacío";
                    result.Success = false;
                    return result;
                }

                if (DataCenter.BusinessProcess.Inventory == null)
                {
                    result.Message = "Módulo de inventario no disponible";
                    result.Success = false;
                    return result;
                }

                var tools = GetTools();
                if (tools == null)
                {
                    result.Message = "Servicio de base de datos no disponible";
                    result.Success = false;
                    return result;
                }

                var pending = new List<PendingImport<GYM_INVENTORY>>();

                for (int r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    var rowNum = r + 2;
                    var shouldFlush = false;
                    try
                    {
                        var item = BuildInventoryItem(row, rowNum, SerializeRow(row), result);
                        if (item == null)
                            continue;

                        pending.Add(new PendingImport<GYM_INVENTORY>(
                            rowNum,
                            $"Producto: {item.PRODUCT_NAME}",
                            item));

                        shouldFlush = pending.Count >= BatchSize;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRows++;
                        result.Errors.Add(new BulkImportError
                        {
                            RowNumber = rowNum,
                            RowData = SerializeRow(row),
                            ErrorMessage = ex.Message
                        });
                        Logger.AddErrorDevExceptionSubModule(SubModule, $"Error en fila {rowNum}: {ex.Message}");
                    }

                    if (shouldFlush)
                        ExecuteBatch(pending, tools, result);
                }

                ExecuteBatch(pending, tools, result);

                result.Success = result.ErrorRows == 0;
                result.Message = $"Importación completada: {result.ImportedRows} importados, {result.ErrorRows} errores, {result.SkippedRows} omitidos";
                Logger.AddInfoSubModule(SubModule, result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error al leer el archivo CSV: {ex.Message}";
                Logger.AddErrorDevExceptionSubModule(SubModule, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Import inventory items from an Excel (.xlsx) file.
        /// Expected columns in first row: ProductName, Description, SKU, Category, Price, Cost, StockQuantity, MinStock
        /// </summary>
        public BulkImportResult ImportInventoryFromExcel(Stream fileStream)
        {
            var result = new BulkImportResult { TableName = "gym_inventory" };

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);
                var rowsUsed = worksheet.RowsUsed().ToList();

                if (rowsUsed.Count <= 1)
                {
                    result.Message = "El archivo Excel está vacío o solo tiene encabezados";
                    result.Success = false;
                    return result;
                }

                var headerRow = rowsUsed[0];
                var headers = new List<string>();
                foreach (var cell in headerRow.CellsUsed())
                {
                    headers.Add(cell.GetString().Trim());
                }

                result.TotalRows = rowsUsed.Count - 1;

                if (DataCenter.BusinessProcess.Inventory == null)
                {
                    result.Message = "Módulo de inventario no disponible";
                    result.Success = false;
                    return result;
                }

                var tools = GetTools();
                if (tools == null)
                {
                    result.Message = "Servicio de base de datos no disponible";
                    result.Success = false;
                    return result;
                }

                var pending = new List<PendingImport<GYM_INVENTORY>>();

                for (int i = 1; i < rowsUsed.Count; i++)
                {
                    var rowNum = i + 1;
                    var row = rowsUsed[i];
                    var cells = row.Cells().ToList();
                    var shouldFlush = false;

                    try
                    {
                        var rowDict = new Dictionary<string, string>();
                        for (int h = 0; h < headers.Count && h < cells.Count; h++)
                        {
                            rowDict[headers[h]] = cells[h].GetString().Trim();
                        }

                        var item = BuildInventoryItem(rowDict, rowNum, $"Fila {rowNum}", result);
                        if (item == null)
                            continue;

                        pending.Add(new PendingImport<GYM_INVENTORY>(
                            rowNum,
                            $"Producto: {item.PRODUCT_NAME}",
                            item));

                        shouldFlush = pending.Count >= BatchSize;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorRows++;
                        result.Errors.Add(new BulkImportError
                        {
                            RowNumber = rowNum,
                            RowData = $"Fila {rowNum}",
                            ErrorMessage = ex.Message
                        });
                        Logger.AddErrorDevExceptionSubModule(SubModule, $"Error en fila {rowNum}: {ex.Message}");
                    }

                    if (shouldFlush)
                        ExecuteBatch(pending, tools, result);
                }

                ExecuteBatch(pending, tools, result);

                result.Success = result.ErrorRows == 0;
                result.Message = $"Importación completada: {result.ImportedRows} importados, {result.ErrorRows} errores, {result.SkippedRows} omitidos";
                Logger.AddInfoSubModule(SubModule, result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error al leer el archivo Excel: {ex.Message}";
                Logger.AddErrorDevExceptionSubModule(SubModule, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Generate a sample CSV template for members import.
        /// </summary>
        public byte[] GenerateMembersCsvTemplate()
        {
            using var writer = new StringWriter();
            writer.WriteLine("FirstName,LastName,Email,Phone,CURP,RFC,Address,EmergencyContact,EmergencyPhone");
            writer.WriteLine("Juan,Perez,juan@example.com,5551234567,JUPE800101HDFNRL09,CUJO800101ABC,Calle 123 Col Centro,Juan Perez Sr,5559876543");
            writer.WriteLine("Maria,Garcia,maria@example.com,5552345678,MARG850202MDFRNS09,GARM850202XYZ,Av Reforma 456,Maria Garcia Mam,5558765432");
            return Encoding.UTF8.GetBytes(writer.ToString());
        }

        /// <summary>
        /// Generate a sample Excel template for members import.
        /// </summary>
        public byte[] GenerateMembersExcelTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Miembros");

            // Headers
            var headers = new[] { "FirstName", "LastName", "Email", "Phone", "CURP", "RFC", "Address", "EmergencyContact", "EmergencyPhone" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            // Sample data
            worksheet.Cell(2, 1).Value = "Juan";
            worksheet.Cell(2, 2).Value = "Perez";
            worksheet.Cell(2, 3).Value = "juan@example.com";
            worksheet.Cell(2, 4).Value = "5551234567";

            worksheet.Cell(3, 1).Value = "Maria";
            worksheet.Cell(3, 2).Value = "Garcia";
            worksheet.Cell(3, 3).Value = "maria@example.com";
            worksheet.Cell(3, 4).Value = "5552345678";

            // Style headers
            var range = worksheet.Range(1, 1, 1, headers.Length);
            range.Style.Fill.SetBackgroundColor(XLColor.LightBlue);
            range.Style.Font.Bold = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Generate a sample CSV template for inventory import.
        /// </summary>
        public byte[] GenerateInventoryCsvTemplate()
        {
            using var writer = new StringWriter();
            writer.WriteLine("ProductName,Description,SKU,Category,Price,Cost,StockQuantity,MinStock");
            writer.WriteLine("Proteína Whey,Proteína de suero 1kg,PRO-001,Suplementos,850.00,600.00,50,10");
            writer.WriteLine("Shaker 500ml,Vaso mezclador,ACC-001,Accesorios,120.00,60.00,100,20");
            return Encoding.UTF8.GetBytes(writer.ToString());
        }

        /// <summary>
        /// Generate a sample Excel template for inventory import.
        /// </summary>
        public byte[] GenerateInventoryExcelTemplate()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventario");

            var headers = new[] { "ProductName", "Description", "SKU", "Category", "Price", "Cost", "StockQuantity", "MinStock" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            worksheet.Cell(2, 1).Value = "Proteína Whey";
            worksheet.Cell(2, 2).Value = "Proteína de suero 1kg";
            worksheet.Cell(2, 3).Value = "PRO-001";
            worksheet.Cell(2, 4).Value = "Suplementos";
            worksheet.Cell(2, 5).Value = 850.00m;
            worksheet.Cell(2, 6).Value = 600.00m;
            worksheet.Cell(2, 7).Value = 50;
            worksheet.Cell(2, 8).Value = 10;

            var range = worksheet.Range(1, 1, 1, headers.Length);
            range.Style.Fill.SetBackgroundColor(XLColor.LightGreen);
            range.Style.Font.Bold = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private string SerializeRow(Dictionary<string, string> row)
        {
            try
            {
                return JsonSerializer.Serialize(row);
            }
            catch
            {
                return string.Join(", ", row.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            }
        }
    }
}
