module ``using-commands-from-shared-module-does-not-work-27``.Shared

open System
open Elmish
open ElmishLand

type SharedModel = unit

type SharedMsg =
    | DoSleep
    | SleepCompleted of unit

let sleep () =
    async {
        do! Async.Sleep 1000
        Browser.Dom.console.log ("Slept for 1 second")
        return ()
    }

let init () = (), Command.ofShared DoSleep

let update (msg: SharedMsg) (model: SharedModel) =
    match msg with
    | DoSleep -> model, Command.ofCmd (Cmd.OfAsync.perform sleep () SleepCompleted)
    | SleepCompleted() -> model, Command.none

// https://elmish.github.io/elmish/docs/subscription.html
let subscriptions _model : (string list * ((SharedMsg -> unit) -> IDisposable)) list = []
