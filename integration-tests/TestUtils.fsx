// Shared test utilities for elmish-land integration tests

module TestUtils

open System
open System.IO
open System.Diagnostics
open System.Text.Json

let runCommand (command: string) (args: string) =
    let psi = ProcessStartInfo()
    psi.FileName <- command
    psi.Arguments <- args
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true

    use proc = Process.Start(psi)

    // Add timeout to prevent hanging
    let timeoutMs = 120000 // 2 minutes

    if not (proc.WaitForExit(timeoutMs)) then
        proc.Kill()
        failwith $"Command timed out after {timeoutMs}ms: {command} {args}"

    let output = proc.StandardOutput.ReadToEnd()
    let error = proc.StandardError.ReadToEnd()

    if proc.ExitCode <> 0 then
        failwith $"Command failed with exit code {proc.ExitCode}:\nOutput: {output}\nError: {error}"

    output

let runElmishLandCommand (args: string) =
    runCommand "dotnet" $"../../../src/bin/Release/net8.0/elmish-land.dll {args}"

let cleanupAndCreateTestDir (dirName: string) =
    if Directory.Exists(dirName) then
        Directory.Delete(dirName, true)

    Directory.CreateDirectory(dirName) |> ignore
    Directory.SetCurrentDirectory(dirName)

let verifyFileExists (filePath: string) (description: string) =
    if not (File.Exists(filePath)) then
        failwith $"❌ ERROR: {description} {filePath} was not created"

    printfn $"✅ {description} {filePath} exists"

let verifyFilesExist (files: string list) (description: string) =
    for file in files do
        verifyFileExists file $"{description} file"

let printSuccess (message: string) = printfn $"✅ {message}"

let printError (message: string) = printfn $"❌ ERROR: {message}"

let printStep (message: string) = printfn $"{message}"

let printSuccessWithColor (message: string) =
    printf "\u001b[32m✓\u001b[0m " // Green checkmark
    printfn $"{message}"

let printErrorWithColor (message: string) =
    printf "\u001b[31m✗\u001b[0m " // Red X
    printfn $"{message}"

let getAllTestDirectories () =
    Directory.GetDirectories(".")
    |> Array.filter (fun dir ->
        let dirName = Path.GetFileName(dir)
        dirName <> "." && dirName <> "..")
    |> Array.map Path.GetFileName

let hasTestScript (dirName: string) =
    File.Exists(Path.Combine(dirName, "test.fsx"))

let hasFsProjFile (dirName: string) =
    Directory.GetFiles(dirName, "*.fsproj") |> Array.length > 0

let runTestInDirectory (dirName: string) =
    let originalDir = Directory.GetCurrentDirectory()

    try
        Directory.SetCurrentDirectory(dirName)
        let result = runCommand "dotnet" "fsi test.fsx"
        Directory.SetCurrentDirectory(originalDir)
        Ok result
    with ex ->
        Directory.SetCurrentDirectory(originalDir)
        Error ex.Message

let checkDotnetVersionInstalled (dirPath: string) =
    let globalJsonPath = Path.Combine(dirPath, "global.json")

    if not (File.Exists(globalJsonPath)) then
        printStep $"No global.json found in {dirPath}, skipping version check"
        Ok()
    else
        try
            let jsonContent = File.ReadAllText(globalJsonPath)
            use jsonDoc = JsonDocument.Parse(jsonContent)

            let requiredVersion =
                try
                    let sdkElement = jsonDoc.RootElement.GetProperty("sdk")
                    let versionElement = sdkElement.GetProperty("version")
                    versionElement.GetString()
                with _ ->
                    ""

            if String.IsNullOrEmpty(requiredVersion) then
                printStep "No SDK version specified in global.json, skipping version check"
                Ok()
            else
                // Check if the specified version is installed
                let installedVersionsOutput = runCommand "dotnet" "--list-sdks"
                let isVersionInstalled = installedVersionsOutput.Contains(requiredVersion)

                if isVersionInstalled then
                    printStep $"✅ Required dotnet SDK version {requiredVersion} is installed"
                    Ok()
                else
                    Error $"Required dotnet SDK version '{requiredVersion}' is not installed."
        with ex ->
            Error $"Failed to process global.json: {ex.Message}"

let buildWithDotnetBuild (dirName: string) =
    let originalDir = Directory.GetCurrentDirectory()

    try
        Directory.SetCurrentDirectory(dirName)

        // Check if required dotnet version is installed
        match checkDotnetVersionInstalled "." with
        | Ok() ->
            let result = runCommand "dotnet" "build"
            Directory.SetCurrentDirectory(originalDir)
            Ok result
        | Error err ->
            Directory.SetCurrentDirectory(originalDir)
            Error err
    with ex ->
        Directory.SetCurrentDirectory(originalDir)
        Error ex.Message
