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

        let settingsFiles =
            Directory.EnumerateFiles(
                AbsoluteProjectDir.asString absoluteProjectDir,
                "elmish-land.json",
                SearchOption.AllDirectories
            )

        if Seq.isEmpty settingsFiles then
            return! Error AppError.ElmishLandProjectMissing
        else
            for settingsFile in settingsFiles do
                let subAbsoluteProjectDir =
                    settingsFile
                    |> FilePath.fromString
                    |> FilePath.directoryPath
                    |> AbsoluteProjectDir

                do! generate subAbsoluteProjectDir dotnetSdkVersion
                do! validate subAbsoluteProjectDir
    }
