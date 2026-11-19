param(
    [string]$Source = $PSScriptRoot,
    [string]$InstallDir = "$env:ProgramFiles\Anxo\GymService",
    [int]$Port = 5000,
    [string]$BindAddress = "0.0.0.0",
    [string]$DatabaseServer = "",
    [string]$DatabaseName = "",
    [string]$DatabaseUser = "",
    [string]$DatabasePassword = "",
    [switch]$OpenFirewall,
    [switch]$NoStart,
    [switch]$PrepareOnly
)

$ErrorActionPreference = 'Stop'
$serviceName = "anxogymmservice"

function Assert-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Ejecuta este instalador como Administrador."
    }
}

if (-not $PrepareOnly) {
    Assert-Admin
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " GymService - Instalación" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Si se pasa un .zip, se descomprime en un temporal.
$tempDir = $null
if ($Source -match '\.zip$') {
    if (-not (Test-Path $Source)) { throw "No se encontró el paquete: $Source" }
    $tempDir = Join-Path $env:TEMP ("GymServiceSetup_" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    Expand-Archive -LiteralPath $Source -DestinationPath $tempDir -Force
    $Source = $tempDir
}

$exeSource = Join-Path $Source 'GymService.exe'
if (-not (Test-Path $exeSource)) {
    throw "No se encontró GymService.exe en '$Source'. Publica primero con publish.ps1 o indica -Source."
}

$exampleConfig = Join-Path $Source 'appsettings.example.json'
Write-Host "Origen:       $Source"
Write-Host "Destino:      $InstallDir"
Write-Host "Servicio:     $serviceName"
Write-Host "Puerto:       $Port ($BindAddress)"
Write-Host ""

# Detener/desinstalar versión previa.
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing -and -not $PrepareOnly) {
    Write-Host "Deteniendo servicio existente..." -ForegroundColor Yellow
    try {
        & $exeSource -stop | Out-Null
    } catch {
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2

    Write-Host "Desinstalando servicio existente..." -ForegroundColor Yellow
    try { & $exeSource -uninstall | Out-Null } catch { }
    Start-Sleep -Seconds 2

    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        sc.exe delete $serviceName | Out-Null
        Start-Sleep -Seconds 2
    }
}

# Copiar archivos conservando appsettings.json y logs.
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Get-ChildItem -LiteralPath $Source -Force |
    Where-Object { $_.Name -notin @('appsettings.json', 'logs') } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $InstallDir -Recurse -Force
    }

# Configuración: solo se crea si no existe.
$configPath = Join-Path $InstallDir 'appsettings.json'
$configCreated = $false
if (-not (Test-Path $configPath)) {
    if (-not (Test-Path $exampleConfig)) {
        throw "Falta appsettings.example.json en el paquete."
    }

    Copy-Item $exampleConfig $configPath -Force
    $configCreated = $true

    $cfg = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json

    $cfg.WebServer.Port = $Port
    $cfg.WebServer.Urls = "http://${BindAddress}:$Port"

    if (-not [string]::IsNullOrWhiteSpace($DatabaseServer)) { $cfg.Database.Server = $DatabaseServer }
    if (-not [string]::IsNullOrWhiteSpace($DatabaseName))   { $cfg.Database.Database = $DatabaseName }
    if (-not [string]::IsNullOrWhiteSpace($DatabaseUser))   { $cfg.Database.User = $DatabaseUser }
    if (-not [string]::IsNullOrWhiteSpace($DatabasePassword)) { $cfg.Database.Password = $DatabasePassword }

    # Genera una clave MFA única para este servidor si sigue el placeholder.
    if ([string]::IsNullOrWhiteSpace($cfg.Authentication.MfaEncryptionKey) -or
        $cfg.Authentication.MfaEncryptionKey -like 'CAMBIAR*') {
        $keyBytes = New-Object byte[] 32
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        $rng.GetBytes($keyBytes)
        $rng.Dispose()
        $cfg.Authentication.MfaEncryptionKey = [Convert]::ToBase64String($keyBytes)
        Write-Host "MfaEncryptionKey generada automáticamente." -ForegroundColor Green
    }

    $json = $cfg | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText($configPath, $json, (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "Configuración creada: $configPath" -ForegroundColor Green

    if ([string]::IsNullOrWhiteSpace($cfg.Email.SmtpUser)) {
        Write-Host "Aviso: Email:SmtpUser está vacío; los códigos OTP no se enviarán por correo." -ForegroundColor Yellow
    }
    if ($cfg.Database.Password -like 'CAMBIAR*') {
        Write-Host "Aviso: configura Database:Password antes de usar el servicio en producción." -ForegroundColor Yellow
    }

    if (-not $PrepareOnly) {
        try {
            icacls $configPath /inheritance:r /grant:r "*S-1-5-18:(R)" "*S-1-5-32-544:(F)" | Out-Null
        } catch {
            Write-Host "Aviso: no se pudieron restringir los permisos de appsettings.json." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "Se conserva la configuración existente: $configPath" -ForegroundColor Yellow
}

if (-not $configCreated -and -not [string]::IsNullOrWhiteSpace($DatabasePassword)) {
    Write-Host "Aviso: la configuración ya existía; -DatabasePassword no se aplicó." -ForegroundColor Yellow
}

if ($PrepareOnly) {
    Write-Host ""
    Write-Host "Modo -PrepareOnly: archivos copiados y configuración lista; no se registra el servicio." -ForegroundColor Yellow
    Write-Host " Destino: $InstallDir" -ForegroundColor White
    Write-Host " Config:  $configPath" -ForegroundColor White
    if ($tempDir) { Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue }
    return
}

# Registrar el servicio.
Write-Host ""
Write-Host "Registrando servicio Windows..." -ForegroundColor Cyan
Push-Location $InstallDir
try {
    & .\GymService.exe -install
    start-sleep -Seconds 2
}
finally {
    Pop-Location
}

if (-not (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
    throw "El servicio no quedó registrado. Revisa los logs de la instalación."
}

# Recuperación ante fallos.
sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/120000/restart/180000 | Out-Null
sc.exe description $serviceName "GymService - API y panel web para la gestion del gimnasio." | Out-Null

if ($OpenFirewall) {
    $ruleName = "GymService (TCP $Port)"
    if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port | Out-Null
        Write-Host "Regla de firewall creada: $ruleName" -ForegroundColor Green
    }
}

if (-not $NoStart) {
    Write-Host "Iniciando servicio..." -ForegroundColor Cyan
    Push-Location $InstallDir
    try {
        & .\GymService.exe -start | Out-Null
    }
    finally {
        Pop-Location
    }

    Start-Sleep -Seconds 4
    $service = Get-Service -Name $serviceName
    if ($service.Status -eq 'Running') {
        Write-Host "Servicio en ejecución." -ForegroundColor Green
    } else {
        Write-Host "El servicio quedó en estado: $($service.Status)" -ForegroundColor Yellow
        Write-Host "Revisa los logs en: $(Join-Path $InstallDir 'logs')" -ForegroundColor Yellow
    }
}

$panelHost = if ($BindAddress -eq '0.0.0.0') { $env:COMPUTERNAME } else { $BindAddress }

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Instalación completada" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host " Panel:      http://${panelHost}:$Port" -ForegroundColor White
Write-Host " Config:     $configPath" -ForegroundColor White
Write-Host " Logs:       $(Join-Path $InstallDir 'logs')" -ForegroundColor White
Write-Host " Servicio:   Get-Service $serviceName" -ForegroundColor White
Write-Host ""

if ($tempDir) {
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}
