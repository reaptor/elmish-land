module ElmishLand.Build

open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine
open ElmishLand.FsProj
open ElmishLand.Process
open Orsak

let successMessage =
    $"""%s{getCommandHeader "build was successful."}
Your app was saved in the 'dist' directory.
"""

let build (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Effect.getLogger ()
        let routeData = getRouteData projectDir
        do! generateRoutesAndApp projectDir routeData

        let workingDir = AbsoluteProjectDir.asFilePath projectDir
        do! validate projectDir

        do!
            runProcess
                workingDir
                "dotnet"
                [| "fable"; "--noCache"; "--run"; "vite"; "build" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        log.Info successMessage
    }
