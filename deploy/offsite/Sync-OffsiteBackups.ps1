# Copia los respaldos de GymService a OneDrive cifrados con AES-256.
#
#   .\Sync-OffsiteBackups.ps1
#   .\Sync-OffsiteBackups.ps1 -SourceDir "D:\GymBackups" -Destination "C:\Users\PC\OneDrive\GymServiceBackups"
#
# Los .bak/.trn se comprimen y cifran con 7-Zip (-mhe=on) antes de copiarlos.
# La contraseña se lee de un archivo protegido; nunca se escribe en el log.
# Si OneDrive no está en ejecución, la tarea falla para que se detecte.

[CmdletBinding()]
param(
    [string]$SourceDir = 'D:\GymBackups',
    [string]$Destination = 'C:\Users\PC\OneDrive\GymServiceBackups',
    [string]$PasswordFile = 'C:\ProgramData\GymService\secrets\offsite-backup.pass',
    [string]$SevenZip = 'C:\Program Files\7-Zip\7z.exe',
    [string]$LogFile = 'C:\ProgramData\GymService\logs\offsite-backup.log',
    [int]$FullRetentionDays = 30,
    [int]$LogRetentionDays = 7
)

$ErrorActionPreference = 'Stop'
$tempDir = 'C:\ProgramData\GymService\offsite-temp'

function Write-Log([string]$message) {
    $dir = Split-Path -Parent $LogFile
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $line = "{0} {1}" -f (Get-Date).ToString('yyyy-MM-dd HH:mm:ss'), $message
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

try {
    if (-not (Test-Path $SourceDir)) { throw "No existe la carpeta de respaldos: $SourceDir" }
    if (-not (Test-Path $SevenZip)) { throw "No se encontró 7-Zip: $SevenZip" }
    if (-not (Get-Process OneDrive -ErrorAction SilentlyContinue)) { throw 'OneDrive no está en ejecución.' }
    if (-not (Test-Path $PasswordFile)) { throw "No se encontró la contraseña: $PasswordFile" }

    $password = (Get-Content $PasswordFile -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($password)) { throw 'La contraseña de respaldo está vacía.' }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

    $files = @(Get-ChildItem $SourceDir -File | Where-Object { $_.Extension -in '.bak', '.trn' })
    if ($files.Count -eq 0) {
        Write-Log 'Sin respaldos nuevos en el origen.'
        exit 0
    }

    $errors = 0
    $copied = 0
    $skipped = 0

    foreach ($file in $files) {
        $archive = Join-Path $Destination ($file.Name + '.7z')
        if ((Test-Path $archive) -and ((Get-Item $archive).LastWriteTime -ge $file.LastWriteTime)) {
            $skipped++
            continue
        }

        $temp = Join-Path $tempDir ('tmp-' + [Guid]::NewGuid().ToString('N') + '.7z')
        Push-Location $SourceDir
        try {
            & $SevenZip a -t7z -mhe=on -mx=5 -y "-p$password" $temp $file.Name 2>&1 | Out-Null
            $code = $LASTEXITCODE
        } finally {
            Pop-Location
        }

        if ($code -ne 0 -or -not (Test-Path $temp)) {
            if (Test-Path $temp) { Remove-Item $temp -Force -ErrorAction SilentlyContinue }
            Write-Log "ERROR cifrando $($file.Name) (7z=$code)"
            $errors++
            continue
        }

        Move-Item -LiteralPath $temp -Destination $archive -Force
        $copied++
        Write-Log "OK $($file.Name) -> $archive"
    }

    $now = Get-Date
    $removed = 0
    Get-ChildItem $Destination -File -Filter '*.bak.7z' |
        Where-Object { $_.LastWriteTime -lt $now.AddDays(-$FullRetentionDays) } |
        ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }
    Get-ChildItem $Destination -File -Filter '*.trn.7z' |
        Where-Object { $_.LastWriteTime -lt $now.AddDays(-$LogRetentionDays) } |
        ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }

    Write-Log "Resumen: copiados=$copied omitidos=$skipped eliminados=$removed errores=$errors"
    if ($errors -gt 0) { exit 1 }
    exit 0
} catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
