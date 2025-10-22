module ElmishLand.Generate

open System
open System.IO
open System.Threading
open ElmishLand.Effect
open ElmishLand.Resources
open ElmishLand.Settings
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine
open ElmishLand.Process

let dotElmishLandDirectory absoluteProjectDir =
    absoluteProjectDir
    |> AbsoluteProjectDir.asFilePath
    |> FilePath.appendParts [ ".elmish-land" ]

let getNugetPackageVersions () =
    nugetDependencies
    |> Seq.map (fun (name, ver) -> $"        <PackageVersion Include=\"%s{name}\" Version=\"%s{ver}\" />")
    |> String.concat "\n"
    |> fun deps -> $"<ItemGroup>\n%s{deps}\n    </ItemGroup>"

let getNugetPackageReferences () =
    nugetDependencies
    |> Seq.map (fun (name, _) -> $"        <PackageReference Include=\"%s{name}\" />")
    |> String.concat "\n"
    |> fun deps -> $"<ItemGroup>\n%s{deps}\n    </ItemGroup>"

let ensureViteInstalled workingDirectory =
    eff {
        do!
            runProcess false workingDirectory "npm" [| "run"; "vite"; "--version" |] CancellationToken.None ignore
            |> Effect.map ignore
    }
    |> Effect.changeError (fun _ -> AppError.ViteNotInstalled)

let generate workingDirectory absoluteProjectDir dotnetSdkVersion =
    eff {
        let! logger = Log().Get()

        logger.Debug("Working driectory: {}", FilePath.asString workingDirectory)
        logger.Debug("Project directory: {}", absoluteProjectDir |> AbsoluteProjectDir.asFilePath |> FilePath.asString)

        let! projectPath = absoluteProjectDir |> FsProjPath.findExactlyOneFromProjectDir
        let projectName = projectPath |> ProjectName.fromFsProjPath

        logger.Debug("Project file: {}", FsProjPath.asString projectPath)
        logger.Debug("Project name: {}", ProjectName.asString projectName)

        let dotElmishLandDirectory = dotElmishLandDirectory absoluteProjectDir

        if Environment.CommandLine.Contains("--clean") then
            Directory.Delete(FilePath.asString dotElmishLandDirectory)

        let nugetDependencies = getNugetPackageReferences ()
        let! settings = getSettings absoluteProjectDir

        writeResource<Base_fsproj_template>
            logger
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            true
            [
                ".elmish-land"
                "Base"
                $"ElmishLand.%s{ProjectName.asString projectName}.Base.fsproj"
            ]
            {
                DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                PackageReferences = nugetDependencies
                ProjectReferences = settings.ProjectReferences
            }

        writeResource<App_fsproj_template>
            logger
            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
            true
            [
                ".elmish-land"
                "App"
                $"ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
            ]
            {
                DotNetVersion = (DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion)
                ProjectReferences =
                    [ $"../../%s{ProjectName.asString projectName}.fsproj" ]
                    |> List.append settings.ProjectReferences
            }


        let! templateData = getTemplateData projectName absoluteProjectDir
        logger.Debug("Using template data: {}", templateData)
        generateFiles logger (AbsoluteProjectDir.asFilePath absoluteProjectDir) templateData
    }
