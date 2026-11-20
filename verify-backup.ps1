# Restaura un respaldo de GymService en una base temporal y comprueba su contenido.
# No sobrescribe la base de producción: si el destino ya existe, exige -Force.
#
#   .\verify-backup.ps1 -BackupFile "C:\...\GymManager_20261120_170000.bak"
#   .\verify-backup.ps1 -BackupFile "..." -TargetDatabase GymService_Ensayo -KeepRestored

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BackupFile,
    [string]$ConfigPath,
    [string]$TargetDatabase,
    [switch]$KeepRestored,
    [switch]$Force,
    [switch]$CheckDb
)

$ErrorActionPreference = 'Stop'

if (-not $ConfigPath) { $ConfigPath = Join-Path $PSScriptRoot 'GymService\appsettings.json' }
if (-not (Test-Path $ConfigPath)) { throw "No se encontró la configuración: $ConfigPath" }

$cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$server = $cfg.Database.Server
$port = $cfg.Database.Port
$user = $cfg.Database.User
$password = $cfg.Database.Password
$sourceDatabase = $cfg.Database.Database

if (-not $TargetDatabase) {
    $TargetDatabase = "GymService_Verify_" + (Get-Date).ToString('yyyyMMdd_HHmmss')
}

if ($TargetDatabase -eq $sourceDatabase) {
    throw "El destino no puede ser la base de producción ($sourceDatabase). Usa otro nombre."
}

function Quote-Name([string]$name) { return "[" + $name.Replace(']', ']]') + "]" }
function Quote-Literal([string]$value) { return "N'" + $value.Replace("'", "''") + "'" }

function New-MasterConnection {
    $cs = "Server=$server,$port;Initial Catalog=master;User Id=$user;Password=$password;" +
          "TrustServerCertificate=True;Encrypt=False;Connect Timeout=30"
    $connection = New-Object System.Data.SqlClient.SqlConnection $cs
    $connection.Open()
    return $connection
}

function Invoke-Sql($connection, [string]$sql, [hashtable]$parameters = @{}) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 0
    foreach ($key in $parameters.Keys) {
        [void]$command.Parameters.AddWithValue($key, $parameters[$key])
    }
    return $command
}

$connection = New-MasterConnection
try {
    $existsCommand = Invoke-Sql $connection "SELECT DB_ID(@name)" @{ '@name' = $TargetDatabase }
    $existsValue = $existsCommand.ExecuteScalar()
    $targetExists = ($null -ne $existsValue) -and ($existsValue -isnot [System.DBNull])

    if ($targetExists -and -not $Force) {
        throw "La base [$TargetDatabase] ya existe. Usa -Force para reemplazarla o elige otro nombre."
    }

    if ($targetExists) {
        Write-Host "Eliminando la base existente [$TargetDatabase] ..."
        $dropSql = @"
IF DB_ID(N'$($TargetDatabase.Replace("'", "''"))') IS NOT NULL
BEGIN
    ALTER DATABASE $(Quote-Name $TargetDatabase) SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE $(Quote-Name $TargetDatabase);
END
"@
        (Invoke-Sql $connection $dropSql).ExecuteNonQuery() | Out-Null
    }

    Write-Host "Leyendo la estructura del respaldo ..."
    $listCommand = Invoke-Sql $connection "RESTORE FILELISTONLY FROM DISK = @path" @{ '@path' = $BackupFile }
    $reader = $listCommand.ExecuteReader()

    $files = @()
    while ($reader.Read()) {
        $files += [pscustomobject]@{
            LogicalName  = [string]$reader['LogicalName']
            PhysicalName = [string]$reader['PhysicalName']
            Type         = [string]$reader['Type']  # D = datos, L = log
        }
    }
    $reader.Close()

    if ($files.Count -eq 0) { throw "El respaldo no contiene archivos de base de datos." }

    $dataPathCommand = Invoke-Sql $connection "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000))"
    $dataPath = [string]$dataPathCommand.ExecuteScalar()
    $logPathCommand = Invoke-Sql $connection "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(4000))"
    $logPath = [string]$logPathCommand.ExecuteScalar()

    $moves = @()
    $index = 1
    foreach ($file in $files) {
        $extension = [System.IO.Path]::GetExtension($file.PhysicalName)
        if ($file.Type -eq 'L') {
            if (-not $extension) { $extension = '.ldf' }
            $targetPath = [System.IO.Path]::Combine($logPath, "$TargetDatabase`_$index$extension")
        }
        else {
            if (-not $extension) { $extension = '.mdf' }
            $targetPath = [System.IO.Path]::Combine($dataPath, "$TargetDatabase`_$index$extension")
        }
        $moves += "MOVE $(Quote-Literal $file.LogicalName) TO $(Quote-Literal $targetPath)"
        $index++
    }

    $restoreSql = "RESTORE DATABASE $(Quote-Name $TargetDatabase) FROM DISK = $(Quote-Literal $BackupFile) WITH " +
                  ($moves -join ', ') + ", RECOVERY, STATS = 10"

    Write-Host "Restaurando en [$TargetDatabase] ..."
    $started = Get-Date
    (Invoke-Sql $connection $restoreSql).ExecuteNonQuery() | Out-Null
    $seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)

    $targetCs = "Server=$server,$port;Initial Catalog=$TargetDatabase;User Id=$user;Password=$password;" +
                "TrustServerCertificate=True;Encrypt=False;Connect Timeout=30"
    $target = New-Object System.Data.SqlClient.SqlConnection $targetCs
    $target.Open()
    try {
        $tableCount = [int](Invoke-Sql $target "SELECT COUNT(*) FROM sys.tables").ExecuteScalar()

        $hasMembers = [int](Invoke-Sql $target "SELECT CASE WHEN OBJECT_ID('GYM_MEMBERS') IS NULL THEN 0 ELSE 1 END").ExecuteScalar()
        $members = $null
        if ($hasMembers -eq 1) {
            $members = (Invoke-Sql $target "SELECT COUNT(*) FROM GYM_MEMBERS").ExecuteScalar()
        }

        if ($CheckDb) {
            Write-Host "Ejecutando DBCC CHECKDB (puede tardar) ..."
            (Invoke-Sql $target "DBCC CHECKDB WITH NO_INFOMSGS").ExecuteNonQuery() | Out-Null
        }

        [pscustomobject]@{
            SourceDatabase = $sourceDatabase
            TargetDatabase = $TargetDatabase
            Tables         = $tableCount
            Members        = if ($null -eq $members) { 'n/a' } else { $members }
            Seconds        = $seconds
            Kept           = [bool]$KeepRestored
        } | Format-List
    }
    finally {
        $target.Close()
    }

    if (-not $KeepRestored) {
        Write-Host "Eliminando la base temporal [$TargetDatabase] ..."
        $dropSql = @"
IF DB_ID(N'$($TargetDatabase.Replace("'", "''"))') IS NOT NULL
BEGIN
    ALTER DATABASE $(Quote-Name $TargetDatabase) SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE $(Quote-Name $TargetDatabase);
END
"@
        (Invoke-Sql $connection $dropSql).ExecuteNonQuery() | Out-Null
    }
}
finally {
    $connection.Close()
}
