module ElmishLand.Build

open System
open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.FsProj

let build (workingDirectory: string option) =
    let exitCode = validate (projectPath workingDirectory)

    if exitCode <> 0 then
        exitCode
    else
        runProcesses
            [
                workingDirectory, "npm", [| "install" |], CancellationToken.None
                workingDirectory, "npm", [| "run"; "elmish-land:build" |], CancellationToken.None
            ]
            (fun () ->
                printfn
                    $"""

        %s{appTitle} (v%s{version}) build was successful.
        ⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺
    """     )
