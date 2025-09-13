module ElmishLand.Base

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open ElmishLand.AppError

type DotnetSdkVersion = | DotnetSdkVersion of Version

module DotnetSdkVersion =
    let fromString (s: string) =
        match Version.TryParse s with
        | true, version -> Some(DotnetSdkVersion version)
        | _ -> None

    let value (DotnetSdkVersion version) = version
    let asString (DotnetSdkVersion version) = version.ToString()

    let asFrameworkVersion (DotnetSdkVersion version) =
        $"net%i{version.Major}.%i{version.Minor}"

let minimumRequiredDotnetSdk = DotnetSdkVersion(Version(6, 0, 100))

let minimumRequiredNode = Version(18, 0)

module String =
    let split (separator: string) (s: string) =
        s.Split(separator, StringSplitOptions.RemoveEmptyEntries)

    let replace (oldValue: string) (newValue: string) (s: string) = s.Replace(oldValue, newValue)

    let asLines (s: string) = s.Split("\n")

    let asKebabCase (s: string) =
        s.Replace(" ", "-").Replace("_", "-").ToLower()

    let trimWhitespace (s: string) = s.Trim().Trim('\r').Trim('\t')

    let eachLine f (s: string) =
        asLines s |> Array.map f |> String.concat "\n"

    let indentLines s = eachLine (fun line -> $"  %s{line}") s

let ofOption error =
    function
    | Some s -> Ok s
    | None -> Error error

let optionIn (x: Option<Result<_, _>>) =
    match x with
    | Some y -> Result.map Some y
    | None -> Ok None

