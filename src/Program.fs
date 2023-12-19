module ElmishLand.Program

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open ElmishLand.Base
open ElmishLand.Init
open ElmishLand.Log
open ElmishLand.Upgrade
open ElmishLand.Server
open ElmishLand.Build
open ElmishLand.AddPage
open ElmishLand.AppError
open Orsak

let (|NotFlag|_|) (x: string) =
    if x.StartsWith("--") then None else Some x

let run argv =
    eff {
        let! log = Log().Get()

        return!
            match List.ofArray argv with
            | "init" :: NotFlag projectDir :: _ ->
                init (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
            | "init" :: _ -> init (AbsoluteProjectDir.getDefaultProjectDir ())
            | "upgrade" :: NotFlag projectDir :: _ ->
                upgrade (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
            | "upgrade" :: _ -> init (AbsoluteProjectDir.getDefaultProjectDir ())
            | "server" :: NotFlag projectDir :: _ ->
                server (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
            | "server" :: _ -> server (AbsoluteProjectDir.getDefaultProjectDir ())
            | "build" :: NotFlag projectDir :: _ ->
                build (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
            | "build" :: _ -> build (AbsoluteProjectDir.getDefaultProjectDir ())
            | "add" :: "page" :: NotFlag url :: _ -> addPage url
            | "add" :: "layout" :: url :: _ -> eff { return () }
            | "routes" :: _ -> eff { return () }
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


[<EntryPoint>]
let main argv =
    (task {
        let! result =
            run argv
            |> Effect.run
                { new ILogProvider with
                    member _.GetLogger(memberName, path, line) = ConsoleLogger(memberName, path, line)
                }

        return handleAppResult (ConsoleLogger("", "", 0)) ignore result
    })
        .Result
