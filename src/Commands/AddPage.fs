module ElmishLand.AddPage

open System
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Effect
open ElmishLand.Resources
open ElmishLand.Settings
open Orsak
open ElmishLand.Base
open ElmishLand.TemplateEngine

let private showProjectDiffPreview (log: ILog) (projectPath: FsProjPath) (filePath: string) =
    let snippet =
        ElmishLand.FsProj.previewAddCompileIncludeSnippet projectPath filePath 5

    if not (String.IsNullOrWhiteSpace snippet) then
        log.Info("Planned project file change (preview):" + snippet)

let promptUserForProjectFileUpdate
    (log: ILog)
    (projectPath: FsProjPath)
    (filePath: string)
    (promptAccept: AutoUpdateCode)
    =
    showProjectDiffPreview log projectPath filePath

    match promptAccept with
    | Accept ->
        log.Info $"🤖 Auto-accepting: Adding '%s{filePath}' to project file"
        true
    | Decline ->
        log.Info $"🤖 Auto-declining: Adding '%s{filePath}' to project file"
        false
    | Ask ->
        log.Info $"\nDo you want to add the new page to your project file '%s{FsProjPath.asString projectPath}'? [y/N]"

        let response = Console.ReadLine()

        String.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
        || String.Equals(response, "yes", StringComparison.OrdinalIgnoreCase)

let addCompileIncludeToProject (log: ILog) (projectPath: FsProjPath) (filePath: string) =
    try
        let projectContent = File.ReadAllText(FsProjPath.asString projectPath)

        // Check if file already exists in project
        if
            projectContent.Contains($"<Compile Include=\"%s{filePath}\" />")
            || projectContent.Contains($"<Compile Include=\"%s{filePath}\"/>")
        then
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

                // Get all compile entries and find the right position for the page file
                let compileEntryPattern = @"<Compile\s+Include=""([^""]+)""\s*/>"
                let compileMatches = Regex.Matches(itemGroupContent, compileEntryPattern)
                let entries = [| for m in compileMatches -> m.Groups.[1].Value |] |> Array.toList

                // Find where to insert: after all layout files in same directory,
                // but as the last page file in that directory
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

                // Determine indentation from within the same ItemGroup
                let normalizedGroup = itemGroupContent.Replace("\r\n", "\n")
                let groupLines: string array = normalizedGroup.Split('\n')

                let leadingWhitespace (s: string) =
                    let mutable i = 0

                    while i < s.Length && (s[i] = ' ' || s[i] = '\t') do
                        i <- i + 1

                    s.Substring(0, i)

                let findLineIndexForCompileInGroup includePath =
                    let escaped = Regex.Escape(includePath)
                    let rx = Regex("<\\s*Compile\\b[^>]*?Include\\s*=\\s*\"" + escaped + "\"[^>]*?/?>")
                    groupLines |> Array.tryFindIndex (fun l -> rx.IsMatch(l))

                let indentForAppendFromGroup () =
                    // Use the full project content instead of just the group content for consistent indentation
                    let projectLines = projectContent.Replace("\r\n", "\n").Split('\n')
                    let rx = Regex("<\\s*Compile\\b", RegexOptions.IgnoreCase)

                    let result =
                        projectLines
                        |> Array.tryPick (fun l -> if rx.IsMatch(l) then Some(leadingWhitespace l) else None)
                        |> Option.defaultValue "    "

                    result

                let indent =
                    match insertPosition with
                    | Some pos ->
                        match findLineIndexForCompileInGroup entries[pos] with
                        | Some i -> leadingWhitespace groupLines[i]
                        | None -> indentForAppendFromGroup ()
                    | None -> indentForAppendFromGroup ()

                let newEntryTrim = $"<Compile Include=\"%s{filePath}\" />"
                let newEntry = indent + newEntryTrim

                let updatedItemGroup =
                    match insertPosition with
                    | Some pos ->
                        // Insert at specific position; include indentation in the match to avoid double-indent
                        let spaced = $"<Compile Include=\"%s{entries.[pos]}\" />"
                        let tight = $"<Compile Include=\"%s{entries.[pos]}\"/>"
                        let spacedWithIndent = indent + spaced
                        let tightWithIndent = indent + tight

                        if itemGroupContent.Contains(spacedWithIndent) then
                            itemGroupContent.Replace(spacedWithIndent, $"%s{newEntry}\n%s{spacedWithIndent}")
                        elif itemGroupContent.Contains(tightWithIndent) then
                            itemGroupContent.Replace(tightWithIndent, $"%s{newEntry}\n%s{tightWithIndent}")
                        elif itemGroupContent.Contains(spaced) then
                            // Fallback: match without indent (rare)
                            itemGroupContent.Replace(spaced, $"%s{newEntry}\n%s{indent}%s{spaced}")
                        elif itemGroupContent.Contains(tight) then
                            itemGroupContent.Replace(tight, $"%s{newEntry}\n%s{indent}%s{tight}")
                        else
                            // Fallback: append at end of group
                            let closingTagLine =
                                let lines = itemGroupContent.Replace("\r\n", "\n").Split('\n')

                                lines
                                |> Array.tryFind (fun l -> l.Contains("</ItemGroup>"))
                                |> Option.defaultValue "  </ItemGroup>"

                            let replacement = $"%s{newEntry}\n%s{closingTagLine}"
                            itemGroupContent.Replace(closingTagLine, replacement)
                    | None ->
                        // Add at the end - find the correct indentation for the closing tag
                        let closingTagLine =
                            let lines = itemGroupContent.Replace("\r\n", "\n").Split('\n')

                            lines
                            |> Array.tryFind (fun l -> l.Contains("</ItemGroup>"))
                            |> Option.defaultValue "  </ItemGroup>"

                        let replacement = $"%s{newEntry}\n%s{closingTagLine}"
                        itemGroupContent.Replace(closingTagLine, replacement)

                let updatedContent = projectContent.Replace(itemGroupContent, updatedItemGroup)

                File.WriteAllText(FsProjPath.asString projectPath, updatedContent)
                true
            else
                log.Info "⚠️  Could not find suitable ItemGroup to add the file to. Please add manually."
                false
    with ex ->
        log.Info $"⚠️  Failed to update project file: %s{ex.Message}. Please add manually."
        false

