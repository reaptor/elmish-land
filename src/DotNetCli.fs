module ElmishLand.DotNetCli

open System
open System.Threading
open System.IO
open System.Text.RegularExpressions
open System.Text.Json
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.Log
open ElmishLand.AppError
open Orsak

let dotnetVersionFromGlobalJson () =
    if File.Exists("global.json") then
        let doc = JsonDocument.Parse(File.ReadAllText("global.json"))

        requireJson "sdk" doc.RootElement
        |> Result.bind (requireJson "version")
        |> Result.map (fun x -> x.GetString())
        |> Result.toOption
        |> Option.bind DotnetSdkVersion.fromString
    else
        None

let getLatestDotnetSdkVersion () =
    eff {
        let! log = Log().Get()

        let! output =
            runProcess
                false
                (FilePath.fromString Environment.CurrentDirectory)
                "dotnet"
                [| "--list-sdks" |]
                CancellationToken.None
                ignore
            |> Effect.changeError (fun _ -> AppError.DotnetSdkNotFound)

        return!
            output
            |> String.asLines
            |> Array.choose (fun line ->
                match DotnetSdkVersion.fromString (Regex.Match(line, "\d.\d.\d{3}").Value) with
                | Some(DotnetSdkVersion version) when version >= (DotnetSdkVersion.value minimumRequiredDotnetSdk) ->
                    Some(DotnetSdkVersion version)
                | _ -> None)
            |> fun sdkVersions ->
                if Array.isEmpty sdkVersions then
                    log.Error("Found no installed dotnet SDKs")
                    Error DotnetSdkNotFound
                else
                    sdkVersions |> Seq.max |> Ok

    }

let getDotnetSdkVersionToUse () =
    match dotnetVersionFromGlobalJson () with
    | Some x -> eff { return! Ok x }
    | None -> getLatestDotnetSdkVersion ()
