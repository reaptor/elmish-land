module CommandTests

open System
open System.IO
open System.Threading.Tasks
open ElmishLand.Base
open ElmishLand.FsProj
open ElmishLand.Generate
open ElmishLand.Init
open ElmishLand.AddPage
open ElmishLand.AddLayout
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
        do! validate projectDir
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

[<Fact>]
let ``addPage creates page with correct indentation in preview and fsproj`` () =
    withNewProject (fun projectDir _ ->
        task {
            // Run addPage with auto-accept = true to avoid user interaction
            let! result, logs = runEff (addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/Page1" true)

            // Verify the operation succeeded
            let _successResult = Expects.ok logs result

            // Verify the preview output has correct indentation
            let infoOutput = logs.Info.ToString()
            let previewLines = infoOutput.Replace("\r\n", "\n").Split('\n')

            // Find the preview section
            let previewStartIndex =
                previewLines
                |> Array.tryFindIndex (fun l -> l.Contains("Planned project file change (preview):"))

            Assert.True(previewStartIndex.IsSome, "Should find preview section in logs")

            // Find the new compile line in preview (marked with green +)
            let previewNewCompileLine =
                previewLines
                |> Array.tryFind (fun l -> l.Contains("+ <Compile Include=\"src/Pages/Page1/Page.fs\""))

            Assert.True(previewNewCompileLine.IsSome, "Should find new compile line in preview")

            // Strip ANSI colors from preview line and check indentation
            let stripAnsi (s: string) =
                System.Text.RegularExpressions.Regex.Replace(s, @"\u001b\[[0-9;]*m", "")

            let cleanPreviewLine = stripAnsi previewNewCompileLine.Value

            // Find existing compile line in preview for comparison
            let previewExistingLine =
                previewLines
                |> Array.tryFind (fun l ->
                    l.Contains("<Compile Include=")
                    && l.Contains("src/Pages/Page.fs")
                    && not (l.Contains("+")))

            Assert.True(previewExistingLine.IsSome, "Should find existing compile line in preview")

            let previewExistingIndent = leadingWhitespace previewExistingLine.Value

            // Extract indentation from the new line in preview (after stripping the "+ " prefix)
            let plusIndex = cleanPreviewLine.IndexOf("+ ")
            let afterPlusPrefix = cleanPreviewLine.Substring(plusIndex + 2) // Skip "+ "
            let previewNewIndent = leadingWhitespace afterPlusPrefix

            // Verify the new line in the preview has the same indentation as existing lines
            // This ensures the preview generation logic is correct
            Assert.Equal(previewExistingIndent, previewNewIndent)

            // Read the updated project file
            let fsprojPath =
                FsProjPath.findExactlyOneFromProjectDir projectDir
                |> Result.defaultWith (failwithf "%A")

            let fsprojContent = File.ReadAllText(FsProjPath.asString fsprojPath)
            let lines = fsprojContent.Replace("\r\n", "\n").Split('\n')

            // Find the new compile line
            let newCompileLine =
                lines |> Array.tryFind (fun l -> l.Contains("src/Pages/Page1/Page.fs"))

            Assert.True(newCompileLine.IsSome, "New compile entry should be added to project file")

            // Find the previous compile line to compare indentation
            let newCompileLineIndex =
                lines |> Array.findIndex (fun l -> l.Contains("src/Pages/Page1/Page.fs"))

            let prevCompileLineIndex =
                [| 0 .. newCompileLineIndex - 1 |]
                |> Array.rev
                |> Array.tryFind (fun i -> lines.[i].Contains("<Compile "))

            Assert.True(prevCompileLineIndex.IsSome, "Should find a previous compile line")

            let prevIndent = leadingWhitespace lines.[prevCompileLineIndex.Value]
            let newIndent = leadingWhitespace lines.[newCompileLineIndex]

            // The key assertion: indentation should match in actual file
            Assert.Equal(prevIndent, newIndent)

            // Verify the page file was created
            let pageFilePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "Page1", "Page.fs")

            Assert.True(File.Exists(pageFilePath), "Page file should be created")
        })

