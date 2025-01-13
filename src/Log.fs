module ElmishLand.Log

open System
open System.Text
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Orsak
open ElmishLand.Base

let private formatMessage (message: string) args =
    args
    |> Array.fold
        (fun (sb: StringBuilder) arg ->
            let i = sb.ToString().IndexOf("{}")
            sb.Replace("{}", (if arg = null then "null" else $"%A{arg}"), i, 2))
        (StringBuilder(message))
    |> _.ToString()

type Logger(memberName: string, path: string, line: int) =
    let path = path.Replace($"%s{__SOURCE_DIRECTORY__}", "")[1..]

    let indent (s: string) =
        String.asLines s |> Array.map (fun line -> $"  %s{line}") |> String.concat "\n"

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
