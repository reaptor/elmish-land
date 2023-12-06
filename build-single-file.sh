dotnet publish ./elmish-land.fsproj -p:PublishSingleFile=true --self-contained true -r osx-arm64 -c Debug --framework net8.0
mkdir -p MyProject
rm -rf MyProject/*
rm -rf MyProject/.config
cp ./bin/Debug/net8.0/osx-arm64/publish/elmish-land ./MyProject
cp ./bin/Debug/net8.0/osx-arm64/publish/elmish-land.pdb ./MyProject