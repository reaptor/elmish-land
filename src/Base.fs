module ElmishLand.Base

open System
open System.Diagnostics

let appTitle = "Elmish Land"
let cliName = "elmish-land"
let version = "0.0.1"

let startProcess (workingDirectory: string option) command (args: string array) =
    let p =
        ProcessStartInfo(
            command,
            args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
        )
        |> Process.Start

    p.OutputDataReceived.Add(fun args -> Console.WriteLine(args.Data))
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()
    p.WaitForExit()

    p.ExitCode
