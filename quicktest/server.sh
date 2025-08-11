#!/bin/bash

cd "$1"
dotnet run --project ../../src/elmish-land.fsproj -- server
