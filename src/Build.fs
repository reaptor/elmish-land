module ElmishLand.Build

open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.AppError

let build (projectDir: AbsoluteProjectDir) =
    let routeData = getRouteData projectDir
    generateRoutesAndApp projectDir routeData

    let workingDir = AbsoluteProjectDir.asFilePath projectDir

    result {
        do! validate projectDir

        do!
            runProcess
                workingDir
                "dotnet"
                [| "fable"; "--noCache"; "--run"; "vite"; "build" |]
                CancellationToken.None
                ignore
            |> Result.map ignore<string>

    }
    |> handleAppResult projectDir (fun () ->
        $"""%s{commandHeader "build was successful."}
Your app was saved in the 'dist' directory.
"""
        |> Log().Info)
