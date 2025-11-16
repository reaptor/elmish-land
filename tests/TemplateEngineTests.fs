module TemplateEngineTests

open System
open System.IO
open ElmishLand.AddPage
open ElmishLand.Base
open ElmishLand.Settings
open ElmishLand.TemplateEngine
open Runner
open TestProjectGeneration
open Xunit
open Orsak

let createTestProjectStructure () =
    let tempDir = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString())
    let testProjectDir = Path.Combine(tempDir, "TestProject")

    Directory.CreateDirectory(Path.Combine(testProjectDir, "src", "Pages", "Test"))
    |> ignore

    // Create Layout.fs file for tests that need it
    File.WriteAllText(Path.Combine(testProjectDir, "src", "Pages", "Test", "Layout.fs"), "module Layout")

    tempDir, testProjectDir

[<Fact>]
let ``Ensure multiple query params are included in UrlUsage (Route.format)`` () =
    let tempDir, testProjectDir = createTestProjectStructure ()
    let projectDir = FilePath.fromString testProjectDir
    let absoluteProjectDir = AbsoluteProjectDir projectDir
    let pageFile = FilePath.appendParts [ "src"; "Pages"; "Test"; "Page.fs" ] projectDir

    let queryParam1 = {
        Name = "first"
        Module = "System"
        Type = "string"
        Parse = None
        Format = None
        Required = false
    }

    let queryParam2 = {
        Name = "second"
        Module = "System"
        Type = "string"
        Parse = None
        Format = None
        Required = false
    }

    let routeParams = RouteParameters [ "/Test", (None, [ queryParam1; queryParam2 ]) ]

    let expectedRoute = {
        Name = "Test"
        RouteName = "TestRoute"
        LayoutName = "Test"
        LayoutModuleName = "TestProject.Pages.Test.Layout"
        LayoutModulePath = "Test"
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ First: string option; Second: string option }"
        RecordConstructor = """{ First = tryGetQuery "first" Some q; Second = tryGetQuery "second" Some q; }"""
        RecordPattern = "{ First = first; Second = second }"
        UrlUsage =
            """"test", [ match first with Some x -> "first",  x | None -> () ] @ [ match second with Some x -> "second",  x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        IsMainLayout = false
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        try
            let! result, logs =
                fileToRoute
                    (ProjectName.fromAbsoluteProjectDir absoluteProjectDir)
                    absoluteProjectDir
                    routeParams
                    pageFile
                |> runEff

            result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
        finally
            if Directory.Exists(tempDir) then
                Directory.Delete(tempDir, true)
    }

[<Fact>]
let ``Ensure all query params with reserved names are generated correctly`` () =
    let tempDir, testProjectDir = createTestProjectStructure ()
    let projectDir = FilePath.fromString testProjectDir
    let absoluteProjectDir = AbsoluteProjectDir projectDir
    let pageFile = FilePath.appendParts [ "src"; "Pages"; "Test"; "Page.fs" ] projectDir

    let queryParamWReservedName = {
        Name = "to"
        Module = "System"
        Type = "string"
        Parse = None
        Format = None
        Required = false
    }

    let routeParams = RouteParameters [ "/Test", (None, [ queryParamWReservedName ]) ]

    let expectedRoute = {
        Name = "Test"
        RouteName = "TestRoute"
        LayoutName = "Test"
        LayoutModuleName = "TestProject.Pages.Test.Layout"
        LayoutModulePath = "Test"
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ To: string option }"
        RecordConstructor = """{ To = tryGetQuery "to" Some q; }"""
        RecordPattern = "{ To = ``to`` }"
        UrlUsage = """"test", [ match ``to`` with Some x -> "to",  x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        IsMainLayout = false
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        try
            let! result, logs =
                fileToRoute
                    (ProjectName.fromAbsoluteProjectDir absoluteProjectDir)
                    absoluteProjectDir
                    routeParams
                    pageFile
                |> runEff

            result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
        finally
            if Directory.Exists(tempDir) then
                Directory.Delete(tempDir, true)
    }

