using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using AxLogger;

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
        /// Import members from a CSV file.
        /// Expected columns: FirstName, LastName, Email, Phone, CURP, RFC, Address, EmergencyContact, EmergencyPhone
        /// </summary>
        public BulkImportResult ImportMembersFromCsv(Stream fileStream, string? encoding = null)
        {
            var result = new BulkImportResult { TableName = "gym_members" };
            var enc = string.IsNullOrEmpty(encoding) ? Encoding.UTF8 : Encoding.GetEncoding(encoding);

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

                var membersModule = DataCenter.BusinessProcess.Members;
                if (membersModule == null)
                {
                    result.Message = "Módulo de miembros no disponible";
                    result.Success = false;
                    return result;
                }

                foreach (var row in rows)
                {
                    var rowNum = rows.IndexOf(row) + 2; // +2: 1-based + header row
                    try
                    {
                        var memberData = new Dictionary<string, object>
                        {
                            ["firstName"] = row.GetValueOrDefault("FirstName", "").Trim(),
                            ["lastName"] = row.GetValueOrDefault("LastName", "").Trim()
                        };

                        // Add optional fields
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("Email")))
                            memberData["email"] = row["Email"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("Phone")))
                            memberData["phone"] = row["Phone"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("CURP")))
                            memberData["curp"] = row["CURP"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("RFC")))
                            memberData["rfc"] = row["RFC"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("Address")))
                            memberData["address"] = row["Address"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("EmergencyContact")))
                            memberData["emergencyContact"] = row["EmergencyContact"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("EmergencyPhone")))
                            memberData["emergencyPhone"] = row["EmergencyPhone"].Trim();

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(memberData["firstName"] as string) ||
                            string.IsNullOrWhiteSpace(memberData["lastName"] as string))
                        {
                            result.ErrorRows++;
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"FirstName='{memberData["firstName"]}', LastName='{memberData["lastName"]}'",
                                ErrorMessage = "Nombre y apellido son obligatorios"
                            });
                            continue;
                        }

                        var success = membersModule.CreateMember(memberData, out var response);
                        if (success)
                        {
                            result.ImportedRows++;
                        }
                        else
                        {
                            result.ErrorRows++;
                            var errorMsg = response.ContainsKey("message") ? response["message"].ToString() ?? "Error desconocido" : "Error desconocido";
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"Nombre: {memberData["firstName"]} {memberData["lastName"]}",
                                ErrorMessage = errorMsg
                            });
                        }
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
                }

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

                var membersModule = DataCenter.BusinessProcess.Members;
                if (membersModule == null)
                {
                    result.Message = "Módulo de miembros no disponible";
                    result.Success = false;
                    return result;
                }

                // Process data rows (skip header)
                for (int i = 1; i < rowsUsed.Count; i++)
                {
                    var rowNum = i + 1; // 1-based
                    var row = rowsUsed[i];
                    var cells = row.Cells().ToList();

                    try
                    {
                        var rowDict = new Dictionary<string, string>();
                        for (int h = 0; h < headers.Count && h < cells.Count; h++)
                        {
                            rowDict[headers[h]] = cells[h].GetString().Trim();
                        }

                        var memberData = new Dictionary<string, object>
                        {
                            ["firstName"] = rowDict.GetValueOrDefault("FirstName", "").Trim(),
                            ["lastName"] = rowDict.GetValueOrDefault("LastName", "").Trim()
                        };

                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("Email")))
                            memberData["email"] = rowDict["Email"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("Phone")))
                            memberData["phone"] = rowDict["Phone"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("CURP")))
                            memberData["curp"] = rowDict["CURP"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("RFC")))
                            memberData["rfc"] = rowDict["RFC"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("Address")))
                            memberData["address"] = rowDict["Address"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("EmergencyContact")))
                            memberData["emergencyContact"] = rowDict["EmergencyContact"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("EmergencyPhone")))
                            memberData["emergencyPhone"] = rowDict["EmergencyPhone"].Trim();

                        if (string.IsNullOrWhiteSpace(memberData["firstName"] as string) ||
                            string.IsNullOrWhiteSpace(memberData["lastName"] as string))
                        {
                            result.ErrorRows++;
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"FirstName='{memberData["firstName"]}', LastName='{memberData["lastName"]}'",
                                ErrorMessage = "Nombre y apellido son obligatorios"
                            });
                            continue;
                        }

                        var success = membersModule.CreateMember(memberData, out var response);
                        if (success)
                        {
                            result.ImportedRows++;
                        }
                        else
                        {
                            result.ErrorRows++;
                            var errorMsg = response.ContainsKey("message") ? response["message"].ToString() ?? "Error desconocido" : "Error desconocido";
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"Nombre: {memberData["firstName"]} {memberData["lastName"]}",
                                ErrorMessage = errorMsg
                            });
                        }
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
                }

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
            var enc = string.IsNullOrEmpty(encoding) ? Encoding.UTF8 : Encoding.GetEncoding(encoding);

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

                var inventoryModule = DataCenter.BusinessProcess.Inventory;
                if (inventoryModule == null)
                {
                    result.Message = "Módulo de inventario no disponible";
                    result.Success = false;
                    return result;
                }

                foreach (var row in rows)
                {
                    var rowNum = rows.IndexOf(row) + 2;
                    try
                    {
                        var productName = row.GetValueOrDefault("ProductName", "").Trim();
                        if (string.IsNullOrWhiteSpace(productName))
                        {
                            result.ErrorRows++;
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = SerializeRow(row),
                                ErrorMessage = "Nombre del producto es obligatorio"
                            });
                            continue;
                        }

                        decimal price = 0;
                        if (!decimal.TryParse(row.GetValueOrDefault("Price", "0"), out price))
                        {
                            result.ErrorRows++;
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"ProductName='{productName}', Price='{row.GetValueOrDefault("Price", "")}'",
                                ErrorMessage = "Precio no es un número válido"
                            });
                            continue;
                        }

                        int stockQty = 0;
                        int.TryParse(row.GetValueOrDefault("StockQuantity", "0"), out stockQty);

                        int minStock = 5;
                        int.TryParse(row.GetValueOrDefault("MinStock", "5"), out minStock);

                        decimal cost = 0;
                        decimal.TryParse(row.GetValueOrDefault("Cost", "0"), out cost);

                        var itemData = new Dictionary<string, object>
                        {
                            ["productName"] = productName,
                            ["price"] = price,
                            ["stock"] = stockQty,
                            ["minStock"] = minStock
                        };

                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("Description")))
                            itemData["description"] = row["Description"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("SKU")))
                            itemData["sku"] = row["SKU"].Trim();
                        if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("Category")))
                            itemData["category"] = row["Category"].Trim();
                        if (cost > 0)
                            itemData["cost"] = cost;

                        var success = inventoryModule.CreateProduct(itemData, out var response);
                        if (success)
                        {
                            result.ImportedRows++;
                        }
                        else
                        {
                            result.ErrorRows++;
                            var errorMsg = response.ContainsKey("message") ? response["message"].ToString() ?? "Error desconocido" : "Error desconocido";
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"Producto: {productName}",
                                ErrorMessage = errorMsg
                            });
                        }
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
                }

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

                var inventoryModule = DataCenter.BusinessProcess.Inventory;
                if (inventoryModule == null)
                {
                    result.Message = "Módulo de inventario no disponible";
                    result.Success = false;
                    return result;
                }

                for (int i = 1; i < rowsUsed.Count; i++)
                {
                    var rowNum = i + 1;
                    var row = rowsUsed[i];
                    var cells = row.Cells().ToList();

                    try
                    {
                        var rowDict = new Dictionary<string, string>();
                        for (int h = 0; h < headers.Count && h < cells.Count; h++)
                        {
                            rowDict[headers[h]] = cells[h].GetString().Trim();
                        }

                        var productName = rowDict.GetValueOrDefault("ProductName", "").Trim();
                        if (string.IsNullOrWhiteSpace(productName))
                        {
                            result.ErrorRows++;
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"Fila {rowNum}",
                                ErrorMessage = "Nombre del producto es obligatorio"
                            });
                            continue;
                        }

                        decimal price = 0;
                        if (!decimal.TryParse(rowDict.GetValueOrDefault("Price", "0"), out price))
                        {
                            result.ErrorRows++;
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"ProductName='{productName}', Price='{rowDict.GetValueOrDefault("Price", "")}'",
                                ErrorMessage = "Precio no es un número válido"
                            });
                            continue;
                        }

                        int stockQty = 0;
                        int.TryParse(rowDict.GetValueOrDefault("StockQuantity", "0"), out stockQty);

                        int minStock = 5;
                        int.TryParse(rowDict.GetValueOrDefault("MinStock", "5"), out minStock);

                        decimal cost = 0;
                        decimal.TryParse(rowDict.GetValueOrDefault("Cost", "0"), out cost);

                        var itemData = new Dictionary<string, object>
                        {
                            ["productName"] = productName,
                            ["price"] = price,
                            ["stock"] = stockQty,
                            ["minStock"] = minStock
                        };

                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("Description")))
                            itemData["description"] = rowDict["Description"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("SKU")))
                            itemData["sku"] = rowDict["SKU"].Trim();
                        if (!string.IsNullOrWhiteSpace(rowDict.GetValueOrDefault("Category")))
                            itemData["category"] = rowDict["Category"].Trim();
                        if (cost > 0)
                            itemData["cost"] = cost;

                        var success = inventoryModule.CreateProduct(itemData, out var response);
                        if (success)
                        {
                            result.ImportedRows++;
                        }
                        else
                        {
                            result.ErrorRows++;
                            var errorMsg = response.ContainsKey("message") ? response["message"].ToString() ?? "Error desconocido" : "Error desconocido";
                            result.Errors.Add(new BulkImportError
                            {
                                RowNumber = rowNum,
                                RowData = $"Producto: {productName}",
                                ErrorMessage = errorMsg
                            });
                        }
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
                }

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
