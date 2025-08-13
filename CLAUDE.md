# Project Build Information

## Building the Project
- Always run `dotnet build` from the root directory of the elmish-land project
- This builds both the main elmish-land project and tests
- Don't specify the project or configuration when building (use "dotnet build" not "dotnet build src/elmish-land.fsproj -c Release")

## Running Integration Tests
- Change to the relevant test directory (e.g., `integration-tests/backwards-compability-with-1-0-4`)
- Run `dotnet fsi test.fsx` to execute the test
- Never manually delete the test projects

## Unit Tests
- Unit tests are located in `tests/*.fs`
- **addPage indentation test**: Verifies both preview and actual .fsproj file have correct indentation
- **addLayout after addPage test**: Verifies the workflow of adding a layout after a page, including:
  - Project file ordering (layout before page)  
  - Page file updates to use specific layout
  - Auto-accept functionality
- Unit tests run much faster (~400-500ms) compared to integration tests (~several seconds)

## Testing your changes
- Always use quicktest/el.sh if you need to test your changes
- NEVER manually delete the quicktest folder or its contents - this is handled by the scripts
- NEVER run elmish-land commands directly with `dotnet ../src/bin/Debug/net8.0/elmish-land.dll` - quicktest/el.sh
- Use "quicktest/el.sh init" to test the init command
- Use "quicktest/el.sh build" to test the build command
- Use "quicktest/el.sh restore" to test the restore command