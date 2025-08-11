# Project Build Information

## Building the Project
- Always run `dotnet build` from the root directory: `/Users/lindasvedberg/Kristofer/elmish-land`
- This builds both the main elmish-land project and tests

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