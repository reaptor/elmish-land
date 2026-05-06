module ElmishLand.DotNetCli

open System
open System.Threading
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.AppError
open Orsak

let getDotnetSdkVersion workingDirectory =
    eff {
        let! versionOutput =
            runProcess false workingDirectory "dotnet" [| "--version" |] CancellationToken.None ignore
            |> Effect.changeError (fun _ -> DotnetSdkNotFound)

        let version = fst versionOutput // Get stdout from tuple

        return!
            match DotnetSdkVersion.fromString version with
            | Some(DotnetSdkVersion version) when version >= (DotnetSdkVersion.value minimumRequiredDotnetSdk) ->
                Ok(DotnetSdkVersion version)
            | _ -> Error DotnetSdkNotFound
    }

let getLatestInstalledDotnetSdkVersion workingDirectory =
    eff {
        let! sdkOutput =
            runProcess false workingDirectory "dotnet" [| "--list-sdks" |] CancellationToken.None ignore
            |> Effect.changeError (fun _ -> DotnetSdkNotFound)

        let stdout = fst sdkOutput

        let versions =
            stdout.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.choose (fun line ->
                let trimmed = line.Trim()
                let space = trimmed.IndexOf(' ')

                if space <= 0 then
                    None
                else
                    DotnetSdkVersion.fromString (trimmed.Substring(0, space)))
            |> Array.filter (fun v -> DotnetSdkVersion.value v >= DotnetSdkVersion.value minimumRequiredDotnetSdk)
            |> Array.sortByDescending DotnetSdkVersion.value

        return!
            match Array.tryHead versions with
            | Some latest -> Ok latest
            | None -> Error DotnetSdkNotFound
    }
