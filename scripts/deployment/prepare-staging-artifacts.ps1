param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $repoRoot

dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Local .NET tool restore failed.' }

dotnet restore AI-Emergency-Response.slnx
if ($LASTEXITCODE -ne 0) { throw 'Solution restore failed.' }

dotnet build AI-Emergency-Response.slnx --configuration $Configuration --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }

$artifactDirectory = Join-Path $repoRoot 'artifacts/staging'
New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
$searchOutputDirectory = Join-Path $artifactDirectory 'search-index-provisioner'
$searchOutputAbsolute = [System.IO.Path]::GetFullPath($searchOutputDirectory)
$artifactRootAbsolute = [System.IO.Path]::GetFullPath($artifactDirectory).TrimEnd('\') + '\'
if (-not $searchOutputAbsolute.StartsWith(
        $artifactRootAbsolute,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Search provisioner output resolved outside the staging artifact directory.'
}
if (Test-Path -LiteralPath $searchOutputAbsolute) {
    Remove-Item -LiteralPath $searchOutputAbsolute -Recurse -Force
}

$nugetCache = Join-Path $env:USERPROFILE '.nuget\packages'
$containerCommands = @(
    'dotnet tool restore',
    'dotnet restore src/EmergencyResponse.Api/EmergencyResponse.Api.csproj',
    "dotnet build src/EmergencyResponse.Api/EmergencyResponse.Api.csproj --configuration $Configuration --no-restore",
    "dotnet ef migrations bundle --no-build --force --project src/EmergencyResponse.Infrastructure/EmergencyResponse.Infrastructure.csproj --startup-project src/EmergencyResponse.Api/EmergencyResponse.Api.csproj --context ApplicationDbContext --configuration $Configuration --self-contained --target-runtime linux-x64 --output artifacts/staging/efbundle",
    'dotnet restore tools/EmergencyResponse.SearchIndexProvisioner/EmergencyResponse.SearchIndexProvisioner.csproj --runtime linux-x64',
    "dotnet publish tools/EmergencyResponse.SearchIndexProvisioner/EmergencyResponse.SearchIndexProvisioner.csproj --configuration $Configuration --runtime linux-x64 --self-contained --no-restore -p:PublishSingleFile=true --output artifacts/staging/search-index-provisioner"
) -join ' && '

docker run --rm `
    --volume "${repoRoot}:/src" `
    --volume "${nugetCache}:/root/.nuget/packages" `
    --workdir /src `
    mcr.microsoft.com/dotnet/sdk:10.0 `
    bash -lc $containerCommands
if ($LASTEXITCODE -ne 0) { throw 'Linux EF bundle or Search index provisioner preparation failed.' }

$bundle = Join-Path $artifactDirectory 'efbundle'
$indexTool = Join-Path $artifactDirectory 'search-index-provisioner/EmergencyResponse.SearchIndexProvisioner'
if (-not (Test-Path $bundle) -or -not (Test-Path $indexTool)) {
    throw 'The Linux staging bundle or Search provisioner output is missing.'
}

Write-Host "Staging migration/search artifacts prepared under $artifactDirectory"
Write-Host 'Copy them to the temporary VNet runner through Azure Bastion; do not store them in source control.'
