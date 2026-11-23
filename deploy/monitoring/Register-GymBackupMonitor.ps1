# Registra el monitoreo diario de respaldos y la verificación semanal de restauración.
#
#   .\Register-GymBackupMonitor.ps1
#   .\Register-GymBackupMonitor.ps1 -Unregister
#
# Requiere Administrador. Ejecuta como SYSTEM.
#   GymService-BackupMonitor   todos los días a las 08:00
#   GymService-RestoreVerify   domingos a las 03:00 (restaura el último .bak.7z)

[CmdletBinding()]
param(
    [string]$DailyAt = '08:00',
    [string]$VerifyAt = '03:00',
    [string]$ScriptPath = 'C:\Program Files\Centro Fit\GymService\Test-GymBackupHealth.ps1',
    [switch]$Unregister
)

$ErrorActionPreference = 'Stop'
$dailyTask = 'GymService-BackupMonitor'
$verifyTask = 'GymService-RestoreVerify'

if ($Unregister) {
    foreach ($name in @($dailyTask, $verifyTask)) {
        Unregister-ScheduledTask -TaskName $name -Confirm:$false -ErrorAction SilentlyContinue
        Write-Host "Tarea eliminada: $name"
    }
    return
}

if (-not (Test-Path $ScriptPath)) { throw "No se encontró el script: $ScriptPath" }

$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 3)

$dailyAction = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`""
$dailyTrigger = New-ScheduledTaskTrigger -Daily -At $DailyAt
Register-ScheduledTask -TaskName $dailyTask -Action $dailyAction -Trigger $dailyTrigger `
    -Settings $settings -Principal $principal `
    -Description 'Comprueba tareas de respaldo, OneDrive, disco y clave MFA; alerta por correo' -Force | Out-Null

$verifyAction = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`" -VerifyRestore"
$verifyTrigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At $VerifyAt
Register-ScheduledTask -TaskName $verifyTask -Action $verifyAction -Trigger $verifyTrigger `
    -Settings $settings -Principal $principal `
    -Description 'Descifra el último respaldo y verifica una restauración real semanal' -Force | Out-Null

Get-ScheduledTask -TaskName $dailyTask, $verifyTask |
    Select-Object TaskName, State |
    Format-Table -AutoSize
