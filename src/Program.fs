open System
open System.IO
open System.Text.RegularExpressions
open Argu
open ElmishLand.Init
open ElmishLand.Server
open ElmishLand.Build
open ElmishLand.Help

type AddArgs =
    | Page
    | Layout

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Page -> "add a new page."
            | Layout -> "add a new layout."

type CliArguments =
    | Init of projectName: string
    | Server
    | Build
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AddArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Init _ -> "create a new project."
            | Server -> "run a local dev server."
            | Build -> "build your app for production."
            | Add _ -> "add pages or layouts to your project."

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ "init"; projectName ] ->
        init projectName
        0
    | [ "server" ] -> server None
    | [ "server"; workingDirectory ] -> server (Some workingDirectory)
    | [ "build" ] -> build None
    | [ "build"; workingDirectory ] -> build (Some workingDirectory)
    | [ "add page"; url ] -> 0
    | [ "add layout"; name ] -> 0
    | [ "routes" ] -> 0
    | _ ->
        help ()
        0
