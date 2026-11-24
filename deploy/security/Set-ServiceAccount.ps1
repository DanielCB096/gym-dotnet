# Configura el servicio GymService con una cuenta de servicio virtual y permisos mínimos.
#
#   .\Set-ServiceAccount.ps1
#   .\Set-ServiceAccount.ps1 -Rollback
#   .\Set-ServiceAccount.ps1 -SkipRestart
#
# Requiere Administrador. La cuenta por defecto es NT SERVICE\<nombre-del-servicio>.
# Concede lectura/ejecución sobre la aplicación, escritura sobre logs y lectura de
# appsettings.json y del PFX de HTTPS. No toca la base de datos: la app sigue usando
# autenticación SQL.

[CmdletBinding()]
param(
    [string]$ServiceName = 'centrofitgymservice',
    [string]$ConfigPath = 'C:\Program Files\Centro Fit\GymService\appsettings.json',
    [string]$Account = '',
    [switch]$Rollback,
    [switch]$SkipRestart,
    [string]$HealthUrl = ''
)

$ErrorActionPreference = 'Stop'

function Assert-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Ejecuta este script como Administrador.'
    }
}

function Get-ServiceSid([string]$name) {
    $output = (sc.exe showsid $name 2>&1 | Out-String)
    $match = [regex]::Match($output, 'S-1-5-80-\d+(?:-\d+)+')
    if (-not $match.Success) { throw "No se pudo obtener el SID de '$name'." }
    return $match.Value
}

function Grant-Acl([string]$path, [string]$rights, [switch]$Recurse) {
    if (-not (Test-Path $path)) { return }
    $icaclsArgs = @($path, '/grant', "*${script:serviceSid}:$rights")
    if ($Recurse) { $icaclsArgs += @('/T', '/C') }
    icacls @icaclsArgs | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls falló sobre '$path' ($LASTEXITCODE)." }
}

Assert-Admin

if (-not (Test-Path $ConfigPath)) { throw "No se encontró la configuración: $ConfigPath" }
$installDir = Split-Path -Parent $ConfigPath
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$serviceSid = Get-ServiceSid $ServiceName

$targetAccount = if ($Rollback) { 'LocalSystem' } elseif ([string]::IsNullOrWhiteSpace($Account)) { "NT SERVICE\$ServiceName" } else { $Account }

$logPath = $config.Logging.LogPath
if ([string]::IsNullOrWhiteSpace($logPath)) { $logPath = 'logs' }
if (-not [System.IO.Path]::IsPathRooted($logPath)) { $logPath = Join-Path $installDir $logPath }

$certPath = $config.Kestrel.Certificates.Default.Path
$certDir = if ($certPath) { Split-Path -Parent $certPath } else { $null }
$outbox = $config.Email.OutboxDirectory

if (-not $SkipRestart -and [string]::IsNullOrWhiteSpace($HealthUrl) -and $config.WebServer.Urls) {
    $baseUrl = ($config.WebServer.Urls -split ',')[0].Trim()
    $baseUrl = $baseUrl -replace '0\.0\.0\.0', '127.0.0.1' -replace '\+', '127.0.0.1'
    $HealthUrl = $baseUrl.TrimEnd('/') + '/health'
}

Write-Host "Servicio: $ServiceName" -ForegroundColor Cyan
Write-Host "Cuenta:   $targetAccount" -ForegroundColor Cyan
Write-Host "SID:      $serviceSid" -ForegroundColor DarkGray

if (-not $Rollback) {
    Write-Host "Aplicando permisos mínimos..." -ForegroundColor Cyan
    Grant-Acl $installDir '(OI)(CI)RX' -Recurse
    if (-not (Test-Path $logPath)) { New-Item -ItemType Directory -Force -Path $logPath | Out-Null }
    Grant-Acl $logPath '(OI)(CI)M' -Recurse
    Grant-Acl $ConfigPath '(R)'

    # Fotos de socios subidas desde el panel.
    $uploadDir = Join-Path $installDir 'wwwroot\uploads\members'
    if (-not (Test-Path $uploadDir)) { New-Item -ItemType Directory -Force -Path $uploadDir | Out-Null }
    Grant-Acl $uploadDir '(OI)(CI)M' -Recurse

    if ($certDir) {
        Grant-Acl $certDir '(OI)(CI)R' -Recurse
        Grant-Acl $certPath '(R)'
    }

    if (-not [string]::IsNullOrWhiteSpace($outbox)) {
        if (-not [System.IO.Path]::IsPathRooted($outbox)) { $outbox = Join-Path $installDir $outbox }
        if (-not (Test-Path $outbox)) { New-Item -ItemType Directory -Force -Path $outbox | Out-Null }
        Grant-Acl $outbox '(OI)(CI)M' -Recurse
    }
}

$wasRunning = (Get-Service $ServiceName).Status -eq 'Running'
if (-not $SkipRestart -and $wasRunning) {
    Stop-Service $ServiceName -Force
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Service $ServiceName).Status -ne 'Stopped' -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
    if ((Get-Service $ServiceName).Status -ne 'Stopped') { throw 'El servicio no se detuvo en 60 segundos.' }
}

$scOutput = (sc.exe config $ServiceName 'obj=' $targetAccount 2>&1 | Out-String)
if ($LASTEXITCODE -ne 0) { throw "No se pudo cambiar la cuenta del servicio: $($scOutput.Trim())" }

if (-not $SkipRestart) {
    Start-Service $ServiceName
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Service $ServiceName).Status -ne 'Running' -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }

    $state = (Get-CimInstance Win32_Service -Filter "Name='$ServiceName'")
    if ($state.State -ne 'Running') {
        # Rollback automático si la cuenta nueva no puede arrancar.
        sc.exe config $ServiceName 'obj=' 'LocalSystem' | Out-Null
        Start-Service $ServiceName
        throw "El servicio no arrancó con '$targetAccount'. Se restauró LocalSystem."
    }

    if (-not [string]::IsNullOrWhiteSpace($HealthUrl)) {
        $health = Invoke-RestMethod $HealthUrl -TimeoutSec 20
        if ($health.status -ne 'healthy') { throw "El servicio arrancó pero /health devolvió '$($health.status)'." }
        Write-Host "Health: $($health.status)" -ForegroundColor Green
    }
}

$final = Get-CimInstance Win32_Service -Filter "Name='$ServiceName'"
Write-Host "Cuenta final: $($final.StartName)" -ForegroundColor Green
Write-Host "Estado:       $($final.State)" -ForegroundColor Green
