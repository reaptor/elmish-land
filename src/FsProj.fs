module ElmishLand.FsProj

open ElmishLand.Effect
open Orsak
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.AppError

/// Ensure that within each folder, any Compile Include ending with 'Page.fs' appears
/// after other Compile entries from the same folder. This function rewrites the project file
/// in-place if reordering is needed.
let ensurePageFilesLast absoluteProjectDir =
    eff {
        let! log = Log().Get()
        let! projectPath = FsProjPath.findExactlyOneFromProjectDir absoluteProjectDir
        let path = FsProjPath.asString projectPath
        let original = File.ReadAllText(path)

        let itemGroupPattern =
            Regex(@"(<ItemGroup[^>]*>[\s\S]*?</ItemGroup>)", RegexOptions.IgnoreCase)

        let compilePattern =
            Regex(@"[ \t]*<Compile\s+Include=""([^""]+)""\s*/>", RegexOptions.IgnoreCase)

        let mutable changed = false

        let newContent =
            itemGroupPattern.Replace(
                original,
                MatchEvaluator(fun ig ->
                    let groupText = ig.Value
                    let matches = compilePattern.Matches(groupText)

                    if matches.Count = 0 then
                        groupText
                    else

                        // Build entries list preserving original text and metadata
                        let entries = [
                            for i in 0 .. matches.Count - 1 do
                                let m = matches[i]
                                let includePath = m.Groups[1].Value

                                let dir =
                                    if includePath.Contains("/") then
                                        includePath.Substring(0, includePath.LastIndexOf('/'))
                                    else
                                        ""

                                let isPage = includePath.EndsWith("Page.fs", System.StringComparison.Ordinal)
                                yield (i, m.Index, m.Length, includePath, dir, isPage, m.Value)
                        ]

                        // Bubble-sort to push Page.fs to end within the same directory
                        let arr = entries |> List.toArray
                        let mutable swappedAny = false

                        for _round in 0 .. (arr.Length) do
                            for i in 0 .. (arr.Length - 2) do
                                let (_, _, _, _, dirA, isPageA, _) = arr[i]
                                let (_, _, _, _, dirB, isPageB, _) = arr[i + 1]

                                if isPageA && (dirA = dirB) && (not isPageB) then
                                    // swap
                                    let tmp = arr[i]
                                    arr[i] <- arr[i + 1]
                                    arr[i + 1] <- tmp
                                    swappedAny <- true

                        if swappedAny then
                            changed <- true

                        // Detect newline style
                        let newline = if groupText.Contains("\r\n") then "\r\n" else "\n"

                        // Reconstruct item group: keep header/footer, replace compile block order
                        let first = matches[0]
                        let last = matches[matches.Count - 1]
                        let prefix = groupText.Substring(0, first.Index)
                        let suffix = groupText.Substring(last.Index + last.Length)

                        let middle =
                            arr |> Array.map (fun (_, _, _, _, _, _, text) -> text) |> String.concat newline

                        prefix + middle + suffix)
            )

        if changed && newContent <> original then
            log.Info("Rewriting project file to ensure Page.fs files are last within their directories: {}", path)
            File.WriteAllText(path, newContent)

        return! Ok()
    }

