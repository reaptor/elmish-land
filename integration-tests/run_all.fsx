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
            printStep $"Running F# test: {dirName}"

            if runTestInDirectory dirName then
                succeeded <- dirName :: succeeded
                printfn ""
            else
                printStep $"Test failed for: {dirName}"
                failed <- dirName :: failed
                exitCode <- 1
                printfn ""
        else
            printStep $"Building F# test project: {dirName}"

            if buildProjectInDirectory dirName then
                succeeded <- dirName :: succeeded
                printfn ""
            else
                printStep $"Build failed for: {dirName}"
                failed <- dirName :: failed
                exitCode <- 1
                printfn ""

// Print results summary
printfn ""
printStep "=== Integration Test Results ==="

if not (List.isEmpty succeeded) then
    printStep $"Succeeded ({List.length succeeded}):"

    for project in List.rev succeeded do
        printSuccessWithColor project

if not (List.isEmpty failed) then
    printStep $"Failed ({List.length failed}):"

    for project in List.rev failed do
        printErrorWithColor project

exit exitCode
