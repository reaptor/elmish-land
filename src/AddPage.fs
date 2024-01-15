module ElmishLand.AddPage

open System
open Orsak
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine
open ElmishLand.Resource

let addPage (url: string) =
    eff {
        let! log = Log().Get()

        let args =
            Environment.GetCommandLineArgs()
            |> Array.pairwise
            |> Array.choose (fun (x, y) ->
                if x.StartsWith("--") && not (y.StartsWith("--")) then
                    Some(x, y)
                else
                    None)
            |> Map

        let projectDir =
            args
            |> Map.tryFind "--project-dir"
            |> function
                | Some projectDir -> projectDir |> FilePath.fromString |> AbsoluteProjectDir.fromFilePath
                | None -> AbsoluteProjectDir.getDefaultProjectDir ()

        log.Debug("Using projectDir: {}", AbsoluteProjectDir.asString projectDir)

        let routeFileParts =
            url
            |> String.split "/"
            |> Array.map (fun x -> $"%s{x[0..0].ToUpper()}%s{x[1..]}")
            |> fun x -> [ "src"; "Pages"; yield! x; "Page.fs" ]

        let routeFilePath =
            projectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts routeFileParts

        log.Debug("routeFilePath: {}", routeFilePath)

        let route = fileToRoute projectDir routeFilePath
        let projectName = projectDir |> ProjectName.fromProjectDir
        let rootModuleName = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded

        do!
            writeResource
                projectDir
                false
                "AddPage.handlebars"
                routeFileParts
                (Some(
                    handlebars {|
                        RootModule = rootModuleName
                        Route = route
                    |}
                ))

        let routeData = getRouteData projectDir
        log.Debug("routeData: {}", routeData)
        do! generateFiles projectDir routeData

        let relativefilePathString = $"""%s{routeFileParts |> String.concat "/"}"""

        $"""%s{getAppTitle ()} added a new page at %s{url}
You can edit your new page here:
./%s{relativefilePathString}

Please add the file to the project using an IDE or add the following line to an
ItemGroup in the project file '%s{projectDir |> FsProjPath.fromProjectDir |> FsProjPath.asString}':
<Compile Include="%s{relativefilePathString}" />
"""
        |> log.Info
    }
