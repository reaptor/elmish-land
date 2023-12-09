module ElmishLand.Build

open System.Threading
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.AppError

let build (projectDir: AbsoluteProjectDir) =
    let routeData = getRouteData projectDir
    generateRoutesAndApp projectDir routeData

    validate projectDir
    |> Result.bind (fun () ->
        let projectDirAsFilePath = AbsoluteProjectDir.asFilePath projectDir

        runProcesses [
            projectDirAsFilePath, "npm", [| "install" |], CancellationToken.None, ignore
            projectDirAsFilePath, "npm", [| "run"; "elmish-land:build" |], CancellationToken.None, ignore
        ])
    |> handleAppResult (fun () -> printfn "%s" (commandHeader "build was successful."))
