module Tests

open System
open System.IO
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open Xunit
open Orsak

let env (logOutput: Expects.LogOutput) =
    { new ILogProvider with
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
    }

let runEff (e: Effect<_, unit, _>) =
    let logOutput: Expects.LogOutput = {
        Info = StringBuilder()
        Debug = StringBuilder()
        Error = StringBuilder()
    }

    task {
        let! result = Effect.run (env logOutput) e
        return result, logOutput
    }

let getFolder () =
    $"""Proj_%s{Guid.NewGuid().ToString().Replace("-", "")}"""

[<Fact>]
let ``Init, generates project`` () =
    task {
        let cts = new CancellationTokenSource()
        cts.CancelAfter(TimeSpan.FromSeconds 30L)
        let rootFolder = getFolder ()

        try
            let! result, logs =
                ElmishLand.Program.run [| "init"; "--project-dir"; rootFolder; "--verbose" |]
                |> runEff

            Expects.ok logs result

            Expects.equalsIgnoringWhitespace logs $"%s{ElmishLand.Init.successMessage ()}\n" (logs.Info.ToString())
        finally
            if Directory.Exists(rootFolder) then
                Directory.Delete(rootFolder, true)
    }

[<Fact>]
let ``Build, builds project`` () =
    task {
        let cts = new CancellationTokenSource()
        cts.CancelAfter(TimeSpan.FromSeconds 30L)
        let folder = getFolder ()

        try
            let! _ = ElmishLand.Program.run [| "init"; "--project-dir"; folder |] |> runEff

            let! result, logs =
                ElmishLand.Program.run [| "build"; "--project-dir"; folder; "--verbose" |]
                |> runEff

            Expects.ok logs result
            Expects.equalsIgnoringWhitespace logs $"%s{ElmishLand.Build.successMessage}\n" (logs.Info.ToString())
        finally
            if Directory.Exists(folder) then
                Directory.Delete(folder, true)
    }
