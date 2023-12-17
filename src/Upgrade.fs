module ElmishLand.Upgrade

open System.IO
open System.Text.RegularExpressions
open System.Threading
open ElmishLand.Base
open ElmishLand.DotNetCli
open ElmishLand.Process
open ElmishLand.Log
open Orsak

let private replaceFile (FilePath filePath) pattern (evaluator: Match -> string) =
    File.ReadAllText(filePath)
    |> fun x -> Regex.Replace(x, pattern, evaluator)
    |> fun x -> File.WriteAllText(filePath, x)

let upgrade (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Effect.getLogger ()

        let projectName = projectDir |> ProjectName.fromProjectDir

        let fsProjPath = FsProjPath.fromProjectDir projectDir |> FsProjPath.asFilePath

        let! dotnetSdkVersion = getLatestDotnetSdkVersion ()

        replaceFile fsProjPath "(<TargetFramework>)([^<]+)(</TargetFramework>)" (fun m ->
            $"%s{m.Groups[1].Value}%s{DotnetSdkVersion.asFrameworkVersion dotnetSdkVersion}%s{m.Groups[3].Value}")

        replaceFile
            (projectDir
             |> AbsoluteProjectDir.asFilePath
             |> FilePath.appendParts [ "global.json" ])
            "(\"version\":\s+\")([^\"]+)(\")"
            (fun m -> $"%s{m.Groups[1].Value}%s{DotnetSdkVersion.asString dotnetSdkVersion}%s{m.Groups[3].Value}")

        do!
            [
                for name, version in getDotnetToolDependencies () do
                    "dotnet", [| "tool"; "update"; name; version |]
                yield! dependencyCommands
            ]
            |> List.map (fun (cmd, args) ->
                AbsoluteProjectDir.asFilePath projectDir, cmd, args, CancellationToken.None, ignore)
            |> runProcesses

        log.Info
            $"""%s{getCommandHeader $"upgraded the project in ./%s{ProjectName.asString projectName}"}
Run the following command to start the development server:

dotnet elmish-land server
"""
    }