[<Fact>]
let ``addLayout after addPage updates project file order and page layout reference`` () =
    withNewProject (fun projectDir _ ->
        task {
            // Step 1: Add a page first
            let! addPageResult, addPageLogs =
                runEff (addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/AnotherPage" true)

            let _addPageSuccess = Expects.ok addPageLogs addPageResult

            // Verify page was added to project file
            let fsprojPath =
                FsProjPath.findExactlyOneFromProjectDir projectDir
                |> Result.defaultWith (failwithf "%A")

            let fsprojContent1 = File.ReadAllText(FsProjPath.asString fsprojPath)

            Assert.True(
                fsprojContent1.Contains("src/Pages/AnotherPage/Page.fs"),
                "Page should be added to project file"
            )

            // Verify page file was created
            let pageFilePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "AnotherPage", "Page.fs")

            Assert.True(File.Exists(pageFilePath), "Page file should be created")

            // Step 2: Add a layout for the same path
            let! addLayoutResult, addLayoutLogs =
                runEff (addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/AnotherPage" true)

            let _addLayoutSuccess = Expects.ok addLayoutLogs addLayoutResult

            // Verify layout was added to project file
            let fsprojContent2 = File.ReadAllText(FsProjPath.asString fsprojPath)

            Assert.True(
                fsprojContent2.Contains("src/Pages/AnotherPage/Layout.fs"),
                "Layout should be added to project file"
            )

            // Verify layout file was created
            let layoutFilePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "AnotherPage", "Layout.fs")

            Assert.True(File.Exists(layoutFilePath), "Layout file should be created")

            // Step 3: Verify correct project file ordering (Layout before Page)
            let layoutIndex = fsprojContent2.IndexOf("src/Pages/AnotherPage/Layout.fs")
            let pageIndex = fsprojContent2.IndexOf("src/Pages/AnotherPage/Page.fs")
            Assert.True(layoutIndex >= 0 && pageIndex >= 0, "Both files should be in project")
            Assert.True(layoutIndex < pageIndex, "Layout file should come before Page file in project")

            // Step 4: Verify page file was updated (layout reference may vary but should not be generic)
            let updatedPageContent = File.ReadAllText(pageFilePath)

            Assert.False(
                updatedPageContent.Contains("| LayoutMsg of Layout.Msg"),
                "Page should not be using the generic Layout.Msg anymore"
            )

            // Step 5: Verify the logs show auto-accept behavior
            let addLayoutLogsText = addLayoutLogs.Info.ToString()
            Assert.True(addLayoutLogsText.Contains("Auto-accepting"), "Should show auto-accept messages")
        })

[<Fact>]
let ``nested layout with page uses correct layout message reference`` () =
    withNewProject (fun projectDir _ ->
        task {
            // Step 1: Add nested layout first
            let! addLayoutResult, addLayoutLogs =
                runEff (addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/PageWithNestedLayout" true)

            let _addLayoutSuccess = Expects.ok addLayoutLogs addLayoutResult

            // Verify layout was added to project file
            let fsprojPath =
                FsProjPath.findExactlyOneFromProjectDir projectDir
                |> Result.defaultWith (failwithf "%A")

            let fsprojContent1 = File.ReadAllText(FsProjPath.asString fsprojPath)

            Assert.True(
                fsprojContent1.Contains("src/Pages/PageWithNestedLayout/Layout.fs"),
                "Nested layout should be added to project file"
            )

            // Verify nested layout file was created
            let nestedLayoutFilePath =
                Path.Combine(
                    AbsoluteProjectDir.asString projectDir,
                    "src",
                    "Pages",
                    "PageWithNestedLayout",
                    "Layout.fs"
                )

            Assert.True(File.Exists(nestedLayoutFilePath), "Nested layout file should be created")

            // Step 2: Add page for the nested layout path
            let! addPageResult, addPageLogs =
                runEff (addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/PageWithNestedLayout" true)

            let _addPageSuccess = Expects.ok addPageLogs addPageResult

            // Verify page was added to project file
            let fsprojContent2 = File.ReadAllText(FsProjPath.asString fsprojPath)

            Assert.True(
                fsprojContent2.Contains("src/Pages/PageWithNestedLayout/Page.fs"),
                "Nested page should be added to project file"
            )

            // Verify nested page file was created
            let nestedPageFilePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "PageWithNestedLayout", "Page.fs")

            Assert.True(File.Exists(nestedPageFilePath), "Nested page file should be created")

            // Step 3: Verify correct project file ordering (Layout before Page)
            let layoutIndex = fsprojContent2.IndexOf("src/Pages/PageWithNestedLayout/Layout.fs")
            let pageIndex = fsprojContent2.IndexOf("src/Pages/PageWithNestedLayout/Page.fs")
            Assert.True(layoutIndex >= 0 && pageIndex >= 0, "Both nested files should be in project")
            Assert.True(layoutIndex < pageIndex, "Nested layout file should come before nested page file")

            // Step 4: Verify main page exists (it's created by withNewProject)
            let mainPageFilePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "Page.fs")

            Assert.True(File.Exists(mainPageFilePath), "Main page file should exist")

            // Step 5: Verify nested page uses correct specific layout message
            let nestedPageContent = File.ReadAllText(nestedPageFilePath)

            Assert.True(
                nestedPageContent.Contains("| LayoutMsg of PageWithNestedLayout.Layout.Msg"),
                "Nested page should reference PageWithNestedLayout.Layout.Msg"
            )

            // Step 6: Verify that both layout files exist
            let mainLayoutFilePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "Layout.fs")

            Assert.True(File.Exists(mainLayoutFilePath), "Main layout file should exist")
            Assert.True(File.Exists(nestedLayoutFilePath), "Nested layout file should exist")
        })

