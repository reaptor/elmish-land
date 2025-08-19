module ElmishLand.FableErrorAnalyzer

open System.Text.RegularExpressions
open ElmishLand.Base
open ElmishLand.Settings

type FableError = {
    OriginalError: string
    UserFriendlyError: string
}

type PageLayoutError = 
    | PageError of string * string  // filename, error message
    | LayoutError of string * string // filename, error message

/// Extract page/layout filename from App.fs error context
let private extractSourceFile (errorText: string) =
    // Look for patterns that indicate the source page/layout
    // Handle patterns like "Pages.Page.Msg" or "Pages.Layout.Props"
    let pagePattern = @"Pages\.(\w+)\.(\w+)"
    let pageMatch = Regex.Match(errorText, pagePattern)
    
    if pageMatch.Success then
        let moduleName = pageMatch.Groups.[1].Value
        let functionOrType = pageMatch.Groups.[2].Value
        
        // Determine if this is a layout or page based on the context
        // Be more specific: only treat as layout if the module name suggests it or specific layout functions are mentioned
        if functionOrType = "Layout" || 
           functionOrType = "Props" ||
           errorText.Contains("routeChanged") ||
           (moduleName.ToLower().Contains("layout")) then
            Some ($"%s{moduleName}/Layout.fs", true) // isLayout = true
        else
            Some ($"%s{moduleName}/Page.fs", false) // isLayout = false
    else
        // For errors without Pages. pattern, try to infer from error content
        // These are typically errors where a string is being used instead of a proper type
        if errorText.Contains("View") || errorText.Contains("Subscriptions") then
            // Default to Page.fs since these are common page-level errors
            Some ("Page/Page.fs", false)
        else
            None

