#!/usr/bin/env dotnet fsi

#load "../TestUtils.fsx"

open TestUtils
open System.IO

printStep "Testing solution file generation during init..."

// Clean up and create test directory
cleanupAndCreateTestDir "App"

printStep "Running elmish-land init..."
runElmishLandCommand "init --verbose" |> ignore

printStep "Verifying solution file was created..."
verifyFileExists "App.sln" "Solution file"

printStep "Verifying projects are added to solution..."

// Check if all expected projects are in the solution file
let expectedProjectNames = [ "ElmishLand.App.Base"; "App"; "ElmishLand.App.App" ]

let solutionContent = File.ReadAllText("App.sln")

for projectName in expectedProjectNames do
    if solutionContent.Contains(projectName) then
        printSuccess $"{projectName} is in the solution"
    else
        printError $"{projectName} is NOT in the solution"
        printStep "Solution contents:"
        printfn "%s" solutionContent
        exit 1

printStep "Verifying all project files exist..."

let expectedProjectFiles = [
    ".elmish-land/Base/ElmishLand.App.Base.fsproj"
    "App.fsproj"
    ".elmish-land/App/ElmishLand.App.App.fsproj"
]

verifyFilesExist expectedProjectFiles "Project"

printStep "Testing dotnet sln list to verify projects are properly added..."
let slnListOutput = runCommand "dotnet" "sln list"
printfn "%s" slnListOutput

printSuccess "All tests passed! Solution file generation works correctly."
