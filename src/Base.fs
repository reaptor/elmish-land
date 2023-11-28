module ElmishLand.Base

open System
open System.Diagnostics
open System.Text
open System.IO
open System.Reflection
open System.Threading
open System.Threading.Tasks

let appTitle = "Elmish Land"
let cliName = "elmish-land"
let version = "0.0.1"

let getTemplatesDir =
    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "src", "templates")

let getProjectDir (projectName: string) =
    Path.Combine(Environment.CurrentDirectory, projectName)

let getProjectPath workingDirectory =
    let projectDir =
        match workingDirectory with
        | Some workingDirectory' -> Path.Combine(Environment.CurrentDirectory, workingDirectory')
        | None -> Environment.CurrentDirectory

    Path.ChangeExtension(Path.Combine(projectDir, DirectoryInfo(projectDir).Name), "fsproj")


let private runProcessInternal
    (workingDirectory: string option)
    (command: string)
    (args: string array)
    (cancellation: CancellationToken)
    =
    let p =
        ProcessStartInfo(
            command,
            args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = defaultArg workingDirectory Environment.CurrentDirectory
        )
        |> Process.Start

    p.OutputDataReceived.Add(fun args -> Console.WriteLine(args.Data))

    p.ErrorDataReceived.Add(fun args ->
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine(args.Data)
        Console.ResetColor())

    p.BeginOutputReadLine()
    p.BeginErrorReadLine()

    while not cancellation.IsCancellationRequested && not p.HasExited do
        Thread.Sleep(100)

    if cancellation.IsCancellationRequested then
        p.Kill(true)
        p.Dispose()
        -1
    else
        p.ExitCode


let rec runProcess
    (workingDirectory: string option)
    (command: string)
    (args: string array)
    cancel
    (completed: unit -> unit)
    =
    let exitCode = runProcessInternal workingDirectory command args cancel

    if exitCode = 0 then
        completed ()

    exitCode

let runProcesses
    (processes: (string option * string * string array * CancellationToken) list)
    (completed: unit -> unit)
    =
    let exitCode =
        processes
        |> List.fold
            (fun previousExitCode (workingDirectory, command, args, cancellation) ->
                if previousExitCode = 0 then
                    runProcessInternal workingDirectory command args cancellation
                else
                    previousExitCode)
            0

    if exitCode = 0 then
        completed ()

    exitCode
