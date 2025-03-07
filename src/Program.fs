﻿module ElmishLand.Program

open System
open System.IO
open ElmishLand.Effect
open Orsak
open ElmishLand.Log
open ElmishLand.Base
open ElmishLand.AppError
open ElmishLand.Init
open ElmishLand.Restore
open ElmishLand.Server
open ElmishLand.Build
open ElmishLand.AddPage
open ElmishLand.AddLayout

let (|NotFlag|_|) (x: string) =
    if x.StartsWith("--") then None else Some x

let run argv =
    eff {
        let! log = Log().Get()

        return!
            match List.ofArray argv with
            | "init" :: _ -> init (AbsoluteProjectDir.create argv)
            | "server" :: _ -> server (AbsoluteProjectDir.create argv)
            | "build" :: _ -> build (AbsoluteProjectDir.create argv)
            | "restore" :: _ -> restore (AbsoluteProjectDir.create argv)
            | "add" :: "page" :: NotFlag url :: _ -> addPage (AbsoluteProjectDir.create argv) url
            | "add" :: "layout" :: NotFlag url :: _ -> addLayout (AbsoluteProjectDir.create argv) url
            | _ ->
                $"""
    %s{getWelcomeTitle ()}
%s{help id}
"""
                |> log.Info

                eff { return () }

    }

type ConsoleLogger(memberName, path, line) =
    let logger = Logger(memberName, path, line)

    interface ILog with
        member _.Debug(message, [<ParamArray>] args: obj array) =
            if logger.IsVerbose then
                Console.ForegroundColor <- ConsoleColor.Gray
                logger.WriteLine Console.Out.WriteLine message args
                Console.ResetColor()

        member _.Info(message, [<ParamArray>] args: obj array) =
            Console.ForegroundColor <- ConsoleColor.Gray
            logger.WriteLine Console.Out.WriteLine message args
            Console.ResetColor()

        member _.Error(message, [<ParamArray>] args: obj array) =
            Console.ForegroundColor <- ConsoleColor.Red
            logger.WriteLine Console.Error.WriteLine message args
            Console.ResetColor()

let handleAppResult (log: ILog) onSuccess =
    function
    | Ok _ ->
        onSuccess ()
        0
    | Error e ->
        match e with
        | ProcessError(error) -> log.Error error
        | FsProjNotFound -> log.Error "No F# project file found."
        | MultipleFsProjFound -> log.Error "Multiple F# project files found."
        | FsProjValidationError errors ->
            for error in errors do
                log.Error error

        | DotnetSdkNotFound ->
            log.Error
                $"""You need to install .NET Core SDK version %s{DotnetSdkVersion.asString minimumRequiredDotnetSdk} or above
https://dotnet.microsoft.com/
"""

        | NodeNotFound ->
            log.Error
                $"""You need to install Node.js version %s{minimumRequiredNode.ToString()} or above
https://nodejs.org/
"""

        | PagesDirectoryMissing ->
            log.Error
                """src/Pages directory is missing. Please run 'elmish-land init'.
"""

        | ViteNotInstalled ->
            log.Error
                """Vite.js is missing. Please install vite (npm install vite --save-dev)'.
"""
        | ElmishLandProjectMissing ->
            log.Error
                """Could not find any elmish-land projects. Create a new project with 'dotnet elmish-land init'.
"""
        | InvalidSettings e ->
            log.Error
                $"""The elmish-land.json configuration file is invalid. %s{e}'.
"""
        | MissingMainLayout ->
            log.Error
                """The main layout file is missing. Create it with "dotnet elmish-land add layout /"'.
"""

        -1


[<EntryPoint>]
let main argv =
    (task {
        let! result =
            run argv
            |> Effect.run
                { new IEffectEnv with
                    member _.GetLogger(memberName, path, line) = ConsoleLogger(memberName, path, line)

                    member _.FilePathExists(filePath, isDirectory: bool) =
                        if isDirectory then
                            Directory.Exists(FilePath.asString filePath)
                        else
                            FilePath.exists filePath

                    member _.GetParentDirectory(filePath) = FilePath.parent filePath

                    member _.GetFilesRecursive(FilePath filePath, searchPattern) =
                        Directory.GetFiles(filePath, searchPattern, EnumerationOptions(RecurseSubdirectories = true))
                        |> Array.map FilePath.fromString

                    member _.ReadAllText(FilePath filePath) = File.ReadAllText(filePath)
                }

        return handleAppResult (ConsoleLogger("", "", 0)) ignore result
    })
        .Result
