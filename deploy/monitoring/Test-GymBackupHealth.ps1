# Comprueba la salud de los respaldos de GymService y alerta por correo si algo falla.
#
#   .\Test-GymBackupHealth.ps1
#   .\Test-GymBackupHealth.ps1 -DryRun
#   .\Test-GymBackupHealth.ps1 -VerifyRestore
#   .\Test-GymBackupHealth.ps1 -VerifyRestore -AlwaysNotify
#
# Revisa tareas Full/Log/Offsite, espacio en disco, OneDrive, antigüedad de los
# archivos offsite y la copia cifrada de la clave MFA. Con -VerifyRestore descifra
# el último respaldo completo y ejecuta verify-backup.ps1 en una base temporal.
#
# Ejecutar como SYSTEM/Administrador. No imprime ni registra contraseñas.

[CmdletBinding()]
param(
    [string]$ConfigPath = 'C:\Program Files\Centro Fit\GymService\appsettings.json',
    [string]$BackupDir = 'D:\GymBackups',
    [string]$OffsiteDir = 'C:\Users\PC\OneDrive\GymServiceBackups',
    [string]$PasswordFile = 'C:\ProgramData\GymService\secrets\offsite-backup.pass',
    [string]$VerifyBackupScript = 'C:\Program Files\Centro Fit\GymService\verify-backup.ps1',
    [string]$SevenZip = 'C:\Program Files\7-Zip\7z.exe',
    [string]$LogFile = 'C:\ProgramData\GymService\logs\backup-monitor.log',
    [string]$AlertTo = 'daniel.cb096@gmail.com',
    [int]$FullMaxAgeHours = 26,
    [int]$LogMaxAgeMinutes = 30,
    [int]$OffsiteMaxAgeMinutes = 90,
    [int]$MinFreeGB = 5,
    [switch]$VerifyRestore,
    [switch]$AlwaysNotify,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$issues = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Write-Log([string]$message) {
    $dir = Split-Path -Parent $LogFile
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $line = "{0} {1}" -f (Get-Date).ToString('yyyy-MM-dd HH:mm:ss'), $message
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

function Add-Issue([string]$message) {
    $issues.Add($message)
    Write-Log "ALERTA: $message"
}

function Add-Warning([string]$message) {
    $warnings.Add($message)
    Write-Log "AVISO: $message"
}

try {
    if (-not (Test-Path $ConfigPath)) { Add-Issue "No existe la configuración: $ConfigPath" }
    $cfg = $null
    if (Test-Path $ConfigPath) {
        $cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
    }

    # Tareas programadas.
    $taskChecks = @(
        @{ Name = 'GymService-Full'; MaxAge = [TimeSpan]::FromHours($FullMaxAgeHours); Label = 'completo' },
        @{ Name = 'GymService-Log'; MaxAge = [TimeSpan]::FromMinutes($LogMaxAgeMinutes); Label = 'log' },
        @{ Name = 'GymService-Offsite'; MaxAge = [TimeSpan]::FromMinutes($OffsiteMaxAgeMinutes); Label = 'offsite' }
    )

    foreach ($check in $taskChecks) {
        $info = Get-ScheduledTaskInfo -TaskName $check.Name -ErrorAction SilentlyContinue
        if (-not $info) {
            Add-Issue "No existe la tarea $($check.Name)"
            continue
        }

        if ($info.LastTaskResult -eq 267009) {
            Add-Warning "La tarea $($check.Name) está en ejecución"
            continue
        }

        if ($null -eq $info.LastTaskResult -or $info.LastTaskResult -ne 0) {
            Add-Issue "La tarea $($check.Name) terminó con resultado $($info.LastTaskResult)"
        }

        if (-not $info.LastRunTime -or $info.LastRunTime -eq [datetime]::MinValue) {
            Add-Issue "La tarea $($check.Name) nunca se ha ejecutado"
        } elseif ((Get-Date) - $info.LastRunTime -gt $check.MaxAge) {
            Add-Issue "La tarea $($check.Name) no se ejecuta desde $($info.LastRunTime.ToString('yyyy-MM-dd HH:mm'))"
        }
    }

    # Espacio local de respaldos.
    if (-not (Test-Path $BackupDir)) {
        Add-Issue "No existe la carpeta local de respaldos: $BackupDir"
    } else {
        $drive = Get-PSDrive -Name (Split-Path -Qualifier $BackupDir).TrimEnd(':') -ErrorAction SilentlyContinue
        if ($drive) {
            $freeGB = [math]::Round($drive.Free / 1GB, 1)
            if ($freeGB -lt $MinFreeGB) {
                Add-Issue "Quedan solo $freeGB GB libres en $($drive.Name): (mínimo $MinFreeGB GB)"
            }
        }
    }

    # OneDrive y archivos offsite.
    if (-not (Get-Process OneDrive -ErrorAction SilentlyContinue)) {
        Add-Issue 'OneDrive no está en ejecución'
    }

    if (-not (Test-Path $OffsiteDir)) {
        Add-Issue "No existe la carpeta offsite: $OffsiteDir"
    } else {
        $offsiteFiles = @(Get-ChildItem $OffsiteDir -File -Filter '*.7z' -ErrorAction SilentlyContinue)
        if ($offsiteFiles.Count -eq 0) {
            Add-Issue "No hay archivos .7z en $OffsiteDir"
        } else {
            $latest = $offsiteFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ((Get-Date) - $latest.LastWriteTime -gt [TimeSpan]::FromMinutes($OffsiteMaxAgeMinutes)) {
                Add-Issue "El último archivo offsite es de $($latest.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))"
            }
        }

        $keyFiles = @(Get-ChildItem $OffsiteDir -File -Filter 'mfa-key-*.7z' -ErrorAction SilentlyContinue)
        if ($keyFiles.Count -eq 0) {
            Add-Warning 'No hay copia cifrada de la clave MFA en offsite'
        } else {
            $latestKey = $keyFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ((Get-Date) - $latestKey.LastWriteTime -gt [TimeSpan]::FromDays(32)) {
                Add-Warning "La copia de la clave MFA tiene más de 32 días: $($latestKey.LastWriteTime.ToString('yyyy-MM-dd'))"
            }
        }
    }

    # Verificación real de restauración.
    if ($VerifyRestore) {
        $tempDir = Join-Path $env:TEMP ('GymRestoreCheck_' + [Guid]::NewGuid().ToString('N'))
        $restoreCopy = Join-Path $BackupDir ('_RestoreCheck_' + [Guid]::NewGuid().ToString('N') + '.bak')
        try {
            $full = Get-ChildItem $OffsiteDir -File -Filter '*.bak.7z' -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if (-not $full) {
                Add-Issue 'No hay respaldo completo .bak.7z para verificar'
            } elseif (-not (Test-Path $PasswordFile)) {
                Add-Issue "No existe la contraseña offsite: $PasswordFile"
            } elseif (-not (Test-Path $VerifyBackupScript)) {
                Add-Issue "No existe verify-backup.ps1: $VerifyBackupScript"
            } else {
                $password = (Get-Content $PasswordFile -Raw).Trim()
                New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
                & $SevenZip x "-p$password" "-o$tempDir" -y $full.FullName 2>&1 | Out-Null
                if ($LASTEXITCODE -ne 0) {
                    Add-Issue "No se pudo descifrar $($full.Name) (7z=$LASTEXITCODE)"
                } else {
                    $bak = Get-ChildItem $tempDir -File -Filter '*.bak' | Select-Object -First 1
                    if (-not $bak) {
                        Add-Issue "El archivo $($full.Name) no contiene un .bak"
                    } else {
                        Copy-Item $bak.FullName $restoreCopy -Force
                        $verifyOutput = & $VerifyBackupScript -ConfigPath $ConfigPath -BackupFile $restoreCopy 2>&1
                        if ($LASTEXITCODE -ne 0) {
                            Add-Issue "La restauración de verificación falló (exit=$LASTEXITCODE): $($verifyOutput | Select-Object -Last 1)"
                        } else {
                            Write-Log "Verificación de restauración correcta: $($full.Name)"
                        }
                    }
                }
            }
        } catch {
            Add-Issue "Error verificando restauración: $($_.Exception.Message)"
        } finally {
            if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue }
            if (Test-Path $restoreCopy) { Remove-Item $restoreCopy -Force -ErrorAction SilentlyContinue }
        }
    }

    $status = if ($issues.Count -eq 0) { 'OK' } else { 'ALERTA' }
    Write-Log "Resultado=$status issues=$($issues.Count) warnings=$($warnings.Count)"

    if ($issues.Count -gt 0 -or $warnings.Count -gt 0 -or $AlwaysNotify) {
        $bodyLines = @(
            "Estado de respaldos GymService: $status",
            "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
            ''
        )
        if ($issues.Count -gt 0) {
            $bodyLines += 'PROBLEMAS:'
            $issues | ForEach-Object { $bodyLines += "- $_" }
        }
        if ($warnings.Count -gt 0) {
            $bodyLines += ''
            $bodyLines += 'AVISOS:'
            $warnings | ForEach-Object { $bodyLines += "- $_" }
        }
        $body = $bodyLines -join "`r`n"
        $subject = if ($issues.Count -gt 0) { 'ALERTA: respaldos GymService' } else { 'Aviso: respaldos GymService' }

        if ($DryRun) {
            Write-Log "DRYRUN correo: $subject`n$body"
        } elseif ($cfg -and $cfg.Email -and $cfg.Email.SmtpServer -and $cfg.Email.SmtpUser -and $cfg.Email.SmtpPassword) {
            try {
                $secure = ConvertTo-SecureString $cfg.Email.SmtpPassword -AsPlainText -Force
                $credential = New-Object System.Management.Automation.PSCredential($cfg.Email.SmtpUser, $secure)
                $from = if ($cfg.Email.FromAddress) { $cfg.Email.FromAddress } else { $cfg.Email.SmtpUser }
                Send-MailMessage -SmtpServer $cfg.Email.SmtpServer -Port ([int]$cfg.Email.SmtpPort) -UseSsl `
                    -Credential $credential -From $from -To $AlertTo -Subject $subject -Body $body -Encoding UTF8
                Write-Log "Correo enviado a $AlertTo"
            } catch {
                Write-Log "ERROR enviando correo: $($_.Exception.Message)"
            }
        } else {
            Write-Log 'SMTP no configurado; no se pudo enviar la alerta'
        }
    }

    if ($issues.Count -gt 0) { exit 1 }
    exit 0
} catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    exit 1
}
