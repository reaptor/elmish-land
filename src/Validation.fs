module ElmishLand.Validation

open System
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open Orsak

type SignatureError = {
    FilePath: string
    FunctionName: string
    ExpectedSignature: string
    ActualSignature: string option
    LineNumber: int option
    ColumnNumber: int option
}

type ValidationError = {
    Errors: SignatureError list
    FableOutput: string
}

let private pageSignaturePattern = @"let\s+page\s+\([^)]*\)\s+\([^)]*\)\s*="
let private layoutSignaturePattern = @"let\s+layout\s+\([^)]*\)\s+\([^)]*\)\s+\([^)]*\)\s*="

let private extractLineNumber (line: string) =
    let pattern = @"\((\d+),(\d+)\)"
    let match' = Regex.Match(line, pattern)
    if match'.Success then
        let lineNum = Int32.Parse(match'.Groups.[1].Value)
        let colNum = Int32.Parse(match'.Groups.[2].Value)
        Some (lineNum, colNum)
    else
        None

let private parsePageFunctionSignature (filePath: string) (content: string) =
    eff {
        let! log = Log().Get()
        log.Debug("Parsing page function signature in: {}", filePath)
        
        let lines = content.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
        let mutable lineNumber = 0
        let mutable foundPageFunction = false
        let mutable actualSignature = None
        let mutable pageFromFound = false
        
        for line in lines do
            lineNumber <- lineNumber + 1
            let trimmedLine = line.Trim()
            if trimmedLine.Contains("let page") && not foundPageFunction then
                foundPageFunction <- true
                actualSignature <- Some trimmedLine
                log.Debug("Found page function in {}", filePath)
            elif trimmedLine.Contains("Page.from") then
                pageFromFound <- true
                log.Debug("Found Page.from call in {}", filePath)
        
        // If we found a page function but no Page.from call, that's an error
        let enhancedSignature = 
            match actualSignature, pageFromFound with
            | Some signature, false -> Some (signature + " // Missing Page.from call")
            | Some signature, true -> Some signature
            | None, _ -> None
        
        return enhancedSignature, if foundPageFunction then Some lineNumber else None
    }

let private parseLayoutFunctionSignature (filePath: string) (content: string) =
    eff {
        let! log = Log().Get()
        log.Debug("Parsing layout function signature in: {}", filePath)
        
        let lines = content.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
        let mutable lineNumber = 0
        let mutable foundLayoutFunction = false
        let mutable actualSignature = None
        let mutable layoutFromFound = false
        
        for line in lines do
            lineNumber <- lineNumber + 1
            let trimmedLine = line.Trim()
            if trimmedLine.Contains("let layout") && not foundLayoutFunction then
                foundLayoutFunction <- true
                actualSignature <- Some trimmedLine
                log.Debug("Found layout function in {}", filePath)
            elif trimmedLine.Contains("Layout.from") then
                layoutFromFound <- true
                log.Debug("Found Layout.from call in {}", filePath)
        
        // If we found a layout function but no Layout.from call, that's an error
        let enhancedSignature = 
            match actualSignature, layoutFromFound with
            | Some signature, false -> Some (signature + " // Missing Layout.from call")
            | Some signature, true -> Some signature
            | None, _ -> None
        
        return enhancedSignature, if foundLayoutFunction then Some lineNumber else None
    }

let validatePageSignature (filePath: FilePath) =
    eff {
        let! log = Log().Get()
        let content = File.ReadAllText(FilePath.asString filePath)
        let! actualSignature, lineNumber = parsePageFunctionSignature (FilePath.asString filePath) content
        
        let expectedSignature = "let page (shared: SharedModel) (route: Route) ="
        
        match actualSignature with
        | Some actual when not (actual.Contains("shared") && actual.Contains("route")) ->
            log.Debug("Page function signature mismatch in {}", FilePath.asString filePath)
            return Some {
                FilePath = FilePath.asString filePath
                FunctionName = "page"
                ExpectedSignature = expectedSignature
                ActualSignature = Some actual
                LineNumber = lineNumber
                ColumnNumber = None
            }
        | Some actual when actual.Contains("// Missing Page.from call") ->
            log.Debug("Page function missing Page.from call in {}", FilePath.asString filePath)
            return Some {
                FilePath = FilePath.asString filePath
                FunctionName = "page"
                ExpectedSignature = "Page function must call Page.from and return a Page instance"
                ActualSignature = Some (actual.Replace(" // Missing Page.from call", ""))
                LineNumber = lineNumber
                ColumnNumber = None
            }
        | Some _ ->
            log.Debug("Page function signature valid in {}", FilePath.asString filePath)
            return None
        | None ->
            log.Debug("No page function found in {}", FilePath.asString filePath)
            return Some {
                FilePath = FilePath.asString filePath
                FunctionName = "page"
                ExpectedSignature = expectedSignature
                ActualSignature = None
                LineNumber = None
                ColumnNumber = None
            }
    }

let validateLayoutSignature (filePath: FilePath) =
    eff {
        let! log = Log().Get()
        let content = File.ReadAllText(FilePath.asString filePath)
        let! actualSignature, lineNumber = parseLayoutFunctionSignature (FilePath.asString filePath) content
        
        let expectedSignature = "let layout (props: Props) (route: Route) (shared: SharedModel) ="
        
        match actualSignature with
        | Some actual when not (actual.Contains("props") && actual.Contains("route") && actual.Contains("shared")) ->
            log.Debug("Layout function signature mismatch in {}", FilePath.asString filePath)
            return Some {
                FilePath = FilePath.asString filePath
                FunctionName = "layout"
                ExpectedSignature = expectedSignature
                ActualSignature = Some actual
                LineNumber = lineNumber
                ColumnNumber = None
            }
        | Some actual when actual.Contains("// Missing Layout.from call") ->
            log.Debug("Layout function missing Layout.from call in {}", FilePath.asString filePath)
            return Some {
                FilePath = FilePath.asString filePath
                FunctionName = "layout"
                ExpectedSignature = "Layout function must call Layout.from and return a Layout instance"
                ActualSignature = Some (actual.Replace(" // Missing Layout.from call", ""))
                LineNumber = lineNumber
                ColumnNumber = None
            }
        | Some _ ->
            log.Debug("Layout function signature valid in {}", FilePath.asString filePath)
            return None
        | None ->
            log.Debug("No layout function found in {}", FilePath.asString filePath)
            return Some {
                FilePath = FilePath.asString filePath
                FunctionName = "layout"
                ExpectedSignature = expectedSignature
                ActualSignature = None
                LineNumber = None
                ColumnNumber = None
            }
    }

let parseFableErrors (fableOutput: string) =
    eff {
        let! log = Log().Get()
        log.Debug("Parsing Fable errors from output")
        
        let lines = fableOutput.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
        let mutable errors = []
        
        for line in lines do
            if line.Contains("error") && line.Contains(".fs") then
                log.Debug("Found potential error line: {}", line)
                // Parse file path and error details from Fable output
                let filePathPattern = @"([^(]+\.fs)"
                let fileMatch = Regex.Match(line, filePathPattern)
                if fileMatch.Success then
                    let filePath = fileMatch.Groups.[1].Value.Trim()
                    let lineColInfo = extractLineNumber line
                    
                    let error = {
                        FilePath = filePath
                        FunctionName = "unknown"
                        ExpectedSignature = "unknown"
                        ActualSignature = None
                        LineNumber = match lineColInfo with Some (line, _) -> Some line | None -> None
                        ColumnNumber = match lineColInfo with Some (_, col) -> Some col | None -> None
                    }
                    errors <- error :: errors
        
        return errors
    }

let validatePageFiles (absoluteProjectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()
        log.Debug("Validating page files in project: {}", AbsoluteProjectDir.asString absoluteProjectDir)
        
        let pagesDir = 
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts ["src"; "Pages"]
        
        if not (Directory.Exists(FilePath.asString pagesDir)) then
            log.Debug("Pages directory not found: {}", FilePath.asString pagesDir)
            return []
        else
            let pageFiles = Directory.GetFiles(FilePath.asString pagesDir, "*.fs", SearchOption.AllDirectories)
            let mutable allErrors = []
            
            for pageFile in pageFiles do
                let fileName = Path.GetFileNameWithoutExtension(pageFile)
                // Skip Layout.fs and NotFound.fs files
                if fileName <> "Layout" && fileName <> "NotFound" then
                    let! error = validatePageSignature (FilePath.fromString pageFile)
                    match error with
                    | Some err -> allErrors <- err :: allErrors
                    | None -> ()
            
            log.Debug("Found {} page signature errors", List.length allErrors)
            return allErrors
    }

let validateLayoutFiles (absoluteProjectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()
        log.Debug("Validating layout files in project: {}", AbsoluteProjectDir.asString absoluteProjectDir)
        
        let pagesDir = 
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts ["src"; "Pages"]
        
        if not (Directory.Exists(FilePath.asString pagesDir)) then
            log.Debug("Pages directory not found: {}", FilePath.asString pagesDir)
            return []
        else
            let layoutFiles = Directory.GetFiles(FilePath.asString pagesDir, "Layout.fs", SearchOption.AllDirectories)
            let mutable allErrors = []
            
            for layoutFile in layoutFiles do
                let! error = validateLayoutSignature (FilePath.fromString layoutFile)
                match error with
                | Some err -> allErrors <- err :: allErrors
                | None -> ()
            
            log.Debug("Found {} layout signature errors", List.length allErrors)
            return allErrors
    }

let formatValidationError (error: SignatureError) =
    let locationInfo = 
        match error.LineNumber, error.ColumnNumber with
        | Some line, Some col -> $" at line %d{line}, column %d{col}"
        | Some line, None -> $" at line %d{line}"
        | None, None -> ""
        | None, Some _ -> ""
    
    match error.ActualSignature with
    | Some actual ->
        $"""Function signature mismatch in %s{error.FilePath}%s{locationInfo}:

Expected: %s{error.ExpectedSignature}
Found:    %s{actual}

The '%s{error.FunctionName}' function must have the correct parameters to work with elmish-land.
Please update the function signature to match the expected format."""
    | None ->
        $"""Missing '%s{error.FunctionName}' function in %s{error.FilePath}:

Expected: %s{error.ExpectedSignature}

The '%s{error.FunctionName}' function is required for elmish-land to work properly.
Please add this function to your file."""

let formatValidationErrors (errors: SignatureError list) =
    errors
    |> List.map formatValidationError
    |> String.concat "\n\n"