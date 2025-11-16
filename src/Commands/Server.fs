module ElmishLand.Server

open System
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
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

        let cancellationSource = new CancellationTokenSource()
        let mutable isViteReady = false
        let mutable hasReadyMessageBeenPrinted = false
        let viteReadyOutputBuilder = StringBuilder()

        let outputHandler (output: string) =
            if
                output.Contains("vite", StringComparison.InvariantCultureIgnoreCase)
                && output.Contains("ready", StringComparison.InvariantCultureIgnoreCase)
            then
                isViteReady <- true

            if isViteReady then
                viteReadyOutputBuilder.AppendLine(output) |> ignore

            let viteReadyOutput = viteReadyOutputBuilder.ToString()

            let urlStart =
                viteReadyOutput.IndexOf("http://", StringComparison.InvariantCultureIgnoreCase)

            if isViteReady && urlStart >= 0 && not hasReadyMessageBeenPrinted then
                stopSpinner ()
                hasReadyMessageBeenPrinted <- true
                let localUrl = viteReadyOutput.Substring(urlStart).Trim()
                Console.WriteLine(getCommandHeader $"development server is running at %s{localUrl}.")
                Console.WriteLine "Ctrl-C to stop."

            if not isVerbose then
                // Check for Vite HMR update messages and rewrite them
                if output.Contains("[vite]") && output.Contains("hmr update") then
                    // Extract timestamp (supports various formats)
                    // Matches patterns like: "9:07:02 PM", "21:07:02", "9:07 PM", "21:07", "21:07:02.123"
                    let timestampPattern = @"^\s*(\d{1,2}:\d{2}(?::\d{2})?(?:\.\d+)?(?:\s*[AP]M)?)"

                    let timestampMatch =
                        System.Text.RegularExpressions.Regex.Match(output, timestampPattern)

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
                    let result = FableOutput.processOutput output "" false

                    if result.Errors.Length > 0 then
                        stopSpinner ()
                        cancellationSource.Cancel()

                    for error in result.Errors do
                        Console.ForegroundColor <- ConsoleColor.Red
                        Console.WriteLine(error)
                        Console.ResetColor()

                    for warning in result.Warnings do
                        Console.ForegroundColor <- ConsoleColor.Yellow
                        Console.WriteLine(warning)
                        Console.ResetColor()

                    // Prompt for auto-fix if there are layout mismatches
                    if result.LayoutMismatches.Length > 0 then
                        FableOutput.promptForAutoFix result.LayoutMismatches |> ignore

        return!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                command
                args
                cancellationSource.Token
                outputHandler
    }

let server workingDirectory absoluteProjectDir (promptBehaviour: UserPromptBehaviour) =
    eff {
        let! log = Log().Get()

        do!
            withSpinner "Starting development server..." (fun stopSpinner ->
                eff {
                    let isVerbose = System.Environment.CommandLine.Contains("--verbose")

                    let! dotnetSdkVersion = getDotnetSdkVersion workingDirectory
                    log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

                    do! generate workingDirectory absoluteProjectDir dotnetSdkVersion

                    do! validate absoluteProjectDir promptBehaviour
                    do! ensureViteInstalled workingDirectory

                    let! result =
                        fableWatch absoluteProjectDir stopSpinner
                        |> Effect.map Ok
                        |> Effect.onError (fun e -> eff { return Error e })

                    match result with
                    | Ok _ -> return! Ok()
                    | Error(AppError.ProcessError e) when e.Contains "dotnet tool restore" ->
                        do!
                            runProcess
                                isVerbose
                                workingDirectory
                                "dotnet"
                                [| "tool"; "restore" |]
                                CancellationToken.None
                                ignore
                            |> Effect.map ignore

                        do! fableWatch absoluteProjectDir stopSpinner |> Effect.map ignore
                    | Error e -> return! Error e
                })
    }
