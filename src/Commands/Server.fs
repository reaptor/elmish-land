module ElmishLand.Server

open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.Generate
open ElmishLand.DotNetCli
open Orsak

let fableWatch absoluteProjectDir =
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
            "watch"
            appFsproj
            "--noCache"
            "--run"
            "vite"
            "--config"
            "vite.config.js"
        |]
        CancellationToken.None
        ignore

let server absoluteProjectDir =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir
        do! ensureViteInstalled ()

        let! result =
            fableWatch absoluteProjectDir
            |> Effect.map Ok
            |> Effect.onError (fun e -> eff { return Error e })

        match result with
        | Ok _ -> return! Ok()
        | Error(AppError.ProcessError e) when e.Contains "dotnet tool restore" ->
            do!
                runProcess true workingDirectory "dotnet" [| "tool"; "restore" |] CancellationToken.None ignore
                |> Effect.map ignore<string>

            do! fableWatch absoluteProjectDir |> Effect.map ignore
        | Error e -> return! Error e
    }