[<Fact>]
let ``validate should only check layout references for pages in project file`` () =
    withNewProject (fun projectDir _ ->
        task {
            // Create an additional page file that is NOT in the project file
            // This page uses the main Layout.Msg which would normally be flagged as wrong
            // if there's a closer layout, but since it's not in the project file,
            // it should not be validated
            let orphanPageDir =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "OrphanPage")

            Directory.CreateDirectory(orphanPageDir) |> ignore

            let orphanPagePath = Path.Combine(orphanPageDir, "Page.fs")

            let orphanPageContent =
                """module OrphanPage.Page

type Model = { Count: int }

type Msg =
    | Increment
    | Decrement
    | LayoutMsg of Layout.Msg  // Using main layout - would be wrong if validated

let init () = { Count = 0 }, Cmd.none

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, Cmd.none
    | Decrement -> { model with Count = model.Count - 1 }, Cmd.none
    | LayoutMsg _ -> model, Cmd.none
"""

            File.WriteAllText(orphanPagePath, orphanPageContent)

            // Also create a layout file in OrphanPage directory
            // This would make the page's layout reference "wrong" if it were validated
            let orphanLayoutPath = Path.Combine(orphanPageDir, "Layout.fs")

            let orphanLayoutContent =
                """module OrphanPage.Layout

type Model = { Title: string }
type Msg = SetTitle of string

let init () = { Title = "Orphan" }, Cmd.none
let update msg model =
    match msg with
    | SetTitle t -> { model with Title = t }, Cmd.none
"""

            File.WriteAllText(orphanLayoutPath, orphanLayoutContent)

            // Run validation - should succeed because the orphan page is not in the project file
            // Note: This test verifies that pages not in the project file are completely ignored,
            // not reported as missing, and not validated for layout references
            let! result, logs = runEff (ElmishLand.FsProj.validate projectDir)

            // The validation should succeed (no errors about the orphan page)
            let _successResult = Expects.ok logs result

            // Verify that the orphan page file exists but is not in the project
            Assert.True(File.Exists(orphanPagePath), "Orphan page file should exist on disk")

            let fsprojPath =
                FsProjPath.findExactlyOneFromProjectDir projectDir
                |> Result.defaultWith (failwithf "%A")

            let fsprojContent = File.ReadAllText(FsProjPath.asString fsprojPath)

            Assert.False(fsprojContent.Contains("OrphanPage"), "Orphan page should not be in project file")
        })

