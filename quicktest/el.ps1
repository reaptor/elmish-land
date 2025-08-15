# PowerShell equivalent of el.sh for testing elmish-land commands

# If the first argument is "init", set up TestProject directory
if ($args[0] -eq "init") {
    if (Test-Path "TestProject") {
        Remove-Item "TestProject" -Recurse -Force
    }
    New-Item -ItemType Directory -Name "TestProject" | Out-Null
}

Set-Location "TestProject"

# Run the elmish-land command with all arguments
dotnet run --project ..\..\src\elmish-land.fsproj -- @args

Set-Location ".."