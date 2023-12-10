open System
open ElmishLand.Base
open ElmishLand.Init
open ElmishLand.Upgrade
open ElmishLand.Server
open ElmishLand.Build
open ElmishLand.AddPage

let (|NotFlag|_|) (x: string) =
    if x.StartsWith("--") then None else Some x

[<EntryPoint>]
let main argv =
    try
        match List.ofArray argv with
        | "init" :: NotFlag projectDir :: _ ->
            init (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "init" :: _ -> init AbsoluteProjectDir.defaultProjectDir
        | "upgrade" :: NotFlag projectDir :: _ ->
            upgrade (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "upgrade" :: _ -> init AbsoluteProjectDir.defaultProjectDir
        | "server" :: NotFlag projectDir :: _ ->
            server (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "server" :: _ -> server AbsoluteProjectDir.defaultProjectDir
        | "build" :: NotFlag projectDir :: _ ->
            build (projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath)
        | "build" :: _ -> build AbsoluteProjectDir.defaultProjectDir
        | "add" :: "page" :: NotFlag url :: _ -> addPage url
        | "add" :: "layout" :: url :: _ -> 0
        | "routes" :: _ -> 0
        | _ ->
            $"""
%s{welcomeTitle.Value}
%s{help id}
"""
            |> indent
            |> printfn "%s"

            0
    with ex ->
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine(ex.ToString())
        Console.ResetColor()
        -1
