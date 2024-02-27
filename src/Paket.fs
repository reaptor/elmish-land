module ElmishLand.Paket

open System.IO
open System.Text.RegularExpressions
open System.Threading
open Orsak
open ElmishLand.Base
open ElmishLand.Process
open ElmishLand.Log

let getPaketDependencies () =
    eff {
        let! log = Log().Get()

        let paketDependenciesFile =
            workingDirectory
            |> FilePath.appendParts [ "paket.dependencies" ]
            |> FilePath.asString

        return
            if File.Exists(paketDependenciesFile) then
                log.Debug("Using {}", paketDependenciesFile)

                File.ReadLines(paketDependenciesFile)
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
            else
                log.Debug("Not using paket")
                []
    }

let ensurePaketInstalled () =
    eff {
        let! paketToolOutput =
            runProcess false workingDirectory "dotnet" [| "tool"; "list" |] CancellationToken.None ignore
            |> Effect.onError (fun e -> Effect.ret <| e.ToString())

        do!
            if paketToolOutput.Contains "paket" then
                Ok()
            else
                Error AppError.PaketNotInstalled

        do!
            runProcess true workingDirectory "dotnet" [| "paket"; "install" |] CancellationToken.None ignore
            |> Effect.map ignore<string>
    }

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
            |> FilePath.asString

        let paketReferencesContent =
            nugetDependencies
            |> Set.map fst
            |> String.concat "\n"
            |> fun deps -> $"group %s{group}%s{deps}"

        File.WriteAllText(paketReferencesPath [ ".elmish-land"; "Base" ], paketReferencesContent)
        File.WriteAllText(paketReferencesPath [ ".elmish-land"; "App" ], paketReferencesContent)
    }
