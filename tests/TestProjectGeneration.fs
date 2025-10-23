module TestProjectGeneration

open System
open System.IO
open System.Threading.Tasks
open ElmishLand.Base
open ElmishLand.FsProj
open ElmishLand.Generate
open ElmishLand.Init
open ElmishLand.TemplateEngine
open FSharp.Compiler.CodeAnalysis
open Runner
open Xunit
open Orsak
open Ionide.ProjInfo

let getFolder () =
    Path.Combine(Environment.CurrentDirectory, "Proj_" + Guid.NewGuid().ToString().Replace("-", ""))

let leadingWhitespace (s: string) =
    let mutable i = 0

    while i < s.Length && (s[i] = ' ' || s[i] = '\t') do
        i <- i + 1

    s.Substring(0, i)

let dotnetSdkVersion = DotnetSdkVersion(Version(9, 0, 100))

let withNewProject (f: AbsoluteProjectDir -> TemplateData -> Task<_>) : Task<unit> =
    task {
        let folder = getFolder ()

        try
            let absoluteProjectDir = AbsoluteProjectDir(FilePath.fromString folder)
            let nodeVersion = Version(20, 0, 0)

            let! result, logOutput =
                eff {
                    let! routeData =
                        initFiles
                            (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                            absoluteProjectDir
                            dotnetSdkVersion
                            nodeVersion
                            Accept

                    return routeData
                }
                |> runEff

            let routeData = Expects.ok logOutput result

            do! f absoluteProjectDir routeData
        finally
            if Directory.Exists(folder) then
                Directory.Delete(folder, true)
    }

let expectProjectIsValid projectDir =
    eff {
        do! generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
        do! validate projectDir Accept
    }

let expectProjectTypeChecks (absoluteProjectDir: AbsoluteProjectDir) =
    task {
        let projectDirectory = DirectoryInfo(AbsoluteProjectDir.asString absoluteProjectDir)
        let toolsPath = Init.init projectDirectory None

        // Run dotnet restore first to ensure all references are available
        let restoreResult =
            System.Diagnostics.Process.Start(
                System.Diagnostics.ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = (AbsoluteProjectDir.asString absoluteProjectDir),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                )
            )

        restoreResult.WaitForExit()

        let loader = WorkspaceLoader.Create(toolsPath, [])

        let (FsProjPath(FilePath absoluteFsprojPath)) =
            FsProjPath.findExactlyOneFromProjectDir absoluteProjectDir
            |> Result.defaultWith (failwithf "%A")

        let projectOptions = loader.LoadProjects([ absoluteFsprojPath ]) |> Seq.toArray

        Assert.True(projectOptions.Length > 0, "Should load at least one project")

        let fsharpProjectOptions =
            FCS.mapToFSharpProjectOptions projectOptions.[0] projectOptions

        let fsharpProjectOptions = {
            fsharpProjectOptions with
                OtherOptions = fsharpProjectOptions.OtherOptions |> Array.distinct
        }

        let checker = FSharp.Compiler.CodeAnalysis.FSharpChecker.Create()

        let! checkResult = checker.ParseAndCheckProject(fsharpProjectOptions) |> Async.StartAsTask

        let buildVerificationResult =
            checkResult.Diagnostics
            |> Array.filter (fun d ->
                d.Severity = FSharp.Compiler.Diagnostics.FSharpDiagnosticSeverity.Warning
                || d.Severity = FSharp.Compiler.Diagnostics.FSharpDiagnosticSeverity.Error)

        if buildVerificationResult.Length > 0 then
            failwithf $"%A{buildVerificationResult}"
    }