let addPage workingDirectory absoluteProjectDir (url: string) (promptAccept: AutoUpdateCode) =
    eff {
        let! log = Log().Get()

        log.Debug("Working driectory: {}", FilePath.asString workingDirectory)
        log.Debug("Project directory: {}", absoluteProjectDir |> AbsoluteProjectDir.asFilePath |> FilePath.asString)

        let! projectPath = absoluteProjectDir |> FsProjPath.findExactlyOneFromProjectDir
        let projectName = projectPath |> ProjectName.fromFsProjPath

        log.Debug("Project file: {}", FsProjPath.asString projectPath)
        log.Debug("Project name: {}", ProjectName.asString projectName)

        let routeFileParts =
            url
            |> String.split "/"
            |> Array.map (fun x -> $"%s{x[0..0].ToUpper()}%s{x[1..]}")
            |> fun x -> [ "src"; "Pages"; yield! x; "Page.fs" ]

        let routeFilePath =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts routeFileParts

        log.Debug("routeFilePath: {}", routeFilePath)

        let! settings = getSettings absoluteProjectDir

        let! route = fileToRoute projectName absoluteProjectDir settings.RouteSettings routeFilePath

        let! projectPath = absoluteProjectDir |> FsProjPath.findExactlyOneFromProjectDir
        let rootModuleName = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded

        writeResource<AddPage_template> log (AbsoluteProjectDir.asFilePath absoluteProjectDir) false routeFileParts {
            ViewModule = settings.View.Module
            ViewType = settings.View.Type
            ScaffoldTextElement = settings.View.TextElement
            RootModule = rootModuleName
            Route = route
        }

        let relativefilePathString = $"""%s{routeFileParts |> String.concat "/"}"""

        // Ask user if they want to automatically add the file to the project
        let shouldUpdateProject =
            promptUserForProjectFileUpdate log projectPath relativefilePathString promptAccept

        let projectUpdateResult =
            if shouldUpdateProject then
                addCompileIncludeToProject log projectPath relativefilePathString
            else
                false

        // Ensure Page.fs entries are last in their directories
        do! ElmishLand.FsProj.writePageFilesLast absoluteProjectDir

        let! routeData = getTemplateData projectName absoluteProjectDir
        log.Debug("routeData: {}", routeData)
        generateFiles log (AbsoluteProjectDir.asFilePath absoluteProjectDir) routeData

        let manualInstructions =
            if not projectUpdateResult then
                $"""
Please add the file to the project using an IDE or add the following line to an
ItemGroup in the project file '%s{FsProjPath.asString projectPath}':
<Compile Include="%s{relativefilePathString}" />
"""
            else
                ""

        $"""%s{getAppTitle ()} added a new page at %s{url}
You can edit your new page here:
./%s{relativefilePathString}%s{manualInstructions}
"""
        |> log.Info
    }
