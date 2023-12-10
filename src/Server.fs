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

        let workingDir = AbsoluteProjectDir.asFilePath projectDir

        result {
            do! validate projectDir

            do!
                [
                    "dotnet", [| "tool"; "restore" |]
                    "dotnet", [| "restore" |]
                    "npm", [| "install" |]
                ]
                |> List.map (fun (cmd, args) -> workingDir, cmd, args, cts.Token, ignore)
                |> runProcesses

            do!
                runProcess workingDir "dotnet" [| "fable"; "--run"; "vite" |] cts.Token (fun output ->
                    let m = Regex.Match(output, "Local:   (http://localhost:\d+)")

                    if m.Success then
                        $"""%s{commandHeader $"is ready at %s{m.Groups[1].Value}"}"""
                        |> indent
                        |> printfn "%s")
                |> Result.map ignore<string>
        }
        |> handleAppResult projectDir ignore
        |> ignore<int>

        resetEvt.WaitOne() |> ignore
        resetEvt.Reset() |> ignore
        cts.Dispose()
        cts <- new CancellationTokenSource()
        loop ()

    loop ()
