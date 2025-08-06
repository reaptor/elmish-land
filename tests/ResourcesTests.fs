module ResourcesTests

open ElmishLand.TemplateEngine
open Xunit
open ElmishLand.Resources

let templateData: TemplateData = {
    ViewModule = "ViewModule" // Ex. Feliz
    ViewType = "ViewType" // Ex. ReactElement
    RootModule = "RootModule" // Ex. MyApp or my-app
    ElmishLandAppProjectFullName = "ElmishLand.RootModule.App"
    Routes = [|
        {
            Name = "RoutesName"
            RouteName = "RoutesRouteName"
            LayoutName = "RoutesLayoutName"
            LayoutModuleName = "RoutesLayoutModuleName"
            MsgName = "RoutesMsgName"
            ModuleName = "RoutesModuleName"
            RecordDefinition = "RoutesRecordDefinition"
            RecordConstructor = "RoutesRecordConstructor"
            RecordPattern = "RoutesRecordPattern"
            UrlUsage = "RoutesUrlUsage"
            UrlPattern = "RoutesUrlPattern"
            UrlPatternWhen = "RoutesUrlPatternWhen"
        }
    |]
    Layouts = [|
        {
            Name = "LayoutsName"
            MsgName = "LayoutsMsgName"
            ModuleName = "LayoutsModuleName"
        }
    |]
    RouteParamModules = [ "RouteParamModules" ]
}

[<Fact>]
let AddLayout_template () =
    getResource<AddLayout_template> {
        ViewModule = "ViewModule"
        ViewType = "ViewType"
        RootModule = "RootModule"
        Layout = {
            Name = "LayoutName"
            MsgName = "LayoutMsgName"
            ModuleName = "LayoutModuleName"
        }
    }
    |> Expects.equals
        """module LayoutModuleName

open ViewModule
open ElmishLand
open RootModule.Shared

type Props = unit

type Model = unit

type Msg = | NoOp

let init () =
    (),
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | NoOp -> model, Command.none

let routeChanged (model: Model) =
    model, Command.none

let view (_model: Model) (content: ViewType) (_dispatch: Msg -> unit) =
    content

let layout (_props: Props) (_route: Route) (_shared: SharedModel) =
    Layout.from init update routeChanged view
"""

[<Fact>]
let AddPage_template () =
    getResource<AddPage_template> {
        ViewModule = "ViewModule"
        ViewType = "ViewType"
        ScaffoldTextElement = "ScaffoldTextElement"
        RootModule = "RootModule"
        Route = {
            Name = "RouteName"
            RouteName = "RouteRouteName"
            LayoutName = "RouteLayoutName"
            LayoutModuleName = "RouteLayoutModuleName"
            MsgName = "RouteMsgName"
            ModuleName = "RouteModuleName"
            RecordDefinition = "RouteRecordDefinition"
            RecordConstructor = "RouteRecordConstructor"
            RecordPattern = "RouteRecordPattern"
            UrlUsage = "RouteUrlUsage"
            UrlPattern = "RouteUrlPattern"
            UrlPatternWhen = "RouteUrlPatternWhen"
        }
    }
    |> Expects.equals
        """module RouteModuleName

open Feliz
open ElmishLand
open RootModule.Shared
open RootModule.Pages

type Model = unit

type Msg =
    | LayoutMsg of Layout.Msg

let init () =
    (),
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none

let view (_model: Model) (_dispatch: Msg -> unit) =
    ScaffoldTextElement "RouteName Page"

let page (_shared: SharedModel) (_route: RouteRouteName) =
    Page.from init update view () LayoutMsg
"""

[<Fact>]
let global_json_template () =
    getResource<global_json_template> {
        DotNetSdkVersion = "DotNetSdkVersion"
    }
    |> Expects.equals
        """{
  "sdk": {
    "version": "DotNetSdkVersion",
    "rollForward": "latestFeature"
  }
}
"""

[<Fact>]
let Directory_Packages_props_template () =
    getResource<Directory_Packages_props_template> { PackageVersions = "PackageVersions" }
    |> Expects.equals
        """<Project>
    <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
        <CentralPackageVersionOverrideEnabled>false</CentralPackageVersionOverrideEnabled>
    </PropertyGroup>
    PackageVersions
</Project>
"""

