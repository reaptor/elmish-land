module ElmishLand.Build

open System
open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.FsProj
open ElmishLand.TemplateEngine

let build (workingDirectory: string option) =
    let projectPath = getProjectPath workingDirectory
    let exitCode = validate projectPath

    let x = getRouteData (Path.GetDirectoryName projectPath)
    0

// if exitCode <> 0 then
//     exitCode
// else
//     runProcesses
//         [
//             workingDirectory, "npm", [| "install" |], CancellationToken.None
//             workingDirectory, "npm", [| "run"; "elmish-land:build" |], CancellationToken.None
//         ]
//         (fun () -> printfn $"""%s{commandHeader "build was successful."}""")
