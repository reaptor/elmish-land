module ElmishLand.FableOutput

open System
open System.IO
open System.Collections.Generic

let private stripAnsiCodes (text: string) =
    text
    |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, @"\x1B\[[^@-~]*[@-~]", "")
    |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, @"[\r\n]", "")

let private isError (line: string) =
    let cleanLine = stripAnsiCodes line
    // Check for actual compilation errors (file path with line/column numbers)
    let isCompilationError =
        cleanLine.Contains(".fs(") && cleanLine.Contains("error FSHARP")

    isCompilationError
    || cleanLine.Contains("Error FS")
    || cleanLine.Contains("FABLE: Cannot find")
    || cleanLine.Contains("Cannot resolve")
    || cleanLine.Contains("Build failed")

let private isWarning (line: string) =
    let cleanLine = stripAnsiCodes line

    cleanLine.Contains("warning", StringComparison.OrdinalIgnoreCase)
    || cleanLine.Contains("Warning FS")

let private detectLayoutMismatch (errorLine: string) (_allErrors: string list) =
    // Pattern: The type 'Layout.Msg' does not match the type 'About.Layout.Msg'
    // Or: The type 'About.Layout.Msg' does not match the type 'Layout.Msg'
    let pattern = @"The type '([^']+)' does not match the type '([^']+)'"
    let regex = System.Text.RegularExpressions.Regex(pattern)
    let match' = regex.Match(errorLine)

    if match'.Success then
        let type1 = match'.Groups.[1].Value
        let type2 = match'.Groups.[2].Value

        // Check if this is a layout message mismatch
        if
            (type1.EndsWith("Layout.Msg") || type1 = "Layout.Msg")
            && (type2.EndsWith("Layout.Msg") || type2 = "Layout.Msg")
        then
            // If error is in generated App.fs, try to determine which page has the error
            if errorLine.Contains(".elmish-land/App/App.fs") then
                // When we have a layout mismatch, we need to check which pages might be affected
                // The error "Layout.Msg doesn't match About.Layout.Msg" suggests a page that should
                // use About.Layout.Msg but is using Layout.Msg instead

                // Extract the layout that should be used (the more specific one)
                let expectedLayout =
                    if
                        type1 <> "Layout.Msg"
                        && type1 <> "Pages.Layout.Msg"
                        && type1.Contains(".Layout.Msg")
                    then
                        type1
                    elif
                        type2 <> "Layout.Msg"
                        && type2 <> "Pages.Layout.Msg"
                        && type2.Contains(".Layout.Msg")
                    then
                        type2
                    else
                        ""

                // Try to infer which pages should use this layout
                // For any nested layout (e.g., Products.Layout.Msg, Users.Settings.Layout.Msg)
                let inferredPage =
                    if expectedLayout.Contains(".Layout.Msg") then
                        // Extract the directory path from the layout name
                        // Examples:
                        // "Products.Layout.Msg" -> "Products"
                        // "Users.Settings.Layout.Msg" -> "Users/Settings"
                        // "Admin.Dashboard.Layout.Msg" -> "Admin/Dashboard"
                        let layoutPath =
                            expectedLayout
                                .Replace("Pages.", "") // Remove Pages prefix if present
                                .Replace(".Layout.Msg", "") // Remove suffix

                        if layoutPath <> "" && layoutPath <> "Pages" then
                            // Convert dots to directory separators
                            let dirPath = layoutPath.Replace(".", "/")
                            sprintf "src/Pages/%s/Page.fs" dirPath
                        else
                            "one of your pages"
                    else
                        "one of your pages"

                // Determine correct and wrong layouts
                // For About page: should use About.Layout.Msg not Pages.Layout.Msg
                let correctLayout =
                    // The specific layout (e.g., About.Layout.Msg) is usually the correct one
                    if type1 <> "Layout.Msg" && type1 <> "Pages.Layout.Msg" && type1.Contains(".") then
                        type1
                    elif type2 <> "Layout.Msg" && type2 <> "Pages.Layout.Msg" && type2.Contains(".") then
                        type2
                    else
                        "Pages.Layout.Msg"

                // The wrong layout is what the page is currently using
                // If the error says "Layout.Msg doesn't match About.Layout.Msg"
                // then the page is using Layout.Msg (wrong) when it should use About.Layout.Msg (correct)
                let wrongLayout =
                    // The wrong layout is the one that's NOT the correct layout
                    if type1 = correctLayout then
                        // If type1 is correct, then type2 is wrong
                        // Normalize "Layout.Msg" to "Pages.Layout.Msg" for consistency
                        if type2 = "Layout.Msg" || type2 = "Pages.Layout.Msg" then
                            "Pages.Layout.Msg"
                        else
                            type2
                    else if
                        // If type2 is correct, then type1 is wrong
                        // Normalize "Layout.Msg" to "Pages.Layout.Msg" for consistency
                        type1 = "Layout.Msg" || type1 = "Pages.Layout.Msg"
                    then
                        "Pages.Layout.Msg"
                    else
                        type1

                Some(inferredPage, wrongLayout, correctLayout)
            else
                // For non-App.fs errors, extract the page path normally
                let pathPattern = @"(.+\.fs)\(\d+,\d+\)"
                let pathMatch = System.Text.RegularExpressions.Regex.Match(errorLine, pathPattern)

                if pathMatch.Success then
                    let pagePath = pathMatch.Groups.[1].Value

                    // Determine the correct layout based on context
                    // The more specific layout (not Pages.Layout.Msg or Layout.Msg) is usually correct
                    let correctLayout =
                        if
                            type1 <> "Layout.Msg"
                            && type1 <> "Pages.Layout.Msg"
                            && type1.Contains(".Layout.Msg")
                        then
                            type1
                        elif
                            type2 <> "Layout.Msg"
                            && type2 <> "Pages.Layout.Msg"
                            && type2.Contains(".Layout.Msg")
                        then
                            type2
                        else if type1 = "Pages.Layout.Msg" || type1 = "Layout.Msg" then
                            type2
                        else
                            type1

                    let wrongLayout = if type1 = correctLayout then type2 else type1

                    Some(pagePath, wrongLayout, correctLayout)
                else
                    None
        else
            None
    else
        None

