# Compila el instalador MSI de GymService con WiX 5.
#
#   .\build-msi.ps1
#   .\build-msi.ps1 -Version 1.2.0 -RunAsSystem   # (no aplica; el servicio ya es LocalSystem)
#   .\build-msi.ps1 -SkipPublish                 # reutiliza artifacts\msi\publish
#
# Requiere WiX 5 (libre):  dotnet tool install -g wix --version 5.0.2
# WiX 6+ exige aceptar el EULA de mantenimiento (OSMF); no se usa aqui.

param(
    [string]$Version = "",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent,
    [switch]$SkipFrontend,
    [switch]$SkipPublish,
    [string]$AnxoRoot = "C:\repositorios\pilot\ANXO LIBRARIES"
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$project = Get-Content (Join-Path $root 'GymService\GymService.csproj')
    $Version = @($project.Project.PropertyGroup.Version) | Where-Object { $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($Version)) { $Version = "1.0.0" }
}
$Version = $Version.TrimStart('v', 'V')

$parts = @($Version.Split('.'))
while ($parts.Count -lt 3) { $parts += '0' }
$msiVersion = ($parts[0..2] -join '.')

$publishDir = Join-Path $root "artifacts\msi\publish"

if (-not $SkipPublish) {
    & (Join-Path $root 'publish.ps1') `
        -NoZip `
        -Output $publishDir `
        -Version $Version `
        -Configuration $Configuration `
        -Runtime $Runtime `
        -AnxoRoot $AnxoRoot `
        -SkipFrontend:$SkipFrontend `
        -FrameworkDependent:$FrameworkDependent

    if ($LASTEXITCODE -ne 0) { throw "publish.ps1 falló con código $LASTEXITCODE" }
}

$wix = $null
$wixCommand = Get-Command wix -ErrorAction SilentlyContinue
if ($wixCommand) { $wix = $wixCommand.Source }
if (-not $wix) {
    $candidate = Join-Path $env:USERPROFILE '.dotnet\tools\wix.exe'
    if (Test-Path $candidate) { $wix = $candidate }
}
if (-not $wix) {
    throw "WiX no está instalado. Ejecuta: dotnet tool install -g wix --version 5.0.2"
}

$versionCheck = & $wix --version 2>&1
if ($versionCheck -match '^7\.' ) {
    throw "WiX 7 requiere aceptar el EULA de mantenimiento (OSMF). Instala la versión 5: dotnet tool install -g wix --version 5.0.2"
}

$output = Join-Path $root "artifacts\GymService-$Version-$Runtime.msi"
if (Test-Path $output) { Remove-Item $output -Force }

$uiExtension = "WixToolset.UI.wixext/$($versionCheck.Split('+')[0])"
& $wix extension add -g $uiExtension 2>&1 | Out-Null

Write-Host "== Generando MSI $Version ==" -ForegroundColor Cyan
& $wix build (Join-Path $root 'installer\GymService.wxs') `
    -arch x64 `
    -ext $uiExtension `
    -d "PublishDir=$publishDir" `
    -d "ProductVersion=$msiVersion" `
    -d "LicenseFile=$(Join-Path $root 'installer\License.rtf')" `
    -o $output

if ($LASTEXITCODE -ne 0) { throw "wix build falló con código $LASTEXITCODE" }

Write-Host "== Validando MSI (ICE) ==" -ForegroundColor Cyan
& $wix msi validate $output
if ($LASTEXITCODE -ne 0) { throw "wix msi validate reportó errores" }

$size = [math]::Round((Get-Item $output).Length / 1MB, 2)
Write-Host ""
Write-Host "MSI: $output ($size MB)" -ForegroundColor Green
Write-Host "Instalación (Administrador): msiexec /i `"$output`" OPEN_FIREWALL=1" -ForegroundColor Green
Write-Host "Tras instalar: edita appsettings.json y arranca el servicio con Start-Service anxogymmservice" -ForegroundColor Yellow
