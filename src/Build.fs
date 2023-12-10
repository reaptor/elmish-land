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

    let workingDir = AbsoluteProjectDir.asFilePath projectDir

    result {
        do! validate projectDir

        do!
            [
                "dotnet", [| "tool"; "restore" |]
                "dotnet", [| "restore" |]
                "npm", [| "install" |]
                "dotnet", [| "fable"; "--noCache"; "--run"; "vite"; "build" |]
            ]
            |> List.map (fun (cmd, args) -> workingDir, cmd, args, CancellationToken.None, ignore)
            |> runProcesses
    }
    |> handleAppResult projectDir (fun () ->
        $"""%s{commandHeader "build was successful."}
Your app was saved in the 'dist' directory.
"""
        |> indent
        |> printfn "%s")
