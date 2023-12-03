open ElmishLand.Base
open ElmishLand.Init
open ElmishLand.Server
open ElmishLand.Build

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ "init" ] ->
        init (ProjectDir.fromFilePath FilePath.currentDirectory)
        0
    | [ "init"; projectDir ] ->
        init (projectDir |> FilePath.fromString |> ProjectDir.fromFilePath)
        0
    | [ "server" ] -> server (ProjectDir.fromFilePath FilePath.currentDirectory)
    | [ "server"; projectDir ] -> server (projectDir |> FilePath.fromString |> ProjectDir.fromFilePath)
    | [ "build" ] -> build (ProjectDir.fromFilePath FilePath.currentDirectory)
    | [ "build"; projectDir ] -> build (projectDir |> FilePath.fromString |> ProjectDir.fromFilePath)
    | [ "add page"; url ] -> 0
    | [ "add layout"; name ] -> 0
    | [ "routes" ] -> 0
    | _ ->
        let welcomeTitle = $"Welcome to %s{appTitle}! (v%s{version})"

        printfn
            $"""
    %s{welcomeTitle}
    %s{String.init welcomeTitle.Length (fun _ -> "-")}
    %s{help id}"""

        0
