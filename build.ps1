param(
    [switch]$Integration,
    [string]$AnxoRoot = "C:\repositorios\pilot\ANXO LIBRARIES"
)

$ErrorActionPreference = 'Stop'

Write-Host "== Build =="
dotnet build GymService.slnx -c Release -p:AnxoRoot="$AnxoRoot"

if ($Integration)
{
    if ([string]::IsNullOrWhiteSpace($env:GYM_TEST_DB_PASSWORD))
    {
        throw "Defina GYM_TEST_DB_PASSWORD (y opcionalmente GYM_TEST_DB_SERVER / GYM_TEST_DB_USER) para ejecutar las pruebas de integración."
    }

    Write-Host "== Tests (unitarias + integración) =="
}
else
{
    Write-Host "== Tests (unitarias) =="
}

dotnet test GymService.Tests\GymService.Tests.csproj -c Release --no-build

Write-Host "== OK =="
