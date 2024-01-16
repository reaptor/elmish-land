module Tests

open System
open System.Collections.Generic
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open Xunit
open Orsak

type MemoryFileSystem() =
    let fs = Dictionary<string, string>()

    interface IFileSystem with
        member _.FileExists(FilePath filePath) = fs.ContainsKey filePath
        member _.DirectoryExists(FilePath filePath) = fs.ContainsKey filePath

        member _.EnsureDirectory(FilePath filePath) =
            if not (fs.ContainsKey filePath) then
                fs.Add(filePath, "")

        member _.WriteAllText(FilePath filePath, contents) = fs[filePath] <- contents
        member _.ReadAllText(FilePath filePath) = fs[filePath]
        member _.ReadAllLines(FilePath filePath) = fs[filePath].Split("\n")

        member _.GetFilesRecursive(FilePath filePath, _) =
            fs.Keys |> Seq.filter (fun fp -> fp.StartsWith filePath) |> Seq.toArray

        member _.DeleteDirectory(FilePath filePath) = fs.Remove(filePath) |> ignore

type TestEffectEnv(logOutput: Expects.LogOutput) =
    interface ILogProvider with
        member _.GetLogger(memberName, path, line) =
            let logger = Logger(memberName, path, line)

            let unindent (s: string) =
                s.Split("\n") |> Array.map _.TrimStart() |> String.concat "\n"

            { new ILog with
                member _.Debug(message, [<ParamArray>] args: obj array) =
                    logger.WriteLine (unindent >> logOutput.Debug.AppendLine >> ignore) message args

                member _.Info(message, [<ParamArray>] args: obj array) =
                    logger.WriteLine (unindent >> logOutput.Info.AppendLine >> ignore) message args

                member _.Error(message, [<ParamArray>] args: obj array) =
                    logger.WriteLine (unindent >> logOutput.Error.AppendLine >> ignore) message args
            }

    interface IFileSystemProvider with
        member _.Create() = MemoryFileSystem()

let runEff (e: Effect<_, unit, _>) =
    let logOutput: Expects.LogOutput = {
        Info = StringBuilder()
        Debug = StringBuilder()
        Error = StringBuilder()
    }

    task {
        let! result = Effect.run (TestEffectEnv(logOutput)) e
        return result, logOutput
    }

let getFolder () =
    $"""Proj_{Guid.NewGuid().ToString().Replace("-", "")}"""

[<Fact>]
let ``Init, generates project`` () =
    task {
        let cts = new CancellationTokenSource()
        cts.CancelAfter(TimeSpan.FromSeconds 30)
        let folder = getFolder ()

        let! result, logs = ElmishLand.Program.run [| "init"; folder; "--verbose" |] |> runEff
        Expects.ok logs result
        let projectDir = folder |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath

        Expects.equalsIgnoringWhitespace
            logs
            $"%s{ElmishLand.Init.successMessage projectDir}\n"
            (logs.Info.ToString())
    }


[<Fact>]
let ``Build, builds project`` () =
    task {
        let cts = new CancellationTokenSource()
        cts.CancelAfter(TimeSpan.FromSeconds 30)
        let folder = getFolder ()

        let! _ = ElmishLand.Program.run [| "init"; folder |] |> runEff

        let! result, logs = ElmishLand.Program.run [| "build"; folder; "--verbose" |] |> runEff

        Expects.ok logs result
        Expects.equalsIgnoringWhitespace logs $"%s{ElmishLand.Build.successMessage}\n" (logs.Info.ToString())

    }
