module ElmishLand.Server

open System.IO
open System.Threading
open System.Threading.Tasks
open ElmishLand.Base

let server workingDirectory =
    let projectPath = FsProj.projectPath workingDirectory
    let projectDir = Path.GetDirectoryName(projectPath)
    let projectFileName = Path.GetFileName(projectPath)
    use watcher = new FileSystemWatcher(projectDir, IncludeSubdirectories = true)

    use mutable cts = new CancellationTokenSource()
    use resetEvt = new AutoResetEvent(false)

    let fileChanged fileName =
        if
            fileName = projectFileName
            || Path.GetExtension(fileName) = ".fs"
               && not (fileName.Contains("bin/"))
               && not (fileName.Contains("obj/"))
        then
            resetEvt.Set() |> ignore
            cts.Cancel()

    watcher.Changed.Add(fun e -> fileChanged e.Name)
    watcher.Created.Add(fun e -> fileChanged e.Name)
    watcher.Deleted.Add(fun e -> fileChanged e.Name)
    watcher.Renamed.Add(fun e -> fileChanged e.Name)

    watcher.EnableRaisingEvents <- true

    runProcesses [ workingDirectory, "npm", [| "install" |], CancellationToken.None ]
    |> ignore

    let rec loop () : int =
        if FsProj.validate projectPath = 0 then
            runProcesses [ workingDirectory, "npm", [| "run"; "elmish-land:start" |], cts.Token ] id
            |> ignore

        resetEvt.WaitOne() |> ignore
        resetEvt.Reset() |> ignore
        cts.Dispose()
        cts <- new CancellationTokenSource()
        loop ()

    loop ()
