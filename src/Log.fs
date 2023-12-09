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

    let writeLine (message: string) args =
        let formattedMsg =
            args
            |> Array.fold
                (fun (sb: StringBuilder) arg ->
                    let i = sb.ToString().IndexOf("{}")
                    sb.Replace("{}", (if arg = null then "null" else $"%A{arg}"), i, 2))
                (StringBuilder(message))

        let time = DateTime.Now.ToString("HH:mm:ss.fff")
        Console.WriteLine $"%s{time} %s{path}(%i{line}): %s{memberName}: %s{formattedMsg.ToString()}"

    let isEnabled = Environment.CommandLine.Contains("--verbose")

    member _.Info(message, [<ParamArray>] args: obj array) =
        if isEnabled then
            Console.ForegroundColor <- ConsoleColor.Gray
            writeLine message args
            Console.ResetColor()

    member _.Error(message, [<ParamArray>] args: obj array) =
        if isEnabled then
            Console.ForegroundColor <- ConsoleColor.Red
            writeLine message args
            Console.ResetColor()
