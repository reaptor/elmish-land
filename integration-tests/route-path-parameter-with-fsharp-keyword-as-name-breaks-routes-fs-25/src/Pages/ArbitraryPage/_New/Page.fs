module ``route-path-parameter-with-fsharp-keyword-as-name-breaks-routes-fs-25``.Pages.ArbitraryPage.New.Page

open Feliz
open ElmishLand
open ``route-path-parameter-with-fsharp-keyword-as-name-breaks-routes-fs-25``.Shared
open ``route-path-parameter-with-fsharp-keyword-as-name-breaks-routes-fs-25``.Pages

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
    Html.text "ArbitraryPage_New Page"

let page (_shared: SharedModel) (_route: ArbitraryPage_NewRoute) =
    Page.from init update view () LayoutMsg
