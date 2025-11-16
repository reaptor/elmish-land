module ElmishLand.AddLayout

open System
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Effect
open ElmishLand.Resources
open ElmishLand.Settings
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine

type PageUpdate = {
    PageFilePath: FilePath
    RelativePath: string
    OldLayoutMsg: string
    NewLayoutMsg: string
    CurrentContent: string
    UpdatedContent: string
}

let findAffectedPages absoluteProjectDir layoutFilePath (routeData: TemplateData) =
    let layoutDir = layoutFilePath |> FilePath.directoryPath

    let pagesDir =
        absoluteProjectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.appendParts [ "src"; "Pages" ]

    routeData.Routes
    |> Array.choose (fun route ->
        // Check all routes to see if their actual page file content uses main layout but should use the new layout
        let modulePathParts =
            route.ModuleName.Replace($"%s{routeData.RootModule}.Pages.", "").Split('.')
            |> Array.filter (fun x -> x <> "Page")

        let pageFilePath =
            if modulePathParts.Length = 0 then
                pagesDir |> FilePath.appendParts [| "Page.fs" |]
            else
                pagesDir |> FilePath.appendParts (Array.append modulePathParts [| "Page.fs" |])

        let pageDir = pageFilePath |> FilePath.directoryPath

        // Check if new layout directory is a parent of or equal to the page directory
        let layoutDirString = FilePath.asString layoutDir
        let pageDirString = FilePath.asString pageDir

        if
            pageDirString.StartsWith(layoutDirString)
            && File.Exists(FilePath.asString pageFilePath)
        then
            let relativePath =
                FilePath.asString pageFilePath
                |> fun path ->
                    path.Replace(FilePath.asString (AbsoluteProjectDir.asFilePath absoluteProjectDir) + "/", "")

            let currentContent = File.ReadAllText(FilePath.asString pageFilePath)
            let oldLayoutMsg = "| LayoutMsg of Layout.Msg"

            // Only process pages that are actually using the main layout in their content
            // but should be using the new closer layout
            if currentContent.Contains(oldLayoutMsg) && not route.IsMainLayout then
                // Calculate the new layout name from the path
                let newLayoutName =
                    layoutDir
                    |> FilePath.asString
                    |> fun path -> path.Replace(FilePath.asString pagesDir + "/", "")
                    |> String.replace "/" "."

                let newLayoutMsg = $"| LayoutMsg of %s{newLayoutName}.Layout.Msg"

                let updatedContent = currentContent.Replace(oldLayoutMsg, newLayoutMsg)

                Some {
                    PageFilePath = pageFilePath
                    RelativePath = relativePath
                    OldLayoutMsg = oldLayoutMsg
                    NewLayoutMsg = newLayoutMsg
                    CurrentContent = currentContent
                    UpdatedContent = updatedContent
                }
            else
                None
        else
            None)

let promptUserForUpdates (log: ILog) (pageUpdates: PageUpdate[]) (promptBehaviour: UserPromptBehaviour) =
    if pageUpdates.Length > 0 then
        match promptBehaviour with
        | AutoAccept ->
            log.Info $"🤖 Auto-accepting: Updating %i{pageUpdates.Length} page(s) to use the new layout:"

            for pageUpdate in pageUpdates do
                log.Info $"  📄 %s{pageUpdate.RelativePath}"

            true
        | AutoDecline ->
            log.Info $"🤖 Auto-declining: Updating %i{pageUpdates.Length} page(s) to use the new layout:"

            for pageUpdate in pageUpdates do
                log.Info $"  📄 %s{pageUpdate.RelativePath}"

            false
        | Ask ->
            log.Info
                $"\n%i{pageUpdates.Length} page(s) are currently using a higher level layout and must be updated to use the new closer layout:"

            for pageUpdate in pageUpdates do
                log.Info $"\n📄 %s{pageUpdate.RelativePath}"

            log.Info "Would you like to update these files automatically? [y/N]"

            let response = Console.ReadLine()

            String.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || String.Equals(response, "yes", StringComparison.OrdinalIgnoreCase)
    else
        false

let applyPageUpdates (log: ILog) (pageUpdates: PageUpdate[]) =
    for pageUpdate in pageUpdates do
        File.WriteAllText(FilePath.asString pageUpdate.PageFilePath, pageUpdate.UpdatedContent)
        log.Info $"✅ Updated %s{pageUpdate.RelativePath}"

