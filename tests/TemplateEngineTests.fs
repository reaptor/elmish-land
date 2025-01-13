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
let xx () =
    let routeParams = RouteParameters [] //of List<string * (RoutePathParameter option * RouteQueryParameter list)>

    let expectedRoute = {
        Name = "Name"
        RouteName = "RouteName"
        LayoutName = "LayoutName"
        LayoutModuleName = "LayoutModuleName"
        MsgName = "MsgName"
        ModuleName = "ModuleName"
        RecordDefinition = "RecordDefinition"
        RecordConstructor = "RecordConstructor"
        RecordPattern = "RecordPattern"
        UrlUsage = "UrlUsage"
        UrlPattern = "UrlPattern"
        UrlPatternWhen = "UrlPatternWhen"
    }

    task {
        let! result, logs =
            fileToRoute (ProjectName.fromAbsoluteProjectDir absoluteProjectDir) absoluteProjectDir routeParams pageFile
            |> runEff

        result |> Expects.ok logs |> Expects.equals logs expectedRoute
    }
