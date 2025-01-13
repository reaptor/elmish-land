module TemplateEngineTests

open System
open System.IO
open System.Text
open System.Threading
open ElmishLand.Base
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
            """"test", [ match first with Some x -> "first", x | None -> () ] @ [ match second with Some x -> "second", x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff

        result |> Expects.ok logs |> Expects.equals logs expectedRoute
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
        UrlUsage = """"test", [ match ``to`` with Some x -> "to", x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff

        result |> Expects.ok logs |> Expects.equals logs expectedRoute
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
        UrlUsage = """"test", [ match queryParam with Some x -> "queryParam", x | None -> () ]"""
        UrlPattern = "[ test; Query q ]"
        UrlPatternWhen = """eq test "test" """.Trim()
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff

        result |> Expects.ok logs |> Expects.equals logs expectedRoute
    }
