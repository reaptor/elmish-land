module FableErrorAnalyzerTests

open System
open Xunit
open ElmishLand.Base
open ElmishLand.FableErrorAnalyzer
open ElmishLand.Settings

let createTestSettings () = {
    View = {|
        Module = "Feliz"
        Type = "ReactElement"
        TextElement = "Html.text"
    |}
    ProjectReferences = []
    DefaultLayoutTemplate = None
    DefaultPageTemplate = None
    RouteSettings = RouteParameters([])
    ServerCommand = None
}

[<Fact>]
let ``hasCompilationErrors detects F# compilation errors`` () =
    let output = "error FS0039: The value or constructor 'init' is not defined"
    let result = hasCompilationErrors output
    Assert.True(result)

[<Fact>]
let ``hasCompilationErrors detects build failures`` () =
    let output = "Build FAILED with 3 errors"
    let result = hasCompilationErrors output
    Assert.True(result)

[<Fact>]
let ``hasCompilationErrors returns false for successful build`` () =
    let output = "Build succeeded. 0 Warning(s) 0 Error(s)"
    let result = hasCompilationErrors output
    Assert.False(result)

[<Fact>]
let ``analyzeAppFsErrors converts FS0039 missing init error to user-friendly message`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The value or constructor 'init' is not defined in 'App.fs(123,45)' via TestProject.Pages.Home.init
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Single(result) |> ignore
    match result.[0] with
    | PageError (file, message) ->
        Assert.Contains("Home/Page.fs", file)
        Assert.Contains("missing required 'init' function", message)
        Assert.Contains("Expected signature", message)
    | _ -> Assert.True(false, "Expected PageError")

[<Fact>]
let ``analyzeAppFsErrors converts layout missing update error to user-friendly message`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The value or constructor 'update' is not defined in 'App.fs(456,78)' via TestProject.Pages.Main.Layout.from
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Single(result) |> ignore
    match result.[0] with
    | LayoutError (file, message) ->
        Assert.Contains("Main/Layout.fs", file)
        Assert.Contains("missing required 'update' function", message)
    | _ -> Assert.True(false, "Expected LayoutError")

[<Fact>]
let ``analyzeAppFsErrors converts type mismatch error to user-friendly message`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0001: Type mismatch in 'App.fs(789,12)' via TestProject.Pages.About.init. Expected 'Model * Command' but got 'Model'
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Single(result) |> ignore
    match result.[0] with
    | PageError (file, message) ->
        Assert.Contains("About/Page.fs", file)
        Assert.Contains("incorrect return type", message)
        Assert.Contains("(Model * Command)", message)
    | _ -> Assert.True(false, "Expected PageError")

[<Fact>]
let ``analyzeAppFsErrors handles missing view function with custom view type`` () =
    let settings = { createTestSettings () with
                        View = {|
                            Module = "Fable.React"
                            Type = "ReactElement"
                            TextElement = "str"
                        |} }
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The value 'view' is not defined in 'App.fs(100,20)' via TestProject.Pages.Contact.view
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Single(result) |> ignore
    match result.[0] with
    | PageError (file, message) ->
        Assert.Contains("Contact/Page.fs", file)
        Assert.Contains("missing required 'view' function", message)
        Assert.Contains("ReactElement", message)
    | _ -> Assert.True(false, "Expected PageError")

[<Fact>]
let ``analyzeAppFsErrors handles missing Model type`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The type 'Model' is not defined in 'App.fs(200,30)' via TestProject.Pages.Profile.Model
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Single(result) |> ignore
    match result.[0] with
    | PageError (file, message) ->
        Assert.Contains("Profile/Page.fs", file)
        Assert.Contains("missing required 'Model' type definition", message)
    | _ -> Assert.True(false, "Expected PageError")

[<Fact>]
let ``analyzeAppFsErrors handles missing Props type in layout`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The type 'Props' is not defined in 'App.fs(300,40)' via TestProject.Pages.Sidebar.Props
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Single(result) |> ignore
    match result.[0] with
    | LayoutError (file, message) ->
        Assert.Contains("Sidebar/Layout.fs", file)
        Assert.Contains("missing required 'Props' type definition", message)
    | _ -> Assert.True(false, "Expected LayoutError")

[<Fact>]
let ``analyzeAppFsErrors returns empty list for non-App.fs errors`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The value 'something' is not defined in 'SomeOtherFile.fs(100,20)'
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Empty(result)

