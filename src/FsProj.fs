module ElmishLand.FsProj

open System.IO
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.AppError

let validate (projectDir: AbsoluteProjectDir) =
    let formatError lineNr (line: string) (FilePath filePath) msg =
        let projectPath = FsProjPath.fromProjectDir projectDir
        let colNr = line.IndexOf(filePath)
        $"%s{FsProjPath.asString projectPath}({lineNr},{colNr}) error: %s{msg}."

    let includedFSharpFileInfo =
        File.ReadAllLines(FsProjPath.fromProjectDir projectDir |> FsProjPath.asString)
        |> Array.mapi (fun lineNr line -> (lineNr + 1, line))
        |> Array.choose (fun (lineNr, line) ->
            let m = Regex.Match(line, """\s*<[Cc]ompile\s+[Ii]nclude="([^"]+)" """.TrimEnd())

            if m.Success then
                Some(lineNr, line, m.Groups[1].Value |> FilePath.fromString |> FilePath.ensureRelativeTo projectDir)
            else
                None)
        |> Array.toList

    let pagesDir =
        projectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.appendParts [ "src"; "Pages" ]

    let actualPageFiles =
        Directory.GetFiles(FilePath.asString pagesDir, "Page.fs", SearchOption.AllDirectories)
        |> Array.map FilePath.fromString
        |> Array.map (FilePath.ensureRelativeTo projectDir)
        |> Set.ofArray

    let includedPageFiles =
        includedFSharpFileInfo
        |> List.map (fun (_, _, filePath) -> filePath)
        |> List.filter (fun x -> (FileName.fromFilePath x |> FileName.asString) = "Page.fs")
        |> Set.ofList

    let pageFilesMissingFromFsProj = Set.difference actualPageFiles includedPageFiles

    let errors: string list = [
        match includedFSharpFileInfo with
        | (lineNr, line, filePath) :: _ when (FilePath.asString filePath) <> "src/Routes.fs" ->
            formatError
                lineNr
                line
                filePath
                $"'%s{AbsoluteProjectDir.asString projectDir}/src/Routes.fs' must be the first file in the project"
        | _ -> ()

        yield!
            includedFSharpFileInfo
            |> List.fold
                (fun (prevFilePaths: FilePath list, errors) (lineNr, line, filePath) ->
                    let dir = FilePath.directoryPath filePath

                    match prevFilePaths with
                    | prevFilePath :: _ when
                        (FilePath.fromString "src" |> FilePath.equals dir |> not)
                        && dir <> FilePath.directoryPath prevFilePath
                        && prevFilePaths |> List.map FilePath.directoryPath |> List.contains dir
                        ->
                        filePath :: prevFilePaths,
                        formatError
                            lineNr
                            line
                            filePath
                            "Files in the same directory must be located directly before or after each other"
                        :: errors
                    | prevFilePath :: _ when
                        FileName.fromFilePath prevFilePath |> FileName.equalsString "Page.fs"
                        && FilePath.directoryPath prevFilePath |> FilePath.equals dir
                        ->
                        filePath :: prevFilePaths,
                        formatError (lineNr - 1) line filePath "Page.fs files must be the last file in the directory"
                        :: errors
                    | _ -> filePath :: prevFilePaths, errors)
                ([], [])
            |> snd
            |> List.rev

        match List.tryLast includedFSharpFileInfo with
        | Some(lineNr, line, filePath) when "src/App.fs" |> FilePath.fromString |> FilePath.equals filePath |> not ->
            formatError
                lineNr
                line
                filePath
                $"'%s{AbsoluteProjectDir.asString projectDir}/src/App.fs' must be the last file in the project"
        | _ -> ()

        for pageFile in pageFilesMissingFromFsProj do
            $"""The page '%s{AbsoluteProjectDir.asString projectDir}/%s{FilePath.asString pageFile}' is missing from the project file. Please add the file to the project using an IDE
or add the following line to a ItemGroup in the project file '%s{projectDir |> FsProjPath.fromProjectDir |> FsProjPath.asString}':

<Compile Include="%s{FilePath.asString pageFile}" />
   """
    ]

    match errors with
    | [] -> Ok()
    | errors -> Error(FsProjValidationError errors)
