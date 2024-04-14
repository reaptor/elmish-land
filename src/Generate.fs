module ElmishLand.Generate

open System
open System.IO
open System.Threading
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.Resource
open ElmishLand.Log
open ElmishLand.Paket
open ElmishLand.Process

let dotElmishLandDirectory absoluteProjectDir =
    absoluteProjectDir
    |> AbsoluteProjectDir.asFilePath
    |> FilePath.appendParts [ ".elmish-land" ]

let getNugetDependencies absoluteProjectDir =
    eff {
        match! getPaketDependencies absoluteProjectDir with
        | [] ->
            return
                nugetDependencies
                |> Seq.map (fun (name, ver) -> $"        <PackageReference Include=\"%s{name}\" Version=\"%s{ver}\" />")
                |> String.concat "\n"
                |> fun deps -> $"<ItemGroup>\n%s{deps}\n    </ItemGroup>"
        | _ -> return ""
    }

let ensurePaketInstalled absoluteProjectDir =
    eff {
        match! getPaketDependencies absoluteProjectDir with
        | [] -> ()
        | paketDependencies ->
            do! writePaketReferences absoluteProjectDir paketDependencies
            do! doPaketInstall absoluteProjectDir
            return ()
    }

let ensureViteInstalled () =
    eff {
        do!
            runProcess true workingDirectory "npm" [| "run"; "vite"; "--version" |] CancellationToken.None ignore
            |> Effect.map ignore<string>
    }
    |> Effect.changeError (fun _ -> AppError.ViteNotInstalled)

let generate absoluteProjectDir dotnetSdkVersion =
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

        let dotElmishLandDirectory = dotElmishLandDirectory absoluteProjectDir

        if Environment.CommandLine.Contains("--clean") then
            Directory.Delete(FilePath.asString dotElmishLandDirectory)

        let! nugetDependencies = getNugetDependencies absoluteProjectDir
        let! settings = getSettings absoluteProjectDir

        do!
            writeResourceToProjectDir
                "Base.fsproj.template"
                [
                    ".elmish-land"
                    "Base"
                    $"ElmishLand.%s{ProjectName.asString projectName}.Base.fsproj"
                ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                        PackageReferences = nugetDependencies
                        ProjectReferences = settings.ProjectReferences
                    |}
                ))

        do!
            writeResourceToProjectDir
                "App.fsproj.template"
                [
                    ".elmish-land"
                    "App"
                    $"ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
                ]
                (Some(
                    handlebars {|
                        DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                        ProjectReferences =
                            [ $"../../%s{ProjectName.asString projectName}.fsproj" ]
                            |> List.append settings.ProjectReferences
                    |}
                ))

        let! templateData = getTemplateData projectName absoluteProjectDir
        logger.Debug("Using template data: {}", templateData)
        do! generateFiles (AbsoluteProjectDir.asFilePath absoluteProjectDir) templateData

        match! getPaketDependencies absoluteProjectDir with
        | [] -> ()
        | _ -> do! ensurePaketInstalled absoluteProjectDir
    }
