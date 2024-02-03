module Expects

open System.Text
open ElmishLand.Base

type LogOutput = {
    Info: StringBuilder
    Debug: StringBuilder
    Error: StringBuilder
}

module LogOutput =
    let asString (logs: LogOutput) =
        $"""Debug:
%s{logs.Debug.ToString() |> String.indentLines}

Info:
%s{logs.Info.ToString() |> String.indentLines}

Error:
%s{logs.Error.ToString() |> String.indentLines}"""


let ok logs (x: Result<'a, 'e>) : 'a =
    match x with
    | Ok x -> x
    | Error e -> failwithf $"Expected Ok. Got Error '%A{e}'\n%s{LogOutput.asString logs}"

let equals logs expected actual =
    if expected <> actual then
        failwithf $"Expected %A{expected}. Got %A{actual}\n%s{LogOutput.asString logs}"

let equalsIgnoringWhitespace logs expected actual =
    if
        (expected |> String.eachLine String.trimWhitespace)
        <> (actual |> String.eachLine String.trimWhitespace)
    then
        failwithf $"Expected %A{expected}. Got %A{actual}\n%s{LogOutput.asString logs}"
