module Runner

open System
open System.IO
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open Xunit
open Orsak

// Tests mock package-registry HTTP calls against the local NuGet cache (~/.nuget/packages),
// so that the versions written into Directory.Packages.props are real versions that
// `dotnet restore` can find offline. npm package versions don't matter here because tests
// never run `npm install`, so any plausible version list is fine.
let private nugetPackagesDir =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")

let private cachedNugetVersions (packageNameLowercase: string) =
    let dir = Path.Combine(nugetPackagesDir, packageNameLowercase)

    if Directory.Exists(dir) then
        Directory.GetDirectories(dir) |> Array.map Path.GetFileName |> Array.toList
    else
        []

let private fakeNpmVersions = [ for i in 1..30 -> $"%d{i}.99.0" ]

let private toNugetJson (versions: string list) =
    let entries = versions |> List.map (sprintf "\"%s\"") |> String.concat ","
    $"{{\"versions\":[%s{entries}]}}"

let private toNpmJson (versions: string list) =
    let entries = versions |> List.map (fun v -> $"\"%s{v}\":{{}}") |> String.concat ","
    $"{{\"versions\":{{%s{entries}}}}}"

let private extractNugetPackageName (url: string) =
    let prefix = "https://api.nuget.org/v3-flatcontainer/"
    let suffix = "/index.json"

    if url.StartsWith(prefix) && url.EndsWith(suffix) then
        url.Substring(prefix.Length, url.Length - prefix.Length - suffix.Length)
    else
        ""

let env (logOutput: Expects.LogOutput) =
    { new IEffectEnv with
        member _.GetLogger(memberName, path, line) =
            let logger = Logger(memberName, path, line)

            let unindent (s: string) =
                s.Split("\n") |> Array.map _.TrimStart() |> String.concat "\n"

            { new ILog with
                member _.Debug(message, [<ParamArray>] args: obj array) =
                    logger.WriteLine (unindent >> logOutput.Debug.AppendLine >> ignore) message args

                member _.Info(message, [<ParamArray>] args: obj array) =
                    logger.WriteLine (unindent >> logOutput.Info.AppendLine >> ignore) message args

                member _.Error(message, [<ParamArray>] args: obj array) =
                    logger.WriteLine (unindent >> logOutput.Error.AppendLine >> ignore) message args
            }

        member _.GetAsync(url) =
            task {
                if url.Contains("api.nuget.org") then
                    let name = extractNugetPackageName url
                    return Ok(toNugetJson (cachedNugetVersions name))
                elif url.Contains("registry.npmjs.org") then
                    return Ok(toNpmJson fakeNpmVersions)
                else
                    return
                        Error(
                            ElmishLand.AppError.PackageVersionResolutionFailed $"Unexpected HTTP GET in test: %s{url}"
                        )
            }
    }

let runEff (e: Effect<IEffectEnv, _, _>) =
    let logOutput: Expects.LogOutput = {
        Info = StringBuilder()
        Debug = StringBuilder()
        Error = StringBuilder()
    }

    task {
        let! result = Effect.run (env logOutput) e
        return result, logOutput
    }
