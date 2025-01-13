Remove-Item -Recurse ../ElmishLandMyProject
dotnet run --framework net9.0 --project src/elmish-land.fsproj -- init --project-dir ../ElmishLandMyProject --verbose
