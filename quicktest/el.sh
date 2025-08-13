#!/bin/bash

# If the first argument is "init", set up TestProject directory
if [ "$1" = "init" ]; then
    if [ -d "TestProject" ]; then
        rm -rf "TestProject"
    fi
    mkdir "TestProject"
fi

cd "TestProject"

# Run the elmish-land command with all arguments
dotnet run --project ../../src/elmish-land.fsproj -- "$@"

