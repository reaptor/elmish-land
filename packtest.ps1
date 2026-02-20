#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptDir
try {
    # Extract version from fsproj
    $xml = [xml](Get-Content src/elmish-land.fsproj)
    $version = $xml.Project.PropertyGroup.Version
    Write-Host "Packing elmish-land version $version..."

    # Pack the project
    dotnet pack
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }
    Write-Host "Package created: src/nupkg/elmish-land.$version.nupkg"

    # Clear only the elmish-land package from NuGet cache
    $cacheLine = dotnet nuget locals global-packages -l
    $cacheDir = ($cacheLine -replace 'global-packages: ', '')
    $elmishCachePath = Join-Path $cacheDir "elmish-land"
    if (Test-Path $elmishCachePath) {
        Remove-Item $elmishCachePath -Recurse -Force
        Write-Host "Cleared elmish-land from NuGet cache"
    }

    # Create test directory in OS temp folder (no parent dotnet-tools.json)
    $nupkgDir = Join-Path $scriptDir "src/nupkg"
    $testDir = Join-Path ([System.IO.Path]::GetTempPath()) ("elmish-land-packtest-" + [guid]::NewGuid().ToString("N").Substring(0, 8))
    New-Item $testDir -ItemType Directory -Force | Out-Null
    Push-Location $testDir
    try {
        Write-Host "Installing elmish-land $version as a local tool..."
        dotnet tool install elmish-land --create-manifest-if-needed --add-source $nupkgDir --version $version
        if ($LASTEXITCODE -ne 0) { throw "dotnet tool install failed" }

        Write-Host "Running elmish-land init..."
        dotnet elmish-land init -y
        if ($LASTEXITCODE -ne 0) { throw "elmish-land init failed" }

        Write-Host "Running elmish-land restore..."
        dotnet elmish-land restore
        if ($LASTEXITCODE -ne 0) { throw "elmish-land restore failed" }

        Write-Host "Running elmish-land build..."
        dotnet elmish-land build
        if ($LASTEXITCODE -ne 0) { throw "elmish-land build failed" }

        Write-Host ""
        Write-Host "packtest completed successfully!"
    }
    finally {
        Pop-Location
        Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}
