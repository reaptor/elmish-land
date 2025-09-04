module CommandTests

open System
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.AddPage
open ElmishLand.AddLayout
open Runner
open Xunit

let getFolder () =
    "Proj_" + Guid.NewGuid().ToString().Replace("-", "")

let getSimpleFolder () =
    "TestProject" + Random().Next(1000, 9999).ToString()

let leadingWhitespace (s: string) =
    let mutable i = 0

    while i < s.Length && (s[i] = ' ' || s[i] = '\t') do
        i <- i + 1

    s.Substring(0, i)

let createMinimalProject projectPath =
    let fsprojContent =
        """<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <LangVersion>latest</LangVersion>
    </PropertyGroup>

    <ItemGroup>
        <Compile Include="src/Shared.fs"/>
        <Compile Include="src/Pages/NotFound.fs"/>
        <Compile Include="src/Pages/Layout.fs"/>
        <Compile Include="src/Pages/Page.fs"/>
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include=".elmish-land/Base/ElmishLand.TestProject.Base.fsproj" />
    </ItemGroup>
</Project>
"""

    // Create directory structure
    Directory.CreateDirectory(projectPath) |> ignore
    Directory.CreateDirectory(Path.Combine(projectPath, "src", "Pages")) |> ignore

    Directory.CreateDirectory(Path.Combine(projectPath, ".elmish-land", "Base"))
    |> ignore

    // Create files
    let projectName = Path.GetFileName(projectPath)
    File.WriteAllText(Path.Combine(projectPath, projectName + ".fsproj"), fsprojContent)
    File.WriteAllText(Path.Combine(projectPath, "src", "Shared.fs"), "module Shared")
    File.WriteAllText(Path.Combine(projectPath, "src", "Pages", "NotFound.fs"), "module NotFound")
    File.WriteAllText(Path.Combine(projectPath, "src", "Pages", "Layout.fs"), "module Layout")
    File.WriteAllText(Path.Combine(projectPath, "src", "Pages", "Page.fs"), "module Page")

    File.WriteAllText(
        Path.Combine(projectPath, "elmish-land.json"),
        """{"view": {"module": "Feliz", "type": "ReactElement", "textElement": "Html.text"}}"""
    )

    File.WriteAllText(
        Path.Combine(projectPath, ".elmish-land", "Base", "ElmishLand." + projectName + ".Base.fsproj"),
        "<Project />"
    )

// Simple file system that just delegates to real file system
// This is needed because the addPage function uses direct file system calls
let realFileSystem =
    { new IFileSystem with
        member _.FilePathExists(filePath, isDirectory) =
            let path = FilePath.asString filePath

            if isDirectory then
                Directory.Exists(path)
            else
                File.Exists(path)

        member _.GetParentDirectory(filePath) =
            let path = FilePath.asString filePath
            let parent = Path.GetDirectoryName(path)

            if String.IsNullOrEmpty(parent) then
                None
            else
                Some(FilePath.fromString parent)

        member _.GetFilesRecursive(filePath, searchPattern) =
            let path = FilePath.asString filePath

            if Directory.Exists(path) then
                Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories)
                |> Array.map FilePath.fromString
            else
                [||]

        member _.ReadAllText(filePath) =
            File.ReadAllText(FilePath.asString filePath)
    }

[<Fact>]
let ``addPage creates page with correct indentation in preview and fsproj`` () =
    task {
        let folder = getFolder ()

        try
            // Create minimal project structure - much faster than using init
            createMinimalProject folder

            let absoluteProjectDir = AbsoluteProjectDir(FilePath.fromString folder)

            // Run addPage with auto-accept = true to avoid user interaction
            let! result, logs = runEff realFileSystem (addPage absoluteProjectDir "/Page1" true)

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
            let projectName = Path.GetFileName(folder)
            let fsprojPath = Path.Combine(folder, projectName + ".fsproj")
            let fsprojContent = File.ReadAllText(fsprojPath)
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
            let pageFilePath = Path.Combine(folder, "src", "Pages", "Page1", "Page.fs")
            Assert.True(File.Exists(pageFilePath), "Page file should be created")

        finally
            if Directory.Exists(folder) then
                Directory.Delete(folder, true)
    }

