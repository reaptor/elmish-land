module ElmishLand.AddLayout

open System
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine

let addLayout (url: string) =
    let log = Log()

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
            | None -> AbsoluteProjectDir.defaultProjectDir

    log.Info("Using projectDir: {}", AbsoluteProjectDir.asString projectDir)

    let routeFileParts =
        url.Split("/", StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun x -> $"%s{x[0..0].ToUpper()}%s{x[1..]}")
        |> fun x -> [ "src"; "Pages"; yield! x; "Layout.fs" ]

    let routeFilePath =
        projectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.appendParts routeFileParts

    log.Info("routeFilePath: {}", routeFilePath)

    let route = fileToRoute projectDir routeFilePath
    let projectName = projectDir |> ProjectName.fromProjectDir
    let rootModuleName = projectName |> ProjectName.asString |> quoteIfNeeded

    writeResource
        projectDir
        false
        "Layout.handlebars"
        routeFileParts
        (Some(
            handlebars {|
                RootModule = rootModuleName
                Route = route
            |}
        ))

    let routeData = getRouteData projectDir
    log.Info("routeData: {}", routeData)
    generateRoutesAndApp projectDir routeData

    let relativefilePathString = $"""%s{routeFileParts |> String.concat "/"}"""

    $"""%s{commandHeader $"added a new layout at %s{url}"}
You can edit your new layout here:
./%s{relativefilePathString}

Please add the file to the project using an IDE or add the following line to an
ItemGroup in the project file '%s{projectDir |> FsProjPath.fromProjectDir |> FsProjPath.asString}':
<Compile Include="%s{relativefilePathString}" />
"""
    |> indent
    |> printfn "%s"

    0
