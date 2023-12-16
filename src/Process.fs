module ElmishLand.Process

open System
open System.Diagnostics
open System.Threading
open System.Text
open ElmishLand.Log
open ElmishLand.Base
open ElmishLand.AppError
open System.IO

let private getFullPathOrDefault command =
    System.Environment
        .GetEnvironmentVariable("PATH")
        .Split(";", StringSplitOptions.RemoveEmptyEntries)
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
    (workingDirectory: FilePath)
    (command: string)
    (args: string array)
    (cancellation: CancellationToken)
    (outputReceived: string -> unit)
    =
    let log = Log()

    let command = getFullPathOrDefault command

    log.Debug("Running {} {}", command, args)

    try
        let p =
            ProcessStartInfo(
                command,
                args |> String.concat " ",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = FilePath.asString workingDirectory
            )
            |> Process.Start

        let out = StringBuilder()
        let err = StringBuilder()

        p.OutputDataReceived.Add(fun args ->
            if not (String.IsNullOrEmpty args.Data) then
                log.Debug(args.Data)
                out.AppendLine(args.Data) |> ignore
                outputReceived args.Data)

        p.ErrorDataReceived.Add(fun args ->
            if not (String.IsNullOrEmpty args.Data) then
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
            errorResult ()
        else
            p.WaitForExit()

            if p.ExitCode = 0 then
                Ok(out.ToString())
            else
                errorResult ()
    with ex ->
        ex.ToString() |> AppError.ProcessError |> Error

let runProcess (workingDirectory: FilePath) (command: string) (args: string array) cancel outputReceived =
    runProcessInternal workingDirectory command args cancel outputReceived

let runProcesses (processes: (FilePath * string * string array * CancellationToken * (string -> unit)) list) =
    processes
    |> List.fold
        (fun previousResult (workingDirectory, command, args, cancellation, outputReceived) ->
            previousResult
            |> Result.bind (fun () -> runProcessInternal workingDirectory command args cancellation outputReceived)
            |> Result.map ignore)
        (Ok())
