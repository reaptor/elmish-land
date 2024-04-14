module ElmishLand.FsProj

open Orsak
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.AppError

let validate absoluteProjectDir =
    eff {
        let! log = Log().Get()
        let! projectPath = FsProjPath.findExactlyOneFromProjectDir absoluteProjectDir

        log.Debug("Using {}", absoluteProjectDir)
        log.Debug("Using {}", projectPath)

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

        log.Debug("Using pagesDir {}", pagesDir)

        let actualPageFiles =
            Directory.GetFiles(FilePath.asString pagesDir, "Page.fs", SearchOption.AllDirectories)
            |> Array.map FilePath.fromString
            |> Array.map (FilePath.removePath (absoluteProjectDir |> AbsoluteProjectDir.asFilePath))
            |> Set.ofArray

        log.Debug("actualPageFiles {}", actualPageFiles)

        let includedPageFiles =
            includedFSharpFileInfo
            |> List.map (fun (_, _, filePath) -> filePath)
            |> List.filter (fun x -> (FileName.fromFilePath x |> FileName.asString) = "Page.fs")
            |> Set.ofList

        log.Debug("includedPageFiles {}", includedPageFiles)

        let pageFilesMissingFromFsProj = Set.difference actualPageFiles includedPageFiles

        let errors: string list = [
            yield!
                includedFSharpFileInfo
                |> List.fold
                    (fun (prevFilePaths: FilePath list, errors) (lineNr, line, filePath) ->
                        let dir = FilePath.directoryPath filePath

                        match prevFilePaths with
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
                $"""The page '%s{FilePath.asString pageFile}' is missing from the project file. Please add the file to the project using an IDE
    or add the following line to a ItemGroup in the project file '%s{FsProjPath.asString projectPath}':

    <Compile Include="%s{FilePath.asString pageFile}" />
       """
        ]

        return!
            match errors with
            | [] -> Ok()
            | errors -> Error(FsProjValidationError errors)
    }
