module ``elmish-land``.Pages.Testy.Page

open System
open Feliz
open Elmish

type Model = unit

type Msg = | NoOp

let init () : Model * Cmd<Msg> = (), Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | NoOp -> model, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) : ReactElement = Html.text "Testy"

let subscribe (model: Model) : (string list * ((Msg -> unit) -> IDisposable)) list = []
