module Expects

open System
open System.Text
open Xunit


type LogOutput = {
    Info: StringBuilder
    Debug: StringBuilder
    Error: StringBuilder
}

module LogOutput =
    let private indent (s: string) =
        s.Split(Environment.NewLine)
        |> Array.map (fun line -> $"  %s{line}")
        |> String.concat Environment.NewLine

    let asString (logs: LogOutput) =
        $"""Debug:
%s{logs.Debug.ToString() |> indent}

Info:
%s{logs.Info.ToString() |> indent}

Error:
%s{logs.Error.ToString() |> indent}"""


let ok logs (x: Result<'a, 'e>) : 'a =
    match x with
    | Ok x -> x
    | Error e -> failwithf $"Expected Ok. Got Error '%A{e}'\n%s{LogOutput.asString logs}"

let equals logs expected actual =
    if expected <> actual then
        failwithf $"Expected %A{expected}. Got %A{actual}\n%s{LogOutput.asString logs}"
