module ElmishLand.DotNetCli

open System
open System.Threading
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.Log
open ElmishLand.AppError
open Orsak

let getLatestDotnetSdkVersion () =
    eff {
        let! log = Log().Get()

        let! output =
            runProcess
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
