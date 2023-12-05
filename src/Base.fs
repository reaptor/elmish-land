module ElmishLand.Base

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading

module String =
    let asLines (s: string) = s.Split(Environment.NewLine)

let appTitle = "Elmish Land"
let cliName = "dotnet elmish-land"

let fileVersionInfo =
    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)

let version =
    let v = fileVersionInfo
    $"%i{v.FileMajorPart}.%i{v.FileMinorPart}.%i{v.FileBuildPart}"

let isPreRelease = fileVersionInfo.IsPreRelease

let help eachLine =
    $"""
    Here are the available commands:

    %s{cliName} init ... create a new project in the current directory
    %s{cliName} server ........................ run a local dev server
    %s{cliName} build .................. build your app for production
    %s{cliName} add page <url> ........................ add a new page
    %s{cliName} add layout <name> ................... add a new layout
    %s{cliName} routes ................... list all routes in your app

    Want to learn more? Visit https://github.com/reaptor/elmish-land
    """
    |> fun s -> s.Split(Environment.NewLine)
    |> Array.map eachLine
    |> String.concat Environment.NewLine

let autogenerated =
    $"""//------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by %s{appTitle} (v%s{version}).
%s{help (fun s -> $"// %s{s}")}
// This file will be overwritten when re-executing
// '%s{cliName} server' or '%s{cliName} build'.
// </auto-generated>
//------------------------------------------------------------------------------"""

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

let commandHeader s =
    let header = $"%s{appTitle} (v%s{version}) %s{s}"

    $"""
    %s{header}
    %s{String.init header.Length (fun _ -> "-")}
"""

let canonicalizePath (path: string) = path.Replace("\\", "/")

type FilePath = private | FilePath of string

let (|FilePath|) (FilePath filePath) = filePath

type AbsoluteProjectDir = private | ProjectDir of FilePath

type FileName = private | FileName of string

module FilePath =
    let fromString (filePath: string) = (canonicalizePath filePath) |> FilePath
    let extension (FilePath filePath) = Path.GetExtension(filePath)

    let ensureRelativeTo (ProjectDir(FilePath projectDir)) (FilePath filePath) =
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
        canonicalizePath Environment.CurrentDirectory
        |> Path.TrimEndingDirectorySeparator
        |> FilePath

    let defaultProjectDir = FilePath.asString currentDirectory |> FilePath |> ProjectDir

    let fromFilePath (FilePath absoluteOrRelativeFilePath) =
        if Path.IsPathRooted absoluteOrRelativeFilePath then
            Path.TrimEndingDirectorySeparator absoluteOrRelativeFilePath
        else
            $"%s{FilePath.asString currentDirectory}/%s{Path.TrimEndingDirectorySeparator absoluteOrRelativeFilePath}"
        |> FilePath
        |> ProjectDir

    let asFilePath (ProjectDir projectDir) = projectDir
    let asString (ProjectDir(FilePath projectDir)) = projectDir


module FileName =
    let fromFilePath (FilePath filePath) = Path.GetFileName(filePath) |> FileName
    let asString (FileName fileName) = fileName

    let equalsString s (FileName fileName) =
        fileName.Equals(s, StringComparison.InvariantCulture)

let (|ProjectDir|) (ProjectDir projectDir) = projectDir

type FsProjPath =
    private
    | FsProjPath of FilePath

    static member fromProjectDir(ProjectDir(FilePath projectDir)) =
        Path.ChangeExtension($"%s{projectDir}/%s{DirectoryInfo(projectDir).Name}", "fsproj")
        |> FilePath
        |> FsProjPath

    static member asString(FsProjPath(FilePath str)) = str
    static member asFilePath(FsProjPath filePath) = filePath

let (|FsProjPath|) (FsProjPath projectPath) = projectPath

type ProjectName =
    private
    | ProjectName of string

    static member fromProjectDir(ProjectDir projectDir) =
        projectDir |> FileName.fromFilePath |> FileName.asString |> ProjectName

    static member asString(ProjectName projectName) = projectName

let copyFile (projectDir: AbsoluteProjectDir) src dst replace =
    let templatesDir =
        Assembly.GetExecutingAssembly().Location
        |> FilePath.fromString
        |> FilePath.directoryPath
        |> FilePath.appendParts [ "src"; "templates" ]

    let dstPath = AbsoluteProjectDir.asFilePath projectDir |> FilePath.appendParts dst

    let dstDir = FilePath.directoryPath dstPath |> FilePath.asString

    if not (Directory.Exists dstDir) then
        Directory.CreateDirectory(dstDir) |> ignore

    File.Copy(templatesDir |> FilePath.appendParts src |> FilePath.asString, FilePath.asString dstPath, true)

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
    =
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
    (projectDir: AbsoluteProjectDir)
    (command: string)
    (args: string array)
    cancel
    (completed: unit -> unit)
    =
    let exitCode = runProcessInternal projectDir command args cancel

    if exitCode = 0 then
        completed ()

    exitCode

let runProcesses
    (processes: (AbsoluteProjectDir * string * string array * CancellationToken) list)
    (completed: unit -> unit)
    (failed: unit -> unit)
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

    if exitCode = 0 then completed () else failed ()
    exitCode
