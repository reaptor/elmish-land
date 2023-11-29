﻿open ElmishLand.Init
open ElmishLand.Server
open ElmishLand.Build
open ElmishLand.Help

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