/// Convert App.fs compilation errors to user-friendly messages
let analyzeAppFsErrors (_projectDir: AbsoluteProjectDir) (settings: Settings) (errorOutput: string) =
    let errors = ResizeArray<PageLayoutError>()
    let lines = errorOutput.Split([|'\n'; '\r'|], System.StringSplitOptions.RemoveEmptyEntries)
    
    for line in lines do
        // Look for F# compilation errors in App.fs (handle both formats: "App.fs(" and "./.elmish-land/App/App.fs(")
        if (line.Contains("App.fs(") || line.Contains("/App.fs(")) && 
           (line.Contains("error FS") || line.Contains("error FSHARP") || line.Contains("warning FS") || line.Contains("warning FSHARP")) then
            match extractSourceFile line with
            | Some (sourceFile, isLayout) ->
                let userFriendlyError = 
                    if line.Contains("FS0039") || line.Contains("code 39") then // Value or constructor not defined
                        if line.Contains("init") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'init' function. Expected signature: let init () = (model: Model), Command.none"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing required 'init' function. Expected signature: let init () = (model: Model), Command.none"
                        elif line.Contains("update") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'update' function. Expected signature: let update (msg: Msg) (model: Model) = (model: Model), Command.none"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing required 'update' function. Expected signature: let update (msg: Msg) (model: Model) = (model: Model), Command.none"
                        elif line.Contains("view") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'view' function. Expected signature: let view (model: Model) (content: %s{settings.View.Type}) (dispatch: Msg -> unit) = (%s{settings.View.Type})"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing required 'view' function. Expected signature: let view (model: Model) (dispatch: Msg -> unit) = (%s{settings.View.Type})"
                        elif line.Contains("routeChanged") then
                            $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'routeChanged' function. Expected signature: let routeChanged (model: Model) = (model: Model), Command.none"
                        elif line.Contains("page") || line.Contains("Page<") || line.Contains("MappedPage<") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' has a type mismatch in the 'layout' function. Expected signature: let layout (props: Props) (route: Route) (shared: SharedModel) = Layout.from init update routeChanged view"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' has a type mismatch in the 'page' function. Expected signature: let page (shared: SharedModel) (route: RouteType) = Page.from init update view () LayoutMsg"
                        elif line.Contains("layout") then
                            $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'layout' function. Expected signature: let layout (props: Props) (route: Route) (shared: SharedModel) = Layout.from init update routeChanged view"
                        elif line.Contains("Model") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'Model' type definition"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing required 'Model' type definition"
                        elif line.Contains("Msg") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'Msg' type definition"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing required 'Msg' type definition"
                        elif line.Contains("Props") then
                            $"Layout file 'src/Pages/%s{sourceFile}' is missing required 'Props' type definition"
                        elif line.Contains("View") && line.Contains("does not define") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing the 'view' function. Expected signature: let view (model: Model) (content: %s{settings.View.Type}) (dispatch: Msg -> unit) = (%s{settings.View.Type})"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing the 'view' function. Expected signature: let view (model: Model) (dispatch: Msg -> unit) = (%s{settings.View.Type})"
                        elif line.Contains("Subscriptions") && line.Contains("does not define") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' is missing the 'subscriptions' function. This is optional, but if referenced, should have signature: let subscriptions (model: Model) = Sub.none"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' is missing the 'subscriptions' function. This is optional, but if referenced, should have signature: let subscriptions (model: Model) = Sub.none"
                        else
                            // Generic missing value error
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' has compilation errors. Please check the function signatures match the expected patterns."
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' has compilation errors. Please check the function signatures match the expected patterns."
                    elif line.Contains("FS0001") || line.Contains("Type mismatch") || line.Contains("code 1") then // Type mismatch
                        if line.Contains("init") || line.Contains("update") || line.Contains("routeChanged") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' function has incorrect return type. Functions like init, update, and routeChanged must return (Model * Command)."
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' function has incorrect return type. Functions like init and update must return (Model * Command)."
                        elif line.Contains("page") || line.Contains("Page<") || line.Contains("MappedPage<") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' has a type mismatch in the 'layout' function. The function should return Layout.from with the correct parameters and types."
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' has a type mismatch in the 'page' function. The function should return Page.from with the correct parameters and types."
                        elif line.Contains("view") then
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' view function has incorrect signature. Expected: let view (model: Model) (content: %s{settings.View.Type}) (dispatch: Msg -> unit) = (%s{settings.View.Type})"
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' view function has incorrect signature. Expected: let view (model: Model) (dispatch: Msg -> unit) = (%s{settings.View.Type})"
                        else
                            // Generic type mismatch
                            if isLayout then
                                $"Layout file 'src/Pages/%s{sourceFile}' has type mismatch errors. Please check the function signatures match the expected types."
                            else
                                $"Page file 'src/Pages/%s{sourceFile}' has type mismatch errors. Please check the function signatures match the expected types."
                    else
                        // Other compilation errors
                        if isLayout then
                            $"Layout file 'src/Pages/%s{sourceFile}' has compilation errors: %s{line.Trim()}"
                        else
                            $"Page file 'src/Pages/%s{sourceFile}' has compilation errors: %s{line.Trim()}"
                
                if isLayout then
                    errors.Add(LayoutError (sourceFile, userFriendlyError))
                else
                    errors.Add(PageError (sourceFile, userFriendlyError))
            | None ->
                // App.fs error we couldn't map to a source file - show as is
                ()
    
    errors |> Seq.toList

/// Check if the error output contains compilation errors that should be analyzed
let hasCompilationErrors (output: string) =
    output.Contains("error FS") || output.Contains("error FSHARP") || output.Contains("Build FAILED") || output.Contains("Compilation failed") || (output.Contains("code ") && (output.Contains("error") || output.Contains("Error")))

/// Check if a specific error line is an App.fs error that can be translated to user-friendly format
let isTranslatableAppFsError (line: string) =
    (line.Contains("App.fs(") || line.Contains("/App.fs(")) && 
    (line.Contains("error FS") || line.Contains("error FSHARP") || line.Contains("warning FS") || line.Contains("warning FSHARP"))

/// Process error output and return (translatedErrors, linesToSuppress)
let processErrorOutput (projectDir: AbsoluteProjectDir) (settings: Settings) (errorOutput: string) =
    let translatedErrors = analyzeAppFsErrors projectDir settings errorOutput
    let lines = errorOutput.Split([|'\n'; '\r'|], System.StringSplitOptions.RemoveEmptyEntries)
    
    // Identify which lines should be suppressed (App.fs errors that we translated)
    let linesToSuppress = 
        lines 
        |> Array.filter isTranslatableAppFsError
        |> Set.ofArray
    
    (translatedErrors, linesToSuppress)