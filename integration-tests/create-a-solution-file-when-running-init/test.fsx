#!/usr/bin/env dotnet fsi

#load "../TestUtils.fsx"

open TestUtils
open System.IO

printStep "Testing solution file generation during init..."

// Clean up and create test directory
cleanupAndCreateTestDir "TestProject"

printStep "Running elmish-land init..."
runElmishLandCommand "init" |> ignore

printStep "Verifying solution file was created..."
verifyFileExists "TestProject.sln" "Solution file"

printStep "Verifying projects are added to solution..."

// Check if all expected projects are in the solution file
let expectedProjectNames = [ "ElmishLand.TestProject.Base"; "TestProject"; "ElmishLand.TestProject.App" ]

let solutionContent = File.ReadAllText("TestProject.sln")

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
    ".elmish-land/Base/ElmishLand.TestProject.Base.fsproj"
    "TestProject.fsproj"
    ".elmish-land/App/ElmishLand.TestProject.App.fsproj"
]

verifyFilesExist expectedProjectFiles "Project"

printStep "Testing dotnet sln list to verify projects are properly added..."
let slnListOutput = runCommand "dotnet" "sln list"
printfn "%s" slnListOutput

printSuccess "All tests passed! Solution file generation works correctly."
