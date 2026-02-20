#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Extract version from fsproj
VERSION=$(grep '<Version>' src/elmish-land.fsproj | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/')
echo "Packing elmish-land version $VERSION..."

# Pack the project
dotnet pack
echo "Package created: src/nupkg/elmish-land.$VERSION.nupkg"

# Clear only the elmish-land package from NuGet cache
CACHE_DIR=$(dotnet nuget locals global-packages -l | sed 's/global-packages: //')
if [ -d "${CACHE_DIR}elmish-land" ]; then
    rm -rf "${CACHE_DIR}elmish-land"
    echo "Cleared elmish-land from NuGet cache"
fi

# Create test directory in OS temp folder (no parent dotnet-tools.json)
NUPKG_DIR="$SCRIPT_DIR/src/nupkg"
TEST_DIR="$(dirname "$(mktemp -u)")/elmish-land-packtest-$$"
mkdir -p "$TEST_DIR"
trap 'rm -rf "$TEST_DIR"' EXIT
cd "$TEST_DIR"

echo "Installing elmish-land $VERSION as a local tool..."
dotnet tool install elmish-land --create-manifest-if-needed --add-source "$NUPKG_DIR" --version "$VERSION"

echo "Running elmish-land init..."
dotnet elmish-land init -y

echo "Running elmish-land restore..."
dotnet elmish-land restore

echo "Running elmish-land build..."
dotnet elmish-land build

echo ""
echo "packtest completed successfully!"
