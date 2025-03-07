module TemplateEngineTests

open System
open System.IO
open System.Text
open System.Threading
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.Settings
open ElmishLand.TemplateEngine
open Runner
open Xunit
open Orsak

let projectDir = FilePath.fromString "/TestProject"
let absoluteProjectDir = AbsoluteProjectDir projectDir
let projectName = ProjectName.fromAbsoluteProjectDir absoluteProjectDir
let pageFile = FilePath.appendParts [ "src"; "Pages"; "Test"; "Page.fs" ] projectDir

let layoutFile =
    FilePath.appendParts [ "src"; "Pages"; "Test"; "Layout.fs" ] projectDir

let nullFileSystem =
    { new IFileSystem with
        member this.FilePathExists(path, _isDirectory) = true
        member this.GetFilesRecursive(path, searchPattern) = [||]
        member this.GetParentDirectory(path) = None
        member this.ReadAllText(path) = ""
    }

[<Fact>]
let ``Ensure multiple query params are included in UrlUsage (Route.format)`` () =
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
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ First: string option; Second: string option }"
        RecordConstructor = """{ First = tryGetQuery "first" Some q; Second = tryGetQuery "second" Some q; }"""
        RecordPattern = "{ First = first; Second = second }"
        UrlUsage =
            """"test", [ match first with Some x -> "first",  x | None -> () ] @ [ match second with Some x -> "second",  x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff nullFileSystem

        result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
    }

[<Fact>]
let ``Ensure all query params with reserved names are generated correctly`` () =
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
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ To: string option }"
        RecordConstructor = """{ To = tryGetQuery "to" Some q; }"""
        RecordPattern = "{ To = ``to`` }"
        UrlUsage = """"test", [ match ``to`` with Some x -> "to",  x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff nullFileSystem

        result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
    }

[<Fact>]
let ``Ensure query params are camel cased`` () =
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
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ QueryParam: string option }"
        RecordConstructor = """{ QueryParam = tryGetQuery "queryParam" Some q; }"""
        RecordPattern = "{ QueryParam = queryParam }"
        UrlUsage = """"test", [ match queryParam with Some x -> "queryParam",  x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff nullFileSystem

        result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
    }

[<Fact>]
let ``Ensure query param parse and format are generated correctly`` () =
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
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ FromDate: ApplicationDate option }"
        RecordConstructor = """{ FromDate = tryGetQuery "fromDate" ApplicationDate.fromQueryparam q; }"""
        RecordPattern = "{ FromDate = fromDate }"
        UrlUsage =
            """"test", [ match fromDate with Some x -> "fromDate", ApplicationDate.asQueryparam x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff nullFileSystem

        result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
    }

[<Fact>]
let ``Ensure required query params is generated correctly`` () =
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
        MsgName = "TestMsg"
        ModuleName = "TestProject.Pages.Test.Page"
        RecordDefinition = "{ Order: InternalRevocationListOrder  }"
        RecordConstructor = """{ Order = getQuery "order" InternalRevocationListOrder.fromQueryString q; }"""
        RecordPattern = "{ Order = order }"
        UrlUsage = """"test", [ "order", InternalRevocationListOrder.asQueryString order ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen =
            """eq test "test" && containsQuery "order" InternalRevocationListOrder.fromQueryString q """
                .Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff nullFileSystem

        result |> Expects.ok logs |> Expects.equalsWLogs logs expectedRoute
    }

[<Fact>]
let ``Ensure module names is wrapped in double ticks if project dir contains special characters`` () =
    let projectDir = FilePath.fromString "/test-project"
    let absoluteProjectDir = AbsoluteProjectDir projectDir

    task {
        let! result, logs =
            getTemplateData (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir
            |> runEff
                { new IFileSystem with
                    member this.FilePathExists(path, isDirectory) = true

                    member this.ReadAllText(FilePath path) =
                        match path with
                        | "route" -> "{}"
                        | "/test-project/elmish-land.json" -> "{}"
                        | other -> failwith $"Unexpected path. Got '%s{other}'"

                    member this.GetFilesRecursive(_path, searchPattern) = [|
                        match searchPattern with
                        | "Page.fs" -> FilePath.fromString "page"
                        | "Layout.fs" -> FilePath.fromString "layout"
                        | "route.json" -> FilePath.fromString "route"
                        | other -> failwith $"Unexpected path. Got %s{other}"
                    |]

                    member this.GetParentDirectory(path) = None
                }

        result
        |> Expects.ok logs
        |> Expects.equalsWLogs logs {
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
                    MsgName = "HomeMsg"
                    ModuleName = "``test-project``.Pages.Page"
                    RecordDefinition = "unit"
                    RecordConstructor = "()"
                    RecordPattern = "()"
                    UrlUsage = """ "" """.Trim()
                    UrlPattern = "[ Query q ]"
                    UrlPatternWhen = ""
                }
            |]
            Layouts = [|
                {
                    Name = "Main"
                    MsgName = "MainMsg"
                    ModuleName = "``test-project``.Pages.Layout"
                }
            |]
            RouteParamModules = []
        }
    }
