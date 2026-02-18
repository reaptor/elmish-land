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

let getNodeVersion workingDirectory =
    runProcess false workingDirectory "node" [| "-v" |] CancellationToken.None ignore
    |> Effect.changeError (fun _ -> AppError.NodeNotFound)
    |> Effect.map (fun (output, _) ->
        let versionString = output.Trim()

        let versionWithoutV =
            if versionString.StartsWith("v") then
                versionString.Substring(1)
            else
                versionString

        match Version.TryParse(versionWithoutV) with
        | true, version when version >= minimumRequiredNode -> Ok version
        | _ -> Error NodeNotFound)
    |> Effect.joinResult

let successMessage () =
    let header = getCommandHeader "created a new project!"

    let content =
        """Run the following command to start the development server:

dotnet elmish-land server"""

    getFormattedCommandOutput header content

let promptForRouteMode (promptBehaviour: UserPromptBehaviour) : string =
    match promptBehaviour with
    | AutoAccept -> "hash" // Default to path for --auto-accept
    | AutoDecline -> "path" // Default to hash for --auto-decline (backwards compatible)
    | Ask ->
        Console.WriteLine("Which routing mode would you like to use?")
        Console.WriteLine("(1) Hash [default] - URLs with hash sign (example.com/#about)")
        Console.WriteLine("(2) Path - Clean URLs without hash (example.com/about)")
        Console.Write("Enter choice [1]: ")
        let response = Console.ReadLine()

        match response.Trim().ToLower() with
        | ""
        | "1" -> "hash"
        | "2" -> "path"
        | _ -> "hash" // default to hash for invalid input

let initFiles
    workingDirectory
    (absoluteProjectDir: AbsoluteProjectDir)
    (dotnetSdkVersion: DotnetSdkVersion)
    (nodeVersion: Version)
    (promptBehaviour: UserPromptBehaviour)
    =
    eff {
        let! log = Log().Get()

        if not (Directory.Exists(absoluteProjectDir |> AbsoluteProjectDir.asString)) then
            Directory.CreateDirectory(absoluteProjectDir |> AbsoluteProjectDir.asString)
            |> ignore

        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)
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

        let selectedRouteMode = promptForRouteMode promptBehaviour

        writeResource<``elmish-land_json``>
            log
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            false
            [ "elmish-land.json" ]
            { RouteMode = selectedRouteMode }

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
                    LayoutModulePath = "" // Main layout has no module path
                    MsgName = "HomeMsg"
                    ModuleName = $"%s{rootModuleName}.Pages.Page"
                    RecordDefinition = ""
                    RecordConstructor = "[]"
                    RecordPattern = ""
                    UrlUsage = "\"\""
                    UrlPattern = "[]"
                    UrlPatternWhen = ""
                    IsMainLayout = true
                }

                let mainLayout = {
                    Name = "Main"
                    MsgName = "MainLayoutMsg"
                    ModuleName = $"%s{rootModuleName}.Pages.Layout"
                    ModulePath = "" // Main layout has no module path
                }

                let routeData = {
                    RenderFunction = RenderMethod.asElmishReactFunction settings.Program.RenderMethod
                    RenderTargetElementId = settings.Program.RenderTargetElementId
                    ViewModule = settings.View.Module
                    ViewType = settings.View.Type
                    RootModule = rootModuleName
                    ElmishLandAppProjectFullName = $"ElmishLand.%s{projectName |> ProjectName.asString}.App"
                    Routes = [| homeRoute |]
                    Layouts = [| mainLayout |]
                    RouteParamModules = []
                    UseRouterPathMode = settings.Program.RouteMode.IsPath
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

        do! generate workingDirectory absoluteProjectDir dotnetSdkVersion

        do! validate absoluteProjectDir promptBehaviour

        return routeData
    }

let initCliCommands workingDirectory (absoluteProjectDir: AbsoluteProjectDir) (_routeData: TemplateData) =
    let isVerbose = System.Environment.CommandLine.Contains("--verbose")

    eff {
        let! log = Log().Get()
        let projectName = ProjectName.fromAbsoluteProjectDir absoluteProjectDir

        // Generate solution file
        log.Debug("Generating solution file")

        do!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [| "new"; "sln"; "-n"; ProjectName.asString projectName |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string * string>

        let projectDir =
            AbsoluteProjectDir.asFilePath absoluteProjectDir |> FilePath.asString

        let solutionName =
            if File.Exists(Path.Combine(projectDir, $"%s{ProjectName.asString projectName}.slnx")) then
                $"%s{ProjectName.asString projectName}.slnx"
            else
                $"%s{ProjectName.asString projectName}.sln"

        log.Debug("Detected solution file: {}", solutionName)

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
                    isVerbose
                    (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                    "dotnet"
                    [| "sln"; solutionName; "add"; projectPath |]
                    CancellationToken.None
                    ignore
                |> Effect.map ignore<string * string>

        let dotnetToolsJsonPath =
            workingDirectory |> FilePath.appendParts [ ".config"; "dotnet-tools.json" ]

        let hasDotnetTool name =
            let filepath = FilePath.asString dotnetToolsJsonPath
            File.Exists filepath && (File.ReadAllText filepath).Contains($"\"%s{name}\"")

        let dotnetToolJsonExists = FilePath.exists dotnetToolsJsonPath

        do!
            [
                if not dotnetToolJsonExists then
                    "dotnet", [| "new"; "tool-manifest" |]
                for name, version in getDotnetToolDependencies () do
                    if not <| hasDotnetTool name then
                        "dotnet", [| "tool"; "install"; name; version |]
            ]
            |> List.map (fun (cmd, args) ->
                isVerbose, AbsoluteProjectDir.asFilePath absoluteProjectDir, cmd, args, CancellationToken.None, ignore)
            |> runProcesses

        do!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [|
                    "restore"
                    $".elmish-land/App/ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
                |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string * string>

        do!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "npm"
                [| "install" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string * string>
    }

let init workingDirectory (absoluteProjectDir: AbsoluteProjectDir) (promptBehaviour: UserPromptBehaviour) =
    eff {
        let! log = Log().Get()

        // Get versions first using CLI
        let! dotnetSdkVersion = getDotnetSdkVersion workingDirectory
        let! nodeVersion = getNodeVersion workingDirectory

        // Create files without CLI commands
        let! routeData = initFiles workingDirectory absoluteProjectDir dotnetSdkVersion nodeVersion promptBehaviour

        do!
            withSpinner "Creating your project..." (fun _ ->
                initCliCommands workingDirectory absoluteProjectDir routeData)

        log.Info(successMessage ())
    }
