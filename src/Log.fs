module ElmishLand.Log

open System
open System.Text
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

type Log
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
        let formattedMsg =
            args
            |> Array.fold
                (fun (sb: StringBuilder) arg ->
                    let i = sb.ToString().IndexOf("{}")
                    sb.Replace("{}", (if arg = null then "null" else $"%A{arg}"), i, 2))
                (StringBuilder(message))

        if isVerbose then
            let time = DateTime.Now.ToString("HH:mm:ss.fff")
            $"%s{time} %s{path}(%i{line}): %s{memberName}: %s{formattedMsg.ToString()}"
        else
            $"%s{formattedMsg.ToString()}"
        |> indent
        |> Console.WriteLine

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
