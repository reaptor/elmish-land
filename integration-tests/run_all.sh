#!/bin/bash

exit_code=0
succeeded=()
failed=()

for dir in */; do
    if [ -d "$dir" ]; then
        dirname=${dir%/}
        echo "Building test project: $dirname"
        cd $dirname
        if ! dotnet run --project ../../src/elmish-land.fsproj -- build --verbose; then
            echo "Build failed for: $dirname"
            failed+=("$dirname")
            exit_code=1
        else
            succeeded+=("$dirname")
        fi        
        cd ..
    fi
done

echo ""
echo "=== Integration Test Results ==="
if [ ${#succeeded[@]} -gt 0 ]; then
    echo "Succeeded (${#succeeded[@]}):"
    for project in "${succeeded[@]}"; do
        echo -e "  \033[32m✓\033[0m $project"
    done
fi

if [ ${#failed[@]} -gt 0 ]; then
    echo "Failed (${#failed[@]}):"
    for project in "${failed[@]}"; do
        echo -e "  \033[31m✗\033[0m $project"
    done
fi

exit $exit_code