#!/usr/bin/env dotnet fsi

#load "TestUtils.fsx"

open TestUtils
open System.IO

printStep "Running all integration tests..."
printfn ""

let mutable exitCode = 0
let mutable succeeded = []
let mutable failed = []

// Get all test directories
let testDirectories = getAllTestDirectories ()

for dirName in testDirectories do
    if Directory.Exists(dirName) then
        if hasTestScript dirName then
            printStep $"Running F# test script: {dirName}"

            match runTestInDirectory dirName with
            | Ok output ->
                succeeded <- dirName :: succeeded
                printfn ""
            | Error errorMsg ->
                printStep $"Test failed for: {dirName}"
                printError $"Reason: {errorMsg}"
                failed <- (dirName, errorMsg) :: failed
                exitCode <- 1
                printfn ""
        elif hasFsProjFile dirName then
            printStep $"Building F# project: {dirName}"

            match buildWithDotnetBuild dirName with
            | Ok output ->
                succeeded <- dirName :: succeeded
                printfn ""
            | Error errorMsg ->
                printStep $"Build failed for: {dirName}"
                printError $"Reason: {errorMsg}"
                failed <- (dirName, errorMsg) :: failed
                exitCode <- 1
                printfn ""
        else
            printStep $"Skipping {dirName}: No test.fsx or .fsproj file found"

// Print results summary
printfn ""
printStep "=== Integration Test Results ==="

if not (List.isEmpty succeeded) then
    printStep $"Succeeded ({List.length succeeded}):"

    for project in List.rev succeeded do
        printSuccessWithColor project

if not (List.isEmpty failed) then
    printStep $"Failed ({List.length failed}):"

    for (project, errorMsg) in List.rev failed do
        printErrorWithColor project
        printfn $"    → {errorMsg}"

exit exitCode
