open System
open System.IO
open System.Diagnostics
open System.Text.RegularExpressions

type RouteInfo = { Route: string; Apa: string option }

type Url = | Url of string

module Url =
    let value (Url url) = url

    let toFilePath (Url url) =
        url.Split("/", StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun x -> $"%s{x[0..0].ToUpperInvariant()}%s{x[1..]}")
        |> String.concat "/"
        |> fun x -> Path.Combine(__SOURCE_DIRECTORY__, "src", "Pages", x, "Page.fs")

type RouteFsharpTypeName = | RouteFsharpTypeName of string

module RouteFsharpTypeName =
    let value (RouteFsharpTypeName x) = x

type RouteArg = | RouteArg of string

module RouteArg =
    let value (RouteArg arg) = arg

    let asMatchString (args: RouteArg list) =
        if args.Length > 0 then
            args
            |> List.map value
            |> String.concat ", "
            |> fun args -> if args.Length = 2 then $"%s{args}" else $"(%s{args})"
        else
            ""

    let asParamString args =
        let x = asMatchString args
        if x.Length = 0 then "()" else x

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "src", "Pages")

module String =
    let replace (oldValue: string) (newValue: string) (s: string) = s.Replace(oldValue, newValue)

    let split (separator: string) (s: string) =
        s.Split(separator, StringSplitOptions.RemoveEmptyEntries)

let files () =
    Directory.GetFiles(rootDir, "page.fs", EnumerationOptions(RecurseSubdirectories = true))
    |> Array.sortBy (fun x -> if x.EndsWith("src\Pages\Page.fs") then "" else x)

let routeData (file: string) =
    let route =
        file[0 .. file.Length - 9]
        |> String.replace rootDir ""
        |> String.replace "\\" "/"

    route[1..]
    |> String.split "/"
    |> Array.fold
        (fun (parts, args) part ->
            if part.StartsWith("{") && part.EndsWith("}") then
                $"%s{part[0..0].ToUpper()}{part[1..]}" :: parts,
                RouteArg $"%s{part[1..1].ToLower()}%s{part[2 .. part.Length - 2]}" :: args
            else
                $"%s{part[0..0].ToUpper()}{part[1..]}" :: parts, args)
        ([], [])
    |> fun (parts, args) ->
        let duName = String.concat "_" (List.rev parts)

        Url(if route = "" then "/" else route),
        RouteFsharpTypeName(
            if duName.Contains("{") then $"``%s{duName}``"
            else if duName = "" then "Home"
            else duName
        ),
        List.rev args

let routesData (files: string array) = files |> Array.map routeData

let disclaimer =
    """// THIS FILE IS AUTO GENERATED. ALL CONTENTS WILL BE OVERWRITTEN ON BUILD
//
// Add new page:
//   dotnet fsi Page.fsx add <url>
//
// View pages and routes:
//   dotnet fsi Page.fsx view
//
// Generate App.fs and Routes.fs based on files in src/Pages/:
//   dotnet fsi Page.fsx build
//
// Remove a page:
//   Delete the Page.fs or folder for the page you want to delete and run `dotnet fsi Page.fsx build`
//
// THIS FILE IS AUTO GENERATED. ALL CONTENTS WILL BE OVERWRITTEN ON BUILD
"""

let parseUrl routesData =
    routesData
    |> Array.map (fun (Url url, RouteFsharpTypeName name, args) ->
        let urlSegments =
            url
            |> String.split ("/")
            |> Array.map (fun part ->
                if part.StartsWith("{") && part.EndsWith("}") then
                    part[1 .. part.Length - 2]
                else
                    $"\"{part}\"")
            |> String.concat "; "

        let dd =
            args
            |> List.map RouteArg.value
            |> String.concat ", "
            |> fun x -> if x <> "" then $"(%s{x})" else ""

        let query = if name = "Home" then "" else "; Route.Query _"

        $"""        | [ %s{urlSegments.ToLowerInvariant()}%s{query} ]
        | [ %s{urlSegments.ToLowerInvariant()} ] -> Route.%s{name}{dd}""")
    |> String.concat "\r\n"
    |> fun items ->
        $"""
    let parse (xs: string list) =
        match xs |> List.map (fun x -> x.ToLowerInvariant()) with
%s{items}
        | other ->
            printfn "Route not found: '%%A'" other
            Route.NotFound"""

