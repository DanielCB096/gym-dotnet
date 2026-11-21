# Verifica y repara las secuencias de GymCore (ANXO_SEQUENCES).
#
#   .\Sync-Sequences.ps1
#   .\Sync-Sequences.ps1 -Apply
#
# Por defecto solo informa. Con -Apply sube SEQUENCE_ID cuando la secuencia
# quedaría por debajo del max(ID) de su tabla, evitando violaciones de clave
# primaria después de limpiezas manuales de datos.
#
# Requiere sqlcmd (SQL Server Client Tools). La contraseña se pasa por variable
# de entorno para no mostrarla en la línea de comandos.

[CmdletBinding()]
param(
    [string]$ConfigPath = 'C:\Program Files\Centro Fit\GymService\appsettings.json',
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ConfigPath)) { throw "No se encontró la configuración: $ConfigPath" }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'No se encontró sqlcmd.' }

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$db = $config.Database
if (-not $db -or -not $db.Server) { throw 'La configuración no contiene Database:Server' }

$server = if ($db.Port) { "$($db.Server),$($db.Port)" } else { $db.Server }

$auditSql = @"
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#audit') IS NOT NULL DROP TABLE #audit;
CREATE TABLE #audit (seq nvarchar(200), tbl nvarchar(200), seq_id bigint, max_id bigint);
DECLARE @name nvarchar(200), @tbl nvarchar(200), @stmt nvarchar(max);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
  SELECT SEQUENCE_NAME, TABLE_NAME FROM ANXO_SEQUENCES WHERE ISNULL(TABLE_NAME,'') <> '';
OPEN cur;
FETCH NEXT FROM cur INTO @name, @tbl;
WHILE @@FETCH_STATUS = 0
BEGIN
  SET @stmt = N'INSERT INTO #audit SELECT N''' + REPLACE(@name,'''','''''') + N''', N''' + REPLACE(@tbl,'''','''''') +
    N''', (SELECT SEQUENCE_ID FROM ANXO_SEQUENCES WHERE SEQUENCE_NAME=N''' + REPLACE(@name,'''','''''') + N'''),
    (SELECT ISNULL(MAX(ID),0) FROM [' + REPLACE(@tbl,']',']]') + N'])';
  EXEC sp_executesql @stmt;
  FETCH NEXT FROM cur INTO @name, @tbl;
END
CLOSE cur; DEALLOCATE cur;
SELECT seq, tbl, seq_id, max_id, CASE WHEN seq_id <= max_id THEN 'BAD' ELSE 'OK' END AS state
FROM #audit ORDER BY CASE WHEN seq_id <= max_id THEN 0 ELSE 1 END, tbl;
"@

$repairSql = @"
SET NOCOUNT ON;
DECLARE @name nvarchar(200), @tbl nvarchar(200), @stmt nvarchar(max), @changed int = 0;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
  SELECT SEQUENCE_NAME, TABLE_NAME FROM ANXO_SEQUENCES WHERE ISNULL(TABLE_NAME,'') <> '';
OPEN cur;
FETCH NEXT FROM cur INTO @name, @tbl;
WHILE @@FETCH_STATUS = 0
BEGIN
  SET @stmt = N'UPDATE ANXO_SEQUENCES SET SEQUENCE_ID = CASE ' +
    N'WHEN (SELECT ISNULL(MAX(ID),0)+1 FROM [' + REPLACE(@tbl,']',']]') + N']) > SEQUENCE_ID ' +
    N'THEN (SELECT ISNULL(MAX(ID),0)+1 FROM [' + REPLACE(@tbl,']',']]') + N']) ELSE SEQUENCE_ID END ' +
    N'WHERE SEQUENCE_NAME=N''' + REPLACE(@name,'''','''''') + N''';';
  EXEC sp_executesql @stmt;
  SET @changed += @@ROWCOUNT;
  FETCH NEXT FROM cur INTO @name, @tbl;
END
CLOSE cur; DEALLOCATE cur;
SELECT CONCAT('secuencias actualizadas=', @changed) AS result;
"@

$env:SQLCMDPASSWORD = $db.Password
try {
    Write-Host "== Auditoría de secuencias ($($db.Database)) ==" -ForegroundColor Cyan
    sqlcmd -S $server -U $db.User -d $db.Database -C -b -W -s '|' -Q $auditSql
    if ($LASTEXITCODE -ne 0) { throw "La auditoría falló con código $LASTEXITCODE" }

    if ($Apply) {
        Write-Host ""
        Write-Host "== Reparando secuencias ==" -ForegroundColor Cyan
        sqlcmd -S $server -U $db.User -d $db.Database -C -b -W -Q $repairSql
        if ($LASTEXITCODE -ne 0) { throw "La reparación falló con código $LASTEXITCODE" }

        Write-Host ""
        Write-Host "== Estado final ==" -ForegroundColor Cyan
        sqlcmd -S $server -U $db.User -d $db.Database -C -b -W -s '|' -Q $auditSql
    } else {
        Write-Host ""
        Write-Host "Solo auditoría. Ejecuta con -Apply para corregir las secuencias BAD." -ForegroundColor Yellow
    }
} finally {
    Remove-Item Env:SQLCMDPASSWORD -ErrorAction SilentlyContinue
}