[<Fact>]
let ``Fable build errors should be deduplicated and displayed correctly`` () =
    task {
        // Mock Fable build output with duplicate errors
        let mockOutput =
            """
17:22:36.382 Process.fs(73): runProcessInternal: Running "dotnet" [|"fable";
    "/Users/klofberg/Projects/elmish-land/quicktest/TestProject/.elmish-land/App/ElmishLand.TestProject.App.fsproj";
    "--noCache"; "--run"; "vite"; "build"; "--config"; "vite.config.js"|] in working dir "/Users/klofberg/Projects/elmish-land/quicktest/TestProject"
Fable 4.25.0: F# to JavaScript compiler
Minimum @fable-org/fable-library-js version (when installed from npm): 1.11.0

Thanks to the contributor! @davedawkins
Stand with Ukraine! https://standwithukraine.com.ua/
Parsing .elmish-land/App/ElmishLand.TestProject.App.fsproj...
Started Fable compilation...

Fable compilation finished in 6494ms
./src/Pages/Page.fs(23,5): (23,22) error FSHARP: This value is not a function and cannot be applied. (code 3)
Compilation failed
  17:22:47.945 (0): : Build failed
"""

        // This represents the same error coming through stderr (note: line 23 to match requirement)
        let mockErrorOutput =
            """./src/Pages/Page.fs(23,5): (23,22) error FSHARP: This value is not a function and cannot be applied. (code 3)"""

        // Process the output using FableOutput module - non-verbose mode (false)
        let result = ElmishLand.FableOutput.processOutput mockOutput mockErrorOutput false

        // In non-verbose mode, ONLY the actual error should be shown, NOT "Build failed"
        Assert.Equal(1, result.Errors |> List.length)

        Assert.Contains(
            "./src/Pages/Page.fs(23,5): (23,22) error FSHARP: This value is not a function and cannot be applied. (code 3)",
            result.Errors
        )

        // Verify warnings list is empty for this output
        Assert.Equal(0, result.Warnings |> List.length)
    }

[<Fact>]
let ``Wrong layout reference in page should generate helpful error message`` () =
    withNewProject (fun projectDir _ ->
        task {
            // Step 1: Add an About page with its own layout
            let! addLayoutResult, addLayoutLogs =
                runEff (addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true)

            let _layoutSuccess = Expects.ok addLayoutLogs addLayoutResult

            let! addPageResult, addPageLogs =
                runEff (addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true)

            let _pageSuccess = Expects.ok addPageLogs addPageResult

            // Step 2: Manually edit the About page to use the wrong layout (root Layout instead of About.Layout)
            let aboutPagePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "About", "Page.fs")

            let aboutPageContent = File.ReadAllText(aboutPagePath)

            // Replace the correct layout reference with the wrong one
            let wrongLayoutContent =
                aboutPageContent.Replace("| LayoutMsg of About.Layout.Msg", "| LayoutMsg of Layout.Msg")

            File.WriteAllText(aboutPagePath, wrongLayoutContent)

            // Step 3: Mock the Fable compiler error that would occur
            let mockBuildOutput =
                $"""Fable compilation started...
./src/Pages/About/Page.fs(10,20): (10,30) error FSHARP: The type 'Layout.Msg' does not match the type 'About.Layout.Msg' (code 1)
Compilation failed"""

            let mockErrorOutput =
                """./src/Pages/About/Page.fs(10,20): (10,30) error FSHARP: The type 'Layout.Msg' does not match the type 'About.Layout.Msg' (code 1)"""

            // Step 4: Process the output - this should detect the layout mismatch
            let result =
                ElmishLand.FableOutput.processOutput mockBuildOutput mockErrorOutput false

            // Verify the error is captured and transformed into a helpful message
            Assert.True(result.Errors |> List.length >= 1, "Should have at least one error")

            // The error should be our helpful message
            let error = result.Errors |> List.head
            Assert.Contains("has wrong layout reference", error)
            Assert.Contains("Layout.Msg", error)
            Assert.Contains("About.Layout.Msg", error)

            // Verify we also captured the layout mismatch details
            Assert.True(result.LayoutMismatches |> List.length >= 1, "Should have at least one layout mismatch")
            let mismatch = result.LayoutMismatches |> List.head
            // The path might be either the actual page or inferred from error
            Assert.True(
                mismatch.PagePath.Contains("About") || mismatch.PagePath.Contains("Page.fs"),
                sprintf "Page path should reference About page, got: %s" mismatch.PagePath
            )

            Assert.Equal("Layout.Msg", mismatch.WrongLayout)
            Assert.Equal("About.Layout.Msg", mismatch.CorrectLayout)
        })

