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
    [string]$ConfigPath = 'C:\Program Files\Centro Fit\GymService\appsettings.json',
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

    $errors = 0
    $copied = 0
    $skipped = 0
    $keyArchived = 0
    $now = Get-Date

    # Respaldo cifrado de la clave MFA. Sin esta clave, una base restaurada en otro
    # servidor no puede validar TOTP ni los códigos de respaldo.
    if (Test-Path $ConfigPath) {
        try {
            $cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
            $mfaKey = [string]$cfg.Authentication.MfaEncryptionKey

            if ([string]::IsNullOrWhiteSpace($mfaKey)) {
                Write-Log 'WARN: appsettings no contiene MfaEncryptionKey; no se respaldó la clave MFA.'
            } else {
                $sha = [System.Security.Cryptography.SHA256]::Create()
                try {
                    $hashBytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($mfaKey))
                    $keyHash = ([BitConverter]::ToString($hashBytes)).Replace('-', '').Substring(0, 12).ToLowerInvariant()
                } finally {
                    $sha.Dispose()
                }

                $keyArchive = Join-Path $Destination ("mfa-key-" + $now.ToString('yyyyMMdd') + "-$keyHash.7z")
                if (-not (Test-Path $keyArchive)) {
                    $keyFile = Join-Path $tempDir ('tmp-mfa-' + [Guid]::NewGuid().ToString('N') + '.txt')
                    $keyTemp = Join-Path $tempDir ('tmp-mfa-' + [Guid]::NewGuid().ToString('N') + '.7z')
                    Set-Content -Path $keyFile -Value $mfaKey -NoNewline -Encoding UTF8

                    Push-Location $tempDir
                    try {
                        & $SevenZip a -t7z -mhe=on -mx=5 -y "-p$password" $keyTemp (Split-Path $keyFile -Leaf) 2>&1 | Out-Null
                        $keyCode = $LASTEXITCODE
                    } finally {
                        Pop-Location
                    }

                    if ($keyCode -ne 0 -or -not (Test-Path $keyTemp)) {
                        if (Test-Path $keyTemp) { Remove-Item $keyTemp -Force -ErrorAction SilentlyContinue }
                        Write-Log "ERROR cifrando la clave MFA (7z=$keyCode)"
                        $errors++
                    } else {
                        Move-Item -LiteralPath $keyTemp -Destination $keyArchive -Force
                        icacls $keyArchive /reset /C | Out-Null
                        if ($LASTEXITCODE -ne 0) {
                            Write-Log "ERROR ajustando permisos de $keyArchive (icacls=$LASTEXITCODE)"
                            Remove-Item $keyArchive -Force -ErrorAction SilentlyContinue
                            $errors++
                        } else {
                            $keyArchived = 1
                            Write-Log "OK clave MFA -> $keyArchive"
                        }
                    }

                    Remove-Item $keyFile -Force -ErrorAction SilentlyContinue
                }
            }
        } catch {
            Write-Log "WARN: no se pudo respaldar la clave MFA: $($_.Exception.Message)"
            Get-ChildItem $tempDir -File -Filter 'tmp-mfa-*.txt' -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
        }
    } else {
        Write-Log "WARN: no existe $ConfigPath; no se respaldó la clave MFA."
    }

    $files = @(Get-ChildItem $SourceDir -File | Where-Object { $_.Extension -in '.bak', '.trn' })

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

        # El temporal vive en ProgramData y al moverlo conserva los ACL del origen.
        # OneDrive (el usuario) necesita heredar de la carpeta destino para poder
        # sincronizar el archivo; sin esto queda en "Sincronización pendiente".
        icacls $archive /reset /C | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Log "ERROR ajustando permisos de $archive (icacls=$LASTEXITCODE)"
            Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
            $errors++
            continue
        }

        $copied++
        Write-Log "OK $($file.Name) -> $archive"
    }

    $removed = 0
    Get-ChildItem $Destination -File -Filter '*.bak.7z' |
        Where-Object { $_.LastWriteTime -lt $now.AddDays(-$FullRetentionDays) } |
        ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }
    Get-ChildItem $Destination -File -Filter '*.trn.7z' |
        Where-Object { $_.LastWriteTime -lt $now.AddDays(-$LogRetentionDays) } |
        ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }
    Get-ChildItem $Destination -File -Filter 'mfa-key-*.7z' |
        Where-Object { $_.LastWriteTime -lt $now.AddDays(-$FullRetentionDays) } |
        ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }

    Write-Log "Resumen: copiados=$copied omitidos=$skipped claves=$keyArchived eliminados=$removed errores=$errors"
    if ($errors -gt 0) { exit 1 }
    exit 0
} catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
