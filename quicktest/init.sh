#!/bin/bash

if [ -d "$1" ]; then
    rm -rf "$1"
fi
mkdir "$1"
cd "$1"
dotnet run --project ../../src/elmish-land.fsproj -- init