type LayoutMismatch = {
    PagePath: string
    WrongLayout: string
    CorrectLayout: string
}

type ProcessOutputResult = {
    Errors: string list
    Warnings: string list
    LayoutMismatches: LayoutMismatch list
}

let processOutput (stdout: string) (stderr: string) (isVerbose: bool) =
    let errors = HashSet<string>()
    let warnings = HashSet<string>()

    let layoutMismatchesMap =
        System.Collections.Generic.Dictionary<string, LayoutMismatch>()

    // Collect all lines first for context
    let allLines =
        [|
            stdout.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
            stderr.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
        |]
        |> Array.concat
        |> Array.map stripAnsiCodes
        |> Array.toList

    let processLine (line: string) =
        let cleanLine = line |> stripAnsiCodes |> _.Trim()

        if cleanLine.Length > 0 then
            // Check if it's an actual compilation error or warning
            let isCompilationError =
                cleanLine.Contains(".fs(") && cleanLine.Contains("error FSHARP")

            let isCompilationWarning =
                cleanLine.Contains(".fs(") && cleanLine.Contains("warning FSHARP")

            if isCompilationError then
                // Check for layout mismatch error
                match detectLayoutMismatch cleanLine allLines with
                | Some(pagePath, wrongLayout, correctLayout) ->
                    // Use page path as key to deduplicate
                    let key = pagePath

                    if not (layoutMismatchesMap.ContainsKey(key)) then
                        let helpfulError =
                            sprintf
                                "Page %s has wrong layout reference. It uses '%s' but should use '%s'"
                                pagePath
                                wrongLayout
                                correctLayout

                        errors.Add(helpfulError) |> ignore

                        layoutMismatchesMap.[key] <- {
                            PagePath = pagePath
                            WrongLayout = wrongLayout
                            CorrectLayout = correctLayout
                        }
                | None -> errors.Add(cleanLine) |> ignore
            elif isCompilationWarning then
                warnings.Add(cleanLine) |> ignore
            elif isVerbose then
                // In verbose mode, include other error/warning messages
                if isError cleanLine then
                    errors.Add(cleanLine) |> ignore
                elif isWarning cleanLine then
                    warnings.Add(cleanLine) |> ignore

    // Process stdout
    stdout.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.iter processLine

    // Process stderr
    stderr.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.iter processLine

    {
        Errors = errors |> Seq.toList
        Warnings = warnings |> Seq.toList
        LayoutMismatches = layoutMismatchesMap.Values |> Seq.toList
    }

