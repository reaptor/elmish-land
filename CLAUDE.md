# Project Build Information

## Coding Guidelines

### Extend existing functions instead of creating new ones

When adding functionality, modify the existing function rather than creating a new one alongside it.

#### ❌ Don't do this:
```fsharp
// Existing function
FableOutput.processOutput()

// Adding new function
FableOutput.processOutputSimple()
```

#### ✅ Do this instead:
```fsharp
// Modify the existing function
FableOutput.processOutput() // enhanced with new functionality
```

### When adding functions, check if existing functions exists that can be used.

#### ❌ Don't do this:
```fsharp
// Existing function
let getFolder () =
    "existing folder"
    
let getNewSimpleFolder () =
    "new simple folder"    
```

#### ✅ Do this instead:
```fsharp
// Modify the existing function
// Use or modify the existing function
let getFolder () =
    "existing folder"
```

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
- Unit tests run much faster (~400-500ms) compared to integration tests (~several seconds)

## Testing Workflow
- **TDD Requirement**: ALWAYS use TDD - create unit test first, then implement feature/fix
- **Test File Preference**: ALWAYS prefer existing test files over creating new ones when appropriate
- **Test Execution**: ALWAYS use quicktest/el.sh for testing changes

## Critical Restrictions
- **Never Delete Quicktest**: NEVER manually delete quicktest folder or contents - handled by scripts
- **ABSOLUTELY FORBIDDEN**: NEVER EVER use `net8.0/elmish-land` or `net8.0/elmish-land` from quicktest
- **Only Use el.sh**: ONLY use `./el.sh` commands from within quicktest directory
- **No Direct Binary Access**: NEVER access the elmish-land binary directly for quicktest - ALWAYS use el.sh wrapper

## Required Commands (No confirmation needed - run directly)
Run from project root folder (e.g., `/Users/klofberg/Projects/elmish-land`):

- **Init**: `cd quicktest; ./el.sh init; cd ..`
- **Build**: `cd quicktest; ./el.sh build; cd ..`
- **Restore**: `cd quicktest; ./el.sh restore; cd ..`
- **Server**: `cd quicktest; ./el.sh server; cd ..` (stop with ctrl-c)
- **Add Layout**: `cd quicktest; ./el.sh add layout <name>; cd ..`
- **Add Page**: `cd quicktest; ./el.sh add page <url>; cd ..`
- **Any Other Command**: `cd quicktest; ./el.sh <command> <args>; cd ..`

## Command Format
- **ALWAYS** use `./el.sh <command>` from within quicktest directory
- **NEVER** use `../../src/bin/Debug/net8.0/elmish-land` or any direct binary path
- **NEVER** use any other format for running elmish-land commands in quicktest

## Commit Requirements
- **No Claude References**: NEVER add info about claude code in commit messages
- **Concise Messages**: ALWAYS use concise commit messages
- **No Generated Tags**: NEVER add "Generated with [Claude Code]" to commit messages
- **Changelog Format**: ALWAYS follow existing format when editing CHANGELOG.md
- **Pre-commit Formatting**: ALWAYS run `dotnet fantomas .` from root before committing
- **Staging**: ALWAYS stage all files before committing
