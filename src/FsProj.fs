module ElmishLand.FsProj

open System.IO
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.AppError

let validate absoluteProjectDir =
    result {
        let! projectPath = FsProjPath.findExactlyOneFromProjectDir absoluteProjectDir

        let formatError lineNr (line: string) (FilePath filePath) msg =
            let colNr = line.IndexOf(filePath)
            $"%s{FsProjPath.asString projectPath}(%i{lineNr},%i{colNr}) error: %s{msg}."

        let includedFSharpFileInfo =
            File.ReadAllLines(FsProjPath.asString projectPath)
            |> Array.mapi (fun lineNr line -> (lineNr + 1, line))
            |> Array.choose (fun (lineNr, line) ->
                let m = Regex.Match(line, """\s*<[Cc]ompile\s+[Ii]nclude="([^"]+)" """.TrimEnd())

                if m.Success then
                    Some(
                        lineNr,
                        line,
                        m.Groups[1].Value
                        |> FilePath.fromString
                        |> FilePath.removePath (absoluteProjectDir |> AbsoluteProjectDir.asFilePath)
                    )
                else
                    None)
            |> Array.toList

        let pagesDir =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts [ "src"; "Pages" ]

        let actualPageFiles =
            Directory.GetFiles(FilePath.asString pagesDir, "Page.fs", SearchOption.AllDirectories)
            |> Array.map FilePath.fromString
            |> Array.map (FilePath.removePath (absoluteProjectDir |> AbsoluteProjectDir.asFilePath))
            |> Set.ofArray

        let includedPageFiles =
            includedFSharpFileInfo
            |> List.map (fun (_, _, filePath) -> filePath)
            |> List.filter (fun x -> (FileName.fromFilePath x |> FileName.asString) = "Page.fs")
            |> Set.ofList

        let pageFilesMissingFromFsProj = Set.difference actualPageFiles includedPageFiles

        let errors: string list = [
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
                            formatError
                                (lineNr - 1)
                                line
                                filePath
                                "Page.fs files must be the last file in the directory"
                            :: errors
                        | _ -> filePath :: prevFilePaths, errors)
                    ([], [])
                |> snd
                |> List.rev

            for pageFile in pageFilesMissingFromFsProj do
                $"""The page '%s{FilePath.asString (absoluteProjectDir |> AbsoluteProjectDir.asFilePath)}/%s{FilePath.asString pageFile}' is missing from the project file. Please add the file to the project using an IDE
    or add the following line to a ItemGroup in the project file '%s{FsProjPath.asString projectPath}':

    <Compile Include="%s{FilePath.asString pageFile}" />
       """
        ]

        return!
            match errors with
            | [] -> Ok()
            | errors -> Error(FsProjValidationError errors)
    }
