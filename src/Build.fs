module ElmishLand.Build

open System.Threading
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.FsProj

let build (projectDir: AbsoluteProjectDir) =
    let routeData = getRouteData projectDir
    generateRoutesAndApp projectDir routeData

    validate projectDir
    |> Result.bind (fun () ->
        runProcesses [
            projectDir, "npm", [| "install" |], CancellationToken.None, ignore
            projectDir, "npm", [| "run"; "elmish-land:build" |], CancellationToken.None, ignore
        ])
    |> handleAppResult (fun () -> printfn "%s" (commandHeader "build was successful."))
