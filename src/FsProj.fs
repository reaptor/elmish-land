module ElmishLand.FsProj

open System
open System.IO
open System.Text.RegularExpressions

let projectPath workingDirectory =
    let projectDir =
        match workingDirectory with
        | Some workingDirectory' -> Path.Combine(Environment.CurrentDirectory, workingDirectory')
        | None -> Environment.CurrentDirectory

    Path.ChangeExtension(Path.Combine(projectDir, DirectoryInfo(projectDir).Name), "fsproj")

let validate projectPath =
    let formatError lineNr (line: string) (filepath: string) msg =
        let colNr = line.IndexOf(filepath)
        $"%s{projectPath}({lineNr},{colNr}) error: %s{msg}."

    let printError (error: string) =
        Console.ForegroundColor <- ConsoleColor.Red
        Console.Error.WriteLine(error)
        Console.ResetColor()

    let lines =
        File.ReadAllLines(projectPath)
        |> Array.mapi (fun lineNr line -> (lineNr + 1, line))
        |> Array.choose (fun (lineNr, line) ->
            let m = Regex.Match(line, """\s*<[Cc]ompile\s+[Ii]nclude="([^"]+)" """.TrimEnd())

            if m.Success then
                Some(lineNr, line, m.Groups[1].Value.Replace("\\", "/"))
            else
                None)
        |> Array.toList

    let errors: string list = [
        match lines with
        | (lineNr, line, filePath) :: _ when filePath <> "src/Routes.fs" ->
            formatError lineNr line filePath "Routes.fs must be the first file in the project"
        | _ -> ()

        yield!
            lines
            |> List.fold
                (fun (prevFilePaths: string list, errors) (lineNr, line, filePath) ->
                    let dir = Path.GetDirectoryName(filePath)

                    match prevFilePaths with
                    | prevFilePath :: _ when
                        dir <> "src"
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

        match List.tryLast lines with
        | Some(lineNr, line, filePath) when filePath <> "src/App.fs" ->
            formatError lineNr line filePath "App.fs must be the last file in the project"
        | _ -> ()
    ]

    for error in errors do
        printError error

    if errors.Length = 0 then 0 else -1
