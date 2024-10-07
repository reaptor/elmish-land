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
                    "global.json.template"
                    [ "global.json" ]
                    (Some(
                        handlebars {|
                            DotNetSdkVersion = (DotnetSdkVersion.asString dotnetSdkVersion)
                        |}
                    ))

        do! writeResourceToProjectDir "settings.json" [ ".vscode"; "settings.json" ] None
        do! writeResourceToProjectDir "elmish-land.json" [ "elmish-land.json" ] None

        do!
            writeResource
                workingDirectory
                false
                "Directory.Packages.props.template"
                [ "Directory.Packages.props" ]
                (Some(
                    handlebars {|
                        PackageVersions = getNugetPackageVersions ()
                    |}
                ))

        do!
            writeResourceToProjectDir
                "Project.fsproj.template"
                [ $"%s{ProjectName.asString projectName}.fsproj" ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                        ProjectName = ProjectName.asString projectName
                    |}
                ))

        let npmDependencies =
            npmDependencies
            |> Seq.map (fun (name, ver) -> $"\"%s{name}\": \"^%s{ver}\"")
            |> String.concat ",\n    "

        let npmDevDependencies =
            npmDevDependencies
            |> Seq.map (fun (name, ver) -> $"\"%s{name}\": \"^%s{ver}\"")
            |> String.concat ",\n    "

        do!
            writeResourceToProjectDir
                "package.json.template"
                [ "package.json" ]
                (Some(
                    handlebars {|
                        ProjectName = projectName |> ProjectName.asString |> String.asKebabCase
                        Dependencies = npmDependencies
                        DevDependencies = npmDevDependencies
                    |}
                ))

        do! writeResourceToProjectDir "vite.config.js" [ "vite.config.js" ] None

        do!
            writeResourceToProjectDir
                "index.html.template"
                [ "index.html" ]
                (Some(
                    handlebars {|
                        Title = ProjectName.asString projectName
                    |}
                ))

        let rootModuleName = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded
        let! settings = getSettings absoluteProjectDir

        let! routeData =
            if fsProjExists then
                getTemplateData projectName absoluteProjectDir
            else
                let homeRoute = {
                    Name = ""
                    RouteName = "HomeRoute"
                    LayoutName = ""
                    LayoutModuleName = $"%s{rootModuleName}.Pages.Layout"
                    MsgName = "HomeMsg"
                    ModuleName = $"%s{rootModuleName}.Pages.Page"
                    RecordDefinition = ""
                    RecordConstructor = "[]"
                    RecordPattern = ""
                    UrlUsage = "\"\""
                    UrlPattern = "[]"
                    UrlPatternWhen = ""
                }

                let mainLayout = {
                    Name = "Main"
                    MsgName = "MainLayoutMsg"
                    ModuleName = $"%s{rootModuleName}.Pages.Layout"
                }

                let routeData = {
                    ViewType = settings.View.Type
                    RootModule = rootModuleName
                    Routes = [| homeRoute |]
                    Layouts = [| mainLayout |]
                    RouteParamModules = []
                }

                eff {
                    do!
                        writeResourceToProjectDir
                            "AddPage.template"
                            [ "src"; "Pages"; "Page.fs" ]
                            (Some(
                                handlebars {|
                                    ViewType = settings.View.Type
                                    ScaffoldTextElement = settings.View.ScaffoldTextElement
                                    RootModule = rootModuleName
                                    Route = homeRoute
                                |}
                            ))

                    do!
                        writeResource
                            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                            false
                            "AddLayout.template"
                            [ "src"; "Pages"; "Layout.fs" ]
                            (Some(
                                handlebars {|
                                    ViewType = settings.View.Type
                                    RootModule = rootModuleName
                                    Layout = mainLayout
                                |}
                            ))

                    return routeData
                }

        log.Debug("Using route data {}", routeData)

        do! writeResourceToProjectDir "Shared.template" [ "src"; "Shared.fs" ] (Some(handlebars routeData))

        do! generate absoluteProjectDir dotnetSdkVersion

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

        do!
            runProcess
                true
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [|
                    "restore"
                    $".elmish-land/App/ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
                |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        do!
            runProcess
                true
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "npm"
                [| "install" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        log.Info(successMessage ())
    }
