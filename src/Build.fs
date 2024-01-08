module ElmishLand.Build

open System.Threading
open ElmishLand.Base
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

let build (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersionToUse ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate projectDir dotnetSdkVersion

        do! validate projectDir

        let workingDir = AbsoluteProjectDir.asFilePath projectDir

        do!
            runProcess
                true
                workingDir
                "dotnet"
                [|
                    "fable"
                    ".elmish-land/App/App.fsproj"
                    "--noCache"
                    "--run"
                    "vite"
                    "build"
                    "--config"
                    "vite.config.js"
                |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        log.Info successMessage
    }
