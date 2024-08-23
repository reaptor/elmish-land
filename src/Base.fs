module ElmishLand.Base

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading
open Thoth.Json.Net
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
    let header = $"%s{appTitle} %s{s}"

    $"""
%s{header}
%s{String.init header.Length (fun _ -> "-")}
"""

let canonicalizePath (path: string) = path.Replace("\\", "/")

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

    let appendFilePath (FilePath append) (FilePath basePath) =
        if String.IsNullOrWhiteSpace append then
            FilePath basePath
        else
            $"%s{basePath}/%s{asString <| FilePath append}" |> fromString

    let startsWithParts parts (FilePath path) =
        path.StartsWith(parts |> String.concat "/")

    let endsWithParts parts (FilePath path) =
        path.EndsWith(parts |> String.concat "/")

    let exists (FilePath filePath) = File.Exists(filePath)

    let parts (FilePath path) = String.split "/" path

let workingDirectory = FilePath.fromString Environment.CurrentDirectory

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
    let create commandLineArgs =
        workingDirectory
        |> FilePath.appendFilePath (relativeProjectDir commandLineArgs)
        |> AbsoluteProjectDir

    let asFilePath (AbsoluteProjectDir projectDir) = projectDir
    let asString (AbsoluteProjectDir(FilePath projectDir)) = projectDir

    let asRelativeFilePath (AbsoluteProjectDir absoluteProjectDir) =
        absoluteProjectDir
        |> FilePath.asString
        |> fun x -> x.Replace(FilePath.asString workingDirectory, "")
        |> FilePath

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
        "Elmish", "4.*"
        "Fable.Promise", "3.*"
        "Fable.Elmish.HMR", "7.*"
        "Fable.Elmish.React", "4.*"
        "Feliz", "2.*"
        "Feliz.Router", "4.*"
    ]

let npmDependencies = Set [ "react", "18"; "react-dom", "18" ]

let npmDevDependencies = Set [ "vite", "5" ]

type RoutePathParameter = {
    Module: string
    Type: string
    Parse: string option
    Format: string option
}

type RouteQueryParameter = {
    Name: string
    Module: string
    Type: string
    Parse: string option
    Format: string option
    Required: bool
}

type LayoutName = | LayoutName of string

module LayoutName =
    let asString (LayoutName x) = x

type RouteParameters = | RouteParameters of List<string * (RoutePathParameter option * RouteQueryParameter list)>

module RouteParameters =
    let value (RouteParameters x) = x

type Settings = {
    ViewType: string
    ProjectReferences: string list
    DefaultLayoutTemplate: string option
    DefaultPageTemplate: string option
    RouteSettings: RouteParameters
}

let getSettings absoluteProjectDir =
    result {
        let settingsPath =
            FilePath.appendParts [ "elmish-land.json" ] (AbsoluteProjectDir.asFilePath absoluteProjectDir)

        do!
            if FilePath.exists settingsPath then
                Ok()
            else
                Error ElmishLandProjectMissing

        let trimLeadingSpaces (s: string) =
            s.Split('\n')
            |> Array.fold
                (fun (state: string list, trimCount) s ->
                    if state.Length = 0 && s.Trim().Length = 0 then
                        state, trimCount
                    else
                        let trimCount =
                            match trimCount with
                            | Some trimCount' -> trimCount'
                            | None -> s.Length - s.TrimStart().Length

                        $"    %s{s[trimCount..]}" :: state, Some trimCount)
                ([], None)
            |> fun (xs, _) -> List.rev xs
            |> String.concat "\n"

        let paramsDecoder =
            Decode.object (fun get ->
                get.Optional.Field
                    "pathParameter"
                    (Decode.object (fun get -> {
                        Module = get.Required.Field "module" Decode.string
                        Type = get.Required.Field "type" Decode.string
                        Parse = get.Optional.Field "parse" Decode.string
                        Format = get.Optional.Field "format" Decode.string
                    })),
                get.Optional.Field
                    "queryParameters"
                    (Decode.list (
                        Decode.object (fun get -> {
                            Name = get.Required.Field "name" Decode.string
                            Module = get.Required.Field "module" Decode.string
                            Type = get.Optional.Field "type" Decode.string |> Option.defaultValue "string"
                            Parse = get.Optional.Field "parse" Decode.string
                            Format = get.Optional.Field "format" Decode.string
                            Required = get.Optional.Field "required" Decode.bool |> Option.defaultValue false
                        })
                    ))
                |> Option.toList
                |> List.collect id)

        let! pageSettings =
            Directory.GetFiles(AbsoluteProjectDir.asString absoluteProjectDir, "page.json", SearchOption.AllDirectories)
            |> Array.map (fun file ->
                File.ReadAllText(file)
                |> Decode.fromString paramsDecoder
                |> Result.mapError InvalidSettings
                |> Result.map (fun x ->
                    file
                    |> canonicalizePath
                    |> fun s ->
                        s
                            .Replace($"%s{AbsoluteProjectDir.asString absoluteProjectDir}/src/Pages/", "")
                            .Replace("page.json", "")
                        |> fun s -> if String.IsNullOrWhiteSpace s then s else $"/%s{s}"
                    , x))
            |> Array.toList
            |> Result.sequence

        let decoder =
            Decode.object (fun get -> {
                ViewType =
                    get.Optional.Field "viewType" Decode.string
                    |> Option.defaultValue "Feliz.ReactElement"
                ProjectReferences =
                    get.Optional.Field "projectReferences" (Decode.list Decode.string)
                    |> Option.defaultValue []
                    |> List.map (fun x -> $"../../%s{x}")
                DefaultLayoutTemplate =
                    get.Optional.Field "defaultLayoutTemplate" Decode.string
                    |> Option.map trimLeadingSpaces
                DefaultPageTemplate =
                    get.Optional.Field "defaultPageTemplate" Decode.string
                    |> Option.map trimLeadingSpaces
                RouteSettings = RouteParameters(pageSettings)
            })

        return!
            File.ReadAllText(FilePath.asString settingsPath)
            |> Decode.fromString decoder
            |> Result.mapError InvalidSettings
    }