let validate absoluteProjectDir =
    eff {
        let! log = Log().Get()
        let! projectPath = FsProjPath.findExactlyOneFromProjectDir absoluteProjectDir

        // Auto-fix ordering so Page.fs is last within its directory
        do! ensurePageFilesLast absoluteProjectDir

        log.Debug("Using {}", absoluteProjectDir)
        log.Debug("Using {}", projectPath)

        let formatError lineNr (line: string) (FilePath filePath) msg =
            let colNr = line.IndexOf(filePath)
            $"%s{FsProjPath.asString projectPath}(%i{lineNr},%i{colNr}) error: %s{msg}."

        let includedFSharpFileInfo =
            File.ReadAllLines(FsProjPath.asString projectPath)
            |> Array.mapi (fun lineNr line -> (lineNr + 1, line))
            |> Array.choose (fun (lineNr, line) ->
                let m = Regex.Match(line, """\s*<[Cc]ompile\s+[Ii]nclude="([^"]+)" """.TrimEnd())

                if m.Success then
                    Some(
                        lineNr,
                        line,
                        m.Groups[1].Value
                        |> FilePath.fromString
                        |> FilePath.removePath (absoluteProjectDir |> AbsoluteProjectDir.asFilePath)
                    )
                else
                    None)
            |> Array.toList

        let pagesDir =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts [ "src"; "Pages" ]

        log.Debug("Using pagesDir {}", pagesDir)

        let actualPageFiles =
            Directory.GetFiles(FilePath.asString pagesDir, "Page.fs", SearchOption.AllDirectories)
            |> Array.map FilePath.fromString
            |> Array.map (FilePath.removePath (absoluteProjectDir |> AbsoluteProjectDir.asFilePath))
            |> Set.ofArray

        log.Debug("actualPageFiles {}", actualPageFiles)

        let includedPageFiles =
            includedFSharpFileInfo
            |> List.map (fun (_, _, filePath) -> filePath)
            |> List.filter (fun x -> (FileName.fromFilePath x |> FileName.asString) = "Page.fs")
            |> Set.ofList

        log.Debug("includedPageFiles {}", includedPageFiles)

        let actualLayoutFiles =
            Directory.GetFiles(FilePath.asString pagesDir, "Layout.fs", SearchOption.AllDirectories)
            |> Array.map FilePath.fromString
            |> Array.map (FilePath.removePath (absoluteProjectDir |> AbsoluteProjectDir.asFilePath))
            |> Set.ofArray

        log.Debug("actualLayoutFiles {}", actualLayoutFiles)

        let includedLayoutFiles =
            includedFSharpFileInfo
            |> List.map (fun (_, _, filePath) -> filePath)
            |> List.filter (fun x -> (FileName.fromFilePath x |> FileName.asString) = "Layout.fs")
            |> Set.ofList

        log.Debug("includedLayoutFiles {}", includedLayoutFiles)

        let pageFilesMissingFromFsProj = Set.difference actualPageFiles includedPageFiles

        let layoutFilesMissingFromFsProj =
            Set.difference actualLayoutFiles includedLayoutFiles

        let errors: string list = [
            yield!
                includedFSharpFileInfo
                |> List.fold
                    (fun (prevFilePaths: FilePath list, errors) (lineNr, line, filePath) ->
                        let dir = FilePath.directoryPath filePath

                        match prevFilePaths with
                        | prevFilePath :: _ when
                            FileName.fromFilePath prevFilePath |> FileName.equalsString "Page.fs"
                            && FilePath.directoryPath prevFilePath |> FilePath.equals dir
                            ->
                            filePath :: prevFilePaths,
                            formatError
                                (lineNr - 1)
                                line
                                filePath
                                "Page.fs files must be the last file in the directory"
                            :: errors
                        | _ -> filePath :: prevFilePaths, errors)
                    ([], [])
                |> snd
                |> List.rev

            for pageFile in pageFilesMissingFromFsProj do
                $"""The page '%s{FilePath.asString pageFile}' is missing from the project file. Please add the file to the project using an IDE
    or add the following line to a ItemGroup in the project file '%s{FsProjPath.asString projectPath}':

    <Compile Include="%s{FilePath.asString pageFile}" />
       """

            for layoutFile in layoutFilesMissingFromFsProj do
                $"""The layout '%s{FilePath.asString layoutFile}' is missing from the project file. Please add the file to the project using an IDE
    or add the following line to a ItemGroup in the project file '%s{FsProjPath.asString projectPath}':

    <Compile Include="%s{FilePath.asString layoutFile}" />
       """
        ]

        return!
            match errors with
            | [] -> Ok()
            | errors -> Error(FsProjValidationError errors)
    }