[<Fact>]
let ``Ensure query params are camel cased`` () =
    let tempDir, testProjectDir = createTestProjectStructure ()
    let projectDir = FilePath.fromString testProjectDir
    let absoluteProjectDir = AbsoluteProjectDir projectDir
    let pageFile = FilePath.appendParts [ "src"; "Pages"; "Test"; "Page.fs" ] projectDir

    let queryParamWReservedName = {
        Name = "QueryParam"
        Module = "System"
        Type = "string"
        Parse = None
        Format = None
        Required = false
    }

    let routeParams = RouteParameters [ "/Test", (None, [ queryParamWReservedName ]) ]

    let expectedRoute = {
        Name = "Test"
        RouteName = "TestRoute"
        LayoutName = "Test"
        LayoutModuleName = "TestProject.Pages.Test.Layout"
        LayoutModulePath = "Test"
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ QueryParam: string option }"
        RecordConstructor = """{ QueryParam = tryGetQuery "queryParam" Some q; }"""
        RecordPattern = "{ QueryParam = queryParam }"
        UrlUsage = """"test", [ match queryParam with Some x -> "queryParam",  x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        IsMainLayout = false
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        try
            let! result, logs =
                fileToRoute
                    (ProjectName.fromAbsoluteProjectDir absoluteProjectDir)
                    absoluteProjectDir
                    routeParams
                    pageFile
                |> runEff

            result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
        finally
            if Directory.Exists(tempDir) then
                Directory.Delete(tempDir, true)
    }

[<Fact>]
let ``Ensure query param parse and format are generated correctly`` () =
    let tempDir, testProjectDir = createTestProjectStructure ()
    let projectDir = FilePath.fromString testProjectDir
    let absoluteProjectDir = AbsoluteProjectDir projectDir
    let pageFile = FilePath.appendParts [ "src"; "Pages"; "Test"; "Page.fs" ] projectDir

    let queryParam1 = {
        Name = "fromDate"
        Module = "Common"
        Type = "ApplicationDate"
        Parse = Some "ApplicationDate.fromQueryparam"
        Format = Some "ApplicationDate.asQueryparam"
        Required = false
    }

    let routeParams = RouteParameters [ "/Test", (None, [ queryParam1 ]) ]

    let expectedRoute = {
        Name = "Test"
        RouteName = "TestRoute"
        LayoutName = "Test"
        LayoutModuleName = "TestProject.Pages.Test.Layout"
        LayoutModulePath = "Test"
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ FromDate: ApplicationDate option }"
        RecordConstructor = """{ FromDate = tryGetQuery "fromDate" ApplicationDate.fromQueryparam q; }"""
        RecordPattern = "{ FromDate = fromDate }"
        UrlUsage =
            """"test", [ match fromDate with Some x -> "fromDate", ApplicationDate.asQueryparam x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        IsMainLayout = false
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        try
            let! result, logs =
                fileToRoute
                    (ProjectName.fromAbsoluteProjectDir absoluteProjectDir)
                    absoluteProjectDir
                    routeParams
                    pageFile
                |> runEff

            result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
        finally
            if Directory.Exists(tempDir) then
                Directory.Delete(tempDir, true)
    }

