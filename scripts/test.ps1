$ErrorActionPreference = "Stop"

function Invoke-DotNetTest {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

Write-Host "Running unit tests..."
Invoke-DotNetTest @(
    "test",
    "tests/EmergencyResponse.UnitTests/EmergencyResponse.UnitTests.csproj",
    "--configuration", "Release"
)

Write-Host "Running SQL Server integration tests..."
Invoke-DotNetTest @(
    "test",
    "tests/EmergencyResponse.IntegrationTests/EmergencyResponse.IntegrationTests.csproj",
    "--configuration", "Release"
)

Write-Host "Running the full solution test command..."
Invoke-DotNetTest @("test", "AI-Emergency-Response.slnx", "--configuration", "Release")
