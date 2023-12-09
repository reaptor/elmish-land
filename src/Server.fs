module ElmishLand.Server

open System.IO
open System.Threading
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.AppError

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

        validate projectDir
        |> Result.bind (fun () ->
            runProcess
                (AbsoluteProjectDir.asFilePath projectDir)
                "npm"
                [| "run"; "elmish-land:start" |]
                cts.Token
                (fun output ->
                    let m = Regex.Match(output, "Local:   (http://localhost:\d+)")

                    if m.Success then
                        printfn $"""%s{commandHeader $"is ready at %s{m.Groups[1].Value}"}"""))
        |> handleAppResult ignore
        |> ignore<int>

        resetEvt.WaitOne() |> ignore
        resetEvt.Reset() |> ignore
        cts.Dispose()
        cts <- new CancellationTokenSource()
        loop ()

    loop ()