let routes routesData =
    let routeDu =
        routesData
        |> Array.map (fun (Url url, RouteFsharpTypeName name, args) ->
            let argString =
                args
                |> List.map (fun arg -> $"%s{RouteArg.value arg}: string")
                |> String.concat " * "

            let apa = if argString.Length = 0 then "" else $" of %s{argString}"

            $"    | %s{name}%s{apa}")
        |> String.concat "\r\n"

    let routeAsUrls =
        routesData
        |> Array.map (fun (Url url, RouteFsharpTypeName name, args) ->
            let replacedUrl =
                args
                |> List.fold
                    (fun (url': string) arg ->
                        let argString = RouteArg.value arg

                        url'.ToLowerInvariant()
                        |> String.replace $"{{%s{argString}}}" $"%%s{{%s{argString}}}")
                    url

            $"    | Route.%s{name} %s{RouteArg.asMatchString args} -> $\"%s{replacedUrl}\"")
        |> String.concat "\r\n"

    $"""%s{disclaimer}
module Trinity.Routes

open Feliz.Router

[<RequireQualifiedAccess>]
type Route =
%s{routeDu}
    | NotFound

module Route =
    let asUrl = function
%s{routeAsUrls}
    | Route.NotFound -> "notFound"
%s{parseUrl routesData}
    """

let moduleName (RouteFsharpTypeName name) =
    if name = "Home" then
        "Pages.Page"
    else
        let x =
            name
            |> String.replace "``" ""
            |> String.replace "_" "."
            |> String.replace "{" "``{"
            |> String.replace "}" "}``"

        $"Pages.%s{x}.Page"

let page routesData =
    routesData
    |> Array.map (fun (Url _, name, _) -> $"    | %s{RouteFsharpTypeName.value name} of %s{moduleName name}.Model")
    |> String.concat "\r\n"
    |> fun items ->
        $"""
[<RequireQualifiedAccess>]
type Page =
%s{items}
    | NotFound"""

let model =
    """
type Model = {
    Shared: Pages.Shared.Model
    CurrentRoute: Route
    CurrentPage: Page
}"""

let msgName (RouteFsharpTypeName name) =
    if name.Contains("``") then
        let name = String.replace "``" "" name
        $"``%s{name}Msg``"
    else
        $"%s{name}Msg"

let msg routesData =
    routesData
    |> Array.map (fun (Url _, name, _) -> $"    | %s{msgName name} of %s{moduleName name}.Msg")
    |> String.concat "\r\n"
    |> fun items ->
        $"""
type Msg =
    | SharedMsg of Pages.Shared.Msg
    | RouteChanged of Route
%s{items}
"""

let init routesData =
    routesData
    |> Array.map (fun (Url _, name, (args: RouteArg list)) ->
        $"""    | Route.%s{RouteFsharpTypeName.value name}%s{RouteArg.asMatchString args} ->
        initPage %s{moduleName name}.init %s{RouteArg.asParamString args} Page.%s{RouteFsharpTypeName.value name} %s{msgName name}""")
    |> String.concat "\r\n"
    |> fun items ->
        $"""let init () =
    let initialUrl = Route.parse (Router.currentUrl ())
    let sharedModel, sharedCmd = Pages.Shared.init ()

    let defaultModel = {{
        Shared = sharedModel
        CurrentRoute = initialUrl
        CurrentPage = Page.NotFound
    }}

    let initPage init initArgs page msg =
        let nextModel, nextCmd = init initArgs
        let nextPage = page nextModel

        {{
            defaultModel with
                CurrentPage = nextPage
        }},
        Cmd.batch [ sharedCmd; Cmd.map msg nextCmd ]

    match initialUrl with
%s{items}
    | Route.NotFound ->
        {{
            defaultModel with
                CurrentPage = Page.NotFound
        }},
        Cmd.none
"""

let update routesData =
    routesData
    |> Array.map (fun (Url _, name, (args: RouteArg list)) ->
        $"""    | %s{msgName name} msg', Page.%s{RouteFsharpTypeName.value name} model' ->
        updatePage  %s{moduleName name}.update msg' model' Page.%s{RouteFsharpTypeName.value name} %s{msgName name}""",
        $"""        | Route.%s{RouteFsharpTypeName.value name} %s{RouteArg.asMatchString args} ->
            changeRoute %s{moduleName name}.init %s{RouteArg.asParamString args} Page.%s{RouteFsharpTypeName.value name} %s{msgName name}""")
    |> Array.unzip
    |> fun (x, y) -> String.concat "\r\n" x, String.concat "\r\n" y
    |> fun (msgHandlers, routeHandlers) ->
        $"""let update (msg: Msg) (model: Model) =
    let updatePage update msg' model' page msg =
        let model'', cmd = update msg' model'

        {{
            model with
                CurrentPage = page model''
        }},
        Cmd.map msg cmd
    match msg, model.CurrentPage with
    | SharedMsg msg', _ ->
        let model'', cmd = Pages.Shared.update msg' model.Shared
        {{ model with Shared = model'' }}, Cmd.map SharedMsg cmd
    | RouteChanged nextRoute, _ ->
        let changeRoute init initArgs page msg =
            let model', msg' = init initArgs
            {{
                model with
                    CurrentPage = page model'
                    CurrentRoute = nextRoute
            }},
            Cmd.map msg msg'
        match nextRoute with
%s{routeHandlers}
        | Route.NotFound ->
            {{
                model with
                    CurrentPage = Page.NotFound
                    CurrentRoute = Route.NotFound
            }},
            Cmd.none
%s{msgHandlers}
    | msg', model' ->
        printfn $"Unhandled App.Msg and CurrentPage.Model. Got\nMsg:\n%%A{{msg'}}\nCurrentPage.Model:\n%%A{{model'}}"
        model, Cmd.none
"""

let view routesData =
    routesData
    |> Array.map (fun (Url _, name, args) ->
        $"""        | Page.%s{RouteFsharpTypeName.value name} m -> %s{moduleName name}.view m (%s{msgName name} >> dispatch)""")
    |> String.concat "\r\n"
    |> fun items ->
        $"""let view (model: Model) (dispatch: Msg -> unit) =
    let currentPageView =
        match model.CurrentPage with
%s{items}
        | Page.NotFound -> Html.h1 "Sidan kunde inte hittas"

    React.router [
        router.onUrlChanged (Route.parse >> RouteChanged >> dispatch)
        router.children [ currentPageView ]
    ]
"""

let subscribe routesData =
    routesData
    |> Array.map (fun (Url _, name, args) ->
        $"""        | Page.%s{RouteFsharpTypeName.value name} m -> Sub.map "%s{RouteFsharpTypeName.value name}" %s{msgName name} (%s{moduleName name}.subscribe m)""")
    |> String.concat "\r\n"
    |> fun items ->
        $"""let subscribe model =
    Sub.batch [
        Sub.map "Shared_App" SharedMsg [ Pages.Shared.subscribeShared id ]
        match model.CurrentPage with
%s{items}
        | Page.NotFound -> Sub.none
    ]"""

let app routesData =
    $"""%s{disclaimer}
module Trinity.App

open Elmish
open Elmish.React
open Elmish.HMR
open Feliz
open Feliz.Router
open Trinity.Routes
%s{page routesData}
%s{model}
%s{msg routesData}
%s{init routesData}
%s{update routesData}
%s{view routesData}
%s{subscribe routesData}

Program.mkProgram init update view
|> Program.withErrorHandler (fun (msg, ex) -> printfn "Program error handler:\r\n%%s\r\n%%O" msg ex)
|> Program.withReactBatched "app"
|> Program.withSubscription subscribe
|> Program.run
"""

let pageTemplate (url: Url) =
    let filePath = Url.toFilePath url

    let _, _, args = routeData filePath

    let moduleName =
        (Url.value url)
        |> String.split "/"
        |> Array.map (fun x -> $"%s{x[0..0].ToUpperInvariant()}%s{x[1..]}")
        |> Array.map (fun x -> if x.StartsWith("{") then $"``%s{x}``" else x)
        |> String.concat "."
        |> fun x -> $"Pages.%s{x}.Page"

    $"""module %s{moduleName}

open System
open Feliz
open Elmish

type Model = unit

type Msg = | NoOp

let init %s{(RouteArg.asParamString args)}: Model * Cmd<Msg> =
    (),
    Cmd.none

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | NoOp -> model, Cmd.none

let view (model: Model) (dispatch: Msg -> unit): ReactElement =
    Html.text "%s{moduleName}"

let subscribe (model: Model) : (string list * ((Msg -> unit) -> IDisposable)) list = []

"""

let dotnet command =
    ProcessStartInfo("dotnet", command, WorkingDirectory = __SOURCE_DIRECTORY__)
    |> Process.Start
    |> fun p -> p.WaitForExit()

let viewFiles (files: string array) =
    let filesTrimmed =
        files |> Array.map (fun x -> (String.replace __SOURCE_DIRECTORY__ "" x)[1..])

    let padding =
        filesTrimmed |> Array.fold (fun padding url -> max padding url.Length) 0

    for file in filesTrimmed do
        let url = (file.ToLowerInvariant() |> String.replace "\\" "/")[9 .. file.Length - 9]

        let url = if url = "" then "/" else url
        printfn $"%s{file.PadRight(padding)} %s{url}"

let (|Contains|_|) (x: string) (s: string) = if s.Contains(x) then Some s else None

let updateFsProj (files: string array) =
    for filePath in Directory.GetFiles(__SOURCE_DIRECTORY__, "*.fsproj") do
        let allFsFiles =
            Directory.GetFiles(Path.Combine(__SOURCE_DIRECTORY__, "src"), "*.fs", SearchOption.AllDirectories)
            |> Array.map (fun file ->
                let x = file |> String.replace (Path.Combine(__SOURCE_DIRECTORY__, "src")) ""

                match Path.GetDirectoryName(x), Path.GetFileName(x) with
                | "\\", "Routes.fs" -> ("", 1), file
                | "\\Pages" as dir, "Shared.fs" -> (dir, 1), file
                | "\\", "App.fs" -> ("Ö", 9), file
                | dir, "Layout.fs" -> (dir, 3), file
                | dir, "Page.fs" -> (dir, 4), file
                | dir, _ -> (dir, 2), file)
            |> Array.sortBy fst
            |> Array.map snd

        let newItemGroup =
            allFsFiles
            |> Array.map (fun x ->
                x
                |> String.replace __SOURCE_DIRECTORY__ ""
                |> fun x -> (String.replace "\\" "/" x)[1..])
            |> Array.map (fun x -> $"    <Compile Include=\"%s{x}\" />")
            |> String.concat "\r\n"
            |> fun x -> $"<ItemGroup>    \r\n%s{x}\r\n  </ItemGroup>\r\n</Project>"

        let content =
            File.ReadAllLines(filePath)
            |> Array.choose (fun line ->
                if Regex.IsMatch(line, """<Compile Include="src[\\\\/]""") then
                    None
                else
                    Some line)
            |> String.concat "\r\n"
            |> fun c ->
                Regex
                    .Replace(c, "<ItemGroup>\\s*</ItemGroup>", "")
                    .Replace("\r\n</Project>", newItemGroup)

        File.WriteAllText(filePath, content)

let writeFiles () =
    let files = files ()

    printfn "Generating routes..."
    viewFiles files

    printfn "\r\nWriting 'src/Routes.fs' and 'src/App.fs'"
    File.WriteAllText(Path.Combine(__SOURCE_DIRECTORY__, "src", "Routes.fs"), files |> routesData |> routes)
    File.WriteAllText(Path.Combine(__SOURCE_DIRECTORY__, "src", "App.fs"), files |> routesData |> app)

    dotnet "fantomas ./src/App.fs ./src/Routes.fs"

    updateFsProj (files)

match fsi.CommandLineArgs |> List.ofArray with
| _ :: "add" :: url :: _ ->
    let url = Url url
    let filePath = Url.toFilePath url

    if File.Exists(filePath) then
        printfn $"Page already exists '%s{filePath}'"
    else
        printfn "Creating page '%s'" filePath

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore

        File.WriteAllText(filePath, pageTemplate url)

        printfn ""
        writeFiles ()
| _ :: "view" :: _ ->
    let files = files ()
    viewFiles files
    printfn ""
| _ :: "build" :: _ -> writeFiles ()
| _ -> failwith "Expected one of the following commands: build, add"
