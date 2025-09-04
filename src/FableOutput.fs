module ElmishLand.FableOutput

open System
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

let processOutput (stdout: string) (stderr: string) (isVerbose: bool) =
    let errors = HashSet<string>()
    let warnings = HashSet<string>()

    let processLine (line: string) =
        let cleanLine = line |> stripAnsiCodes |> _.Trim()

        if cleanLine.Length > 0 then
            // Check if it's an actual compilation error or warning
            let isCompilationError =
                cleanLine.Contains(".fs(") && cleanLine.Contains("error FSHARP")

            let isCompilationWarning =
                cleanLine.Contains(".fs(") && cleanLine.Contains("warning FSHARP")

            if isCompilationError then
                errors.Add(cleanLine) |> ignore
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

    errors |> Seq.toList, warnings |> Seq.toList

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

let displayOutput (errors: string list) (warnings: string list) (isVerbose: bool) =
    if not isVerbose then
        displayWarnings warnings
        displayErrors errors
