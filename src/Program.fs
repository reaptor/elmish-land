open ElmishLand.Base
open ElmishLand.Init
open ElmishLand.Server
open ElmishLand.Build

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ "init"; projectDir ] ->
        init projectDir
        0
    | [ "server" ] -> server None
    | [ "server"; workingDirectory ] -> server (Some workingDirectory)
    | [ "build" ] -> build None
    | [ "build"; workingDirectory ] -> build (Some workingDirectory)
    | [ "add page"; url ] -> 0
    | [ "add layout"; name ] -> 0
    | [ "routes" ] -> 0
    | _ ->
        let welcomeTitle = $"Welcome to %s{appTitle}! (v%s{version})"
        printfn $"""
    %s{welcomeTitle}
    %s{String.init welcomeTitle.Length (fun _ -> "-")}
    %s{help id}"""
        0