[<Fact>]
let ``addLayout after addPage updates project file order and page layout reference`` () =
    task {
        let folder = getSimpleFolder ()

        try
            // Create minimal project structure - much faster than using init
            createMinimalProject folder

            let absoluteProjectDir = AbsoluteProjectDir(FilePath.fromString folder)

            // Step 1: Add a page first
            let! addPageResult, addPageLogs = runEff realFileSystem (addPage absoluteProjectDir "/AnotherPage" true)
            let _addPageSuccess = Expects.ok addPageLogs addPageResult

            // Verify page was added to project file
            let projectName = Path.GetFileName(folder)
            let fsprojPath = Path.Combine(folder, projectName + ".fsproj")
            let fsprojContent1 = File.ReadAllText(fsprojPath)

            Assert.True(
                fsprojContent1.Contains("src/Pages/AnotherPage/Page.fs"),
                "Page should be added to project file"
            )

            // Verify page file was created
            let pageFilePath = Path.Combine(folder, "src", "Pages", "AnotherPage", "Page.fs")
            Assert.True(File.Exists(pageFilePath), "Page file should be created")

            // Step 2: Add a layout for the same path
            let! addLayoutResult, addLayoutLogs =
                runEff realFileSystem (addLayout absoluteProjectDir "/AnotherPage" true)

            let _addLayoutSuccess = Expects.ok addLayoutLogs addLayoutResult

            // Verify layout was added to project file
            let fsprojContent2 = File.ReadAllText(fsprojPath)

            Assert.True(
                fsprojContent2.Contains("src/Pages/AnotherPage/Layout.fs"),
                "Layout should be added to project file"
            )

            // Verify layout file was created
            let layoutFilePath =
                Path.Combine(folder, "src", "Pages", "AnotherPage", "Layout.fs")

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

        finally
            if Directory.Exists(folder) then
                Directory.Delete(folder, true)
    }

[<Fact>]
let ``nested layout with page uses correct layout message reference`` () =
    task {
        let folder = getSimpleFolder ()

        try
            // Create minimal project structure - much faster than using init
            createMinimalProject folder

            let absoluteProjectDir = AbsoluteProjectDir(FilePath.fromString folder)

            // Step 1: Add nested layout first
            let! addLayoutResult, addLayoutLogs =
                runEff realFileSystem (addLayout absoluteProjectDir "/PageWithNestedLayout" true)

            let _addLayoutSuccess = Expects.ok addLayoutLogs addLayoutResult

            // Verify layout was added to project file
            let projectName = Path.GetFileName(folder)
            let fsprojPath = Path.Combine(folder, projectName + ".fsproj")
            let fsprojContent1 = File.ReadAllText(fsprojPath)

            Assert.True(
                fsprojContent1.Contains("src/Pages/PageWithNestedLayout/Layout.fs"),
                "Nested layout should be added to project file"
            )

            // Verify nested layout file was created
            let nestedLayoutFilePath =
                Path.Combine(folder, "src", "Pages", "PageWithNestedLayout", "Layout.fs")

            Assert.True(File.Exists(nestedLayoutFilePath), "Nested layout file should be created")

            // Step 2: Add page for the nested layout path
            let! addPageResult, addPageLogs =
                runEff realFileSystem (addPage absoluteProjectDir "/PageWithNestedLayout" true)

            let _addPageSuccess = Expects.ok addPageLogs addPageResult

            // Verify page was added to project file
            let fsprojContent2 = File.ReadAllText(fsprojPath)

            Assert.True(
                fsprojContent2.Contains("src/Pages/PageWithNestedLayout/Page.fs"),
                "Nested page should be added to project file"
            )

            // Verify nested page file was created
            let nestedPageFilePath =
                Path.Combine(folder, "src", "Pages", "PageWithNestedLayout", "Page.fs")

            Assert.True(File.Exists(nestedPageFilePath), "Nested page file should be created")

            // Step 3: Verify correct project file ordering (Layout before Page)
            let layoutIndex = fsprojContent2.IndexOf("src/Pages/PageWithNestedLayout/Layout.fs")
            let pageIndex = fsprojContent2.IndexOf("src/Pages/PageWithNestedLayout/Page.fs")
            Assert.True(layoutIndex >= 0 && pageIndex >= 0, "Both nested files should be in project")
            Assert.True(layoutIndex < pageIndex, "Nested layout file should come before nested page file")

            // Step 4: Verify main page exists (it's created by createMinimalProject)
            let mainPageFilePath = Path.Combine(folder, "src", "Pages", "Page.fs")
            Assert.True(File.Exists(mainPageFilePath), "Main page file should exist")

            // Step 5: Verify nested page uses correct specific layout message
            let nestedPageContent = File.ReadAllText(nestedPageFilePath)

            Assert.True(
                nestedPageContent.Contains("| LayoutMsg of PageWithNestedLayout.Layout.Msg"),
                "Nested page should reference PageWithNestedLayout.Layout.Msg"
            )

            // Step 6: Verify that both layout files exist
            let mainLayoutFilePath = Path.Combine(folder, "src", "Pages", "Layout.fs")
            Assert.True(File.Exists(mainLayoutFilePath), "Main layout file should exist")
            Assert.True(File.Exists(nestedLayoutFilePath), "Nested layout file should exist")

        finally
            if Directory.Exists(folder) then
                Directory.Delete(folder, true)
    }

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
        let errors, warnings =
            ElmishLand.FableOutput.processOutput mockOutput mockErrorOutput false

        // In non-verbose mode, ONLY the actual error should be shown, NOT "Build failed"
        Assert.Equal(1, errors |> List.length)

        Assert.Contains(
            "./src/Pages/Page.fs(23,5): (23,22) error FSHARP: This value is not a function and cannot be applied. (code 3)",
            errors
        )

        // Verify warnings list is empty for this output
        Assert.Equal(0, warnings |> List.length)
    }
