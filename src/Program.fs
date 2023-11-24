open System
open System.IO
open System.Text.RegularExpressions
open Argu
open ElmishLand.Init
open ElmishLand.Server
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

let validateFsProj (fsProjPath: string) =
    let formatError lineNr (line: string) (filepath: string) msg =
        let colNr = line.IndexOf(filepath)
        $"%s{fsProjPath}({lineNr},{colNr}) error: %s{msg}."

    let printError (error: string) =
        Console.ForegroundColor <- ConsoleColor.DarkRed
        Console.Error.WriteLine(error)
        Console.ResetColor()

    let lines =
        File.ReadAllLines(fsProjPath)
        |> Array.mapi (fun lineNr line -> (lineNr + 1, line))
        |> Array.choose (fun (lineNr, line) ->
            let m = Regex.Match(line, """\s*<[Cc]ompile\s+[Ii]nclude="([^"]+)" """.TrimEnd())

            if m.Success then
                Some(lineNr, line, m.Groups[1].Value)
            else
                None)
        |> Array.toList

    match lines with
    | (lineNr, line, filePath) :: _ when filePath <> "Routes.fs" ->
        formatError lineNr line filePath "Routes.fs must be the first file in the project"
        |> printError
    | _ -> ()

    lines
    |> List.fold
        (fun (prevFilePaths: string list, errors) (lineNr, line, filePath) ->
            let dir = Path.GetDirectoryName(filePath)

            match prevFilePaths with
            | prevFilePath :: _ when
                dir <> ""
                && dir <> Path.GetDirectoryName(prevFilePath)
                && prevFilePaths |> List.map Path.GetDirectoryName |> List.contains dir
                ->
                filePath :: prevFilePaths,
                formatError
                    lineNr
                    line
                    filePath
                    "Files in the same directory must be located directly before or after each other"
                :: errors
            | prevFilePath :: _ when
                Path
                    .GetFileName(prevFilePath)
                    .Equals("Page.fs", StringComparison.InvariantCultureIgnoreCase)
                && dir = Path.GetDirectoryName(prevFilePath)
                ->
                filePath :: prevFilePaths,
                formatError (lineNr - 1) line filePath "Page.fs files must be the last file in the directory"
                :: errors
            | _ -> filePath :: prevFilePaths, errors)
        ([], [])
    |> snd
    |> List.rev
    |> List.iter printError

    match List.tryLast lines with
    | Some(lineNr, line, filePath) when filePath <> "App.fs" ->
        formatError lineNr line filePath "App.fs must be the last file in the project"
        |> printError
    | _ -> ()


[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ "init"; projectName ] ->
        init projectName
        0
    | [ "server" ] -> server None
    | [ "server"; workingDirectory ] -> server (Some workingDirectory)
    | [ "build" ] -> 0
    | [ "add page"; url ] -> 0
    | [ "add layout"; name ] -> 0
    | [ "routes" ] -> 0
    | _ ->
        help ()
        0
