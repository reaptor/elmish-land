module ElmishLand.Generate

open System
open System.IO
open System.Text
open System.Text.Json
open System.Threading
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.Resource
open ElmishLand.Process
open ElmishLand.Log

let settingsArrayToHtmlElements (name: string) close (arr: JsonElement array) =
    arr
    |> Array.fold
        (fun xs elem ->
            let sb = StringBuilder()
            sb.Append($"<%s{name} ") |> ignore

            for x in elem.EnumerateObject() do
                sb.Append($"""%s{x.Name}="%s{x.Value.GetString()}" """) |> ignore

            sb.Remove(sb.Length - 1, 1) |> ignore

            if close then
                sb.Append($"></%s{name}>") |> ignore
            else
                sb.Append(">") |> ignore

            sb.ToString() :: xs)
        []

let generate (projectDir: AbsoluteProjectDir) dotnetSdkVersion =
    eff {
        let! logger = Log().Get()

        let projectName = projectDir |> ProjectName.fromProjectDir

        let writeResource = writeResource projectDir true

        let dotElmishLandDirectory =
            AbsoluteProjectDir.asFilePath projectDir
            |> FilePath.appendParts [ ".elmish-land" ]

        if Environment.CommandLine.Contains("--clean") then
            Directory.Delete(FilePath.asString dotElmishLandDirectory)

        if not (Directory.Exists(FilePath.asString dotElmishLandDirectory)) then
            do!
                writeResource
                    "Base.fsproj.handlebars"
                    [ ".elmish-land"; "Base"; "Base.fsproj" ]
                    (Some(
                        handlebars {|
                            DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                        |}
                    ))

            do!
                writeResource
                    "App.fsproj.handlebars"
                    [ ".elmish-land"; "App"; "App.fsproj" ]
                    (Some(
                        handlebars {|
                            DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                            ProjectReference =
                                $"""<ProjectReference Include="../../%s{ProjectName.asString projectName}.fsproj" />"""
                        |}
                    ))

            do!
                nugetDependencyCommands
                |> List.map (fun (cmd, args) -> true, dotElmishLandDirectory, cmd, args, CancellationToken.None, ignore)
                |> runProcesses

            do!
                npmDependencyCommands
                |> List.map (fun (cmd, args) ->
                    true, AbsoluteProjectDir.asFilePath projectDir, cmd, args, CancellationToken.None, ignore)
                |> runProcesses

        let routeData = getRouteData projectDir
        logger.Debug("Using route data: {}", routeData)
        do! generateFiles projectDir routeData
    }
