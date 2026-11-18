# GymService - Uninstallation Script
# Run as Administrator
# This script stops and uninstalls the GymService Windows Service

param(
    [string]$ServiceName = "anxogymmservice",
    [string]$ExePath = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "GymService - Windows Service Uninstallation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Determine executable path
if ([string]::IsNullOrEmpty($ExePath)) {
    $ExePath = Join-Path $PSScriptRoot "GymService.exe"
}

# Check if executable exists
if (-not (Test-Path $ExePath)) {
    Write-Host "ERROR: GymService.exe not found at: $ExePath" -ForegroundColor Red
    exit 1
}

# Check if service exists
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
    exit 0
}

# Show current status
Write-Host "Service Name:    $ServiceName" -ForegroundColor Green
Write-Host "Display Name:    $($service.DisplayName)" -ForegroundColor Green
Write-Host "Status:          $($service.Status)" -ForegroundColor Green
Write-Host "Startup Type:    $($service.StartType)" -ForegroundColor Green
Write-Host ""

# Confirm uninstallation
$choice = Read-Host "Are you sure you want to uninstall this service? (y/n)"
if ($choice -ne "y" -and $choice -ne "Y") {
    Write-Host "Uninstallation cancelled." -ForegroundColor Yellow
    exit 0
}

# Stop the service if running
if ($service.Status -eq "Running") {
    Write-Host ""
    Write-Host "Stopping service..." -ForegroundColor Cyan
    try {
        & $ExePath -stop
        Start-Sleep -Seconds 2
        
        $service = Get-Service -Name $ServiceName
        if ($service.Status -ne "Stopped") {
            Write-Host "Service did not stop cleanly. Force stopping..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force
        }
        Write-Host "Service stopped." -ForegroundColor Green
    } catch {
        Write-Host "WARNING: Could not stop service: $_" -ForegroundColor Yellow
        Write-Host "Attempting force stop..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    }
}

# Uninstall the service
Write-Host ""
Write-Host "Uninstalling service..." -ForegroundColor Cyan
try {
    & $ExePath -uninstall
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $service) {
        Write-Host "Service uninstalled successfully!" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Service still exists. Attempting manual removal..." -ForegroundColor Yellow
        sc.exe delete $ServiceName
        Write-Host "Service removed." -ForegroundColor Green
    }
} catch {
    Write-Host "ERROR during uninstallation: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: appsettings.json and logs are preserved." -ForegroundColor Yellow
Write-Host "To remove all data, manually delete:" -ForegroundColor Yellow
Write-Host "  - $(Split-Path $ExePath)\appsettings.json" -ForegroundColor White
Write-Host "  - $(Split-Path $ExePath)\logs\" -ForegroundColor White
Write-Host ""
