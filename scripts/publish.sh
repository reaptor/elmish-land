dotnet publish --framework net8.0 --self-contained -r osx-arm64 -p:PublishSingleFile=true -p:UseAppHost=true
cp ./src/bin/Release/net8.0/osx-arm64/publish/elmish-land ./MyProject
