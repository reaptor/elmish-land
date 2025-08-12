param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName
)

Set-Location $ProjectName
dotnet run --project ../../src/elmish-land.fsproj -- build