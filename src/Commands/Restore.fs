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

        let fsProjFiles =
            Directory.EnumerateFiles(
                AbsoluteProjectDir.asString absoluteProjectDir,
                "*.fsproj",
                SearchOption.AllDirectories
            )
            |> Seq.filter (fun x -> (x |> FilePath.fromString |> FsProjPath |> getElmishLandPropertyGroup).IsSome)

        if Seq.isEmpty fsProjFiles then
            return! Error AppError.ElmishLandProjectMissing
        else
            for fsProj in fsProjFiles do
                let subAbsoluteProjectDir =
                    fsProj
                    |> FilePath.fromString
                    |> FilePath.directoryPath
                    |> AbsoluteProjectDir

                do! generate subAbsoluteProjectDir dotnetSdkVersion
                do! validate subAbsoluteProjectDir
    }
