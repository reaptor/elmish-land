module ``backwards-compability-with-1-0-4``.Pages.Layout

open Feliz
open ElmishLand
open ``backwards-compability-with-1-0-4``.Shared

type Props = unit

type Model = unit

type Msg = | NoOp

let init () = (), Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | NoOp -> model, Command.none

let routeChanged (model: Model) = model, Command.none

let view (_model: Model) (content: ReactElement) (_dispatch: Msg -> unit) = content

let layout (_props: Props) (_route: Route) (_shared: SharedModel) =
    Layout.from init update routeChanged view
