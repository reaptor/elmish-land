module Runner

open System
open System.IO
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open Xunit
open Orsak

let env (logOutput: Expects.LogOutput) (fileSystem: IFileSystem) =
    { new IEffectEnv with
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

        member _.FilePathExists(fp, isDirectory) =
            fileSystem.FilePathExists(fp, isDirectory)

        member _.GetParentDirectory(filePath) = fileSystem.GetParentDirectory(filePath)

        member _.GetFilesRecursive(filePath, searchPattern) =
            fileSystem.GetFilesRecursive(filePath, searchPattern)

        member _.ReadAllText(filePath) = fileSystem.ReadAllText(filePath)
    }

let runEff fileSystem (e: Effect<IEffectEnv, _, _>) =
    let logOutput: Expects.LogOutput = {
        Info = StringBuilder()
        Debug = StringBuilder()
        Error = StringBuilder()
    }

    task {
        let! result = Effect.run (env logOutput fileSystem) e
        return result, logOutput
    }
