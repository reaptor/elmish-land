module ElmishLand.Server

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
            elif isVerbose then
                () // In verbose mode, output is already printed by runProcess

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
                |> Effect.map ignore<string>

            do! fableWatch absoluteProjectDir stopSpinner |> Effect.map ignore
        | Error e ->
            stopSpinner ()
            return! Error e
    }
