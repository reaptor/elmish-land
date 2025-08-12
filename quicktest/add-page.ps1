param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,
    [Parameter(Mandatory=$true)]
    [string]$PagePath
)

Set-Location $ProjectName
dotnet run --project ../../src/elmish-land.fsproj -- add page $PagePath