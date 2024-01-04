module ElmishLand.Server

open System.Threading
open System.Text.Json
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.FsProj
open ElmishLand.Process
open ElmishLand.Generate
open ElmishLand.DotNetCli
open Orsak
open System.Text

let settingsArrayToHtmlElements (name: string) close (arr: JsonElement array) =
    arr
    |> Array.fold
        (fun xs elem ->
            let sb = StringBuilder()
            sb.Append($"<%s{name} ") |> ignore

            for x in elem.EnumerateObject() do
                sb.Append($"""%s{x.Name}="%s{x.Value.GetString()}" """) |> ignore

            sb.Remove(sb.Length - 1, 1) |> ignore

            if close then
                sb.Append($"></%s{name}>") |> ignore
            else
                sb.Append(">") |> ignore

            sb.ToString() :: xs)
        []

let server (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersionToUse ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate projectDir dotnetSdkVersion

        do! validate projectDir

        let workingDir = AbsoluteProjectDir.asFilePath projectDir

        let mutable isViteRunning = false
        let mutable isParsing = false

        do!
            runProcess
                true
                workingDir
                "dotnet"
                [|
                    "fable"
                    "watch"
                    ".elmish-land/App/App.fsproj"
                    "--run"
                    "vite"
                    "--config"
                    "vite.config.js"
                |]
                CancellationToken.None
                (fun output ->
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
