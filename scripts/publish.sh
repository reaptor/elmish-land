PROJECT_PATH="$(dirname ${0})/../src"
dotnet publish $PROJECT_PATH --framework net8.0 --self-contained -r osx-arm64 -p:PublishSingleFile=true -p:UseAppHost=true
cp $PROJECT_PATH/bin/Release/net8.0/osx-arm64/publish/elmish-land ./
