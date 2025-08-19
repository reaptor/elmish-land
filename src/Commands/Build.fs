module ElmishLand.Build

open System
open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.Generate
open ElmishLand.Settings
open ElmishLand.FableErrorAnalyzer
open Orsak

let successMessage =
    $"""%s{getCommandHeader "build was successful."}
Your app was saved in the 'dist' directory.
"""

let fableBuild absoluteProjectDir isVerbose =
    eff {
        let projectName = ProjectName.fromAbsoluteProjectDir absoluteProjectDir

        let appFsproj =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts [
                ".elmish-land"
                "App"
                $"ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
            ]
            |> FilePath.asString

        let! settings = getSettings absoluteProjectDir
        let mutable collectedOutput = ""

        let fableOutputHandler (output: string) =
            collectedOutput <- collectedOutput + output + "\n"
            
            // Always show Fable compilation errors and warnings, even without --verbose
            let isError = 
                output.Contains("error FS") || 
                output.Contains("warning FS") || 
                output.Contains("error FABLE") ||
                output.Contains("warning FABLE") ||
                output.Contains("Build FAILED") ||
                output.Contains("Build failed") ||
                output.Contains("Compilation failed") ||
                output.Contains("error:") ||
                output.Contains("Error:") ||
                (output.Contains("error") && (output.Contains(".fs(") || output.Contains(".fsx("))) ||
                (output.Contains("Error") && (output.Contains(".fs(") || output.Contains(".fsx("))) ||
                output.Contains("MSBUILD : error") ||
                output.Contains("CSC : error") ||
                (output.Contains("[ERROR]") || output.Contains("[WARN]"))

            if isError then
                // Check if this line can be translated to user-friendly format
                let isTranslatable = isTranslatableAppFsError output
                
                if isTranslatable then
                    // For translatable App.fs errors, process immediately and show user-friendly message instead
                    if hasCompilationErrors output then
                        let pageLayoutErrors = analyzeAppFsErrors absoluteProjectDir settings output
                        for pageLayoutError in pageLayoutErrors do
                            match pageLayoutError with
                            | PageError (_, message) -> System.Console.Error.WriteLine($"  → %s{message}")
                            | LayoutError (_, message) -> System.Console.Error.WriteLine($"  → %s{message}")
                    
                    // Don't show the original cryptic error
                else
                    // For non-translatable errors, show the original error
                    System.Console.Error.WriteLine(output)
            elif isVerbose then
                () // In verbose mode, output is already printed by runProcess

        return!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [|
                    "fable"
                    appFsproj
                    "--noCache"
                    "--run"
                    "vite"
                    "build"
                    "--config"
                    "vite.config.js"
                |]
                CancellationToken.None
                fableOutputHandler
    }


let build absoluteProjectDir =
    let isVerbose = System.Environment.CommandLine.Contains("--verbose")
    let stopSpinner = createSpinner "Building your project..."

    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir
        do! ensureViteInstalled ()

        let! result =
            fableBuild absoluteProjectDir isVerbose
            |> Effect.map Ok
            |> Effect.onError (fun e -> eff { return Error e })

        match result with
        | Ok _ ->
            stopSpinner ()
            log.Info successMessage
            return! Ok()
        | Error(AppError.ProcessError e) when e.Contains "dotnet tool restore" ->
            do!
                runProcess isVerbose workingDirectory "dotnet" [| "tool"; "restore" |] CancellationToken.None ignore
                |> Effect.map ignore<string>

            let! retryResult =
                fableBuild absoluteProjectDir isVerbose
                |> Effect.map Ok
                |> Effect.onError (fun e -> eff { return Error e })
            match retryResult with
            | Ok _ ->
                stopSpinner ()
                log.Info successMessage
                return! Ok()
            | Error retryError ->
                stopSpinner ()
                return! Error retryError
        | Error e ->
            stopSpinner ()
            return! Error e
    }
