module ElmishLand.Init

open System
open System.IO
open System.Reflection
open System.Threading
open ElmishLand.Effect
open ElmishLand.Resources
open ElmishLand.Settings
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.DotNetCli
open ElmishLand.Process
open ElmishLand.AppError
open ElmishLand.FsProj
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
            writeResource<global_json_template> log workingDirectory false [ "global.json" ] {
                DotNetSdkVersion = (DotnetSdkVersion.asString dotnetSdkVersion)
            }

        writeResource<settings_json>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ ".vscode"; "settings.json" ]
            Settings_json

        writeResource<``elmish-land_json``>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ "elmish-land.json" ]
            Elmish_land_json

        writeResource<Directory_Packages_props_template> log workingDirectory false [ "Directory.Packages.props" ] {
            PackageVersions = getNugetPackageVersions ()
        }

        writeResource<Project_fsproj_template>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ $"%s{ProjectName.asString projectName}.fsproj" ]
            {
                DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                ProjectName = ProjectName.asString projectName
            }

        let npmDependencies =
            npmDependencies
            |> Seq.map (fun (name, ver) -> $"\"%s{name}\": \"^%s{ver}\"")
            |> String.concat ",\n    "

        let npmDevDependencies =
            npmDevDependencies
            |> Seq.map (fun (name, ver) -> $"\"%s{name}\": \"^%s{ver}\"")
            |> String.concat ",\n    "

        writeResource<package_json_template>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ "package.json" ]
            {
                ProjectName = projectName |> ProjectName.asString |> String.asKebabCase
                Dependencies = npmDependencies
                DevDependencies = npmDevDependencies
            }

        writeResource<vite_config_js>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ "vite.config.js" ]
            Vite_config_js

        writeResource<index_html_template> log (AbsoluteProjectDir.asFilePath absoluteProjectDir) false [ "index.html" ] {
            Title = ProjectName.asString projectName
        }

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
                    ViewModule = settings.View.Module
                    ViewType = settings.View.Type
                    RootModule = rootModuleName
                    ElmishLandAppProjectFullName = $"ElmishLand.%s{projectName |> ProjectName.asString}.App"
                    Routes = [| homeRoute |]
                    Layouts = [| mainLayout |]
                    RouteParamModules = []
                }

                writeResource<NotFound_template>
                    log
                    (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                    false
                    [ "src"; "Pages"; "NotFound.fs" ]
                    {
                        ScaffoldTextElement = settings.View.TextElement
                        RootModule = rootModuleName
                    }

                writeResource<AddPage_template>
                    log
                    (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                    false
                    [ "src"; "Pages"; "Page.fs" ]
                    {
                        ViewModule = settings.View.Module
                        ViewType = settings.View.Type
                        ScaffoldTextElement = settings.View.TextElement
                        RootModule = rootModuleName
                        Route = homeRoute
                    }

                writeResource<AddLayout_template>
                    log
                    (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                    false
                    [ "src"; "Pages"; "Layout.fs" ]
                    {
                        ViewModule = settings.View.Module
                        ViewType = settings.View.Type
                        RootModule = rootModuleName
                        Layout = mainLayout
                    }

                eff { return routeData }


        log.Debug("Using route data {}", routeData)

        writeResource<Shared_template>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ "src"; "Shared.fs" ]
            (Shared_template routeData)

        do! generate absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir

        // Generate solution file
        log.Debug("Generating solution file")
        let solutionName = $"%s{ProjectName.asString projectName}.sln"

        do!
            runProcess
                true
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [| "new"; "sln"; "-n"; ProjectName.asString projectName |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        // Add projects to solution in the specified order
        let projectsToAdd = [
            $".elmish-land/Base/ElmishLand.%s{ProjectName.asString projectName}.Base.fsproj"
            $"%s{ProjectName.asString projectName}.fsproj"
            $".elmish-land/App/ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
        ]

        for projectPath in projectsToAdd do
            log.Debug("Adding project to solution: {}", projectPath)

            do!
                runProcess
                    true
                    (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                    "dotnet"
                    [| "sln"; solutionName; "add"; projectPath |]
                    CancellationToken.None
                    ignore
                |> Effect.map ignore<string>

        let dotnetToolsJsonPath =
            workingDirectory |> FilePath.appendParts [ ".config"; "dotnet-tools.json" ]

        let hasDotnetTool name =
            let filepath = FilePath.asString dotnetToolsJsonPath
            File.Exists filepath && (File.ReadAllText filepath).Contains($"\"%s{name}\"")

        let! fs = FileSystem.get ()
        let dotnetToolJsonExists = fs.FilePathExists(dotnetToolsJsonPath, false)

        do!
            [
                if not dotnetToolJsonExists then
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