[<Fact>]
let Project_fsproj_template () =
    getResource<Project_fsproj_template> {
        DotNetVersion = "DotNetVersion"
        ProjectName = "ProjectName"
    }
    |> Expects.equals
        """<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>DotNetVersion</TargetFramework>
        <LangVersion>latest</LangVersion>
    </PropertyGroup>

    <ItemGroup>
        <Compile Include="src/Shared.fs"/>
        <Compile Include="src/Pages/NotFound.fs"/>
        <Compile Include="src/Pages/Layout.fs"/>
        <Compile Include="src/Pages/Page.fs"/>
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include=".elmish-land/Base/ElmishLand.ProjectName.Base.fsproj" />
    </ItemGroup>
</Project>
"""

[<Fact>]
let package_json_template () =
    getResource<package_json_template> {
        ProjectName = "ProjectName"
        Dependencies = "Dependencies"
        DevDependencies = "DevDependencies"
    }
    |> Expects.equals
        """{
  "name": "ProjectName",
  "version": "1.0.0",
  "type": "module",
  "scripts": {
      "start": "dotnet elmish-land server",
      "build": "dotnet elmish-land build"
  },
  "dependencies": {
    Dependencies
  },
  "devDependencies": {
    DevDependencies
  }
}
"""

[<Fact>]
let index_html_template () =
    getResource<index_html_template> { Title = "Title" }
    |> Expects.equals
        """<!DOCTYPE html>
<html lang="en">
    <head>
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <meta http-equiv="X-UA-Compatible" content="IE=edge">
        <meta charset="UTF-8">
        <title>Title</title>
    </head>
    <body>
        <div id="app"></div>
        <script type="module" src=".elmish-land/App/App.fs.js"></script>
    </body>
</html>
"""

[<Fact>]
let NotFound_template () =
    getResource<NotFound_template> {
        ScaffoldTextElement = "ScaffoldTextElement"
        RootModule = "RootModule"
    }
    |> Expects.equals
        """module RootModule.Pages.NotFound

open Feliz

let view () =
    ScaffoldTextElement "Page not found"
"""

[<Fact>]
let Shared_template () =
    getResource<Shared_template> (Shared_template templateData)
    |> Expects.equals
        """module RootModule.Shared

open System
open ElmishLand

type SharedModel = unit

type SharedMsg = | NoOp

let init () =
    (), Command.none

let update (msg: SharedMsg) (model: SharedModel) =
    match msg with
    | NoOp -> model, Command.none

// https://elmish.github.io/elmish/docs/subscription.html
let subscriptions _model : (string list * ((SharedMsg -> unit) -> IDisposable)) list = []
"""

[<Fact>]
let Base_fsproj_template () =
    getResource<Base_fsproj_template> {
        DotNetVersion = "DotNetVersion"
        PackageReferences = "PackageReferences"
        ProjectReferences = [ "ProjectReferences" ]
    }
    |> Expects.equals
        """<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>DotNetVersion</TargetFramework>
        <LangVersion>latest</LangVersion>
        <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
        <WarnOn></WarnOn>
        <NoWarn></NoWarn>
    </PropertyGroup>

    <ItemGroup>
        <Compile Include="Routes.fs"/>
        <Compile Include="Command.fs"/>
        <Compile Include="Layout.fs"/>
        <Compile Include="Page.fs"/>
    </ItemGroup>

    PackageReferences

    <ItemGroup>
        <ProjectReference Include="ProjectReferences" />
    </ItemGroup>

</Project>
"""

[<Fact>]
let App_fsproj_template () =
    getResource<App_fsproj_template> {
        DotNetVersion = "DotNetVersion"
        ProjectReferences = [ "ProjectReferences" ]
    }
    |> Expects.equals
        """<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>DotNetVersion</TargetFramework>
        <LangVersion>latest</LangVersion>
        <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
        <WarnOn></WarnOn>
        <NoWarn></NoWarn>
    </PropertyGroup>

    <ItemGroup>
        <Compile Include="App.fs"/>
    </ItemGroup>

    <ItemGroup>
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="ProjectReferences" />
    </ItemGroup>

</Project>
"""

