module ElmishLand.Generate

open System
open System.IO
open System.Threading
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.Resource
open ElmishLand.Process
open ElmishLand.Log
open ElmishLand.Paket
open ElmishLand.DotNetCli
open ElmishLand.FsProj

let dotElmishLandDirectory absoluteProjectDir =
    absoluteProjectDir
    |> AbsoluteProjectDir.asFilePath
    |> FilePath.appendParts [ ".elmish-land" ]

let restore absoluteProjectDir =
    eff {
        match! getPaketDependencies () with
        | [] ->
            do!
                nugetDependencyCommands
                |> List.map (fun (cmd, args) ->
                    true, dotElmishLandDirectory absoluteProjectDir, cmd, args, CancellationToken.None, ignore)
                |> runProcesses
        | paketDependencies ->
            do! writePaketReferences absoluteProjectDir paketDependencies
            do! ensurePaketInstalled ()

        do!
            npmDependencyCommands
            |> List.map (fun (cmd, args) ->
                true, absoluteProjectDir |> AbsoluteProjectDir.asFilePath, cmd, args, CancellationToken.None, ignore)
            |> runProcesses
    }

let generate absoluteProjectDir dotnetSdkVersion doRestore =
    eff {
        let! logger = Log().Get()

        logger.Debug("Working driectory: {}", FilePath.asString workingDirectory)
        logger.Debug("Project directory: {}", absoluteProjectDir |> AbsoluteProjectDir.asFilePath |> FilePath.asString)

        let! projectPath = absoluteProjectDir |> FsProjPath.findExactlyOneFromProjectDir
        let projectName = projectPath |> ProjectName.fromFsProjPath

        logger.Debug("Project file: {}", FsProjPath.asString projectPath)
        logger.Debug("Project name: {}", ProjectName.asString projectName)

        let writeResourceToProjectDir =
            writeResource (AbsoluteProjectDir.asFilePath absoluteProjectDir) true

        let dotElmishLandDirectory =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts [ ".elmish-land" ]

        if Environment.CommandLine.Contains("--clean") then
            Directory.Delete(FilePath.asString dotElmishLandDirectory)

        do!
            writeResourceToProjectDir
                "Base.fsproj.handlebars"
                [ ".elmish-land"; "Base"; "Base.fsproj" ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                    |}
                ))

        do!
            writeResourceToProjectDir
                "App.fsproj.handlebars"
                [ ".elmish-land"; "App"; "App.fsproj" ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                        ProjectReference =
                            $"""<ProjectReference Include="../../%s{ProjectName.asString projectName}.fsproj" />"""
                    |}
                ))

        if doRestore then
            do! restore absoluteProjectDir

        let! routeData = getRouteData projectName absoluteProjectDir
        logger.Debug("Using route data: {}", routeData)
        do! generateFiles (AbsoluteProjectDir.asFilePath absoluteProjectDir) routeData
    }

let generateCommand absoluteProjectDir =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        do! generate absoluteProjectDir dotnetSdkVersion false

        do! validate absoluteProjectDir
    }
