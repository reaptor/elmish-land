module ElmishLand.AddLayout

open ElmishLand.Effect
open ElmishLand.Resources
open ElmishLand.Settings
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine

let addLayout absoluteProjectDir (url: string) =
    eff {
        let! log = Log().Get()

        log.Debug("Working driectory: {}", FilePath.asString workingDirectory)
        log.Debug("Project directory: {}", absoluteProjectDir |> AbsoluteProjectDir.asFilePath |> FilePath.asString)

        let! projectPath = absoluteProjectDir |> FsProjPath.findExactlyOneFromProjectDir
        let projectName = projectPath |> ProjectName.fromFsProjPath

        log.Debug("Project file: {}", FsProjPath.asString projectPath)
        log.Debug("Project name: {}", ProjectName.asString projectName)

        let layoutFileParts =
            url
            |> String.split "/"
            |> Array.map (fun x -> $"%s{x[0..0].ToUpper()}%s{x[1..]}")
            |> fun x -> [ "src"; "Pages"; yield! x; "Layout.fs" ]

        let layoutFilePath =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts layoutFileParts

        log.Debug("layoutFilePath: {}", layoutFilePath)

        let layout = fileToLayout projectName absoluteProjectDir layoutFilePath
        let! projectPath = absoluteProjectDir |> FsProjPath.findExactlyOneFromProjectDir
        let rootModuleName = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded
        let! settings = getSettings absoluteProjectDir

        writeResource<AddLayout_template> log (AbsoluteProjectDir.asFilePath absoluteProjectDir) false layoutFileParts {
            ViewModule = settings.View.Module
            ViewType = settings.View.Type
            RootModule = rootModuleName
            Layout = layout
        }

        let! routeData = getTemplateData projectName absoluteProjectDir
        log.Debug("routeData: {}", routeData)
        generateFiles log (AbsoluteProjectDir.asFilePath absoluteProjectDir) routeData

        let relativefilePathString = $"""%s{layoutFileParts |> String.concat "/"}"""

        $"""%s{getAppTitle ()} added a new page at %s{url}
You can edit your new page here:
./%s{relativefilePathString}

Please add the file to the project using an IDE or add the following line to an
ItemGroup in the project file '%s{FsProjPath.asString projectPath}':
<Compile Include="%s{relativefilePathString}" />
"""
        |> log.Info
    }