/// Build a preview snippet showing where a new Compile Include line would be inserted
/// in the project file, with a green "+" line and N lines of context above and below.
/// Returns empty string if no reasonable preview can be created.
let previewAddCompileIncludeSnippet (projectPath: FsProjPath) (filePath: string) (contextLines: int) : string =
    try
        let projectContent: string = File.ReadAllText(FsProjPath.asString projectPath)

        let compileItemGroupPattern: string =
            "(<ItemGroup[^>]*>[\\s\\S]*?<Compile\\s+Include[^>]*>[\\s\\S]*?</ItemGroup>)"

        let matches = Regex.Matches(projectContent, compileItemGroupPattern)

        if matches.Count = 0 then
            ""
        else
            // Use the same item group as the inserter: the last ItemGroup that contains Compile entries
            let lastGroup = matches[matches.Count - 1]
            let itemGroupContent = lastGroup.Value

            let compileEntryPattern: string = "<Compile\\s+Include=\"([^\"]+)\"\\s*/>"
            let compileMatches = Regex.Matches(itemGroupContent, compileEntryPattern)
            let entries: string list = [ for m in compileMatches -> m.Groups.[1].Value ]

            // Determine directory of the new file
            let newFileDir: string =
                if filePath.Contains("/") then
                    filePath.Substring(0, filePath.LastIndexOf("/"))
                else
                    ""

            // Find insertion position similar to AddPage/AddLayout logic
            let insertPosition =
                entries
                |> List.tryFindIndex (fun (entry: string) ->
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

            let compileLineText = sprintf "<Compile Include=\"%s\" />" filePath
            let newEntryTrim = compileLineText.Trim()
            let green (s: string) = "\u001b[32m" + s + "\u001b[0m"

            // Normalize lines and create helpers over the whole file
            let normalized = projectContent.Replace("\r\n", "\n")
            let lines: string array = normalized.Split('\n')

            let tryIndex (needle: string) =
                lines |> Array.tryFindIndex (fun (l: string) -> l.Contains(needle))

            let tryLastIndex (needle: string) =
                lines
                |> Array.mapi (fun i l -> i, l)
                |> Array.fold (fun acc (i, l) -> if l.Contains(needle) then Some i else acc) None

            // Robust line lookup for a given Compile Include path, tolerant to whitespace and '/>' spacing
            let tryCompileLineIndex (includePath: string) : int option =
                let escaped = Regex.Escape(includePath)
                let pattern = "<\\s*Compile\\b[^>]*?Include\\s*=\\s*\"" + escaped + "\"[^>]*?/?>"
                let rx = Regex(pattern)
                lines |> Array.tryFindIndex (fun l -> rx.IsMatch(l))

            let leadingWhitespace (s: string) =
                let mutable i = 0

                while i < s.Length && (s[i] = ' ' || s[i] = '\t') do
                    i <- i + 1

                s.Substring(0, i)

            let buildWindowWith (greenLine: string) (insertAt: int) =
                let startIdx = max 0 (insertAt - contextLines)
                let endIdx = min (lines.Length - 1) (insertAt + contextLines)

                let above =
                    if insertAt > startIdx then
                        String.concat "\n" lines[startIdx .. insertAt - 1]
                    else
                        ""

                let below =
                    if insertAt <= endIdx then
                        String.concat "\n" lines[insertAt..endIdx]
                    else
                        ""

                let prefix = "\n...\n"
                let suffix = "\n..."

                if above = "" && below = "" then
                    "\n" + greenLine
                elif above = "" then
                    prefix + greenLine + "\n" + below + suffix
                elif below = "" then
                    prefix + above + "\n" + greenLine + suffix
                else
                    prefix + above + "\n" + greenLine + "\n" + below + suffix

            match insertPosition with
            | Some pos ->
                // Insert occurs before the anchor entry line within the same ItemGroup
                let anchorPath = entries |> List.item pos

                match tryCompileLineIndex anchorPath with
                | Some i ->
                    let indent = leadingWhitespace lines[i]
                    let greenLine = indent + green ("+ " + newEntryTrim)
                    buildWindowWith greenLine i
                | None ->
                    // Fallback: attempt literal variants
                    let anchor1 = sprintf "<Compile Include=\"%s\" />" anchorPath
                    let anchor2 = sprintf "<Compile Include=\"%s\"/>" anchorPath

                    match tryIndex anchor1 with
                    | Some i ->
                        let indent = leadingWhitespace lines[i]
                        let greenLine = indent + green ("+ " + newEntryTrim)
                        buildWindowWith greenLine i
                    | None ->
                        match tryIndex anchor2 with
                        | Some i ->
                            let indent = leadingWhitespace lines[i]
                            let greenLine = indent + green ("+ " + newEntryTrim)
                            buildWindowWith greenLine i
                        | None -> "\n" + green ("+ " + newEntryTrim)
            | None ->
                // Append before the closing tag of the SAME compile ItemGroup, not the last in file
                // Find the last compile entry in this group within the whole file, then the next </ItemGroup>
                let lastEntryAnchorPathOpt = entries |> List.tryLast

                let resultOpt =
                    match lastEntryAnchorPathOpt with
                    | Some lastPath ->
                        match tryCompileLineIndex lastPath with
                        | Some iEntry ->
                            // Find the next line with </ItemGroup> after iEntry
                            let rec loop i =
                                if i >= lines.Length then None
                                elif lines[i].Contains("</ItemGroup>") then Some(iEntry, i)
                                else loop (i + 1)

                            loop (iEntry + 1)
                        | None -> None
                    | None -> None

                match resultOpt with
                | Some(iEntry, iClose) ->
                    let indent = leadingWhitespace lines[iEntry]
                    let greenLine = indent + green ("+ " + newEntryTrim)
                    buildWindowWith greenLine iClose
                | None ->
                    // Fallback: use the last closing tag found in the file and try to infer indent
                    match tryLastIndex "</ItemGroup>" with
                    | Some iClose ->
                        // Look backwards for a compile line to take indent from
                        let rec findCompileBefore i =
                            if i < 0 then
                                None
                            else if Regex.IsMatch(lines[i], "<\\s*Compile\\b", RegexOptions.IgnoreCase) then
                                Some i
                            else
                                findCompileBefore (i - 1)

                        let indent =
                            match findCompileBefore (iClose - 1) with
                            | Some i -> leadingWhitespace lines[i]
                            | None -> "    " // default 4 spaces

                        let greenLine = indent + green ("+ " + newEntryTrim)
                        buildWindowWith greenLine iClose
                    | None -> "\n" + green ("+ " + newEntryTrim)
    with _ ->
        ""
