module ElmishLand.Paket

open System.IO
open System.Text.RegularExpressions
open System.Threading
open Orsak
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.Log

let getPaketDependencies absoluteProjectDir =
    eff {
        let! log = Log().Get()

        let getPaketDependenciesFile filePath =
            filePath |> FilePath.appendParts [ "paket.dependencies" ] |> FilePath.asString

        let paketDependenciesPath =
            let path =
                getPaketDependenciesFile (AbsoluteProjectDir.asFilePath absoluteProjectDir)

            if File.Exists path then
                Some path
            else
                let path = getPaketDependenciesFile workingDirectory
                if File.Exists path then Some path else None

        return
            match paketDependenciesPath with
            | Some paketDependenciesPath ->
                log.Debug("Using {}", paketDependenciesPath)

                File.ReadLines(paketDependenciesPath)
                |> Seq.fold
                    (fun groups line ->
                        match Regex.Match(line, "group (.*)") with
                        | m when m.Success -> (m.Groups[1].Value, Set.empty) :: groups
                        | _ ->
                            match Regex.Match(line, "nuget ([^\s]*)") with
                            | m when m.Success ->
                                match groups with
                                | [] -> [ "", Set [ m.Groups[1].Value ] ]
                                | [ group, dependencies ] -> [ group, Set.add m.Groups[1].Value dependencies ]
                                | (group, dependencies) :: rest ->
                                    (group, Set.add m.Groups[1].Value dependencies) :: rest
                            | _ -> groups)
                    []
            | None ->
                log.Debug("Not using paket")
                []
    }

let doPaketInstall absoluteProjectDir =
    let inner (log: ILog) dir =
        let configFile = FilePath.appendParts [ ".config"; "dotnet-tools.json" ] dir

        if FilePath.exists configFile then
            let contents = File.ReadAllText(FilePath.asString configFile)

            if contents.Contains "paket" then
                log.Info("Running paket install in {}", FilePath.asString dir)

                runProcess true dir "dotnet" [| "paket"; "install" |] CancellationToken.None ignore
                |> Effect.map (fun _ -> true)
            else
                eff { return false }
        else
            eff { return false }

    eff {
        let! log = Log().Get()

        let! x = inner log (AbsoluteProjectDir.asFilePath absoluteProjectDir)

        return!
            if x then
                eff { return () }
            else
                inner log workingDirectory
                |> Effect.bind (function
                    | true -> eff { return () }
                    | false -> eff { return! Error AppError.PaketNotInstalled })
    }

let private writeAllText path content =
    let dirPath = path |> FilePath.directoryPath |> FilePath.asString

    if not (Directory.Exists dirPath) then
        Directory.CreateDirectory(dirPath) |> ignore

    File.WriteAllText(FilePath.asString path, content)

let writePaketReferences absoluteProjectDir paketDependencies =
    eff {
        let requiredDependencies = Set.map fst nugetDependencies

        let! group =
            paketDependencies
            |> List.tryPick (function
                | group, dependencies when Set.isSubset requiredDependencies dependencies -> Some group
                | _ -> None)
            |> function
                | Some group when group = "" -> Ok ""
                | Some group -> Ok $"%s{group}\n\n"
                | None -> Error AppError.DepsMissingFromPaket

        let paketReferencesPath pathParts =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts (pathParts @ [ "paket.references" ])

        let paketReferencesContent =
            nugetDependencies
            |> Set.map fst
            |> String.concat "\n"
            |> fun deps -> $"group %s{group}%s{deps}"

        writeAllText (paketReferencesPath [ ".elmish-land"; "Base" ]) paketReferencesContent
        writeAllText (paketReferencesPath [ ".elmish-land"; "App" ]) paketReferencesContent
    }
