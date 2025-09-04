module ElmishLand.Server

open System
open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.Generate
open ElmishLand.DotNetCli
open ElmishLand.Settings
open ElmishLand.FableOutput
open Orsak

let fableWatch absoluteProjectDir stopSpinner =
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
        let isVerbose = System.Environment.CommandLine.Contains("--verbose")
        let mutable isViteReady = false
        let mutable localUrl = ""

        let command, args =
            match settings.ServerCommand with
            | Some(command, args) -> command, args
            | None ->
                "dotnet",
                [|
                    "fable"
                    "watch"
                    appFsproj
                    "--noCache"
                    "--run"
                    "vite"
                    "--config"
                    "vite.config.js"
                |]

        let outputHandler (output: string) =
            // Check if this is the VITE ready message
            if output.Contains("VITE") && output.Contains("ready in") then
                stopSpinner ()
                isViteReady <- true
            // Capture the Local URL and display the final message
            elif isViteReady && output.Contains("Local:") then
                // Extract URL from "  ➜  Local:   http://localhost:5173/"
                let urlStart = output.IndexOf("http://")

                if urlStart >= 0 then
                    localUrl <- output.Substring(urlStart).Trim()

                    System.Console.WriteLine(getCommandHeader $"development server is running at %s{localUrl}.")
                    System.Console.WriteLine "Ctrl-C to stop."
            elif not isVerbose then
                // Check for Vite HMR update messages and rewrite them
                if output.Contains("[vite]") && output.Contains("hmr update") then
                    // Extract timestamp (supports various formats)
                    // Matches patterns like: "9:07:02 PM", "21:07:02", "9:07 PM", "21:07", "21:07:02.123"
                    let timestampPattern = @"^\s*(\d{1,2}:\d{2}(?::\d{2})?(?:\.\d+)?(?:\s*[AP]M)?)"
                    let timestampMatch = System.Text.RegularExpressions.Regex.Match(output, timestampPattern)
                    
                    // Extract the file path after "hmr update"
                    let hmrIndex = output.IndexOf("hmr update")
                    if timestampMatch.Success && hmrIndex >= 0 then
                        let timestamp = timestampMatch.Groups.[1].Value.Trim()
                        let filePath = output.Substring(hmrIndex + "hmr update".Length).Trim()
                        Console.WriteLine($"%s{timestamp} Updated %s{filePath}")
                    else
                        // Fallback: just show the cleaned message if parsing fails
                        Console.WriteLine(output)
                else
                    // In non-verbose mode, display errors and warnings with colors
                    let errors, warnings = FableOutput.processOutput output "" false

                    for error in errors do
                        Console.ForegroundColor <- ConsoleColor.Red
                        Console.WriteLine(error)
                        Console.ResetColor()

                    for warning in warnings do
                        Console.ForegroundColor <- ConsoleColor.Yellow
                        Console.WriteLine(warning)
                        Console.ResetColor()

        return!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                command
                args
                CancellationToken.None
                outputHandler
    }

let server absoluteProjectDir =
    let stopSpinner = createSpinner "Starting development server..."

    eff {
        let! log = Log().Get()
        let isVerbose = System.Environment.CommandLine.Contains("--verbose")

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir
        do! ensureViteInstalled ()

        let! result =
            fableWatch absoluteProjectDir stopSpinner
            |> Effect.map Ok
            |> Effect.onError (fun e -> eff { return Error e })

        match result with
        | Ok _ -> return! Ok()
        | Error(AppError.ProcessError e) when e.Contains "dotnet tool restore" ->
            do!
                runProcess isVerbose workingDirectory "dotnet" [| "tool"; "restore" |] CancellationToken.None ignore
                |> Effect.map ignore

            do! fableWatch absoluteProjectDir stopSpinner |> Effect.map ignore
        | Error e ->
            stopSpinner ()
            return! Error e
    }
