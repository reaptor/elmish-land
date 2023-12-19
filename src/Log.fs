module ElmishLand.Log

open System
open System.Text
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Orsak

let private formatMessage (message: string) args =
    args
    |> Array.fold
        (fun (sb: StringBuilder) arg ->
            let i = sb.ToString().IndexOf("{}")
            sb.Replace("{}", (if arg = null then "null" else $"%A{arg}"), i, 2))
        (StringBuilder(message))
    |> _.ToString()

type ILog =
    abstract member Debug: message: string * [<ParamArray>] args: obj array -> unit
    abstract member Info: message: string * [<ParamArray>] args: obj array -> unit
    abstract member Error: message: string * [<ParamArray>] args: obj array -> unit

type ILogProvider =
    abstract member GetLogger: string * string * int -> ILog

type Log
    (
        [<CallerMemberName; Optional; DefaultParameterValue("")>] memberName: string,
        [<CallerFilePath; Optional; DefaultParameterValue("")>] path: string,
        [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line: int
    ) =
    member _.Get() =
        Effect.Create(fun (provider: #ILogProvider) -> provider.GetLogger(memberName, path, line))

type Logger(memberName: string, path: string, line: int) =
    let path = path.Replace($"%s{__SOURCE_DIRECTORY__}", "")[1..]

    let indent (s: string) =
        s.Split('\n')
        |> Array.map (fun line -> $"  %s{line}")
        |> String.concat Environment.NewLine

    member _.IsVerbose = Environment.CommandLine.Contains("--verbose")

    member this.WriteLine (writeLine: string -> unit) (message: string) args =
        let formattedMsg = formatMessage message args

        if this.IsVerbose then
            let time = DateTime.Now.ToString("HH:mm:ss.fff")
            $"%s{time} %s{path}(%i{line}): %s{memberName}: %s{formattedMsg}"
        else
            $"%s{formattedMsg}"
        |> indent
        |> writeLine
