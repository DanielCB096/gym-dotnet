param(
    [string]$InstallDir = "$env:ProgramFiles\Centro Fit\GymService",
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'
$serviceName = "centrofitgymservice"

function Assert-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Ejecuta este desinstalador como Administrador."
    }
}

Assert-Admin

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " GymService - Desinstalación" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$exe = Join-Path $InstallDir 'GymService.exe'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if (-not $service -and -not (Test-Path $exe)) {
    Write-Host "El servicio no está instalado." -ForegroundColor Yellow
    exit 0
}

if ($service) {
    Write-Host "Deteniendo servicio..." -ForegroundColor Cyan
    if (Test-Path $exe) {
        try { & $exe -stop | Out-Null } catch { }
    }
    Start-Sleep -Seconds 2
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue

    Write-Host "Desregistrando servicio..." -ForegroundColor Cyan
    if (Test-Path $exe) {
        try { & $exe -uninstall | Out-Null } catch { }
    }
    Start-Sleep -Seconds 2

    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        sc.exe delete $serviceName | Out-Null
    }
}

if ($RemoveData) {
    if (Test-Path $InstallDir) {
        Write-Host "Eliminando archivos, configuración y logs..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force $InstallDir
    }
} else {
    Write-Host "Se conservan la configuración y los logs en: $InstallDir" -ForegroundColor Yellow
    Write-Host "Usa -RemoveData para borrarlos también." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Desinstalación completada." -ForegroundColor Green
