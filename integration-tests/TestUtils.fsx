// Shared test utilities for elmish-land integration tests

module TestUtils

open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions

let runCommand (command: string) (args: string) =
    let psi = ProcessStartInfo()
    psi.FileName <- command
    psi.Arguments <- args
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    
    use proc = Process.Start(psi)
    proc.WaitForExit()
    
    let output = proc.StandardOutput.ReadToEnd()
    let error = proc.StandardError.ReadToEnd()
    
    if proc.ExitCode <> 0 then
        failwith $"Command failed with exit code {proc.ExitCode}:\nOutput: {output}\nError: {error}"
    
    output

let runElmishLandCommand (args: string) =
    runCommand "dotnet" $"run --project ../../../src/elmish-land.fsproj -- {args}"

let cleanupAndCreateTestDir (dirName: string) =
    if Directory.Exists(dirName) then
        Directory.Delete(dirName, true)
    
    Directory.CreateDirectory(dirName) |> ignore
    Directory.SetCurrentDirectory(dirName)

let verifyFileExists (filePath: string) (description: string) =
    if not (File.Exists(filePath)) then
        failwith $"❌ ERROR: {description} {filePath} was not created"
    printfn $"✅ {description} {filePath} exists"

let verifyFileContains (filePath: string) (expectedContent: string) (description: string) =
    let content = File.ReadAllText(filePath)
    if content.Contains(expectedContent) then
        printfn $"✅ {description}"
    else
        let relevantLines = 
            content.Split('\n')
            |> Array.filter (fun line -> line.Contains(expectedContent.Split(' ').[0]))
        
        printfn $"❌ ERROR: {description}"
        printfn $"Expected: {expectedContent}"
        printfn "Found:"
        if relevantLines.Length > 0 then
            relevantLines |> Array.iter (printfn "  %s")
        else
            printfn "  No relevant content found"
        exit 1

let verifyFilesExist (files: string list) (description: string) =
    for file in files do
        verifyFileExists file $"{description} file"

let insertIntoFirstItemGroup (filePath: string) (compileInclude: string) =
    let content = File.ReadAllText(filePath)
    let pattern = @"</ItemGroup>"
    let replacement = $"    <Compile Include=\"{compileInclude}\" />\n  </ItemGroup>"
    
    // Only replace the first occurrence
    let regex = Regex(pattern)
    let newContent = regex.Replace(content, replacement, 1)
    File.WriteAllText(filePath, newContent)

let printSuccess (message: string) =
    printfn $"✅ {message}"

let printError (message: string) =
    printfn $"❌ ERROR: {message}"

let printStep (message: string) =
    printfn $"{message}"

let printSuccessWithColor (message: string) =
    printf "\u001b[32m✓\u001b[0m "  // Green checkmark
    printfn $"{message}"

let printErrorWithColor (message: string) =
    printf "\u001b[31m✗\u001b[0m "  // Red X
    printfn $"{message}"

let getAllTestDirectories () =
    Directory.GetDirectories(".")
    |> Array.filter (fun dir -> 
        let dirName = Path.GetFileName(dir)
        dirName <> "." && dirName <> "..")
    |> Array.map Path.GetFileName

let hasTestScript (dirName: string) =
    File.Exists(Path.Combine(dirName, "test.fsx"))

let runTestInDirectory (dirName: string) =
    let originalDir = Directory.GetCurrentDirectory()
    try
        Directory.SetCurrentDirectory(dirName)
        let result = runCommand "dotnet" "fsi test.fsx"
        Directory.SetCurrentDirectory(originalDir)
        true
    with
    | ex ->
        Directory.SetCurrentDirectory(originalDir)
        false

let buildProjectInDirectory (dirName: string) =
    let originalDir = Directory.GetCurrentDirectory()
    try
        Directory.SetCurrentDirectory(dirName)
        let result = runCommand "dotnet" "run --project ../../src/elmish-land.fsproj -- build --verbose"
        Directory.SetCurrentDirectory(originalDir)
        true
    with
    | ex ->
        Directory.SetCurrentDirectory(originalDir)
        false