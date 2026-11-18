# GymService - Deployment Guide

## Overview

GymService is a Windows Service that provides a REST API for gym management. This guide covers building, installing, configuring, and managing the service.

## Prerequisites

- **.NET 8.0 SDK** or Runtime (Windows)
- **SQL Server** (Express, Developer, or Standard)
- **Windows 10/11** or **Windows Server 2016+**
- **Administrator privileges** for service installation

## Build

```powershell
# Navigate to project directory
cd C:\proyectos\gimnasio-dotnet\GymService

# Build in Release mode
dotnet build -c Release

# Or publish as self-contained
dotnet publish -c Release -o .\publish
```

## Configuration

Copia la plantilla y edita los valores reales:

```powershell
Copy-Item appsettings.example.json appsettings.json
```

> `appsettings.json` contiene credenciales y está ignorado por git. Nunca lo subas al repositorio.

Edita `appsettings.json` en el directorio de salida:

```json
{
  "Database": {
    "Server": "localhost",
    "Database": "GymManager",
    "User": "sa",
    "Password": "",
    "Port": 1433
  },
  "WebServer": {
    "Port": 5000,
    "Urls": "http://localhost:5000",
    "CorsOrigins": "http://localhost:3000"
  },
  "Authentication": {
    "TokenLifetimeHours": 8,
    "OtpExpiryMinutes": 10
  }
}
```

## Installation

### Option 1: PowerShell Script (Recommended)

Run PowerShell as **Administrator**:

```powershell
cd C:\proyectos\gimnasio-dotnet\GymService\bin\Release\net10.0-windows7.0
.\install-service.ps1
```

The script will:
1. Install the Windows Service
2. Set startup type to Automatic
3. Configure recovery options (auto-restart on failure)
4. Set service description
5. Start the service

### Option 2: Manual Installation

```powershell
# Install
.\GymService.exe -install

# Configure startup type
Set-Service -Name anxogymmservice -StartupType Automatic

# Configure recovery
sc.exe failure anxogymmservice reset= 86400 actions= restart/60000/restart/120000/restart/180000

# Start
.\GymService.exe -start
```

## Service Management

### Built-in Commands

```powershell
# Install service
.\GymService.exe -install

# Uninstall service
.\GymService.exe -uninstall

# Start service
.\GymService.exe -start

# Stop service
.\GymService.exe -stop

# Run in console mode (for debugging)
.\GymService.exe -console

# Show help
.\GymService.exe -help
```

### Windows Service Manager

```powershell
# Open Services console
services.msc

# Find "Anxo Gym Manager Service"
```

### PowerShell Management

```powershell
# Check status
Get-Service anxogymmservice

# Start
Start-Service anxogymmservice

# Stop
Stop-Service anxogymmservice

# Restart
Restart-Service anxogymmservice

# Set startup type
Set-Service -Name anxogymmservice -StartupType Automatic
```

## Uninstallation

### Option 1: PowerShell Script

```powershell
.\uninstall-service.ps1
```

### Option 2: Manual

```powershell
.\GymService.exe -stop
.\GymService.exe -uninstall
```

## Health Checks

Once running, verify the service:

```powershell
# Full health check
curl http://localhost:5000/health

# Readiness check
curl http://localhost:5000/ready

# Liveness probe
curl http://localhost:5000/health/live
```

## Swagger UI

Access the interactive API documentation at:
```
http://localhost:5000/swagger
```

## Logs

Logs are stored in the `logs` directory next to the executable:

```
C:\proyectos\gimnasio-dotnet\GymService\bin\Release\net8.0-windows7.0\logs\
```

## Recovery Options

The installation script configures automatic recovery:

| Failure | Action | Delay |
|---------|--------|-------|
| 1st | Restart | 60 seconds |
| 2nd | Restart | 120 seconds |
| 3rd | Restart | 180 seconds |
| Reset | Counter reset | 1 day |

## Troubleshooting

### Service won't start

1. Check logs in the `logs` directory
2. Verify `appsettings.json` exists and is valid
3. Check SQL Server is running and accessible
4. Run in console mode for detailed output: `.\GymService.exe -console`

### Port already in use

Change the port in `appsettings.json`:
```json
"WebServer": {
  "Port": 5001
}
```

### SQL Server connection failed

1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure SQL Server accepts TCP connections
4. Verify firewall allows port 1433

## Service Account

By default, the service runs as **Local System**. To change:

```powershell
# Run as Network Service
sc.exe config anxogymmservice obj= "NT AUTHORITY\NetworkService"

# Run as specific user
sc.exe config anxogymmservice obj= "DOMAIN\Username" password= "Password"
```

## First-Time Setup

After installation, create the first admin user:

```powershell
# Use the bootstrap endpoint
curl -X POST http://localhost:5000/api/auth/bootstrap `
  -H "Content-Type: application/json" `
  -d '{
    "username": "admin",
    "email": "admin@centrofit.com",
    "password": "your-secure-password",
    "fullName": "System Administrator"
  }'
```