[<Fact>]
let ``analyzeAppFsErrors handles multiple errors`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
error FS0039: The value 'init' is not defined in 'App.fs(123,45)' via TestProject.Pages.Home.init
error FS0039: The value 'update' is not defined in 'App.fs(456,78)' via TestProject.Pages.Main.Layout.from
error FS0039: The type 'Model' is not defined in 'App.fs(789,12)' via TestProject.Pages.About.Model
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Equal(3, result.Length)
    
    // Should have mix of page and layout errors
    let pageErrors = result |> List.choose (function | PageError (f, m) -> Some (f, m) | _ -> None)
    let layoutErrors = result |> List.choose (function | LayoutError (f, m) -> Some (f, m) | _ -> None)
    
    Assert.Equal(2, pageErrors.Length) // Home.init and About.Model
    Assert.Equal(1, layoutErrors.Length) // Main.Layout.from

[<Fact>]
let ``analyzeAppFsErrors handles real Fable error format`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
./.elmish-land/App/App.fs(123,73): (123,142) error FSHARP: Type mismatch. Expecting a    'string -> MappedPage<Pages.Page.Msg,Pages.Page.Model,Pages.Layout.Msg,Pages.Layout.Props>'    but given a    'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b> -> MappedPage<Pages.Page.Msg,'a,Pages.Layout.Msg,'b>'    The type 'string' does not match the type 'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b>' (code 1)
./.elmish-land/App/App.fs(245,62): (245,66) error FSHARP: The type 'String' does not define the field, constructor or member 'View'. (code 39)
./.elmish-land/App/App.fs(270,111): (270,124) error FSHARP: The type 'String' does not define the field, constructor or member 'Subscriptions'. Maybe you want one of the following:   Substring (code 39)
"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Equal(3, result.Length)
    
    // First error should be page type mismatch
    match result.[0] with
    | PageError (file, message) ->
        Assert.Contains("Page/Page.fs", file)
        Assert.Contains("type mismatch", message)
    | _ -> Assert.True(false, "Expected PageError for first error")
    
    // Second error should be about missing View
    match result.[1] with
    | PageError (file, message) ->
        Assert.Contains("Page/Page.fs", file)
        Assert.Contains("view", message)
    | _ -> Assert.True(false, "Expected PageError for second error")
    
    // Third error should be about missing Subscriptions
    match result.[2] with
    | PageError (file, message) ->
        Assert.Contains("Page/Page.fs", file)
        Assert.Contains("subscriptions", message)
    | _ -> Assert.True(false, "Expected PageError for third error")

