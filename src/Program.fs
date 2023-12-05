open ElmishLand.Base
open ElmishLand.Init
open ElmishLand.Server
open ElmishLand.Build

[<EntryPoint>]
let main argv =
    let welcomeTitle = $"Welcome to %s{appTitle}"
    printfn "%s" welcomeTitle

    match List.ofArray argv with
    | [ "init" ] -> init AbsoluteProjectDir.defaultProjectDir
    | [ "init"; projectDir ] -> init (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
    | [ "server" ] -> server AbsoluteProjectDir.defaultProjectDir
    | [ "server"; projectDir ] -> server (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
    | [ "build" ] -> build AbsoluteProjectDir.defaultProjectDir
    | [ "build"; projectDir ] -> build (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
    | [ "add page"; url ] -> 0
    | [ "add layout"; name ] -> 0
    | [ "routes" ] -> 0
    | _ ->
        printfn
            $"""
    %s{String.init welcomeTitle.Length (fun _ -> "-")}
    %s{help id}"""

        0
