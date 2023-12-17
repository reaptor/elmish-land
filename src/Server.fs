module ElmishLand.Server

open System.IO
open System.Threading
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.AppError
open Orsak

let server (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Effect.getLogger ()
        let routeData = getRouteData projectDir
        do! generateRoutesAndApp projectDir routeData

        do! validate projectDir

        let workingDir = AbsoluteProjectDir.asFilePath projectDir

        let mutable isViteRunning = false
        let mutable isParsing = false

        do!
            runProcess workingDir "dotnet" [| "fable"; "watch"; "--run"; "vite" |] CancellationToken.None (fun output ->
                let m = Regex.Match(output, "(http[s]?://[^:]+:\d+)")

                if m.Success then
                    isViteRunning <- true
                    $"""%s{getCommandHeader $"is ready at %s{m.Groups[1].Value}"}""" |> log.Info

                if isViteRunning then
                    let m = Regex.Match(output, "Started Fable compilation")

                    if m.Success then
                        log.Info "Compiling ..."

                    let m = Regex.Match(output, "Fable compilation finished in ([^\s]+)")

                    if m.Success then
                        log.Info $"Compiled in %s{m.Groups[1].Value}"

                    if Regex.IsMatch(output, "Parsing") then
                        isParsing <- true
                        log.Info "Project file changed... "

                    if isParsing && Regex.IsMatch(output, "Watching") then
                        isParsing <- false
                        log.Info "done")
            |> Effect.map ignore<string>
    }
// |> handleAppResult projectDir ignore
