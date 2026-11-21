# Instala la CA local de Centro Fit para que el navegador confíe en
# https://192.168.100.53:5055 sin advertencias.
#
#   .\Instalar-CA-Windows.ps1            # solo el usuario actual (sin UAC)
#   .\Instalar-CA-Windows.ps1 -Machine   # todos los usuarios (requiere Administrador)
#
# El archivo CentroFit-CA.cer solo contiene la parte pública de la CA.

[CmdletBinding()]
param(
    [switch]$Machine,
    [string]$ServerUrl = 'https://192.168.100.53:5055'
)

$ErrorActionPreference = 'Stop'

$cer = Join-Path $PSScriptRoot 'CentroFit-CA.cer'
if (-not (Test-Path $cer)) {
    throw "No se encontró $cer. Descomprime el ZIP completo antes de ejecutar este script."
}

if ($Machine) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Para instalar para todos los usuarios ejecuta PowerShell como Administrador y usa -Machine.'
    }
    $location = 'LocalMachine'
} else {
    $location = 'CurrentUser'
}

$cert = Import-Certificate -FilePath $cer -CertStoreLocation "Cert:\$location\Root"

Write-Host ''
Write-Host 'Certificado instalado correctamente.' -ForegroundColor Green
Write-Host "Almacén: $location\Root"
Write-Host "Asunto:  $($cert.Subject)"
Write-Host "Huella:  $($cert.Thumbprint)"
Write-Host ''
Write-Host "Cierra y abre el navegador y prueba: $ServerUrl"
Write-Host 'Si usas Firefox, activa security.enterprise_roots.enabled en about:config.'
