module ElmishLand.DotNetCli

open System
open System.IO
open System.Threading
open System.Text.RegularExpressions
open System.Runtime.InteropServices
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.Log
open ElmishLand.AppError

let checkIfDotnetIsInstalled () =
    if
        (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
         && not (File.Exists("C:\\program files\\dotnet\\dotnet.exe")))
        || (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            && not (File.Exists("/usr/local/share/dotnet/dotnet")))
        || (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            && not (File.Exists("/home/user/share/dotnet/dotnet")))
    then
        Error AppError.DotnetSdkNotFound
    else
        Ok()

let getLatestDotnetSdkVersion () =
    let log = Log()

    result {
        do! checkIfDotnetIsInstalled ()

        let! output =
            runProcess
                (FilePath.fromString Environment.CurrentDirectory)
                "dotnet"
                [| "--list-sdks" |]
                CancellationToken.None
                ignore
            |> Result.mapError (fun _ -> AppError.DotnetSdkNotFound)

        return!
            output.Split(Environment.NewLine)
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
