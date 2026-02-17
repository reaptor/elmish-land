#!/usr/bin/env pwsh

# If the first argument is "init", set up TestProject directory
if ($args[0] -eq "init") {
    if (Test-Path "TestProject") {
        Remove-Item "TestProject" -Recurse -Force
    }
    New-Item "TestProject" -ItemType Directory | Out-Null
}

Push-Location "TestProject"
try {
    # Run the elmish-land command with all arguments
    dotnet run --project ../../src/elmish-land.fsproj -- @args
}
finally {
    Pop-Location
}
