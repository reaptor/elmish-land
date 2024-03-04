module ElmishLand.Init

open System
open System.IO
open System.Reflection
open System.Threading
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.Process
open ElmishLand.AppError
open ElmishLand.FsProj
open ElmishLand.Resource
open ElmishLand.Generate
open ElmishLand.Paket

let getNodeVersion () =
    runProcess false workingDirectory "node" [| "-v" |] CancellationToken.None ignore
    |> Effect.changeError (fun _ -> AppError.NodeNotFound)
    |> Effect.map (fun output ->
        match Version.TryParse(output[1..]) with
        | true, version when version >= minimumRequiredNode -> Ok version
        | _ -> Error NodeNotFound)
    |> Effect.joinResult

let successMessage () =
    $"""%s{getCommandHeader "created a new project"}
Run the following command to start the development server:

dotnet elmish-land server
"""

let init (absoluteProjectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        if not (Directory.Exists(absoluteProjectDir |> AbsoluteProjectDir.asString)) then
            Directory.CreateDirectory(absoluteProjectDir |> AbsoluteProjectDir.asString)
            |> ignore

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        let! nodeVersion = getNodeVersion ()
        log.Debug("Using Node.js: {}", nodeVersion)

        log.Debug("Initializing project. {}", AbsoluteProjectDir.asString absoluteProjectDir)

        let assembly = Assembly.GetExecutingAssembly()

        log.Debug("Resources in assembly:")

        for resource in assembly.GetManifestResourceNames() do
            log.Debug(resource)

        let writeResourceToProjectDir =
            writeResource (AbsoluteProjectDir.asFilePath absoluteProjectDir) false

        let projectName = ProjectName.fromAbsoluteProjectDir absoluteProjectDir

        let fsProjName = $"%s{ProjectName.asString projectName}.fsproj"

        let fsProjPath =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts [ fsProjName ]
            |> FsProjPath

        log.Debug("Project path {}", FsProjPath.asString fsProjPath)

        let fsProjExists = File.Exists(FsProjPath.asString fsProjPath)

        if
            not
            <| File.Exists(workingDirectory |> FilePath.appendParts [ "global.json" ] |> FilePath.asString)
        then
            do!
                writeResource
                    workingDirectory
                    false
                    "global.json.handlebars"
                    [ "global.json" ]
                    (Some(
                        handlebars {|
                            DotNetSdkVersion = (DotnetSdkVersion.asString dotnetSdkVersion)
                        |}
                    ))

        do! writeResourceToProjectDir "settings.json" [ ".vscode"; "settings.json" ] None

        do!
            writeResourceToProjectDir
                "Project.fsproj.handlebars"
                [ $"%s{ProjectName.asString projectName}.fsproj" ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                    |}
                ))

        do!
            writeResourceToProjectDir
                "package.json.handlebars"
                [ "package.json" ]
                (Some(
                    handlebars {|
                        ProjectName = projectName |> ProjectName.asString |> String.asKebabCase
                    |}
                ))

        do! writeResourceToProjectDir "vite.config.js" [ "vite.config.js" ] None

        do!
            writeResourceToProjectDir
                "index.html.handlebars"
                [ "index.html" ]
                (Some(
                    handlebars {|
                        Title = ProjectName.asString projectName
                    |}
                ))

        let rootModuleName = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded

        let! routeData =
            if fsProjExists then
                getRouteData projectName absoluteProjectDir
            else
                let homeRoute = {
                    Name = "Home"
                    RouteName = "HomeRoute"
                    MsgName = "HomeMsg"
                    ModuleName = $"%s{rootModuleName}.Pages.Home.Page"
                    RecordDefinition = ""
                    RecordConstructor = "[]"
                    RecordConstructorWithQuery = "query"
                    RecordPattern = ""
                    UrlUsage = "\"\""
                    UrlPattern = "[]"
                    UrlPatternWithQuery = "[ Query q ]"
                    UrlPatternWhen = ""
                }

                let routeData = {
                    RootModule = rootModuleName
                    Routes = [| homeRoute |]
                }

                eff {
                    do!
                        writeResourceToProjectDir
                            "AddPage.handlebars"
                            [ "src"; "Pages"; "Home"; "Page.fs" ]
                            (Some(
                                handlebars {|
                                    RootModule = rootModuleName
                                    Route = homeRoute
                                |}
                            ))

                    return routeData
                }

        log.Debug("Using route data {}", routeData)

        do! writeResourceToProjectDir "Shared.handlebars" [ "src"; "Shared.fs" ] (Some(handlebars routeData))

        let doRestore =
            not (Directory.Exists(absoluteProjectDir |> dotElmishLandDirectory |> FilePath.asString))

        do! generate absoluteProjectDir dotnetSdkVersion doRestore

        do! validate absoluteProjectDir

        let dotnetToolsJsonPath =
            workingDirectory |> FilePath.appendParts [ ".config"; "dotnet-tools.json" ]

        let hasDotnetTool name =
            let filepath = FilePath.asString dotnetToolsJsonPath
            File.Exists filepath && (File.ReadAllText filepath).Contains($"\"%s{name}\"")

        do!
            [
                if not (FilePath.exists dotnetToolsJsonPath) then
                    "dotnet", [| "new"; "tool-manifest" |]
                for name, version in getDotnetToolDependencies () do
                    if not <| hasDotnetTool name then
                        "dotnet", [| "tool"; "install"; name; version |]
            ]
            |> List.map (fun (cmd, args) ->
                true, AbsoluteProjectDir.asFilePath absoluteProjectDir, cmd, args, CancellationToken.None, ignore)
            |> runProcesses

        match! getPaketDependencies () with
        | [] -> ()
        | paketDependencies ->
            do! writePaketReferences absoluteProjectDir paketDependencies
            do! ensurePaketInstalled ()

        do!
            runProcess
                true
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [| "restore"; ".elmish-land/App/App.fsproj" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        log.Info(successMessage ())
    }