[<Fact>]
let Routes_template () =
    getResource<Routes_template> (Routes_template templateData)
    |> Expects.equals
        """//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by elmish-land.
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ElmishLand

open System
open Feliz.Router
open RouteParamModules

[<AutoOpen>]
module Routes =

    type RoutesRouteName = RoutesRecordDefinition

    [<RequireQualifiedAccess>]
    type Route =
        | RoutesName of RoutesRouteName
        | NotFound

    module Route =
        let internal formatGuid (x: Guid) = string x
        let internal formatInt (x: int) = string x
        let internal formatInt64 (x: int64) = string x
        let internal formatBool (x: bool) = string x
        let internal formatFloat (x: float) = string x
        let internal formatDecimal (x: Decimal) = string x

        let private parseGuid (x: string) =
            match Guid.TryParse x with
            | true, x' -> Some x'
            | _ -> None

        let private parseInt (x: string) =
            match Int32.TryParse x with
            | true, x' -> Some x'
            | _ -> None

        let private parseInt64 (x: string) =
            match Int64.TryParse x with
            | true, x' -> Some x'
            | _ -> None

        let private parseBool (x: string) =
            match bool.TryParse x with
            | true, x' -> Some x'
            | _ -> None

        let private parseFloat (x: string) =
            match Single.TryParse x with
            | true, x' -> Some x'
            | _ -> None

        let private parseDecimal (x: string) =
            match Decimal.TryParse x with
            | true, x' -> Some x'
            | _ -> None

        let format =
            function
            | Route.RoutesName RoutesRecordPattern -> Router.format(RoutesUrlUsage)
            | Route.NotFound -> "notFound"

        // Need to define our own from Feliz.Router.Route.Query
        // because we need to check if the value is a query parameter
        let private (|Query|_|) (input: string) =
            try
                if input.StartsWith("?") then
                    let urlParams = Router.createUrlSearchParams input
                    Some [ for entry in urlParams.entries() -> entry.[0], entry.[1] ]
                else
                    Some []
            with
            | _ -> Some []

        let private eq x y =
            String.Equals(x, y, StringComparison.InvariantCultureIgnoreCase)

        let private containsQuery name (parser: string -> _ option) query =
            query
            |> List.exists (fun (name', value) -> eq name' name && (parser value).IsSome)

        let private tryGetQuery name (parser: string -> _ option) query =
            query
            |> List.tryPick (fun (name', value) ->
                match parser value with
                | Some value' when eq name' name -> Some value'
                | _ -> None
            )

        let private getQuery name (parser: string -> _ option) query =
            tryGetQuery name parser query |> Option.defaultWith (fun () -> failwithf "Query parameter '%s' not found" name)

        let parse (xs: string list) =
            let xs =
                match xs with
                | [] -> [ "?" ]
                | xs when not <| (List.last xs).StartsWith("?") -> List.append xs [ "?" ]
                | xs -> xs
            match xs with
            | RoutesUrlPattern when RoutesUrlPatternWhen -> Route.RoutesName RoutesRecordConstructor
            | other ->
                printfn "Route not found: '%A'" other
                Route.NotFound

        let isEqualWithoutPathAndQuery route1 route2 =
            match route1, route2 with
            | Route.RoutesName _, Route.RoutesName _ -> true
            | _ -> false
"""

[<Fact>]
let Command_fs_template () =
    getResource<Command_fs_template> (Command_fs_template templateData)
    |> Expects.equals
        """//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by elmish-land.
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ElmishLand

open Elmish
open Feliz
open ElmishLand.Routes
open ElmishLand.Routes.Route

[<RequireQualifiedAccess>]
type Command<'msg, 'sharedMsg, 'layoutMsg> private =
    | None
    | Batch of Command<'msg, 'sharedMsg, 'layoutMsg> list
    | Cmd of Cmd<'msg>
    | SharedMsg of 'sharedMsg
    | LayoutMsg of 'layoutMsg

module Command =
    let none = Command.None

    let ofPromise p arg ofSuccess = Cmd.OfPromise.perform p arg ofSuccess |> Command.Cmd

    let tryOfPromise p arg ofSuccess ofError = Cmd.OfPromise.either p arg ofSuccess ofError |> Command.Cmd

    let ofCmd (cmd: Cmd<'msg>) = Command.Cmd(cmd)

    let ofMsg (msg: 'msg) =
        Command.Cmd(Cmd.ofMsg msg)

    let batch cmds = Command.Batch cmds

    let ofShared msg = Command.SharedMsg msg

    let ofLayout msg = Command.LayoutMsg (msg)

    let navigate route = route |> format |> Router.Cmd.navigate |> ofCmd

    let rec map f mapLayout command =
        match command with
        | Command.None -> Command.None
        | Command.Batch cmds -> Command.Batch (cmds |> List.map (map f mapLayout))
        | Command.Cmd cmd -> Command.Cmd (Cmd.map f cmd)
        | Command.SharedMsg msg -> Command.SharedMsg msg
        | Command.LayoutMsg msg -> Command.LayoutMsg (mapLayout msg)
"""