let private showProjectDiffPreview (log: ILog) (projectPath: FsProjPath) (filePath: string) =
    let snippet =
        ElmishLand.FsProj.previewAddCompileIncludeSnippet projectPath filePath 5

    if not (String.IsNullOrWhiteSpace snippet) then
        log.Info("Planned project file change (preview):" + snippet)

let promptUserForProjectFileUpdate
    (log: ILog)
    (projectPath: FsProjPath)
    (filePath: string)
    (promptBehaviour: UserPromptBehaviour)
    =
    match promptBehaviour with
    | AutoAccept ->
        showProjectDiffPreview log projectPath filePath
        log.Info $"🤖 Auto-accepting: Adding '%s{filePath}' to project file"
        true
    | AutoDecline ->
        showProjectDiffPreview log projectPath filePath
        log.Info $"🤖 Auto-declining: Adding '%s{filePath}' to project file"
        false
    | Ask ->
        showProjectDiffPreview log projectPath filePath

        log.Info
            $"\nDo you want to add the new layout to your project file '%s{FsProjPath.asString projectPath}'? [y/N]"

        let response = Console.ReadLine()

        String.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
        || String.Equals(response, "yes", StringComparison.OrdinalIgnoreCase)

let addCompileIncludeToProject (log: ILog) (projectPath: FsProjPath) (filePath: string) =
    try
        let projectContent = File.ReadAllText(FsProjPath.asString projectPath)

        // Check if file already exists in project
        if projectContent.Contains($"<Compile Include=\"%s{filePath}\" />") then
            log.Info $"📄 File '%s{filePath}' already exists in project file"
            true
        else
            // Find the last </ItemGroup> that contains <Compile Include entries
            let compileItemGroupPattern =
                @"(<ItemGroup[^>]*>[\s\S]*?<Compile\s+Include[^>]*>[\s\S]*?</ItemGroup>)"

            let matches = Regex.Matches(projectContent, compileItemGroupPattern)

            if matches.Count > 0 then
                let lastMatch = matches[matches.Count - 1]
                let itemGroupContent = lastMatch.Value

                // Extract directory from the new file path for ordering
                let newFileDir =
                    if filePath.Contains("/") then
                        filePath.Substring(0, filePath.LastIndexOf("/"))
                    else
                        ""

                // Get all compile entries and find the right position for the layout file
                let compileEntryPattern = @"<Compile\s+Include=""([^""]+)""\s*/>"
                let compileMatches = Regex.Matches(itemGroupContent, compileEntryPattern)
                let entries = [| for m in compileMatches -> m.Groups.[1].Value |] |> Array.toList

                // Find insert position
                let insertPosition =
                    entries
                    |> List.tryFindIndex (fun entry ->
                        let entryDir =
                            if entry.Contains("/") then
                                entry.Substring(0, entry.LastIndexOf("/"))
                            else
                                ""

                        if entryDir = newFileDir && entry.EndsWith("Page.fs") then
                            true
                        elif entryDir > newFileDir then
                            true
                        else
                            false)

                // Determine indentation from the anchor line (or previous compile) to keep alignment
                let normalized = projectContent.Replace("\r\n", "\n")
                let lines: string array = normalized.Split('\n')

                let leadingWhitespace (s: string) =
                    let mutable i = 0

                    while i < s.Length && (s[i] = ' ' || s[i] = '\t') do
                        i <- i + 1

                    s.Substring(0, i)

                let findLineIndexForCompile includePath =
                    let escaped = Regex.Escape(includePath)
                    let rx = Regex("<\\s*Compile\\b[^>]*?Include\\s*=\\s*\"" + escaped + "\"[^>]*?/?>")
                    lines |> Array.tryFindIndex (fun l -> rx.IsMatch(l))

                let indent =
                    match insertPosition with
                    | Some pos ->
                        match findLineIndexForCompile entries[pos] with
                        | Some i -> leadingWhitespace lines[i]
                        | None -> "    "
                    | None ->
                        // find last compile line to copy its indent
                        let rx = Regex("<\\s*Compile\\b", RegexOptions.IgnoreCase)

                        lines
                        |> Array.mapi (fun i l -> i, l)
                        |> Array.rev
                        |> Array.tryPick (fun (_, l) -> if rx.IsMatch(l) then Some(leadingWhitespace l) else None)
                        |> Option.defaultValue "    "

                let newEntry = indent + $"<Compile Include=\"%s{filePath}\" />"

                match insertPosition with
                | Some pos ->
                    // Insert at specific position (before first page file in same directory)
                    let entryToReplace = $"<Compile Include=\"%s{entries.[pos]}\" />"

                    let updatedContent =
                        itemGroupContent.Replace(entryToReplace, $"%s{newEntry}\n%s{indent}%s{entryToReplace}")

                    File.WriteAllText(
                        FsProjPath.asString projectPath,
                        projectContent.Replace(itemGroupContent, updatedContent)
                    )

                    log.Info $"✅ Added '%s{filePath}' to project file"
                | None ->
                    // Add at the end
                    let updatedContent =
                        itemGroupContent.Replace("</ItemGroup>", $"%s{newEntry}\n  </ItemGroup>")

                    File.WriteAllText(
                        FsProjPath.asString projectPath,
                        projectContent.Replace(itemGroupContent, updatedContent)
                    )

                    log.Info $"✅ Added '%s{filePath}' to project file"

                true
            else
                log.Info "⚠️  Could not find suitable ItemGroup to add the file to. Please add manually."
                false
    with ex ->
        log.Info $"⚠️  Failed to update project file: %s{ex.Message}. Please add manually."
        false

