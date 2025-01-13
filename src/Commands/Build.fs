module ElmishLand.Build

open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.Generate
open Orsak

let successMessage =
    $"""%s{getCommandHeader "build was successful."}
Your app was saved in the 'dist' directory.
"""

let fableBuild absoluteProjectDir =
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
        true
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

let build absoluteProjectDir =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir
        do! ensureViteInstalled ()

        let! result =
            fableBuild absoluteProjectDir
            |> Effect.map Ok
            |> Effect.onError (fun e -> eff { return Error e })

        match result with
        | Ok _ ->
            log.Info successMessage
            return! Ok()
        | Error(AppError.ProcessError e) when e.Contains "dotnet tool restore" ->
            do!
                runProcess true workingDirectory "dotnet" [| "tool"; "restore" |] CancellationToken.None ignore
                |> Effect.map ignore<string>

            do! fableBuild absoluteProjectDir |> Effect.map (fun _ -> log.Info successMessage)
        | Error e -> return! Error e
    }