[<Fact>]
let Page_fs_template () =
    getResource<Page_fs_template> (Page_fs_template templateData)
    |> Expects.equals
        """//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by elmish-land.
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ElmishLand

open System.Runtime.CompilerServices
open ViewModule

[<assembly: InternalsVisibleTo("ElmishLand.RootModule.App")>]
do()

type Page<'sharedMsg, 'pageModel, 'pageMsg, 'layoutMsg, 'layoutProps> =
    internal {
        Init: unit -> 'pageModel * Command<'pageMsg, 'sharedMsg, 'layoutMsg>
        Update: 'pageMsg -> 'pageModel -> 'pageModel * Command<'pageMsg, 'sharedMsg, 'layoutMsg>
        View: 'pageModel -> ('pageMsg -> unit) -> ViewType
        Subscriptions: 'pageModel -> (string list * (('pageMsg -> unit) -> System.IDisposable)) list
        LayoutProps: 'layoutProps
        LayoutMsgToPageMsg: 'layoutMsg -> 'pageMsg
    }

module Page =
    let from init update view layoutProps layoutMsgToPageMsg=
        {
            Init = init
            Update = update
            View = view
            Subscriptions = fun _ -> []
            LayoutProps = layoutProps
            LayoutMsgToPageMsg = layoutMsgToPageMsg
        }

    let withSubscriptions subscriptions (page: Page<'sharedMsg, 'pageModel, 'pageMsg, 'layoutMsg, 'layoutProps>) =
        { page with Subscriptions = subscriptions }
"""

[<Fact>]
let Layout_fs_template () =
    getResource<Layout_fs_template> (Layout_fs_template templateData)
    |> Expects.equals
        """//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by elmish-land.
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ElmishLand

open System.Runtime.CompilerServices
open Feliz

[<assembly: InternalsVisibleTo("ElmishLand.RootModule.App")>]
do()

type Layout<'sharedMsg, 'layoutModel, 'layoutMsg> =
    internal {
        Init: unit -> 'layoutModel * Command<'layoutMsg, 'sharedMsg, 'layoutMsg>
        Update: 'layoutMsg -> 'layoutModel -> 'layoutModel * Command<'layoutMsg, 'sharedMsg, 'layoutMsg>
        RouteChanged: 'layoutModel -> 'layoutModel * Command<'layoutMsg, 'sharedMsg, 'layoutMsg>
        View: 'layoutModel -> ViewType -> ('layoutMsg -> unit) -> ViewType
        Subscriptions: 'layoutModel -> (string list * (('layoutMsg -> unit) -> System.IDisposable)) list
    }

module Layout =
    let from init update routeChanged view =
        {
            Init = init
            Update = update
            RouteChanged = routeChanged
            View = view
            Subscriptions = fun _ -> []
        }

    let withSubscriptions subscriptions (layout: Layout<'sharedMsg, 'layoutModel, 'layoutMsg>) =
        { layout with Subscriptions = subscriptions }
"""

