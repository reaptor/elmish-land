param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName
)

if (Test-Path $ProjectName) {
    Remove-Item -Path $ProjectName -Recurse -Force
}
New-Item -ItemType Directory -Path $ProjectName | Out-Null
Set-Location $ProjectName
dotnet run --project ../../src/elmish-land.fsproj -- init