let displayErrors (errors: string list) =
    if errors.Length > 0 then
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine("\nErrors:")

        for error in errors do
            Console.WriteLine($"  %s{error}")

        Console.ResetColor()

let displayWarnings (warnings: string list) =
    if warnings.Length > 0 then
        Console.ForegroundColor <- ConsoleColor.Yellow
        Console.WriteLine("\nWarnings:")

        for warning in warnings do
            Console.WriteLine($"  %s{warning}")

        Console.ResetColor()

let fixLayoutReference (pagePath: string) (wrongLayout: string) (correctLayout: string) =
    try
        let content = File.ReadAllText(pagePath)

        // Extract just the last part of the type for matching
        // e.g., "Pages.About.Layout.Msg" -> "About.Layout.Msg"
        // or "Layout.Msg" -> "Layout.Msg"
        let getShortForm (layoutType: string) =
            if layoutType.Contains("Pages.") && layoutType.StartsWith("Pages.") then
                layoutType.Substring(6) // Remove "Pages." prefix
            else
                layoutType

        // Also handle case where the file might have just "Layout.Msg"
        // but we're looking for "Pages.Layout.Msg"
        let wrongLayoutShort = getShortForm wrongLayout

        let wrongLayoutSimple =
            if wrongLayout = "Pages.Layout.Msg" || wrongLayout = "Layout.Msg" then
                "Layout.Msg"
            else
                wrongLayoutShort

        // Build pattern that matches either form
        let pattern =
            if wrongLayout = "Pages.Layout.Msg" || wrongLayout = "Layout.Msg" then
                // When the wrong layout is the root layout, match just "Layout.Msg"
                // because that's what appears in the source file
                @"\|\s*LayoutMsg\s+of\s+Layout\.Msg\b"
            elif wrongLayoutSimple = "Layout.Msg" then
                // Match just "Layout.Msg" (common case)
                @"\|\s*LayoutMsg\s+of\s+Layout\.Msg\b"
            else
                // Match either the full form or short form
                sprintf
                    @"\|\s*LayoutMsg\s+of\s+(%s|%s)\b"
                    (System.Text.RegularExpressions.Regex.Escape(wrongLayout))
                    (System.Text.RegularExpressions.Regex.Escape(wrongLayoutShort))

        // For replacement, use the short form if it's a nested layout
        let correctLayoutForReplacement =
            if
                correctLayout.Contains(".Layout.Msg")
                && correctLayout <> "Layout.Msg"
                && correctLayout <> "Pages.Layout.Msg"
            then
                getShortForm correctLayout
            else if correctLayout = "Pages.Layout.Msg" then
                "Layout.Msg"
            else
                correctLayout

        let replacement = sprintf "| LayoutMsg of %s" correctLayoutForReplacement

        let newContent =
            System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement)

        if newContent <> content then
            File.WriteAllText(pagePath, newContent)
            true
        else
            false
    with _ ->
        false

