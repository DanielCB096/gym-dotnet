# Respaldo verificado de la base de datos de GymService (SQL Server).
#
#   .\backup.ps1
#   .\backup.ps1 -Database GymManager -BackupDir "D:\Backups" -RetentionDays 14
#
# El archivo .bak se escribe en el servidor SQL (no en este equipo). Copia después
# el archivo a un almacenamiento externo si el servidor no está respaldado aparte.

[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string]$Database,
    [string]$BackupDir,
    [string]$CopyTo,
    [int]$RetentionDays = 14,
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
    if (-not $BackupDir) {
        $command = Invoke-Command $connection "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))"
        $BackupDir = [string]$command.ExecuteScalar()
        if ([string]::IsNullOrWhiteSpace($BackupDir)) { throw "El servidor no reporta una carpeta de respaldo por defecto." }
    }

    $fileName = "{0}_{1}.bak" -f $Database, (Get-Date).ToString('yyyyMMdd_HHmmss')
    $backupFile = [System.IO.Path]::Combine($BackupDir, $fileName)
    $backupName = "GymService full backup $Database"

    Write-Host "Respaldando [$Database] en $backupFile ..."
    $started = Get-Date

    $command = Invoke-Command $connection @"
BACKUP DATABASE @db
TO DISK = @path
WITH INIT, CHECKSUM, COMPRESSION, NAME = @name
"@ @{ '@db' = $Database; '@path' = $backupFile; '@name' = $backupName }
$command.ExecuteNonQuery() | Out-Null

    $verified = $false
    if (-not $NoVerify) {
        Write-Host "Verificando el respaldo (RESTORE VERIFYONLY) ..."
        $command = Invoke-Command $connection "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM" @{ '@path' = $backupFile }
        $command.ExecuteNonQuery() | Out-Null
        $verified = $true
    }

    $command = Invoke-Command $connection "RESTORE HEADERONLY FROM DISK = @path" @{ '@path' = $backupFile }
    $reader = $command.ExecuteReader()
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
                $pattern = "{0}_*.bak" -f $Database
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
        Database  = $Database
        BackupFile = $backupFile
        SizeMB    = [math]::Round($sizeBytes / 1MB, 2)
        Verified  = $verified
        Seconds   = $seconds
        CopiedTo  = $copiedTo
        RemovedOldCopies = $removed
    } | Format-List
}
finally {
    $connection.Close()
}
