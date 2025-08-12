param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,
    [Parameter(Mandatory=$true)]
    [string]$LayoutPath
)

Set-Location $ProjectName
dotnet run --project ../../src/elmish-land.fsproj -- add layout $LayoutPath