let findPagesWithWrongLayout (_wrongLayout: string) (correctLayout: string) =
    // Search for all Page.fs files that might have the wrong layout
    let pagesDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "Pages")
    let mutable foundPages = []

    if Directory.Exists(pagesDir) then
        // Find all Page.fs files recursively
        let pageFiles = Directory.GetFiles(pagesDir, "Page.fs", SearchOption.AllDirectories)

        for pageFile in pageFiles do
            try
                let content = File.ReadAllText(pageFile)
                // Look for the pattern "| LayoutMsg of Layout.Msg" (wrong) when it should be something else
                let pattern = @"\|\s*LayoutMsg\s+of\s+Layout\.Msg\b"

                if System.Text.RegularExpressions.Regex.IsMatch(content, pattern) then
                    // This page uses Layout.Msg - check if it should use something else
                    // based on its directory structure
                    let relativePath = Path.GetRelativePath(pagesDir, pageFile)

                    // If the page is in a subdirectory that has its own layout, it's wrong
                    if correctLayout.Contains(".Layout.Msg") && correctLayout <> "Pages.Layout.Msg" then
                        // Extract the expected directory path from the correct layout
                        // Examples:
                        // "Products.Layout.Msg" -> check if page is in "Products/"
                        // "Users.Settings.Layout.Msg" -> check if page is in "Users/Settings/"
                        let layoutPath =
                            correctLayout.Replace("Pages.", "").Replace(".Layout.Msg", "").Replace(".", "/") // Convert dots to path separators

                        // Check if this page is in that directory
                        let pageDir = Path.GetDirectoryName(relativePath).Replace("\\", "/")

                        if
                            layoutPath <> ""
                            && (pageDir = layoutPath || pageDir.StartsWith(layoutPath + "/"))
                        then
                            foundPages <- pageFile :: foundPages
            with _ ->
                ()

    foundPages

let promptForAutoFix (layoutMismatches: LayoutMismatch list) =
    if layoutMismatches.Length > 0 then
        Console.WriteLine()
        Console.Write("Would you like to automatically fix these layout references? [Y/n]: ")
        let response = Console.ReadLine()

        if response = "" || response.ToLower() = "y" || response.ToLower() = "yes" then
            let mutable fixedCount = 0

            for mismatch in layoutMismatches do
                // Check if we have a generic page path (couldn't determine exact page)
                let isGenericPath =
                    mismatch.PagePath.Contains("a page in")
                    || mismatch.PagePath.Contains("one of your pages")

                if isGenericPath then
                    // Find all pages with the wrong layout
                    let pagesToFix =
                        findPagesWithWrongLayout mismatch.WrongLayout mismatch.CorrectLayout

                    for pageFile in pagesToFix do
                        if fixLayoutReference pageFile mismatch.WrongLayout mismatch.CorrectLayout then
                            let relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), pageFile)
                            Console.ForegroundColor <- ConsoleColor.Green
                            Console.WriteLine($"  ✓ Fixed: %s{relativePath}")
                            Console.ResetColor()
                            fixedCount <- fixedCount + 1
                        else
                            let relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), pageFile)
                            Console.ForegroundColor <- ConsoleColor.Red
                            Console.WriteLine($"  ✗ Failed to fix: %s{relativePath}")
                            Console.ResetColor()
                else
                    // Handle specific page path as before
                    let cleanPath =
                        mismatch.PagePath
                            .Replace("./", "")
                            .Replace(".elmish-land/App/App.fs", "")
                            .Trim('/')

                    let absolutePath =
                        if Path.IsPathRooted(cleanPath) then
                            cleanPath
                        elif cleanPath.Length > 0 then
                            Path.Combine(Directory.GetCurrentDirectory(), cleanPath)
                        else
                            ""

                    if absolutePath.Length > 0 && File.Exists(absolutePath) then
                        if fixLayoutReference absolutePath mismatch.WrongLayout mismatch.CorrectLayout then
                            Console.ForegroundColor <- ConsoleColor.Green
                            Console.WriteLine($"  ✓ Fixed: %s{mismatch.PagePath}")
                            Console.ResetColor()
                            fixedCount <- fixedCount + 1
                        else
                            Console.ForegroundColor <- ConsoleColor.Red
                            Console.WriteLine($"  ✗ Failed to fix: %s{mismatch.PagePath}")
                            Console.ResetColor()
                    else
                        Console.ForegroundColor <- ConsoleColor.Red
                        Console.WriteLine($"  ✗ File not found: %s{cleanPath} (resolved to: %s{absolutePath})")
                        Console.ResetColor()

            if fixedCount > 0 then
                Console.WriteLine()
                Console.ForegroundColor <- ConsoleColor.Green
                Console.WriteLine($"Fixed %d{fixedCount} layout reference(s). Please rebuild your project.")
                Console.ResetColor()
                true
            else
                false
        else
            false
    else
        false

let displayOutput (errors: string list) (warnings: string list) (isVerbose: bool) =
    if not isVerbose then
        displayWarnings warnings
        displayErrors errors