[<Fact>]
let ``analyzeAppFsErrors handles page function returning string instead of Page from quicktest scenario`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    // Real error output from quicktest when page function returns "apa" instead of Page.from
    let errorOutput = """./.elmish-land/App/App.fs(123,73): (123,142) error FSHARP: Type mismatch. Expecting a    'string -> MappedPage<Pages.Page.Msg,Pages.Page.Model,Pages.Layout.Msg,Pages.Layout.Props>'    but given a    'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b> -> MappedPage<Pages.Page.Msg,'a,Pages.Layout.Msg,'b>'    The type 'string' does not match the type 'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b>' (code 1)
./.elmish-land/App/App.fs(245,62): (245,66) error FSHARP: The type 'String' does not define the field, constructor or member 'View'. (code 39)
./.elmish-land/App/App.fs(270,111): (270,124) error FSHARP: The type 'String' does not define the field, constructor or member 'Subscriptions'. Maybe you want one of the following:   Substring (code 39)"""
    
    let result = analyzeAppFsErrors projectDir settings errorOutput
    
    Assert.Equal(3, result.Length)
    
    // All errors should point to Page.fs and provide helpful guidance
    for i in 0..2 do
        match result.[i] with
        | PageError (file, message) ->
            Assert.Contains("Page/Page.fs", file)
            // Check that the messages provide helpful guidance about the page function
            if i = 0 then
                Assert.Contains("page", message.ToLower())
                Assert.Contains("Page.from", message)
            elif i = 1 then
                Assert.Contains("view", message.ToLower())
            else // i = 2
                Assert.Contains("subscriptions", message.ToLower())
        | LayoutError _ -> Assert.True(false, $"Expected PageError for error %d{i}, got LayoutError")

[<Fact>]
let ``analyzeAppFsErrors should help determine when to suppress original Fable output`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    
    // Test case 1: Errors that can be translated (should be suppressed)
    let translatableErrorOutput = """
./.elmish-land/App/App.fs(123,73): (123,142) error FSHARP: Type mismatch. Expecting a    'string -> MappedPage<Pages.Page.Msg,Pages.Page.Model,Pages.Layout.Msg,Pages.Layout.Props>'    but given a    'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b> -> MappedPage<Pages.Page.Msg,'a,Pages.Layout.Msg,'b>'    The type 'string' does not match the type 'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b>' (code 1)
./.elmish-land/App/App.fs(245,62): (245,66) error FSHARP: The type 'String' does not define the field, constructor or member 'View'. (code 39)
"""
    
    let translatableResult = analyzeAppFsErrors projectDir settings translatableErrorOutput
    
    // Should have translations for App.fs errors
    Assert.Equal(2, translatableResult.Length)
    Assert.All(translatableResult, fun error ->
        match error with
        | PageError (_, message) -> Assert.NotEmpty(message)
        | LayoutError (_, message) -> Assert.NotEmpty(message)
    )
    
    // Test case 2: Errors that cannot be translated (should NOT be suppressed)
    let nonTranslatableErrorOutput = """
SomeOtherFile.fs(10,5): error FS0001: This value is not a function
AnotherFile.fs(20,10): error FS0039: The value 'something' is not defined
"""
    
    let nonTranslatableResult = analyzeAppFsErrors projectDir settings nonTranslatableErrorOutput
    
    // Should have no translations for non-App.fs errors
    Assert.Empty(nonTranslatableResult)

[<Fact>]
let ``processErrorOutput correctly identifies lines to suppress`` () =
    let settings = createTestSettings ()
    let projectDir = AbsoluteProjectDir(FilePath.fromString "/test/project")
    let errorOutput = """
./.elmish-land/App/App.fs(123,73): (123,142) error FSHARP: Type mismatch. Expecting a    'string -> MappedPage<Pages.Page.Msg,Pages.Page.Model,Pages.Layout.Msg,Pages.Layout.Props>'    but given a    'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b> -> MappedPage<Pages.Page.Msg,'a,Pages.Layout.Msg,'b>'    The type 'string' does not match the type 'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b>' (code 1)
./.elmish-land/App/App.fs(245,62): (245,66) error FSHARP: The type 'String' does not define the field, constructor or member 'View'. (code 39)
SomeOtherFile.fs(10,5): error FS0001: This value is not a function
Build FAILED with 3 errors
"""
    
    let (translatedErrors, linesToSuppress) = processErrorOutput projectDir settings errorOutput
    
    // Should have 2 translated errors
    Assert.Equal(2, translatedErrors.Length)
    
    // Should suppress 2 lines (the App.fs error lines)
    Assert.Equal(2, linesToSuppress.Count)
    
    // Check that App.fs lines are in suppression set
    let appFsLine1 = "./.elmish-land/App/App.fs(123,73): (123,142) error FSHARP: Type mismatch. Expecting a    'string -> MappedPage<Pages.Page.Msg,Pages.Page.Model,Pages.Layout.Msg,Pages.Layout.Props>'    but given a    'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b> -> MappedPage<Pages.Page.Msg,'a,Pages.Layout.Msg,'b>'    The type 'string' does not match the type 'Page<SharedMsg,'a,Pages.Page.Msg,Pages.Layout.Msg,'b>' (code 1)"
    let appFsLine2 = "./.elmish-land/App/App.fs(245,62): (245,66) error FSHARP: The type 'String' does not define the field, constructor or member 'View'. (code 39)"
    
    Assert.Contains(appFsLine1, linesToSuppress)
    Assert.Contains(appFsLine2, linesToSuppress)
    
    // Check that non-App.fs lines are NOT in suppression set
    let otherLine = "SomeOtherFile.fs(10,5): error FS0001: This value is not a function"
    let buildLine = "Build FAILED with 3 errors"
    Assert.DoesNotContain(otherLine, linesToSuppress)
    Assert.DoesNotContain(buildLine, linesToSuppress)