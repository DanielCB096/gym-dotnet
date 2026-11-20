param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "",
    [switch]$FrameworkDependent,
    [switch]$SkipFrontend,
    [switch]$NoZip,
    [string]$AnxoRoot = "",
    [string]$Output = ""
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root "artifacts\GymService-$Runtime"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$project = Get-Content (Join-Path $root 'GymService\GymService.csproj')
    $Version = @($project.Project.PropertyGroup.Version) | Where-Object { $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($Version)) { $Version = "1.0.0" }
} else {
    $Version = $Version.TrimStart('v', 'V')
}

Write-Host "== GymService publish $version ($Runtime) ==" -ForegroundColor Cyan

# 1. Frontend
if (-not $SkipFrontend) {
    Write-Host "== Frontend ==" -ForegroundColor Cyan
    Push-Location (Join-Path $root 'GymServiceFront')
    try {
        if (-not (Test-Path 'node_modules')) {
            Write-Host "Instalando dependencias del frontend..." -ForegroundColor Yellow
            npm ci
        }
        npm run build
    }
    finally {
        Pop-Location
    }
}

# 2. Backend
Write-Host "== Backend ==" -ForegroundColor Cyan

if (Test-Path $Output) {
    Remove-Item -Recurse -Force $Output
}

$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }

$publishArgs = @(
    'publish', (Join-Path $root 'GymService\GymService.csproj'),
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', $selfContained,
    "-p:Version=$Version",
    '-o', $Output
)
if (-not [string]::IsNullOrWhiteSpace($AnxoRoot)) {
    $publishArgs += "-p:AnxoRoot=$AnxoRoot"
}

dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falló con código $LASTEXITCODE"
}

# 3. Panel web dentro del servicio
if (-not $SkipFrontend) {
    $dist = Join-Path $root 'GymServiceFront\dist'
    if (-not (Test-Path $dist)) {
        throw "No se encontró el build del frontend en $dist"
    }

    $wwwroot = Join-Path $Output 'wwwroot'
    New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
    Copy-Item (Join-Path $dist '*') $wwwroot -Recurse -Force
    Write-Host "Panel web copiado a $wwwroot" -ForegroundColor Green
}

# 4. Instalador y configuración de ejemplo
Copy-Item (Join-Path $root 'GymServiceSetup\install.ps1') $Output -Force
Copy-Item (Join-Path $root 'GymServiceSetup\uninstall.ps1') $Output -Force
Copy-Item (Join-Path $root 'GymService\appsettings.example.json') (Join-Path $Output 'appsettings.example.json') -Force

# Nunca publicar credenciales locales
$localConfig = Join-Path $Output 'appsettings.json'
if (Test-Path $localConfig) {
    Remove-Item $localConfig -Force
}

Set-Content -Path (Join-Path $Output 'VERSION.txt') -Value "GymService $version ($Runtime)" -Encoding UTF8

# 5. ZIP
$zipPath = Join-Path $root "artifacts\GymService-$version-$Runtime.zip"

if (-not $NoZip) {
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $Output '*') -DestinationPath $zipPath -Force
    Write-Host "Paquete: $zipPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "Listo. Contenido en: $Output" -ForegroundColor Green
Write-Host "Instalación (Administrador): $Output\install.ps1" -ForegroundColor Green
Write-Host ""
