# Registra (o elimina) las tareas programadas de respaldo de GymService.
#
#   .\register-backup-tasks.ps1
#   .\register-backup-tasks.ps1 -FullAt "03:30" -LogIntervalMinutes 15
#   .\register-backup-tasks.ps1 -RunAsSystem     # requiere Administrador
#   .\register-backup-tasks.ps1 -Unregister
#
# Sin -RunAsSystem las tareas corren con el usuario actual (solo con sesión iniciada);
# para un servidor usa -RunAsSystem desde una consola de Administrador.

[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string]$Database,
    [string]$BackupDir,
    [string]$CopyTo,
    [string]$FullAt = "02:00",
    [int]$LogIntervalMinutes = 15,
    [int]$FullRetentionDays = 14,
    [int]$LogRetentionDays = 3,
    [string]$TaskPrefix = "GymService",
    [switch]$RunAsSystem,
    [switch]$Unregister
)

$ErrorActionPreference = 'Stop'

if (-not $ConfigPath) { $ConfigPath = Join-Path $PSScriptRoot 'GymService\appsettings.json' }
if (-not (Test-Path $ConfigPath)) { throw "No se encontró la configuración: $ConfigPath" }

$cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
if (-not $Database) { $Database = $cfg.Database.Database }

$fullTask = "$TaskPrefix-Full"
$logTask = "$TaskPrefix-Log"

if ($Unregister) {
    foreach ($name in @($fullTask, $logTask)) {
        if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
            Unregister-ScheduledTask -TaskName $name -Confirm:$false
            Write-Host "Tarea eliminada: $name"
        }
        else {
            Write-Host "No existe la tarea: $name"
        }
    }
    return
}

if ($RunAsSystem) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principalCheck = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principalCheck.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Para -RunAsSystem ejecuta este script desde una consola de Administrador."
    }
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
}
else {
    $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Limited
}

$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2)

# El respaldo completo diario puede despertar el equipo. Los de log/offsite no,
# para no despertarlo cada 15 minutos.
$fullSettings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
    -WakeToRun

function Quote-Argument([string]$value) {
    if ($value -match '\s') { return '"' + $value + '"' }
    return $value
}

function New-BackupAction([string]$scriptName, [string[]]$extraArguments) {
    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $PSScriptRoot $scriptName),
        "-ConfigPath", $ConfigPath,
        "-Database", $Database
    )

    if ($BackupDir) { $arguments += @("-BackupDir", $BackupDir) }
    if ($CopyTo) { $arguments += @("-CopyTo", $CopyTo) }
    $arguments += $extraArguments

    $argumentLine = ($arguments | ForEach-Object { Quote-Argument $_ }) -join ' '
    return New-ScheduledTaskAction -Execute "powershell.exe" -Argument $argumentLine
}

$fullAction = New-BackupAction "backup.ps1" @("-RetentionDays", "$FullRetentionDays")
$logAction = New-BackupAction "backup-log.ps1" @("-RetentionDays", "$LogRetentionDays")

$fullTrigger = New-ScheduledTaskTrigger -Daily -At $FullAt
$logTrigger = New-ScheduledTaskTrigger -Once -At ((Get-Date).AddMinutes(1)) `
    -RepetitionInterval (New-TimeSpan -Minutes $LogIntervalMinutes) `
    -RepetitionDuration (New-TimeSpan -Days 3650)

Register-ScheduledTask -TaskName $fullTask `
    -Action $fullAction `
    -Trigger $fullTrigger `
    -Settings $fullSettings `
    -Principal $principal `
    -Description "Respaldo completo de $Database (GymService)" `
    -Force | Out-Null

Register-ScheduledTask -TaskName $logTask `
    -Action $logAction `
    -Trigger $logTrigger `
    -Settings $settings `
    -Principal $principal `
    -Description "Respaldo de log de $Database cada $LogIntervalMinutes minutos (GymService)" `
    -Force | Out-Null

Get-ScheduledTask -TaskName $fullTask, $logTask |
    Select-Object TaskName, State |
    Format-Table -AutoSize
