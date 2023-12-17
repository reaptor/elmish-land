module Tests

open System
open System.IO
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Log
open Xunit
open Orsak
open Xunit.Abstractions

type LogOutput = {
    Info: StringBuilder
    Debug: StringBuilder
    Error: StringBuilder
}

module LogOutput =
    let writeAll (output: ITestOutputHelper) (logs: LogOutput) =
        output.WriteLine(logs.Debug.ToString())
        output.WriteLine(logs.Info.ToString())
        output.WriteLine(logs.Error.ToString())

let env (logOutput: LogOutput) =
    { new ILogProvider with
        member _.GetLogger() =
            { new ILog with
                member _.Debug(message, [<ParamArray>] args: obj array) =
                    formatMessage message args
                    |> logOutput.Debug.AppendLine
                    |> ignore<StringBuilder>

                member _.Info(message, [<ParamArray>] args: obj array) =
                    formatMessage message args |> logOutput.Info.AppendLine |> ignore<StringBuilder>

                member _.Error(message, [<ParamArray>] args: obj array) =
                    formatMessage message args
                    |> logOutput.Error.AppendLine
                    |> ignore<StringBuilder>
            }
    }

let runEff (e: Effect<_, unit, _>) =
    let logOutput = {
        Info = StringBuilder()
        Debug = StringBuilder()
        Error = StringBuilder()
    }

    task {
        let! result = Effect.run (env logOutput) e
        return result, logOutput
    }

type Init(output: ITestOutputHelper) =
    [<Fact>]
    member _.``Generates project``() =
        task {
            let cts = new CancellationTokenSource()
            cts.CancelAfter(TimeSpan.FromSeconds 30)
            let folder = Guid.NewGuid().ToString()

            try
                let! result, logs = ElmishLand.Program.run [| "init"; folder; "--verbose" |] |> runEff
                LogOutput.writeAll output logs
                Expects.ok result
                let projectDir = folder |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath
                Expects.equals $"%s{ElmishLand.Init.successMessage projectDir}\n" (logs.Info.ToString())
            finally
                if Directory.Exists(folder) then
                    Directory.Delete(folder, true)
        }

type Build(output: ITestOutputHelper) =
    [<Fact>]
    member _.``Build project``() =
        task {
            let cts = new CancellationTokenSource()
            cts.CancelAfter(TimeSpan.FromSeconds 30)
            let folder = $"""Proj_{Guid.NewGuid().ToString().Replace("-", "")}"""

            try
                let! _ = ElmishLand.Program.run [| "init"; folder |] |> runEff

                let! result, logs = ElmishLand.Program.run [| "build"; folder; "--verbose" |] |> runEff

                LogOutput.writeAll output logs
                Expects.ok result

                Expects.equals $"%s{ElmishLand.Build.successMessage}\n" (logs.Info.ToString())
            finally
                if Directory.Exists(folder) then
                    Directory.Delete(folder, true)
        }