let addLayout workingDirectory absoluteProjectDir (url: string) (promptBehaviour: UserPromptBehaviour) =
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

        // Get current route data to identify affected pages
        let! routeData = getTemplateData projectName absoluteProjectDir
        log.Debug("routeData: {}", routeData)

        // Find pages that should use the new layout but are still using main layout
        let affectedPages = findAffectedPages absoluteProjectDir layoutFilePath routeData

        // Ask user if they want to update the affected pages
        let shouldUpdate = promptUserForUpdates log affectedPages promptBehaviour

        if shouldUpdate then
            applyPageUpdates log affectedPages
            log.Info "\n🔄 Regenerating project files with updated page references..."

        let relativefilePathString = $"""%s{layoutFileParts |> String.concat "/"}"""

        // Ask user if they want to automatically add the layout file to the project
        let shouldUpdateProject =
            if affectedPages.Length > 0 && not shouldUpdate then
                // Don't ask about project file if we already prompted about page updates and user declined
                false
            else
                promptUserForProjectFileUpdate log projectPath relativefilePathString promptBehaviour

        let projectUpdateResult =
            if shouldUpdateProject then
                addCompileIncludeToProject log projectPath relativefilePathString
            else
                false

        // Ensure Page.fs entries are last in their directories
        do! ElmishLand.FsProj.writePageFilesLast absoluteProjectDir promptBehaviour

        // Generate files (this will include any updated pages if user chose to update them)
        generateFiles log (AbsoluteProjectDir.asFilePath absoluteProjectDir) routeData

        let pageUpdateSummary =
            if shouldUpdate && affectedPages.Length > 0 then
                $"\n✅ Updated %i{affectedPages.Length} page file(s) to use the new layout."
            elif affectedPages.Length > 0 && not shouldUpdate then
                let pageList =
                    affectedPages |> Array.map (fun p -> p.RelativePath) |> String.concat "\n  - "

                $"""
⚠️  WARNING: The following page files should use this new layout but are still referencing a higher level layout:
  - %s{pageList}

These pages need to be updated manually or by running this command again and choosing 'y' when prompted.
You can also run 'elmish-land restore' to regenerate all files.
"""
            else
                ""

        let manualInstructions =
            if not projectUpdateResult then
                $"""
Please add the file to the project using an IDE or add the following line to an
ItemGroup in the project file '%s{FsProjPath.asString projectPath}':
<Compile Include="%s{relativefilePathString}" />
"""
            else
                ""

        $"""%s{getAppTitle ()} added a new layout at %s{url}
You can edit your new layout here:
./%s{relativefilePathString}%s{manualInstructions}%s{pageUpdateSummary}
"""
        |> log.Info
    }