type ResultBuilder() =
    member _.Return(x) = Ok x

    member _.ReturnFrom(m: Result<_, _>) = m

    member _.Bind(m, f) = Result.bind f m
    member _.Bind((m, error): (Option<'T> * 'E), f) = m |> ofOption error |> Result.bind f

    member _.Zero() = None

    member _.Combine(m, f) = Result.bind f m

    member _.Delay(f: unit -> _) = f

    member _.Run(f) = f ()

    member __.TryWith(m, h) =
        try
            __.ReturnFrom(m)
        with e ->
            h e

    member __.TryFinally(m, compensation) =
        try
            __.ReturnFrom(m)
        finally
            compensation ()

    member __.Using(res: #IDisposable, body) =
        __.TryFinally(
            body res,
            fun () ->
                match res with
                | null -> ()
                | disp -> disp.Dispose()
        )

    member __.While(guard, f) =
        if not (guard ()) then
            Ok()
        else
            do f () |> ignore
            __.While(guard, f)

    member __.For(sequence: seq<_>, body) =
        __.Using(sequence.GetEnumerator(), (fun enum -> __.While(enum.MoveNext, __.Delay(fun () -> body enum.Current))))

let result = ResultBuilder()

module Result =
    let inline sequence xs =
        xs
        |> List.fold
            (fun (state: Result<List<_>, _>) (x: Result<_, _>) ->
                result {
                    let! state' = state
                    let! x' = x
                    return! Ok(x' :: state')
                })
            (Ok [])

let getAppFilePath () =
    let location = Assembly.GetExecutingAssembly().Location

    let location =
        if String.IsNullOrEmpty location then
            Process.GetCurrentProcess().MainModule.FileName
        else
            location

    location

let private getFileVersionInfo () =
    let appFilePath = getAppFilePath ()
    let fvi = FileVersionInfo.GetVersionInfo(appFilePath)
    if fvi.FileVersion = null then None else Some fvi

let getVersion () =
    let fileVersionInfo = getFileVersionInfo ()

    match fileVersionInfo with
    | Some fvi ->
        if fvi.ProductVersion.Contains("+") then
            fvi.ProductVersion[0 .. fvi.ProductVersion.IndexOf("+") - 1]
        else
            fvi.ProductVersion
    | None -> "unknown"

let getAppTitle () =
    let version = getVersion ()
    $"Elmish Land (v%s{version})"


let getWelcomeTitle () =
    let appTitle = getAppTitle ()
    let title = $"Welcome to %s{appTitle}"

    $"""
%s{title}
%s{String.init title.Length (fun _ -> "-")}
"""

let cliName = "dotnet elmish-land"

let help eachLine =
    // %s{cliName} upgrade ....... upgrade project to the latest elmish-land
    // %s{cliName} add layout <name> ...................... add a new layout
    $"""Here are the available commands:

%s{cliName} init ...... create a new project in the current directory
%s{cliName} server ........................... run a local dev server
%s{cliName} build ..................... build your app for production
%s{cliName} restore ....... restores dependencies and generates files
%s{cliName} add page <url> ........................... add a new page

Want to learn more? Visit https://elmish.land
"""
    |> String.eachLine eachLine

let getCommandHeader s =
    let appTitle = getAppTitle ()
    $"%s{appTitle} %s{s}"

let getFormattedCommandOutput (header: string) (content: string) =
    let separator = String.init (header.Length + 4) (fun _ -> "⎺")

    $"""%s{header}
%s{separator}
%s{content |> String.eachLine (fun line -> $"%s{line}")}
"""

let showWaitingIndicator (message: string) (action: unit -> 'a) =
    let isVerbose = System.Environment.CommandLine.Contains("--verbose")

    if isVerbose then
        // In verbose mode, just execute without spinner
        action ()
    else
        let spinChars = [| '⠋'; '⠙'; '⠹'; '⠸'; '⠼'; '⠴'; '⠦'; '⠧'; '⠇'; '⠏' |]
        let mutable spinIndex = 0
        let mutable isCompleted = false

        // Start spinner in background
        let spinnerTask =
            System.Threading.Tasks.Task.Run(fun () ->
                while not isCompleted do
                    printf $"\r%c{spinChars[spinIndex]} %s{message}"
                    Console.Out.Flush()
                    spinIndex <- (spinIndex + 1) % spinChars.Length
                    System.Threading.Thread.Sleep(100)

                printf "\r%s\r" (String(' ', message.Length + 2)) // Clear the line completely
                Console.Out.Flush())

        try
            // Execute the action
            let result = action ()
            isCompleted <- true
            spinnerTask.Wait(1000) |> ignore // Wait max 1 second for cleanup
            result
        with ex ->
            isCompleted <- true
            spinnerTask.Wait(1000) |> ignore
            reraise ()

let createSpinner (message: string) =
    let isVerbose = System.Environment.CommandLine.Contains("--verbose")
    let spinChars = [| '⠋'; '⠙'; '⠹'; '⠸'; '⠼'; '⠴'; '⠦'; '⠧'; '⠇'; '⠏' |]
    let mutable spinIndex = 0
    let mutable isCompleted = false
    let mutable spinnerTask = None

    if not isVerbose then
        let task =
            System.Threading.Tasks.Task.Run(fun () ->
                while not isCompleted do
                    printf $"\r%c{spinChars[spinIndex]} %s{message}"
                    Console.Out.Flush()
                    spinIndex <- (spinIndex + 1) % spinChars.Length
                    System.Threading.Thread.Sleep(100)

                printf "\r%s\r" (String(' ', message.Length + 2)) // Clear the line completely
                Console.Out.Flush())

        spinnerTask <- Some task

    let stopSpinner () =
        if not isVerbose then
            isCompleted <- true

            match spinnerTask with
            | Some task -> task.Wait(1000) |> ignore // Wait max 1 second for cleanup
            | None -> ()

    stopSpinner

let canonicalizePath (path: string) =
    let p = path.Replace("\\", "/")
    if p.EndsWith("/") then p[0 .. p.Length - 2] else p

type FilePath = private | FilePath of string

let (|FilePath|) (FilePath filePath) = filePath

type FileName = private | FileName of string

module FilePath =
    let fromString (filePath: string) = (canonicalizePath filePath) |> FilePath
    let extension (FilePath filePath) = Path.GetExtension(filePath)

    let removePath (FilePath pathToRemove) (FilePath filePath) =
        filePath.Replace($"%s{pathToRemove}/", "") |> fromString

    let directoryPath (FilePath filePath) =
        Path.GetDirectoryName(filePath) |> fromString

    let directoryName (FilePath filePath) =
        filePath
        |> String.split "/"
        |> Array.tryLast
        |> Option.defaultValue ""
        |> fromString

    let asString (FilePath filePath) = filePath

    let equals (FilePath filePath1) (FilePath filePath2) =
        filePath1.Equals(filePath2, StringComparison.InvariantCulture)

    let appendParts append (FilePath basePath) =
        let appendPath = append |> String.concat "/"
        $"%s{basePath}/%s{appendPath}" |> fromString

    let appendFilePath append (FilePath basePath) =
        $"%s{basePath}/%s{asString append}" |> fromString

    let startsWithParts parts (FilePath path) =
        path.StartsWith(parts |> String.concat "/")

    let endsWithParts parts (FilePath path) =
        path.EndsWith(parts |> String.concat "/")

    let exists (FilePath filePath) = File.Exists(filePath)

    let parts (FilePath path) = String.split "/" path

    let parent (FilePath path) =
        Directory.GetParent(path)
        |> Option.ofObj
        |> Option.map (fun di -> di.FullName |> canonicalizePath |> FilePath)

    let directoryExists (FilePath path) = Directory.Exists(path)

    let readAllText (FilePath path) = File.ReadAllText(path)

    let getFilesRecursive (FilePath path) searchPattern =
        if Directory.Exists(path) then
            Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories)
            |> Array.map fromString
        else
            [||]

// let workingDirectory = FilePath.fromString Environment.CurrentDirectory

let relativeProjectDir commandLineArgs =
    match
        Regex.Match(
            commandLineArgs |> String.concat " ",
            """--project-dir\s+"([^"]+)|--project-dir\s([^\s]+) """.Trim()
        )
    with
    | m when m.Success && m.Groups[1].Success -> FilePath.fromString m.Groups[1].Value
    | m when m.Success && m.Groups[2].Success -> FilePath.fromString m.Groups[2].Value
    | _ -> FilePath ""

type AbsoluteProjectDir = | AbsoluteProjectDir of FilePath

module AbsoluteProjectDir =
    let create workingDirectory commandLineArgs =
        workingDirectory
        |> FilePath.appendFilePath (relativeProjectDir commandLineArgs)
        |> AbsoluteProjectDir

    let asFilePath (AbsoluteProjectDir projectDir) = projectDir
    let asString (AbsoluteProjectDir(FilePath projectDir)) = projectDir

module FileName =
    let fromFilePath (FilePath filePath) = Path.GetFileName(filePath) |> FileName
    let asString (FileName fileName) = fileName

    let withoutExtension (FileName fileName) =
        Path.GetFileNameWithoutExtension fileName |> FileName

    let equalsString s (FileName fileName) =
        fileName.Equals(s, StringComparison.InvariantCulture)

type FsProjPath =
    | FsProjPath of FilePath

    static member findExactlyOneFromProjectDir(AbsoluteProjectDir(FilePath filePath)) =
        Directory.GetFiles(filePath, "*.fsproj")
        |> Array.toList
        |> function
            | [ fsproj ] -> fsproj |> FilePath.fromString |> FsProjPath |> Ok
            | [] -> Error FsProjNotFound
            | _ -> Error MultipleFsProjFound

    static member asString(FsProjPath(FilePath str)) = str
    static member asFilePath(FsProjPath filePath) = filePath

let (|FsProjPath|) (FsProjPath projectPath) = projectPath

type ProjectName =
    private
    | ProjectName of string

    static member fromFsProjPath(FsProjPath fsProjPath) =
        fsProjPath
        |> FileName.fromFilePath
        |> FileName.withoutExtension
        |> FileName.asString
        |> ProjectName

    static member fromAbsoluteProjectDir(absoluteProjectDir) =
        absoluteProjectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.directoryName
        |> FilePath.asString
        |> ProjectName

    static member asString(ProjectName projectName) = projectName

let getDotnetToolDependencies () = [ "fable", "--version 4.*" ]

let nugetDependencies =
    Set [
        "FSharp.Core", "9.0.303"
        "Elmish", "4.2.0"
        "Fable.Promise", "3.2.0"
        "Fable.Elmish.HMR", "7.0.0"
        "Fable.Elmish.React", "4.0.0"
        "Feliz", "2.9.0"
        "Feliz.Router", "4.0.0"
    ]

let npmDependencies = Set [ "react", "19"; "react-dom", "19" ]

let npmDevDependencies = Set [ "vite", "6" ]
