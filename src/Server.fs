module ElmishLand.Server

open System
open System.Threading
open System.Text.Json
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

        do!
            runProcess true workingDir "npm" [| "run"; "start" |] CancellationToken.None ignore
            |> Effect.map ignore<string>
    }
