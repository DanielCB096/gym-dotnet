param(
    [switch]$Integration,
    [string]$AnxoRoot = ""
)

$ErrorActionPreference = 'Stop'

Write-Host "== Build =="
$buildArgs = @('build', 'GymService.slnx', '-c', 'Release')
if (-not [string]::IsNullOrWhiteSpace($AnxoRoot)) {
    $buildArgs += "-p:AnxoRoot=$AnxoRoot"
}
dotnet @buildArgs

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
