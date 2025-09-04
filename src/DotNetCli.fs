module ElmishLand.DotNetCli

open System.Threading
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.AppError
open Orsak

let getDotnetSdkVersion () =
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
