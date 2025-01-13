module ElmishLand.Process

open System
open System.Diagnostics
open System.Threading
open System.Text
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.Base
open ElmishLand.AppError
open System.IO
open Orsak

let private getFullPathOrDefault command =
    Environment.GetEnvironmentVariable("PATH")
    |> String.split ";"
    |> Array.tryPick (fun x ->
        let fullPath = Path.Combine(x, command)

        let rec inner =
            function
            | [] -> None
            | extension :: rest ->
                let filePathWithExtension = Path.ChangeExtension(fullPath, extension)

                if File.Exists(filePathWithExtension) then
                    Some filePathWithExtension
                else
                    inner rest

        inner [ "exe"; "cmd"; "bat"; "sh"; "" ])
    |> Option.defaultValue command

let private runProcessInternal
    (printOutput: bool)
    (FilePath workingDir)
    (command: string)
    (args: string array)
    (cancellation: CancellationToken)
    (outputReceived: string -> unit)
    =
    eff {
        let! log = Log().Get()

        let command = getFullPathOrDefault command

        log.Debug("Running {} {} in working dir {}", command, args, workingDir)

        try
            let p =
                ProcessStartInfo(
                    command,
                    args |> String.concat " ",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WorkingDirectory = workingDir
                )
                |> Process.Start

            let out = StringBuilder()
            let err = StringBuilder()

            p.OutputDataReceived.Add(fun args ->
                if not (String.IsNullOrEmpty args.Data) then
                    if printOutput then
                        Console.WriteLine(args.Data)
                    else
                        log.Debug(args.Data)

                    out.AppendLine(args.Data) |> ignore
                    outputReceived args.Data)

            p.ErrorDataReceived.Add(fun args ->
                if not (String.IsNullOrEmpty args.Data) then
                    if printOutput then
                        Console.Error.WriteLine(args.Data)
                    else
                        log.Error(args.Data)

                    err.AppendLine(args.Data) |> ignore)

            p.BeginOutputReadLine()
            p.BeginErrorReadLine()

            while not cancellation.IsCancellationRequested && not p.HasExited do
                Thread.Sleep(100)

            let errorResult () = err.ToString() |> ProcessError |> Error

            if cancellation.IsCancellationRequested then
                p.Kill(true)
                p.Dispose()
                return! errorResult ()
            else
                p.WaitForExit()

                if p.ExitCode = 0 then
                    return! Ok(out.ToString())
                else
                    return! errorResult ()
        with ex ->
            return! ex.ToString() |> AppError.ProcessError |> Error

    }

let runProcess printOutput workingDir (command: string) (args: string array) cancel outputReceived =
    runProcessInternal printOutput workingDir command args cancel outputReceived

let runProcesses (processes: (bool * FilePath * string * string array * CancellationToken * (string -> unit)) list) =
    eff {
        return!
            processes
            |> List.fold
                (fun previousResult (printOutput, workingDir, command, args, cancellation, outputReceived) ->
                    previousResult
                    |> Effect.bind (fun () ->
                        runProcessInternal printOutput workingDir command args cancellation outputReceived)
                    |> Effect.map ignore)
                (eff { return! Ok() })
    }