[<Fact>]
let ``Add layout and then page, page uses correct layout`` () =
    withNewProject (fun projectDir _ ->
        eff {
            do! addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true
            do! addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true

            do! expectProjectIsValid projectDir
            do! expectProjectTypeChecks projectDir
        }
        |> Expects.effectOk runEff)

[<Fact>]
let ``Add nested layouts and then pages, page uses correct layout`` () =
    withNewProject (fun projectDir _ ->
        eff {
            do! addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true
            do! addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true

            do! addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About/Me" true
            do! addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About/Me" true

            do! expectProjectIsValid projectDir
            do! expectProjectTypeChecks projectDir
        }
        |> Expects.effectOk runEff)

[<Fact>]
let ``Nested page with wrong layout reference should generate correct error message`` () =
    withNewProject (fun projectDir _ ->
        task {
            // Step 1: Add About layout
            let! addLayoutResult, addLayoutLogs =
                runEff (addLayout (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true)

            let _layoutSuccess = Expects.ok addLayoutLogs addLayoutResult

            // Step 2: Add About page
            let! aboutResult, aboutLogs =
                runEff (addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About" true)

            let _aboutSuccess = Expects.ok aboutLogs aboutResult

            // Step 3: Add nested About/Me page
            let! nestedResult, nestedLogs =
                runEff (addPage (AbsoluteProjectDir.asFilePath projectDir) projectDir "/About/Me" true)

            let _nestedSuccess = Expects.ok nestedLogs nestedResult

            // Step 4: Manually change the About/Me page to use wrong layout (root Layout instead of About.Layout)
            let aboutMePagePath =
                Path.Combine(AbsoluteProjectDir.asString projectDir, "src", "Pages", "About", "Me", "Page.fs")

            let content = File.ReadAllText(aboutMePagePath)

            let wrongContent =
                content.Replace("| LayoutMsg of About.Layout.Msg", "| LayoutMsg of Layout.Msg")

            File.WriteAllText(aboutMePagePath, wrongContent)

            // Step 5: Simulate build error output for nested page (error in App.fs)
            let mockBuildOutput =
                """Fable compilation started...
./.elmish-land/App/App.fs(180,20): (180,30) error FSHARP: The type 'Layout.Msg' does not match the type 'About.Layout.Msg' (code 1)
Compilation failed"""

            let mockErrorOutput =
                """./.elmish-land/App/App.fs(180,20): (180,30) error FSHARP: The type 'Layout.Msg' does not match the type 'About.Layout.Msg' (code 1)"""

            // Step 6: Process the output
            let result =
                ElmishLand.FableOutput.processOutput mockBuildOutput mockErrorOutput false

            // Verify the error is captured with correct message
            Assert.True(result.Errors |> List.length >= 1, "Should have at least one error")

            let error = result.Errors |> List.head
            Assert.Contains("has wrong layout reference", error)
            // Should mention it's a page in the About directory
            Assert.Contains("src/Pages/About/", error)
            Assert.Contains("Pages.Layout.Msg", error)
            Assert.Contains("About.Layout.Msg", error)

            // Verify layout mismatch is captured
            Assert.True(result.LayoutMismatches |> List.length >= 1, "Should have at least one layout mismatch")
            let mismatch = result.LayoutMismatches |> List.head
            Assert.Equal("Pages.Layout.Msg", mismatch.WrongLayout)
            Assert.Equal("About.Layout.Msg", mismatch.CorrectLayout)
        })

[<Fact>]
let ``initFiles creates project files without CLI commands`` () =
    withNewProject (fun absoluteProjectDir routeData ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir

            // Verify project directory was created
            Assert.True(Directory.Exists(folder), "Project directory should be created")

            // Verify essential files were created
            let projectName = Path.GetFileName(folder)

            // Check F# project file
            let fsprojPath = Path.Combine(folder, projectName + ".fsproj")
            Assert.True(File.Exists(fsprojPath), "F# project file should be created")

            // Check package.json
            let packageJsonPath = Path.Combine(folder, "package.json")
            Assert.True(File.Exists(packageJsonPath), "package.json should be created")

            // Check vite.config.js
            let viteConfigPath = Path.Combine(folder, "vite.config.js")
            Assert.True(File.Exists(viteConfigPath), "vite.config.js should be created")

            // Check index.html
            let indexHtmlPath = Path.Combine(folder, "index.html")
            Assert.True(File.Exists(indexHtmlPath), "index.html should be created")

            // Check elmish-land.json
            let elmishLandJsonPath = Path.Combine(folder, "elmish-land.json")
            Assert.True(File.Exists(elmishLandJsonPath), "elmish-land.json should be created")

            // Check src/Pages structure
            let srcPagesPath = Path.Combine(folder, "src", "Pages")
            Assert.True(Directory.Exists(srcPagesPath), "src/Pages directory should be created")

            // Check NotFound.fs
            let notFoundPath = Path.Combine(folder, "src", "Pages", "NotFound.fs")
            Assert.True(File.Exists(notFoundPath), "NotFound.fs should be created")

            // Check Page.fs (home page)
            let pagePath = Path.Combine(folder, "src", "Pages", "Page.fs")
            Assert.True(File.Exists(pagePath), "Page.fs should be created")

            // Check Layout.fs
            let layoutPath = Path.Combine(folder, "src", "Pages", "Layout.fs")
            Assert.True(File.Exists(layoutPath), "Layout.fs should be created")

            // Check Shared.fs
            let sharedPath = Path.Combine(folder, "src", "Shared.fs")
            Assert.True(File.Exists(sharedPath), "Shared.fs should be created")

            // Check .vscode/settings.json
            let vscodeSettingsPath = Path.Combine(folder, ".vscode", "settings.json")
            Assert.True(File.Exists(vscodeSettingsPath), ".vscode/settings.json should be created")

            // Check generated files in .elmish-land
            let baseProjectPath =
                Path.Combine(folder, ".elmish-land", "Base", $"ElmishLand.%s{projectName}.Base.fsproj")

            Assert.True(File.Exists(baseProjectPath), "Base project should be generated")

            let appProjectPath =
                Path.Combine(folder, ".elmish-land", "App", $"ElmishLand.%s{projectName}.App.fsproj")

            Assert.True(File.Exists(appProjectPath), "App project should be generated")

            // Verify routeData was returned
            Assert.NotNull(routeData)
            Assert.True(routeData.Routes.Length > 0, "Should have at least one route")
            Assert.True(routeData.Layouts.Length > 0, "Should have at least one layout")
        })

[<Fact>]
let ``Add page command with multiple URL parts, own layout and auto updating layout reference, updates correct layout reference in page``
    ()
    =
    withNewProject (fun absoluteProjectDir _ ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir

            do!
                eff {
                    do! addLayout (FilePath.fromString folder) absoluteProjectDir "/About/Me" true
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/About/Me" true
                }
                |> Expects.effectOk runEff

            File.ReadAllText(Path.Combine(folder, "src", "Pages", "About", "Me", "Page.fs"))
            |> Expects.containsSubstring "| LayoutMsg of About.Me.Layout.Msg" // Ensure that the auto accept write correct namespace
        })

[<Fact>]
let ``Ensure auto accept layout change write correct namespace`` () =
    withNewProject (fun absoluteProjectDir _ ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir

            do!
                eff {
                    do! addLayout (FilePath.fromString folder) absoluteProjectDir "/Hello/Me" true
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/Hello/Me" true
                    do! addLayout (FilePath.fromString folder) absoluteProjectDir "/Hello" true
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/Hello" true
                }
                |> Expects.effectOk runEff

            let expectedPagePath =
                Path.Combine(folder, "src", "Pages", "Hello", "Me", "Page.fs")

            Assert.True(File.Exists(expectedPagePath), "Page not found")

            File.ReadAllText(expectedPagePath)
            |> Expects.containsSubstring "| LayoutMsg of Hello.Me.Layout.Msg" // Ensure that the auto accept write correct namespace
        })

[<Fact>]
let ``Init command creates a buildable project`` () =
    withNewProject (fun projectDir _ -> expectProjectTypeChecks projectDir)
