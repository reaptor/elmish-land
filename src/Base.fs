module ElmishLand.Base

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading
open System.Text
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

type Log
    (
        [<CallerMemberName; Optional; DefaultParameterValue("")>] memberName: string,
        [<CallerFilePath; Optional; DefaultParameterValue("")>] path: string,
        [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line: int
    ) =

    let path =
        path.Replace($"%s{__SOURCE_DIRECTORY__}%s{Path.DirectorySeparatorChar.ToString()}", "")

    let writeLine (message: string) args =
        let formattedMsg =
            args
            |> Array.fold
                (fun (sb: StringBuilder) arg ->
                    let i = sb.ToString().IndexOf("{}")
                    sb.Replace("{}", (if arg = null then "null" else $"%A{arg}"), i, 2))
                (StringBuilder(message))

        Console.WriteLine $"%s{path}(%i{line}): %s{memberName}: %s{formattedMsg.ToString()}"

    let isEnabled = Environment.CommandLine.Contains("--verbose")

    member _.Info(message, [<ParamArray>] args: obj array) =
        if isEnabled then
            Console.ForegroundColor <- ConsoleColor.Gray
            writeLine message args
            Console.ResetColor()

    member _.Error(message, [<ParamArray>] args: obj array) =
        if isEnabled then
            Console.ForegroundColor <- ConsoleColor.Red
            writeLine message args
            Console.ResetColor()

module String =
    let asLines (s: string) = s.Split(Environment.NewLine)

    let asKebabCase (s: string) =
        s.Replace(" ", "-").Replace("_", "-").ToLower()

let appFilePath =
    lazy
        let location = Assembly.GetExecutingAssembly().Location

        let location =
            if String.IsNullOrEmpty location then
                Process.GetCurrentProcess().MainModule.FileName
            else
                location

        Log().Info(location)
        location

let private fileVersionInfo =
    lazy
        let fvi = FileVersionInfo.GetVersionInfo(appFilePath.Value)
        let fvi = if fvi.FileVersion = null then None else Some fvi
        Log().Info("{}", fvi)
        fvi

let version =
    lazy
        match fileVersionInfo.Value with
        | Some fvi -> $"%i{fvi.FileMajorPart}.%i{fvi.FileMinorPart}.%i{fvi.FileBuildPart}"
        | None -> "unknown"

let isPreRelease =
    lazy
        match fileVersionInfo.Value with
        | Some fvi ->
            fvi.ProductVersion.Contains("-alpha.")
            || fvi.ProductVersion.Contains("-beta.")
            || fvi.ProductVersion.Contains("-rc.")
            || fvi.ProductVersion.Contains("+Branch.")
        | None -> true

let prereleseText = lazy if isPreRelease.Value then " preview" else ""

let appTitle = lazy $"Elmish Land (v%s{version.Value}%s{prereleseText.Value})"

let cliName = "dotnet elmish-land"

let help eachLine =
    $"""
    Here are the available commands:

    %s{cliName} init ... create a new project in the current directory
    %s{cliName} server ........................ run a local dev server
    %s{cliName} build .................. build your app for production
    %s{cliName} add page <url> ........................ add a new page
    %s{cliName} add layout <name> ................... add a new layout
    %s{cliName} routes ................... list all routes in your app

    Want to learn more? Visit https://elmish.land
    """
    |> fun s -> s.Split(Environment.NewLine)
    |> Array.map eachLine
    |> String.concat Environment.NewLine

let autogenerated =
    lazy
        $"""//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by %s{appTitle.Value}.
%s{help (fun s -> $"// %s{s}")}
// This file will be overwritten when re-executing
// '%s{cliName} server' or '%s{cliName} build'.
// </auto-generated>
//------------------------------------------------------------------------------"""

let private printError (text: string) =
    Console.ForegroundColor <- ConsoleColor.Red
    Console.Error.WriteLine(text)
    Console.ResetColor()

let getTemplatesDir =
    Path.Combine(Path.GetDirectoryName(appFilePath.Value), "src", "templates")

let getProjectDir (projectName: string) =
    Path.Combine(Environment.CurrentDirectory, projectName)

let getProjectPath workingDirectory =
    let projectDir =
        match workingDirectory with
        | Some workingDirectory' -> Path.Combine(Environment.CurrentDirectory, workingDirectory')
        | None -> Environment.CurrentDirectory

    Path.ChangeExtension(Path.Combine(projectDir, DirectoryInfo(projectDir).Name), "fsproj")

let commandHeader s =
    let header = $"%s{appTitle.Value} %s{s}"

    $"""
    %s{header}
    %s{String.init header.Length (fun _ -> "-")}
"""

let canonicalizePath (path: string) = path.Replace("\\", "/")

type FilePath = private | FilePath of string

let (|FilePath|) (FilePath filePath) = filePath

type AbsoluteProjectDir = private | AbsoluteProjectDir of FilePath

type FileName = private | FileName of string

module FilePath =
    let fromString (filePath: string) = (canonicalizePath filePath) |> FilePath
    let extension (FilePath filePath) = Path.GetExtension(filePath)

    let ensureRelativeTo (AbsoluteProjectDir(FilePath projectDir)) (FilePath filePath) =
        filePath.Replace($"%s{projectDir}/", "") |> FilePath

    let directoryPath (FilePath filePath) =
        Path.GetDirectoryName(filePath) |> FilePath

    let asString (FilePath filePath) = filePath

    let equals (FilePath filePath1) (FilePath filePath2) =
        filePath1.Equals(filePath2, StringComparison.InvariantCulture)

    let appendParts append (FilePath basePath) =
        let appendPath = append |> String.concat "/"
        $"%s{basePath}/%s{appendPath}" |> FilePath

    let appendFilePath append (FilePath basePath) =
        $"%s{basePath}/%s{asString append}" |> FilePath

    let startsWithParts parts (FilePath path) =
        path.StartsWith(parts |> String.concat "/")

    let endsWithParts parts (FilePath path) =
        path.EndsWith(parts |> String.concat "/")

