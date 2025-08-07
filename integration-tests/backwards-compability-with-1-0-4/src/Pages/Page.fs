module ``backwards-compability-with-1-0-4``.Pages.Page

open Feliz
open ElmishLand
open ``backwards-compability-with-1-0-4``.Shared
open ``backwards-compability-with-1-0-4``.Pages

type Model = unit

type Msg = | LayoutMsg of Layout.Msg

let init () = (), Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none

let view (_model: Model) (_dispatch: Msg -> unit) = Html.text " Page"

let page (_shared: SharedModel) (_route: HomeRoute) = Page.from init update view () LayoutMsg
