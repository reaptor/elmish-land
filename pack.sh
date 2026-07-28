dotnet nuget locals all -c
dotnet tool restore
dotnet pack

# Extract version from fsproj so this script never pins a stale version
VERSION=$(grep '<Version>' src/elmish-land.fsproj | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/')

rm -rf integration-test
mkdir -p integration-test
pushd .
cd integration-test
dotnet new tool-manifest
dotnet tool install elmish-land --version "$VERSION" --add-source ../src/nupkg

dotnet elmish-land init --auto-accept
dotnet elmish-land restore
dotnet elmish-land build
dotnet elmish-land add layout "/another-layout"  --auto-accept
dotnet elmish-land add page "/another-page"  --auto-accept

popd .
