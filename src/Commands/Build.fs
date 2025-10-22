module ElmishLand.Build

open System
open System.IO
open System.Threading
open System.Text
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.Generate
open ElmishLand.FableOutput
open Orsak

let successMessage =
    $"""%s{getCommandHeader "build was successful."}
Your app was saved in the 'dist' directory.
"""

let fableBuild absoluteProjectDir isVerbose =
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
        ignore

let build workingDirectory absoluteProjectDir =
    let isVerbose = System.Environment.CommandLine.Contains("--verbose")
    let stopSpinner = createSpinner "Building your project..."

    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion workingDirectory
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate workingDirectory absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir
        do! ensureViteInstalled workingDirectory

        let! result =
            fableBuild absoluteProjectDir isVerbose
            |> Effect.map Ok
            |> Effect.onError (fun e -> eff { return Error e })

        match result with
        | Ok(stdout, stderr) ->
            stopSpinner ()
            // Process and display any warnings from successful build
            if not isVerbose then
                let result = FableOutput.processOutput stdout stderr isVerbose
                FableOutput.displayWarnings result.Warnings

            log.Info successMessage
            return! Ok()
        | Error(AppError.ProcessError output) ->
            // The output is combined stdout + stderr. Since we can't reliably split them,
            // treat it all as stdout for processing
            let stdout = output
            let stderr = ""

            if output.Contains "dotnet tool restore" then
                do!
                    runProcess isVerbose workingDirectory "dotnet" [| "tool"; "restore" |] CancellationToken.None ignore
                    |> Effect.map ignore

                do!
                    fableBuild absoluteProjectDir isVerbose
                    |> Effect.map (fun (stdout2, stderr2) ->
                        stopSpinner ()

                        if not isVerbose then
                            let result = FableOutput.processOutput stdout2 stderr2 isVerbose
                            FableOutput.displayWarnings result.Warnings

                        log.Info successMessage)
                    |> Effect.onError (fun e2 ->
                        eff {
                            stopSpinner ()

                            match e2 with
                            | AppError.ProcessError output2 ->
                                // The output is combined stdout + stderr. Since we can't reliably split them,
                                // treat it all as stdout for processing
                                let stdout2 = output2
                                let stderr2 = ""

                                if not isVerbose then
                                    let result = FableOutput.processOutput stdout2 stderr2 isVerbose

                                    FableOutput.displayOutput result.Errors result.Warnings isVerbose

                                    // Prompt for auto-fix if there are layout mismatches
                                    if result.LayoutMismatches.Length > 0 then
                                        FableOutput.promptForAutoFix result.LayoutMismatches |> ignore
                            | _ -> ()

                            return! Error e2
                        })
            else
                stopSpinner ()
                // Process and display errors/warnings for failed build
                if not isVerbose then
                    let result = FableOutput.processOutput stdout stderr isVerbose

                    FableOutput.displayOutput result.Errors result.Warnings isVerbose

                    // Prompt for auto-fix if there are layout mismatches
                    if result.LayoutMismatches.Length > 0 then
                        let wasFixed = FableOutput.promptForAutoFix result.LayoutMismatches

                        if wasFixed then
                            // If we fixed something, suggest rebuilding
                            return! Error(AppError.ProcessError "")
                        else
                            // Return empty error since we've already displayed the errors
                            return! Error(AppError.ProcessError "")
                    else
                        // Return empty error since we've already displayed the errors
                        return! Error(AppError.ProcessError "")
                else
                    // In verbose mode, return the full output
                    return! Error(AppError.ProcessError output)
        | Error e ->
            stopSpinner ()
            return! Error e
    }
