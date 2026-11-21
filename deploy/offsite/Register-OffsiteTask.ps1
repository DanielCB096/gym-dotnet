# Registra la tarea GymService-Offsite (cada 15 minutos, como SYSTEM).
#
#   .\Register-OffsiteTask.ps1
#   .\Register-OffsiteTask.ps1 -Unregister
#
# Requiere Administrador. La tarea ejecuta el script instalado junto al servicio.

[CmdletBinding()]
param(
    [switch]$Unregister
)

$ErrorActionPreference = 'Stop'
$taskName = 'GymService-Offsite'
$scriptPath = 'C:\Program Files\Centro Fit\GymService\Sync-OffsiteBackups.ps1'

if ($Unregister) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host "Tarea eliminada: $taskName"
    return
}

if (-not (Test-Path $scriptPath)) { throw "No se encontró el script: $scriptPath" }

$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""
$trigger = New-ScheduledTaskTrigger -Once -At ((Get-Date).AddMinutes(2)) `
    -RepetitionInterval (New-TimeSpan -Minutes 15) `
    -RepetitionDuration (New-TimeSpan -Days 3650)
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 1)

Register-ScheduledTask -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Description 'Copia cifrada de respaldos GymService a OneDrive (7-Zip AES-256)' `
    -Force | Out-Null

Get-ScheduledTask -TaskName $taskName |
    Select-Object TaskName, State |
    Format-Table -AutoSize
