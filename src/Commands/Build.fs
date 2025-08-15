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
open ElmishLand.Validation
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

let enhancedFableBuild absoluteProjectDir isVerbose =
    eff {
        let! result = 
            fableBuild absoluteProjectDir isVerbose
            |> Effect.map Ok
            |> Effect.onError (fun e -> eff { return Error e })

        match result with
        | Ok output -> return! Ok output
        | Error (AppError.ProcessError errorOutput) ->
            // Check for compilation errors that might be function signature issues
            if errorOutput.Contains("error") && (errorOutput.Contains(".fs") || errorOutput.Contains("Pages")) then
                let! pageErrors = validatePageFiles absoluteProjectDir
                let! layoutErrors = validateLayoutFiles absoluteProjectDir
                let allErrors = List.append pageErrors layoutErrors
                
                if not (List.isEmpty allErrors) then
                    let enhancedErrorMessage = 
                        $"""Build failed with validation errors:

%s{formatValidationErrors allErrors}

Original Fable output:
%s{errorOutput}"""
                    return! Error (AppError.ValidationError enhancedErrorMessage)
                else
                    return! Error (AppError.ProcessError errorOutput)
            else
                return! Error (AppError.ProcessError errorOutput)
        | Error e -> return! Error e
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
            enhancedFableBuild absoluteProjectDir isVerbose
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
                enhancedFableBuild absoluteProjectDir isVerbose
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
