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
    runProcess false (FilePath.fromString Environment.CurrentDirectory) "node" [| "-v" |] CancellationToken.None ignore
    |> Effect.changeError (fun _ -> AppError.NodeNotFound)
    |> Effect.map (fun output ->
        match Version.TryParse(output[1..]) with
        | true, version when version >= minimumRequiredNode -> Ok version
        | _ -> Error NodeNotFound)
    |> Effect.joinResult

let successMessage projectDir =
    let projectName = projectDir |> ProjectName.fromProjectDir

    $"""%s{getCommandHeader $"created a new project in ./%s{ProjectName.asString projectName}"}
Run the following command to start the development server:

dotnet elmish-land server
"""

let init (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        let projectName = projectDir |> ProjectName.fromProjectDir

        let! dotnetSdkVersion = getDotnetSdkVersionToUse ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        let! nodeVersion = getNodeVersion ()
        log.Debug("Using Node.js: {}", nodeVersion)

        log.Debug("Initializing project. {}", AbsoluteProjectDir.asString projectDir)

        let assembly = Assembly.GetExecutingAssembly()

        log.Debug("Resources in assembly:")

        for resource in assembly.GetManifestResourceNames() do
            log.Debug(resource)

        let writeResource = writeResource projectDir false

        let fsProjPath = FsProjPath.fromProjectDir projectDir
        log.Debug("Project path {}", fsProjPath)

        let fsProjExists = File.Exists(FsProjPath.asString fsProjPath)

        do!
            writeResource
                "global.json.handlebars"
                [ "global.json" ]
                (Some(
                    handlebars {|
                        DotNetSdkVersion = (DotnetSdkVersion.asString dotnetSdkVersion)
                    |}
                ))

        do! writeResource "settings.json" [ ".vscode"; "settings.json" ] None

        do!
            writeResource
                "Project.fsproj.handlebars"
                [ $"%s{ProjectName.asString projectName}.fsproj" ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                    |}
                ))

        do!
            writeResource
                "package.json.handlebars"
                [ "package.json" ]
                (Some(
                    handlebars {|
                        ProjectName = projectName |> ProjectName.asString |> String.asKebabCase
                    |}
                ))

        do! writeResource "vite.config.js" [ "vite.config.js" ] None

        do!
            writeResource
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
                eff { return getRouteData projectDir }
            else
                let homeRoute = {
                    Name = "Home"
                    MsgName = "HomeMsg"
                    ModuleName = $"%s{rootModuleName}.Pages.Home.Page"
                    ArgsDefinition = ""
                    ArgsUsage = ""
                    ArgsPattern = ""
                    UrlUsage = "\"\""
                    UrlPattern = "[]"
                    UrlPatternWithQuery = "[ Route.Query query ]"
                }

                let routeData = {
                    Autogenerated = getAutogenerated ()
                    RootModule = rootModuleName
                    Routes = [| homeRoute |]
                }

                eff {
                    do!
                        writeResource
                            "Page.handlebars"
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

        do! writeResource "Shared.handlebars" [ "src"; "Shared.fs" ] (Some(handlebars routeData))

        do! generate projectDir dotnetSdkVersion

        do! validate projectDir

        do!
            [
                if not (FilePath.exists projectDir [ ".config"; "dotnet-tools.json" ]) then
                    "dotnet", [| "new"; "tool-manifest"; "--force" |]
                for name, version in getDotnetToolDependencies () do
                    "dotnet", [| "tool"; "install"; name; version |]
                "dotnet", [| "new"; "sln" |]
                "dotnet", [| "sln"; "add"; ".elmish-land/Base/Base.fsproj" |]
                "dotnet", [| "sln"; "add"; $"%s{ProjectName.asString projectName}.fsproj" |]
                "dotnet", [| "sln"; "add"; ".elmish-land/App/App.fsproj" |]
            ]
            |> List.map (fun (cmd, args) ->
                true, AbsoluteProjectDir.asFilePath projectDir, cmd, args, CancellationToken.None, ignore)
            |> runProcesses

        do!
            runProcess
                true
                (AbsoluteProjectDir.asFilePath projectDir)
                "dotnet"
                [| "restore" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string>

        log.Info(successMessage projectDir)
    }
