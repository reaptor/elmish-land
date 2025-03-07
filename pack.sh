dotnet nuget locals all -c
dotnet tool restore
dotnet pack

rm -rf integration-test
mkdir -p integration-test
pushd .
cd integration-test
dotnet new tool-manifest
dotnet tool install elmish-land --version 1.0.4 --add-source ../src/nupkg

dotnet elmish-land init
dotnet elmish-land restore
dotnet elmish-land build
dotnet elmish-land server
dotnet elmish-land add layout "/another-layout"
dotnet elmish-land add page "/another-page"

popd .