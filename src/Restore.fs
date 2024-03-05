module ElmishLand.Restore

open System.IO
open Orsak
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.FsProj
open ElmishLand.Generate
open ElmishLand.Base

let restore absoluteProjectDir =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        let elmishLandJsonFiles =
            Directory.EnumerateFiles(
                AbsoluteProjectDir.asString absoluteProjectDir,
                "elmish-land.json",
                SearchOption.AllDirectories
            )

        if Seq.isEmpty elmishLandJsonFiles then
            return! Error AppError.ElmishLandProjectMissing
        else
            for elmishLandJson in elmishLandJsonFiles do
                let subAbsoluteProjectDir =
                    elmishLandJson
                    |> FilePath.fromString
                    |> FilePath.directoryPath
                    |> AbsoluteProjectDir

                do! generate subAbsoluteProjectDir dotnetSdkVersion
                do! validate subAbsoluteProjectDir
    }
