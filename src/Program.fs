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

type private Log
    (
        [<CallerMemberName; Optional; DefaultParameterValue("")>] memberName: string,
        [<CallerFilePath; Optional; DefaultParameterValue("")>] path: string,
        [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line: int
    ) =
    let path = path.Replace($"%s{__SOURCE_DIRECTORY__}", "")[1..]

    let isVerbose = Environment.CommandLine.Contains("--verbose")

    let indent (s: string) =
        s.Split('\n')
        |> Array.map (fun line -> $"  %s{line}")
        |> String.concat Environment.NewLine

    let writeLine (message: string) args =
        let formattedMsg = formatMessage message args

        if isVerbose then
            let time = DateTime.Now.ToString("HH:mm:ss.fff")
            $"%s{time} %s{path}(%i{line}): %s{memberName}: %s{formattedMsg}"
        else
            $"%s{formattedMsg}"
        |> indent
        |> Console.Out.WriteLine

    static member val Out = Console.Out with get, set

    interface ILog with
        member _.Debug(message, [<ParamArray>] args: obj array) =
            if isVerbose then
                Console.ForegroundColor <- ConsoleColor.Gray
                writeLine message args
                Console.ResetColor()

        member _.Info(message, [<ParamArray>] args: obj array) =
            Console.ForegroundColor <- ConsoleColor.Gray
            writeLine message args
            Console.ResetColor()

        member _.Error(message, [<ParamArray>] args: obj array) =
            Console.ForegroundColor <- ConsoleColor.Red
            writeLine message args
            Console.ResetColor()

let run argv =
    eff {
        let! log = Effect.getLogger ()

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

[<EntryPoint>]
let main argv =
    (task {
        let! result =
            run argv
            |> Effect.run
                { new ILogProvider with
                    member _.GetLogger() = Log()
                }

        return handleAppResult (Log()) ignore result
    })
        .Result
