# Genera el ZIP que se entrega a los equipos cliente: certificado público de la CA
# local + instalador de Windows + instrucciones para Android/iOS/kiosco.
#
#   .\Build-ClientPackage.ps1
#   .\Build-ClientPackage.ps1 -OutputZip "D:\Entrega\CentroFit-CA.zip"
#
# No incluye el PFX ni ninguna clave privada.

[CmdletBinding()]
param(
    [string]$CertPath = 'C:\ProgramData\GymService\certs\gymservice-root-ca.cer',
    [string]$ClientSource = '',
    [string]$OutputZip = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ClientSource)) { $ClientSource = Join-Path $PSScriptRoot 'client' }
if ([string]::IsNullOrWhiteSpace($OutputZip)) { $OutputZip = Join-Path $env:USERPROFILE 'Desktop\CentroFit-CA.zip' }

if (-not (Test-Path $CertPath)) { throw "No se encontró el certificado: $CertPath" }
if (-not (Test-Path $ClientSource)) { throw "No se encontró la carpeta de cliente: $ClientSource" }

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $CertPath
if ($cert.HasPrivateKey) { throw 'El archivo de certificado contiene una clave privada; usa solo la CA pública.' }

$stage = Join-Path ([IO.Path]::GetTempPath()) ("centrofit-ca-" + [Guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $stage | Out-Null
    Copy-Item $CertPath (Join-Path $stage 'CentroFit-CA.cer') -Force
    Copy-Item (Join-Path $ClientSource '*') $stage -Force -Recurse

    $outputDir = Split-Path -Parent $OutputZip
    if ($outputDir -and -not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
    }
    if (Test-Path $OutputZip) { Remove-Item $OutputZip -Force }

    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $OutputZip -CompressionLevel Optimal

    $hash = (Get-FileHash $OutputZip -Algorithm SHA256).Hash
    $size = [math]::Round((Get-Item $OutputZip).Length / 1KB, 1)

    Write-Host ''
    Write-Host 'Paquete de CA generado.' -ForegroundColor Green
    Write-Host "ZIP:    $OutputZip ($size KB)"
    Write-Host "SHA256: $hash"
    Write-Host "CA:     $($cert.Subject) / $($cert.Thumbprint) / vence $($cert.NotAfter.ToString('yyyy-MM-dd'))"
    Write-Host ''
    Write-Host 'Contenido:'
    Get-ChildItem $OutputZip | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::OpenRead($OutputZip).Entries | ForEach-Object { " - $($_.FullName)" }
} finally {
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}
