# Lanza el kiosco de Centro Fit en este PC.
#
#   .\Start-Kiosk.ps1                 # usa la segunda pantalla; si no hay, no hace nada
#   .\Start-Kiosk.ps1 -ForcePrimary   # usa la pantalla principal (para probar)
#   .\Start-Kiosk.ps1 -ForcePrimary -Windowed  # ventana de prueba, no pantalla completa
#
# El perfil de Edge es dedicado y guarda el token del dispositivo "Kiosco PC".
# El teclado físico funciona con dígitos, Backspace, Enter y Escape.

[CmdletBinding()]
param(
    [switch]$ForcePrimary,
    [switch]$Windowed,
    [string]$Url = 'https://192.168.100.53:5055/kiosk-app/',
    [string]$EdgePath = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
    [string]$ProfileDir = (Join-Path $env:LOCALAPPDATA 'GymServiceKiosk\profile')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $EdgePath)) { throw "No se encontró Edge en $EdgePath" }
if (-not (Test-Path $ProfileDir)) { throw "No se encontró el perfil del kiosco en $ProfileDir. Configura el token primero." }

Add-Type -AssemblyName System.Windows.Forms
$screens = [System.Windows.Forms.Screen]::AllScreens
$primary = $screens | Where-Object { $_.Primary } | Select-Object -First 1
$secondary = $screens | Where-Object { -not $_.Primary } | Select-Object -First 1

$target = $secondary
if (-not $target) {
    if (-not $ForcePrimary) {
        Write-Host 'No hay segunda pantalla conectada. Conecta una y vuelve a ejecutar el acceso directo.'
        exit 0
    }
    $target = $primary
}

$left = $target.Bounds.X
$top = $target.Bounds.Y
$width = $target.Bounds.Width
$height = $target.Bounds.Height

$arguments = @(
    "--user-data-dir=$ProfileDir",
    '--no-first-run',
    '--no-default-browser-check',
    '--disable-sync',
    '--overscroll-history-navigation=0',
    '--disable-pinch'
)

if ($Windowed) {
    $width = 1280
    $height = 800
    $left = $target.Bounds.X + 80
    $top = $target.Bounds.Y + 80
    $arguments += @(
        "--window-position=$left,$top",
        "--window-size=$width,$height",
        "--app=$Url"
    )
} else {
    $arguments += @(
        "--window-position=$left,$top",
        "--window-size=$width,$height",
        '--kiosk',
        '--edge-kiosk-type=fullscreen',
        $Url
    )
}

Start-Process -FilePath $EdgePath -ArgumentList $arguments
