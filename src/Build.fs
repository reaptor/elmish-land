module ElmishLand.Build

open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine
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

        do! generate projectDir

        do! validate projectDir

        let workingDir =
            AbsoluteProjectDir.asFilePath projectDir
            |> FilePath.appendParts [ ".elmish-land" ]

        do!
            runProcess
                workingDir
                "dotnet"
                [| "fable"; "App/App.fsproj"; "--noCache"; "--run"; "vite"; "build" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        log.Info successMessage
    }