[<Fact>]
let App_template () =
    getResource<App_template> (App_template templateData)
    |> Expects.equals
        """//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by elmish-land.
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

module ElmishLand.RootModule.App

open Elmish
open Elmish.HMR
open Feliz
open Feliz.Router
open ViewModule
open ElmishLand
open RootModule
open RootModule.Shared

[<RequireQualifiedAccess>]
type LayoutName =
    | LayoutsName
    | None

[<RequireQualifiedAccess>]
type Layout =
    | LayoutsName of LayoutsModuleName.Props * LayoutsModuleName.Model
    | None

[<RequireQualifiedAccess>]
type LayoutMsg =
    | LayoutsMsgName of LayoutsModuleName.Msg
    | NoOp

[<RequireQualifiedAccess>]
type PageMsg =
    | RoutesMsgName of RoutesModuleName.Msg

type Msg =
    | SharedMsg of SharedMsg
    | RouteChanged of Route
    | PageMsg of PageMsg
    | LayoutMsg of LayoutMsg

type MappedPage<'pageMsg, 'pageModel, 'layoutMsg, 'layoutProps> =
    {
        Init: unit -> 'pageModel * Command<Msg, SharedMsg, Msg>
        Update: 'pageMsg -> 'pageModel -> 'pageModel * Command<Msg, SharedMsg, Msg>
        View: 'pageModel -> ('pageMsg -> unit) -> ViewType
        Subscriptions: 'pageModel -> (string list * (('pageMsg -> unit) -> System.IDisposable)) list
        LayoutProps: 'layoutProps
        LayoutMsgToPageMsg: 'layoutMsg -> 'pageMsg
        LayoutMsgToMsg: 'layoutMsg -> Msg
    }

[<RequireQualifiedAccess>]
type PageModel =
    | RoutesName of RoutesModuleName.Model
    | NotFound

[<RequireQualifiedAccess>]
type Page =
    | RoutesName of MappedPage<RoutesModuleName.Msg, RoutesModuleName.Model, RoutesLayoutModuleName.Msg, RoutesLayoutModuleName.Props>
    | NotFound

type Model = {
    Shared: SharedModel
    CurrentRoute: Route
    CurrentPage: Page
    CurrentPageModel: PageModel
    CurrentLayout: Layout
    CurrentLayoutName: LayoutName
    PageModelByLayout: Map<LayoutName, Page>
}

let rec commandToCmd fromSharedMsg fromLayoutMsg command =
    match command with
    | Command.None -> Cmd.none
    | Command.Batch cmds -> Cmd.batch (List.map (commandToCmd fromSharedMsg fromLayoutMsg) cmds)
    | Command.Cmd cmd -> cmd
    | Command.SharedMsg msg -> Cmd.ofMsg (fromSharedMsg msg)
    | Command.LayoutMsg msg -> Cmd.ofMsg (fromLayoutMsg msg)

let rec sharedCommandToCmd (command: Command<SharedMsg, SharedMsg, _>): Cmd<Msg> = 
    match command with
    | Command.None -> Cmd.none
    | Command.Batch cmds -> Cmd.batch (List.map sharedCommandToCmd cmds)
    | Command.Cmd cmd -> Cmd.map SharedMsg cmd
    | Command.SharedMsg msg -> Cmd.ofMsg (SharedMsg msg)
    | Command.LayoutMsg msg -> failwith "Layout messages should not occur in shared commands"
    
let getLayoutsNameLayout currentLayout currentRoute sharedModel layoutProps =
    let layout = LayoutsModuleName.layout layoutProps currentRoute sharedModel
    match currentLayout with
    | Layout.LayoutsName (_, m) -> layout.RouteChanged m
    | _ -> layout.Init ()

let mapPage (f: 'pageMsg -> Msg) (mapLayout: 'layoutMsg -> Msg) (p: Page<SharedMsg, 'pageModel, 'pageMsg, 'layoutMsg, 'layoutProps>)
    : MappedPage<'pageMsg, 'pageModel, 'layoutMsg, 'layoutProps> =
    let init = p.Init >> fun (m, c) -> m, Command.map f mapLayout c
    let update =
        p.Update >>
        fun f' -> f'
                >> fun (m: 'pageModel, c: Command<'pageMsg, SharedMsg, 'layoutMsg>) ->
                    (m, Command.map f mapLayout c)
    let layoutMsgToPageMsg layoutMsg =
        p.LayoutMsgToPageMsg layoutMsg |> f

    {
        Init = init
        Update = update
        View = p.View
        Subscriptions = p.Subscriptions
        LayoutProps = p.LayoutProps
        LayoutMsgToPageMsg = p.LayoutMsgToPageMsg
        LayoutMsgToMsg = layoutMsgToPageMsg
    }

let layoutMsgToPageMsg page f layoutMsg = page.LayoutMsgToPageMsg layoutMsg |> f

let initRoutesNamePage model route sharedCmd =
    let mappedPage = (RoutesModuleName.page model.Shared route |> mapPage (PageMsg.RoutesNameMsg >> PageMsg) (LayoutMsg.RoutesLayoutNameMsg >> LayoutMsg))
    let pageModel, pageCmd = mappedPage.Init ()
    let layoutModel, layoutCmd = (getRoutesLayoutNameLayout model.CurrentLayout (Route.RoutesName route)) model.Shared mappedPage.LayoutProps
    let layout = Layout.RoutesLayoutName (mappedPage.LayoutProps, layoutModel)
    {
        model with
            CurrentRoute = Route.RoutesName route
            CurrentPage = Page.RoutesName mappedPage
            CurrentPageModel = PageModel.RoutesName pageModel
            CurrentLayout = layout
            CurrentLayoutName = LayoutName.RoutesLayoutName
            PageModelByLayout = Map.change LayoutName.RoutesLayoutName (fun _ -> Some (Page.RoutesName mappedPage)) model.PageModelByLayout
    },
    Command.batch [
        sharedCmd
        pageCmd
        Command.map mappedPage.LayoutMsgToMsg mappedPage.LayoutMsgToMsg layoutCmd
        Command.map (LayoutMsg.RoutesLayoutNameMsg >> LayoutMsg) (LayoutMsg.RoutesLayoutNameMsg >> LayoutMsg) layoutCmd
    ] |> commandToCmd SharedMsg id

let init () =
    let initialUrl = Route.parse (Router.currentUrl ())
    let sharedModel, sharedCmd = Shared.init ()

    let defaultModel = {
        Shared = sharedModel
        CurrentRoute = initialUrl
        CurrentPage = Page.NotFound
        CurrentPageModel = PageModel.NotFound
        CurrentLayout = Layout.None
        CurrentLayoutName = LayoutName.None
        PageModelByLayout = Map.empty
    }

    match initialUrl with
    | Route.RoutesName route ->
        initRoutesNamePage defaultModel route sharedCmd
    | Route.NotFound ->
        {
            defaultModel with
                CurrentPage = Page.NotFound
        },
        Cmd.none

let update (msg: Msg) (model: Model) =
    let updateLayout (model: Model) (layout: Layout<_,_,_>) props model' mapLayout layoutMsg msg pageCmd =
        let model'', cmd = layout.Update layoutMsg model'

        {
            model with
                CurrentLayout = mapLayout (props, model'')
        },
        Command.batch [
            Command.map msg msg cmd
            pageCmd
        ]
        |> commandToCmd SharedMsg id

    match msg with
    | SharedMsg msg' ->
        let model'', cmd = Shared.update msg' model.Shared
        { model with Shared = model'' }, sharedCommandToCmd cmd
    | RouteChanged nextRoute ->
        if model.CurrentRoute = nextRoute then
            model, Cmd.none
        else
            match nextRoute with
            | Route.RoutesName route ->
                initRoutesNamePage model route Command.none
            | Route.NotFound ->
                {
                    model with
                        CurrentPage = Page.NotFound
                        CurrentRoute = Route.NotFound
                        CurrentLayout = Layout.None
                        CurrentLayoutName = LayoutName.None
                },
                Cmd.none
    | PageMsg pageMsg ->
        match model.CurrentPage, pageMsg, model.CurrentPageModel with
        | Page.RoutesName mappedPage, PageMsg.RoutesNameMsg pageMsg', PageModel.RoutesName pageModel ->
            let pageModel, pageCmd = mappedPage.Update pageMsg' pageModel
            {
                model with
                    CurrentPageModel = PageModel.RoutesName pageModel
            },
            commandToCmd SharedMsg id pageCmd
        | currentPage, pageMsg, currentPageModel ->
            printfn $"Unhandled CurrentPage, PageMsg, CurrentPageModel, CurrentRoute. Got\nCurrentPage:\n%A{currentPage}\nPageMsg:\n%A{pageMsg}\nCurrentPageModel:\n%A{currentPageModel}"
            model, Cmd.none
    | LayoutMsg layoutMsg ->
        let updatePage layoutMsg =
            model.PageModelByLayout
            |> Map.tryFind model.CurrentLayoutName
            |> function
                | Some p ->
                    match p, model.CurrentPageModel, layoutMsg with
                    | Page.RoutesName mappedPage, PageModel.RoutesName m, LayoutMsg.RoutesLayoutNameMsg layoutMsg' ->
                        let pageMsg = mappedPage.LayoutMsgToPageMsg layoutMsg'
                        let m, cmd = mappedPage.Update pageMsg m
                        { model with
                            CurrentPageModel = PageModel.RoutesName m
                        }
                        , cmd
                    | _ -> model, Command.none
                | _ -> model, Command.none

        match layoutMsg, model.CurrentLayout with
        | LayoutMsg.LayoutsNameMsg layoutMsg', Layout.LayoutsName (props, model') ->
            let model, pageCmd = updatePage (LayoutMsg.LayoutsNameMsg layoutMsg')
            let layout = (LayoutsModuleName.layout props model.CurrentRoute model.Shared)
            updateLayout model layout props model' Layout.LayoutsName layoutMsg' (LayoutMsg.LayoutsNameMsg >> LayoutMsg) pageCmd
        | layoutMsg', layout ->
            printfn $"Unhandled LayoutMsg and CurrentLayout. Got\nLayoutMsg:\n%A{layoutMsg'}\nCurrentLayout:\n%A{layout}"
            model, Cmd.none

let inline (|Renderable|) (o: 'x when 'x: (member Render: unit -> ReactElement)) = o

let view (model: Model) (dispatch: Msg -> unit) =
    let currentPageView =
        match model.CurrentPageModel, model.CurrentRoute with
        | PageModel.RoutesName m, Route.RoutesName route ->
            (RoutesModuleName.page model.Shared route).View m (PageMsg.RoutesMsgName >> PageMsg >> dispatch)
        | _ -> RootModule.Pages.NotFound.view ()

    let currentView =
        match model.CurrentLayout with
        | Layout.LayoutsName (props, m) ->
            (LayoutsModuleName.layout props model.CurrentRoute model.Shared).View m currentPageView (LayoutMsg.LayoutsMsgName >> LayoutMsg >> dispatch)
        | Layout.None -> currentPageView

    let currentReactElement =
        match currentView with
        | Renderable x -> x.Render()

    React.router [
        router.onUrlChanged (Route.parse >> RouteChanged >> dispatch)
        router.children [ currentReactElement ]
    ]

let subscribe model =
    Sub.batch [
        Sub.map "Shared" SharedMsg (Shared.subscriptions model.Shared)
        match model.CurrentLayout with
        | Layout.LayoutsName (props, m) -> Sub.map "LayoutLayoutsName" (LayoutMsg.LayoutsMsgName >> LayoutMsg) ((LayoutsModuleName.layout props model.CurrentRoute model.Shared).Subscriptions m)
        | _ -> Sub.none
        match model.CurrentRoute, model.CurrentPageModel with
        | Route.RoutesName route, PageModel.RoutesName pageModel ->
            Sub.map "PageRoutesName" (PageMsg.RoutesMsgName >> PageMsg) ((RoutesModuleName.page model.Shared route).Subscriptions pageModel)
        | _ -> Sub.none
    ]

Program.mkProgram init update view
|> Program.withErrorHandler (fun (msg, ex) -> printfn "Program error handler:\r\n%s\r\n%O" msg ex)
|> Program.withReactBatched "app"
|> Program.withSubscription subscribe
|> Program.run
"""

[<Fact>]
let settings_json () =
    getResource<settings_json> Settings_json
    |> Expects.equals
        """{
  "files.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/*.fs.js": true,
    "**/fable_modules": true,
    "**/node_modules": true
  }
}
"""

[<Fact>]
let ``elmish-land_json`` () =
    getResource<``elmish-land_json``> Elmish_land_json
    |> Expects.equals
        """{
  "view": {
    "module": "Feliz",
    "type": "ReactElement",
    "textElement": "Html.text"
  },
  "projectReferences": []
}
"""

[<Fact>]
let vite_config_js () =
    getResource<vite_config_js> Vite_config_js
    |> Expects.equals
        """import { defineConfig } from 'vite'

export default defineConfig({
    build: {
        outDir: "dist"
    }
})
"""