module AbsoluteProjectDir =
    let private currentDirectory =
        lazy
            Log().Info(Environment.CurrentDirectory)

            canonicalizePath Environment.CurrentDirectory
            |> Path.TrimEndingDirectorySeparator
            |> FilePath

    let defaultProjectDir =
        FilePath.asString currentDirectory.Value |> FilePath |> AbsoluteProjectDir

    let fromFilePath (FilePath absoluteOrRelativeFilePath) =
        if Path.IsPathRooted absoluteOrRelativeFilePath then
            Path.TrimEndingDirectorySeparator absoluteOrRelativeFilePath
        else
            $"%s{currentDirectory.Value |> FilePath.asString}/%s{Path.TrimEndingDirectorySeparator absoluteOrRelativeFilePath}"
        |> FilePath
        |> AbsoluteProjectDir

    let asFilePath (AbsoluteProjectDir projectDir) = projectDir
    let asString (AbsoluteProjectDir(FilePath projectDir)) = projectDir


module FileName =
    let fromFilePath (FilePath filePath) = Path.GetFileName(filePath) |> FileName
    let asString (FileName fileName) = fileName

    let equalsString s (FileName fileName) =
        fileName.Equals(s, StringComparison.InvariantCulture)

let (|AbsoluteProjectDir|) (AbsoluteProjectDir projectDir) = projectDir

type FsProjPath =
    private
    | FsProjPath of FilePath

    static member fromProjectDir(AbsoluteProjectDir(FilePath projectDir)) =
        Path.ChangeExtension($"%s{projectDir}/%s{DirectoryInfo(projectDir).Name}", "fsproj")
        |> FilePath
        |> FsProjPath

    static member asString(FsProjPath(FilePath str)) = str
    static member asFilePath(FsProjPath filePath) = filePath

let (|FsProjPath|) (FsProjPath projectPath) = projectPath

type ProjectName =
    private
    | ProjectName of string

    static member fromProjectDir(AbsoluteProjectDir projectDir) =
        projectDir |> FileName.fromFilePath |> FileName.asString |> ProjectName

    static member asString(ProjectName projectName) = projectName

type AppError =
    | ProcessError of string
    | FsProjValidationError of string list

let handleAppResult onSuccess =
    function
    | Ok() ->
        onSuccess ()
        0
    | Error(ProcessError(error)) ->
        printError error
        -1
    | Error(FsProjValidationError errors) ->
        for error in errors do
            printError error

        -1

let writeResource (projectDir: AbsoluteProjectDir) (name: string) dst replace =
    let dstPath = AbsoluteProjectDir.asFilePath projectDir |> FilePath.appendParts dst

    let dstDir = FilePath.directoryPath dstPath |> FilePath.asString

    if not (Directory.Exists dstDir) then
        Directory.CreateDirectory(dstDir) |> ignore

    let assembly = Assembly.GetExecutingAssembly()
    let resourceName = $"templates.%s{name}"

    Log()
        .Info("Writing resource '{}' to '{}'", resourceName, FilePath.asString dstPath)

    use stream = assembly.GetManifestResourceStream($"templates.%s{name}")
    use reader = new StreamReader(stream)
    let fileContents = reader.ReadToEnd()
    File.WriteAllText(FilePath.asString dstPath, fileContents)

    match replace with
    | Some f ->
        File.ReadAllText(FilePath.asString dstPath)
        |> f
        |> (fun x -> File.WriteAllText(FilePath.asString dstPath, x))
    | None -> ()

let private runProcessInternal
    (projectDir: AbsoluteProjectDir)
    (command: string)
    (args: string array)
    (cancellation: CancellationToken)
    (outputReceived: string -> unit)
    =
    let log = Log()

    log.Info("Running {} {}", command, args)

    let p =
        ProcessStartInfo(
            command,
            args |> String.concat " ",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            WorkingDirectory = AbsoluteProjectDir.asString projectDir
        )
        |> Process.Start

    let err = StringBuilder()

    p.OutputDataReceived.Add(fun args ->
        if not (String.IsNullOrEmpty args.Data) then
            log.Info(args.Data)
            outputReceived args.Data)

    p.ErrorDataReceived.Add(fun args ->
        if not (String.IsNullOrEmpty args.Data) then
            log.Error(args.Data)
            err.AppendLine(args.Data) |> ignore)

    p.BeginOutputReadLine()
    p.BeginErrorReadLine()

    while not cancellation.IsCancellationRequested && not p.HasExited do
        Thread.Sleep(100)

    let errorResult () =
        (err.ToString()) |> ProcessError |> Error

    if cancellation.IsCancellationRequested then
        p.Kill(true)
        p.Dispose()
        errorResult ()
    else if p.ExitCode = 0 then
        Ok()
    else
        errorResult ()


let runProcess (projectDir: AbsoluteProjectDir) (command: string) (args: string array) cancel =
    runProcessInternal projectDir command args cancel

let runProcesses (processes: (AbsoluteProjectDir * string * string array * CancellationToken * (string -> unit)) list) =
    processes
    |> List.fold
        (fun previousResult (workingDirectory, command, args, cancellation, outputReceived) ->
            previousResult
            |> Result.bind (fun () -> runProcessInternal workingDirectory command args cancellation outputReceived))
        (Ok())
