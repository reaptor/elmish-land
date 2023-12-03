module ElmishLand.Build

open System.Threading
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.FsProj

let build (projectDir: AbsoluteProjectDir) =
    let routeData = getRouteData projectDir
    generateRoutesAndApp projectDir routeData

    if validate projectDir = 0 then
        runProcesses
            [
                projectDir, "npm", [| "install" |], CancellationToken.None
                projectDir, "npm", [| "run"; "elmish-land:build" |], CancellationToken.None
            ]
            id
        |> ignore

    printfn "%s" (commandHeader "build was successful.")
    0
