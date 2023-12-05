module ElmishLand.Server

open System.IO
open System.Threading
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.FsProj

let server (projectDir: AbsoluteProjectDir) =
    use watcher =
        new FileSystemWatcher(AbsoluteProjectDir.asString projectDir, IncludeSubdirectories = true)

    use mutable cts = new CancellationTokenSource()
    use resetEvt = new AutoResetEvent(false)

    let fileChanged (filePath: FilePath) =
        if
            (filePath |> FilePath.startsWithParts [ "src" ])
            && FilePath.extension filePath = ".fs"
        then
            resetEvt.Set() |> ignore
            cts.Cancel()

    watcher.Changed.Add(fun e -> fileChanged (FilePath.fromString e.Name))
    watcher.Created.Add(fun e -> fileChanged (FilePath.fromString e.Name))
    watcher.Deleted.Add(fun e -> fileChanged (FilePath.fromString e.Name))
    watcher.Renamed.Add(fun e -> fileChanged (FilePath.fromString e.Name))

    watcher.EnableRaisingEvents <- true

    let rec loop () : int =
        watcher.EnableRaisingEvents <- false
        let routeData = getRouteData projectDir
        generateRoutesAndApp projectDir routeData
        watcher.EnableRaisingEvents <- true

        if validate projectDir = 0 then
            runProcesses [ projectDir, "npm", [| "run"; "elmish-land:start" |], cts.Token ] id
            |> ignore

        resetEvt.WaitOne() |> ignore
        resetEvt.Reset() |> ignore
        cts.Dispose()
        cts <- new CancellationTokenSource()
        loop ()

    loop ()
