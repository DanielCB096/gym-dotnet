# Respaldo del log de transacciones de GymService (SQL Server).
# Requiere que la base esté en modelo FULL y que exista un respaldo completo previo.
#
#   .\backup-log.ps1
#   .\backup-log.ps1 -Database GymManager -BackupDir "D:\SQLBackups" -RetentionDays 3

[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string]$Database,
    [string]$BackupDir,
    [string]$CopyTo,
    [int]$RetentionDays = 3,
    [switch]$NoVerify
)

$ErrorActionPreference = 'Stop'

if (-not $ConfigPath) { $ConfigPath = Join-Path $PSScriptRoot 'GymService\appsettings.json' }
if (-not (Test-Path $ConfigPath)) { throw "No se encontró la configuración: $ConfigPath" }

$cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$server = $cfg.Database.Server
$port = $cfg.Database.Port
$user = $cfg.Database.User
$password = $cfg.Database.Password
if (-not $Database) { $Database = $cfg.Database.Database }

function New-MasterConnection {
    $cs = "Server=$server,$port;Initial Catalog=master;User Id=$user;Password=$password;" +
          "TrustServerCertificate=True;Encrypt=False;Connect Timeout=30"
    $connection = New-Object System.Data.SqlClient.SqlConnection $cs
    $connection.Open()
    return $connection
}

function Invoke-Command($connection, [string]$sql, [hashtable]$parameters = @{}) {
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
    $modelCommand = Invoke-Command $connection "SELECT recovery_model_desc FROM sys.databases WHERE name = @db" @{ '@db' = $Database }
    $model = $modelCommand.ExecuteScalar()

    if ($null -eq $model -or $model -is [System.DBNull]) {
        throw "La base [$Database] no existe."
    }

    if ($model -ne 'FULL') {
        throw "La base [$Database] está en modelo $model. Para respaldos de log:" +
              " ejecuta ALTER DATABASE [$Database] SET RECOVERY FULL, luego .\backup.ps1 y vuelve a intentar."
    }

    $fullCommand = Invoke-Command $connection @"
SELECT TOP 1 1 FROM msdb.dbo.backupset
WHERE database_name = @db AND type = 'D'
"@ @{ '@db' = $Database }
    $hasFull = $fullCommand.ExecuteScalar()

    if ($null -eq $hasFull) {
        throw "La base [$Database] no tiene un respaldo completo registrado. Ejecuta primero .\backup.ps1."
    }

    if (-not $BackupDir) {
        $pathCommand = Invoke-Command $connection "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"
        $BackupDir = [string]$pathCommand.ExecuteScalar()
        if ([string]::IsNullOrWhiteSpace($BackupDir)) { throw "El servidor no reporta una carpeta de respaldo por defecto." }
    }

    $fileName = "{0}_log_{1}.trn" -f $Database, (Get-Date).ToString('yyyyMMdd_HHmmss')
    $backupFile = [System.IO.Path]::Combine($BackupDir, $fileName)
    $backupName = "GymService log backup $Database"

    Write-Host "Respaldando el log de [$Database] en $backupFile ..."
    $started = Get-Date

    $backupCommand = Invoke-Command $connection @"
BACKUP LOG @db
TO DISK = @path
WITH INIT, CHECKSUM, COMPRESSION, NAME = @name
"@ @{ '@db' = $Database; '@path' = $backupFile; '@name' = $backupName }
    $backupCommand.ExecuteNonQuery() | Out-Null

    $verified = $false
    if (-not $NoVerify) {
        Write-Host "Verificando el respaldo de log ..."
        $verifyCommand = Invoke-Command $connection "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM" @{ '@path' = $backupFile }
        $verifyCommand.ExecuteNonQuery() | Out-Null
        $verified = $true
    }

    $headerCommand = Invoke-Command $connection "RESTORE HEADERONLY FROM DISK = @path" @{ '@path' = $backupFile }
    $reader = $headerCommand.ExecuteReader()
    $sizeBytes = 0
    if ($reader.Read()) {
        $sizeBytes = [int64]$reader['CompressedBackupSize']
        if ($sizeBytes -le 0) { $sizeBytes = [int64]$reader['BackupSize'] }
    }
    $reader.Close()

    $removed = 0
    $copiedTo = $null
    if ($CopyTo) {
        if (-not (Test-Path $CopyTo)) {
            New-Item -ItemType Directory -Force -Path $CopyTo | Out-Null
        }

        try {
            $copiedTo = Join-Path $CopyTo $fileName
            Copy-Item -LiteralPath $backupFile -Destination $copiedTo -Force

            if ($RetentionDays -gt 0) {
                $cutoff = (Get-Date).AddDays(-$RetentionDays)
                $pattern = "{0}_log_*.trn" -f $Database
                Get-ChildItem -Path $CopyTo -Filter $pattern -File |
                    Where-Object { $_.LastWriteTime -lt $cutoff } |
                    ForEach-Object {
                        Remove-Item -LiteralPath $_.FullName -Force
                        $removed++
                    }
            }
        }
        catch {
            Write-Warning "No se pudo copiar el respaldo a $CopyTo (¿es accesible desde este equipo?): $($_.Exception.Message)"
            $copiedTo = $null
        }
    }

    $seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
    [pscustomobject]@{
        Database   = $Database
        BackupFile = $backupFile
        SizeMB     = [math]::Round($sizeBytes / 1MB, 2)
        Verified   = $verified
        Seconds    = $seconds
        CopiedTo   = $copiedTo
        RemovedOldCopies = $removed
    } | Format-List
}
finally {
    $connection.Close()
}
