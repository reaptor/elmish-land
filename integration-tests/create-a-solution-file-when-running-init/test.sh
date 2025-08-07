#!/bin/bash

set -e

echo "Testing solution file generation during init..."

# Clean up and create test directory
rm -rf App
mkdir App
cd App

echo "Running elmish-land init..."
dotnet run --project ../../../src/elmish-land.fsproj -- init --verbose

echo "Verifying solution file was created..."
if [ ! -f "App.sln" ]; then
    echo "❌ ERROR: App.sln file was not created"
    exit 1
fi

echo "✅ Solution file App.sln was created"

echo "Verifying projects are added to solution..."

# Check if all expected projects are in the solution file
# Note: dotnet uses backslashes in solution files regardless of platform
expected_project_names=(
    "ElmishLand.App.Base"
    "App"
    "ElmishLand.App.App"
)

for project_name in "${expected_project_names[@]}"; do
    if grep -q "$project_name" App.sln; then
        echo "✅ $project_name is in the solution"
    else
        echo "❌ ERROR: $project_name is NOT in the solution"
        echo "Solution contents:"
        cat App.sln
        exit 1
    fi
done

echo "Verifying all project files exist..."
expected_project_files=(
    ".elmish-land/Base/ElmishLand.App.Base.fsproj"
    "App.fsproj" 
    ".elmish-land/App/ElmishLand.App.App.fsproj"
)

for project in "${expected_project_files[@]}"; do
    if [ -f "$project" ]; then
        echo "✅ $project file exists"
    else
        echo "❌ ERROR: $project file does not exist"
        exit 1
    fi
done

echo "Testing dotnet sln list to verify projects are properly added..."
dotnet sln list

echo "✅ All tests passed! Solution file generation works correctly."