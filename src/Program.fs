open System
open ElmishLand.Base
open ElmishLand.Init
open ElmishLand.Server
open ElmishLand.Build

let (|NonOption|_|) (x: string) =
    if x.StartsWith("--") then None else Some x

[<EntryPoint>]
let main argv =
    try
        match List.ofArray argv with
        | "init" :: NonOption projectDir :: _ ->
            init (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "init" :: _ -> init AbsoluteProjectDir.defaultProjectDir
        | "server" :: NonOption projectDir :: _ ->
            server (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "server" :: _ -> server AbsoluteProjectDir.defaultProjectDir
        | "build" :: NonOption projectDir :: _ ->
            build (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "build" :: _ -> build AbsoluteProjectDir.defaultProjectDir
        | "add" :: "page" :: url :: _ -> 0
        | "add" :: "layout" :: url :: _ -> 0
        | "routes" :: _ -> 0
        | _ ->
            let welcomeTitle = $"Welcome to %s{appTitle.Value}"

            printfn
                $"""
        %s{welcomeTitle}
        %s{String.init welcomeTitle.Length (fun _ -> "-")}
        %s{help id}"""

            0
    with ex ->
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine(ex.ToString())
        Console.ResetColor()
        -1