[<Fact>]
let ``Ensure required query params is generated correctly`` () =
    let tempDir, testProjectDir = createTestProjectStructure ()
    let projectDir = FilePath.fromString testProjectDir
    let absoluteProjectDir = AbsoluteProjectDir projectDir
    let pageFile = FilePath.appendParts [ "src"; "Pages"; "Test"; "Page.fs" ] projectDir

    let queryParam1 = {
        Name = "order"
        Module = "Common"
        Type = "InternalRevocationListOrder"
        Parse = Some "InternalRevocationListOrder.fromQueryString"
        Format = Some "InternalRevocationListOrder.asQueryString"
        Required = true
    }

    let routeParams = RouteParameters [ "/Test", (None, [ queryParam1 ]) ]

    let expectedRoute = {
        Name = "Test"
        RouteName = "TestRoute"
        LayoutName = "Test"
        LayoutModuleName = "TestProject.Pages.Test.Layout"
        LayoutModulePath = "Test"
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ Order: InternalRevocationListOrder  }"
        RecordConstructor = """{ Order = getQuery "order" InternalRevocationListOrder.fromQueryString q; }"""
        RecordPattern = "{ Order = order }"
        UrlUsage = """"test", [ "order", InternalRevocationListOrder.asQueryString order ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen =
            """eq test "test" && containsQuery "order" InternalRevocationListOrder.fromQueryString q """.Trim()
        IsMainLayout = false
    }

    task {
        try
            let! result, logs =
                fileToRoute
                    (ProjectName.fromAbsoluteProjectDir absoluteProjectDir)
                    absoluteProjectDir
                    routeParams
                    pageFile
                |> runEff

            result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
        finally
            if Directory.Exists(tempDir) then
                Directory.Delete(tempDir, true)
    }

[<Fact>]
let ``Ensure module names is wrapped in double ticks if project dir contains special characters`` () =
    let tempDir = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString())
    let testProjectDir = Path.Combine(tempDir, "test-project")
    let projectDir = FilePath.fromString testProjectDir
    let absoluteProjectDir = AbsoluteProjectDir projectDir

    task {
        try
            // Create test structure with special characters in project name
            Directory.CreateDirectory(Path.Combine(testProjectDir, "src", "Pages"))
            |> ignore

            File.WriteAllText(Path.Combine(testProjectDir, "src", "Pages", "Page.fs"), "module Page")
            File.WriteAllText(Path.Combine(testProjectDir, "src", "Pages", "Layout.fs"), "module Layout")
            File.WriteAllText(Path.Combine(testProjectDir, "elmish-land.json"), "{}")

            let! result, logs =
                getTemplateData (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir
                |> runEff

            result
            |> Expects.ok logs
            |> Expects.equalsWLogs logs {
                RenderFunction = "withReactSynchronous"
                RenderTargetElementId = "app"
                ViewModule = "Feliz"
                ViewType = "ReactElement"
                RootModule = "``test-project``"
                ElmishLandAppProjectFullName = "ElmishLand.test-project.App"
                Routes = [|
                    {
                        Name = "Home"
                        RouteName = "HomeRoute"
                        LayoutName = "Main"
                        LayoutModuleName = "``test-project``.Pages.Layout"
                        LayoutModulePath = ""
                        MsgName = "HomeMsg"
                        ModuleName = "``test-project``.Pages.Page"
                        RecordDefinition = "unit"
                        RecordConstructor = "()"
                        RecordPattern = "()"
                        UrlUsage = """ "" """.Trim()
                        UrlPattern = "[ Query q ]"
                        IsMainLayout = true
                        UrlPatternWhen = ""
                    }
                |]
                Layouts = [|
                    {
                        Name = "Main"
                        MsgName = "MainMsg"
                        ModuleName = "``test-project``.Pages.Layout"
                        ModulePath = ""
                    }
                |]
                RouteParamModules = []
                UseRouterPathMode = false
            }
        finally
            if Directory.Exists(tempDir) then
                Directory.Delete(tempDir, true)
    }

[<Fact>]
let ``Ensure static routes takes precedence over dynamic routes`` () =
    withNewProject (fun absoluteProjectDir _ ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir

            let! routes =
                eff {
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/_HomeParam" Accept
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/A" Accept
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/A/_AParam" Accept
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/B/C/_BCParam" Accept
                    do! addPage (FilePath.fromString folder) absoluteProjectDir "/B/C" Accept

                    let! templateData =
                        getTemplateData (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir

                    return templateData.Routes
                }
                |> Expects.effectOk runEff

            Expects.equals 6 routes.Length

            Expects.equals [ "Home"; "A"; "B_C"; "HomeParam"; "A_AParam"; "B_C_BCParam" ] [
                routes[0].Name
                routes[1].Name
                routes[2].Name
                routes[3].Name
                routes[4].Name
                routes[5].Name
            ]
        })
