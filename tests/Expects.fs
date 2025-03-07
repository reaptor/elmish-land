module Expects

open System.Text
open ElmishLand.Base
open Xunit
open Xunit.Sdk

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

let equalsWLogs (logs: LogOutput) expected actual =
    if expected <> actual then
        try
            Assert.Equivalent(expected, actual) // Equivalent makes a object comparion and prints the diffing properties on the object.
        with ex ->
            raise
            <| XunitException($"Expected %A{expected}.\n\nActual %A{actual}\n\nLogs:\n%s{LogOutput.asString logs}", ex)

let equals expected actual = Assert.Equivalent(expected, actual) // Equivalent makes a object comparion and prints the diffing properties on the object.

let equalsIgnoringWhitespace logs expected actual =
    if
        (expected |> String.eachLine String.trimWhitespace)
        <> (actual |> String.eachLine String.trimWhitespace)
    then
        failwithf $"Expected %A{expected}.\n\nActual %A{actual}\n\nLogs:\n%s{LogOutput.asString logs}"
