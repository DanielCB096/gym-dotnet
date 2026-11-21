# GymService - Installation Script
# Run as Administrator
# This script installs GymService as a Windows Service with recovery options

param(
    [string]$ServiceName = "centrofitgymservice",
    [string]$ServiceDisplayName = "Centro Fit Gym Service",
    [string]$ExePath = "",
    [string]$StartType = "auto",
    [string]$ServiceAccount = "localsystem"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GymService - Windows Service Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Determine executable path
if ([string]::IsNullOrEmpty($ExePath)) {
    $ExePath = Join-Path $PSScriptRoot "GymService.exe"
}

# Check if executable exists
if (-not (Test-Path $ExePath)) {
    Write-Host "ERROR: GymService.exe not found at: $ExePath" -ForegroundColor Red
    Write-Host "Please build the project first or specify the path with -ExePath" -ForegroundColor Yellow
    exit 1
}

Write-Host "Executable: $ExePath" -ForegroundColor Green
Write-Host "Service Name: $ServiceName" -ForegroundColor Green
Write-Host "Display Name: $ServiceDisplayName" -ForegroundColor Green
Write-Host "Start Type: $StartType" -ForegroundColor Green
Write-Host "Account: $ServiceAccount" -ForegroundColor Green
Write-Host ""

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Service '$ServiceName' already exists." -ForegroundColor Yellow
    $choice = Read-Host "Do you want to reinstall? (y/n)"
    if ($choice -ne "y" -and $choice -ne "Y") {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Uninstalling existing service..." -ForegroundColor Yellow
    & $ExePath -uninstall
    Start-Sleep -Seconds 2
}

# Install the service using the built-in command
Write-Host ""
Write-Host "Installing service..." -ForegroundColor Cyan
try {
    & $ExePath -install
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service installed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Service installation completed (exit code: $LASTEXITCODE)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR during installation: $_" -ForegroundColor Red
    exit 1
}

# Wait for service to appear
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "ERROR: Service was not registered correctly." -ForegroundColor Red
    exit 1
}

# Configure startup type
Write-Host ""
Write-Host "Configuring startup type: $StartType" -ForegroundColor Cyan
try {
    $startTypeValue = switch ($StartType.ToLower()) {
        "auto" { "Automatic" }
        "delayed" { "Automatic" }
        "manual" { "Manual" }
        "disable" { "Disabled" }
        default { "Automatic" }
    }
    
    Set-Service -Name $ServiceName -StartupType $startTypeValue
    Write-Host "Startup type set to: $startTypeValue" -ForegroundColor Green
    
    if ($StartType.ToLower() -eq "delayed") {
        # Configure delayed start via registry
        $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
        Set-ItemProperty -Path $regPath -Name "Start" -Value 2 -Type DWord
        Set-ItemProperty -Path $regPath -Name "DelayedAutostart" -Value 1 -Type DWord
        Write-Host "Delayed start configured" -ForegroundColor Green
    }
} catch {
    Write-Host "WARNING: Could not set startup type: $_" -ForegroundColor Yellow
}

# Configure recovery options
Write-Host ""
Write-Host "Configuring recovery options..." -ForegroundColor Cyan
try {
    # On first failure: Restart (action 1), wait 60 seconds
    # On second failure: Restart (action 1), wait 120 seconds
    # On third failure: Restart (action 1), wait 180 seconds
    # Reset counter after: 1 day (86400 seconds)
    # Restart service on: 0 (no additional actions)
    sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/120000/restart/180000
    Write-Host "Recovery options configured:" -ForegroundColor Green
    Write-Host "  - 1st failure: Restart after 60 seconds" -ForegroundColor Green
    Write-Host "  - 2nd failure: Restart after 120 seconds" -ForegroundColor Green
    Write-Host "  - 3rd failure: Restart after 180 seconds" -ForegroundColor Green
    Write-Host "  - Reset counter: 1 day" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Could not configure recovery options: $_" -ForegroundColor Yellow
}

# Configure service description
Write-Host ""
Write-Host "Setting service description..." -ForegroundColor Cyan
try {
    sc.exe description $ServiceName "Centro Fit Gym Service - API REST para el sistema de gestión del gimnasio Centro Fit. Proporciona endpoints para gestión de socios, ventas, inventario, clases, y más."
    Write-Host "Description set" -ForegroundColor Green
} catch {
    Write-Host "WARNING: Could not set description: $_" -ForegroundColor Yellow
}

# Cuenta de servicio virtual y permisos mínimos.
$configPath = Join-Path (Split-Path -Parent $ExePath) 'appsettings.json'
$accountScript = Join-Path $PSScriptRoot 'Set-ServiceAccount.ps1'
if ((Test-Path $accountScript) -and (Test-Path $configPath)) {
    try {
        & $accountScript -ServiceName $ServiceName -ConfigPath $configPath -SkipRestart
    } catch {
        Write-Host "WARNING: no se pudo aplicar la cuenta virtual: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Start the service
Write-Host ""
Write-Host "Starting service..." -ForegroundColor Cyan
try {
    & $ExePath -start
    Start-Sleep -Seconds 3
    
    $serviceStatus = Get-Service -Name $ServiceName
    if ($serviceStatus.Status -eq "Running") {
        Write-Host "Service started successfully!" -ForegroundColor Green
    } else {
        Write-Host "Service status: $($serviceStatus.Status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "WARNING: Could not start service: $_" -ForegroundColor Yellow
    Write-Host "You can start it manually with: $ExePath -start" -ForegroundColor Yellow
}

# Verify appsettings.json exists
Write-Host ""
$appsettingsPath = Join-Path (Split-Path $ExePath) "appsettings.json"
if (Test-Path $appsettingsPath) {
    Write-Host "Configuration file found: $appsettingsPath" -ForegroundColor Green
} else {
    Write-Host "WARNING: appsettings.json not found at: $appsettingsPath" -ForegroundColor Yellow
    Write-Host "Please copy appsettings.json to the service directory before starting." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Name:    $ServiceName" -ForegroundColor White
Write-Host "Display Name:    $ServiceDisplayName" -ForegroundColor White
Write-Host "Executable:      $ExePath" -ForegroundColor White
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Cyan
Write-Host "  Start:         & '$ExePath' -start" -ForegroundColor White
Write-Host "  Stop:          & '$ExePath' -stop" -ForegroundColor White
Write-Host "  Uninstall:     & '$ExePath' -uninstall" -ForegroundColor White
Write-Host "  Console mode:  & '$ExePath' -console" -ForegroundColor White
Write-Host ""
Write-Host "Service Manager:  services.msc" -ForegroundColor White
Write-Host "Logs:             $(Split-Path $ExePath)\logs\" -ForegroundColor White
Write-Host ""
