# Genera/renueva el certificado TLS de GymService para una IP de la red local.
# Crea una CA local (si no existe), emite el certificado del servidor con SAN de IP,
# actualiza appsettings.json (Kestrel) y reinicia el servicio.
#
#   .\New-GymServerCertificate.ps1
#   .\New-GymServerCertificate.ps1 -IpAddress 192.168.100.53 -ValidDays 825
#
# Ejecutar como Administrador. La CA raíz se exporta a la carpeta de certificados
# para instalarla en los equipos cliente; su clave privada permanece en el servidor.

[CmdletBinding()]
param(
    [string]$IpAddress = '192.168.100.53',
    [string]$ConfigPath = 'C:\Program Files\Anxo\GymService\appsettings.json',
    [string]$CertDir = 'C:\ProgramData\GymService\certs',
    [int]$ValidDays = 825,
    [switch]$SkipRestart
)

$ErrorActionPreference = 'Stop'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'Ejecuta este script como Administrador.' }
if (-not (Test-Path $ConfigPath)) { throw "No se encontró la configuración: $ConfigPath" }

New-Item -ItemType Directory -Force -Path $CertDir | Out-Null
icacls $CertDir /inheritance:r /grant:r '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' '*S-1-5-32-545:(OI)(CI)R' | Out-Null

$rootSubject = 'CN=GymService LAN Root CA'
$root = Get-ChildItem Cert:\LocalMachine\My |
    Where-Object { $_.Subject -eq $rootSubject -and $_.HasPrivateKey } |
    Sort-Object NotAfter -Descending | Select-Object -First 1

if (-not $root) {
    Write-Host "Creando CA local '$rootSubject' ..."
    $root = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $rootSubject `
        -KeyUsage CertSign, CRLSign, DigitalSignature `
        -KeyExportPolicy NonExportable `
        -KeyLength 4096 `
        -KeyAlgorithm RSA `
        -HashAlgorithm SHA256 `
        -CertStoreLocation Cert:\LocalMachine\My `
        -NotAfter (Get-Date).AddYears(10) `
        -TextExtension @('2.5.29.19={critical}{text}ca=1&pathlength=1')
}

$rootCer = Join-Path $CertDir 'gymservice-root-ca.cer'
Export-Certificate -Cert $root -FilePath $rootCer -Type CERT -Force | Out-Null
Import-Certificate -FilePath $rootCer -CertStoreLocation Cert:\LocalMachine\Root | Out-Null

$leafSubject = "CN=$IpAddress"
$leaf = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $leafSubject `
    -Signer $root `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -CertStoreLocation Cert:\LocalMachine\My `
    -NotAfter (Get-Date).AddDays($ValidDays) `
    -TextExtension @(
        "2.5.29.17={text}IPAddress=$IpAddress&IPAddress=127.0.0.1",
        '2.5.29.37={text}1.3.6.1.5.5.7.3.1'
    )

$pfxPath = Join-Path $CertDir 'gymservice.pfx'
$bytes = New-Object byte[] 24
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$pfxPassword = [Convert]::ToBase64String($bytes)
Export-PfxCertificate -Cert $leaf -FilePath $pfxPath -Password (ConvertTo-SecureString $pfxPassword -AsPlainText -Force) -Force | Out-Null
icacls $pfxPath /inheritance:r /grant:r '*S-1-5-18:F' '*S-1-5-32-544:F' | Out-Null

# Limpiar certificados anteriores del mismo sujeto emitidos por esta CA.
Get-ChildItem Cert:\LocalMachine\My |
    Where-Object { $_.Subject -eq $leafSubject -and $_.Issuer -eq $root.Subject -and $_.Thumbprint -ne $leaf.Thumbprint } |
    ForEach-Object { Remove-Item "Cert:\LocalMachine\My\$($_.Thumbprint)" -Force -ErrorAction SilentlyContinue }

$cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$port = if ($cfg.WebServer.Port) { [int]$cfg.WebServer.Port } else { 5000 }
$backup = Join-Path $CertDir ("appsettings-backup-" + (Get-Date).ToString('yyyyMMdd_HHmmss') + '.json')
Copy-Item $ConfigPath $backup -Force
icacls $backup /inheritance:r /grant:r '*S-1-5-18:F' '*S-1-5-32-544:F' | Out-Null

$cfg.WebServer.Urls = "https://0.0.0.0:$port"
$certificates = [pscustomobject]@{
    Default = [pscustomobject]@{ Path = $pfxPath; Password = $pfxPassword }
}
if (-not $cfg.Kestrel) {
    $cfg | Add-Member -MemberType NoteProperty -Name Kestrel -Value ([pscustomobject]@{}) -Force
}
$cfg.Kestrel | Add-Member -MemberType NoteProperty -Name Certificates -Value $certificates -Force
[IO.File]::WriteAllText($ConfigPath, ($cfg | ConvertTo-Json -Depth 12), (New-Object Text.UTF8Encoding($false)))
icacls $ConfigPath /inheritance:r /grant:r '*S-1-5-18:F' '*S-1-5-32-544:F' | Out-Null

Write-Host "Certificado emitido: $leafSubject (vence $($leaf.NotAfter.ToString('yyyy-MM-dd')))"
Write-Host "PFX: $pfxPath"
Write-Host "CA raíz para clientes: $rootCer"

if (-not $SkipRestart) {
    Restart-Service anxogymmservice -Force
    Start-Sleep -Seconds 10
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $health = (Invoke-WebRequest "https://$IpAddress`:$port/health" -UseBasicParsing -TimeoutSec 20).Content | ConvertFrom-Json
    Write-Host "Servicio: $((Get-Service anxogymmservice).Status); health: $($health.status); versión: $($health.components.'gym-readiness'.data.version)"
}
