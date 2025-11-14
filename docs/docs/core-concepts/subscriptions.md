---
sidebar_position: 6
---

# Subscriptions

Sometimes we have a source of events that runs independently of Elmish Land, like a timer. We can use subscriptions to control when those sources are running, 
and forward its events to our `update` function.

Let's define our `Model` and `Msg` types. `Model` will hold the current state and `Msg` will tell us the nature of the change that 
we need to apply to the current state.

```fsharp
module MyProject.Pages.Home.Page

open System
open Feliz
open Elmish
open ElmishLand.Routes
open ElmishLand.Page
open MyProject
open MyProject.Shared

type Model =
    {
        Now: DateTime
        Interval: int
    }

type Msg =
    | DateTimeChanged of DateTime
    | LayoutMsg of Layout.Msg
```

Now let's define init, update and view.

```fsharp
let init () =
    {
        Now = DateTime.Now
        Interval = 1000
    }, Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | DateTimeChanged now ->
        { model with
            Now = now
        }, Command.none
    | LayoutMsg _ -> model, Command.none

let view (model: Model) (dispatch: Msg -> unit) =
    Html.text $"The time is: %s{model.Now.ToString()}"
```

Now lets define our timer subscription:

```fsharp
let onEverySecond model dispatch =
    let everySecond () = dispatch (DateTimeChanged DateTime.Now)
    let intervalId = Browser.Dom.window.setInterval(everySecond, model.Interval)
    React.createDisposable (fun () ->
        Browser.Dom.window.clearInterval(intervalId)
    )

let subscriptions model =
    [
        [ "everySecond" ], onEverySecond model
    ]

let page (shared: SharedModel) (route: HomeRoute) =
    Page.from init update view () LayoutMsg
    |> Page.withSubscriptions subscriptions
```

`subscriptions` answers the question: "Which subscriptions should be running?" `subscriptions` is provided the current page state, `model`, 
to use for decisions. When the model changes, `subscriptions` is called. Elmish Land then starts or stops subscriptions to match what is returned.

A subscription has an ID, `[ "everySecond" ]` here, and a start function. The ID needs to be unique within that page.

We use the `Page.withSubscriptions` to ensure that our subscriptions all called.

:::info
**Why is ID a list?**

This allows us to include dependencies. [Later we will use this](/docs/core-concepts/subscriptions#ids-and-dependencies) to change the timer's interval.
:::

## Conditional subscriptions

In the above example, the timer subscription is always returned from `subscriptions`, so it will stay running as long as the page is running. 
Let's look at an example where the timer can be turned off.

First we add the field `Enabled` and a msg `Toggle` to change it.

```fsharp
module MyProject.Pages.Home.Page

type Model =
    {
        Now: DateTime
        Interval: int
        Enabled: bool
    }

type Msg =
    | DateTimeChanged of DateTime
    | Toggle of bool
    
let init () =
    {
        Now = DateTime.Now
        Interval = 1000
        Enabled = true
    }, Command.none      
```

Now let's handle the `Toggle` message.

```fsharp
let update (msg: Msg) (model: Model) =
    match msg with
    | DateTimeChanged now ->
        { model with
            Now = now
        }, Command.none
    | Toggle enabled ->
        { model with
            Enabled = enabled
        }, Command.none
```

Next, we change the `subscriptions` function to check `Enabled` before including the timer subscription.

```fsharp
let subscriptions model =
    [
        if model.Enabled then
            [ "everySecond" ], onEverySecond model
    ]
```

Now let's add an HTML view to visualize and control the timer.

```fsharp
let view (model: Model) (dispatch: Msg -> unit): ReactElement =
    Html.div [
        Html.p $"The time is: %s{model.Now.ToString()}"
        Html.label [
            Html.input [
                prop.type'.checkbox
                prop.isChecked model.Enabled
                prop.onCheckedChange (fun enabled -> dispatch (Toggle enabled))
            ]
            Html.text "Enabled"
        ]
    ]
```

## IDs and dependencies

Earlier we noted that ID is a list so that you can add dependencies to it. We'll use that to improve the last example.

In that example, the timer's interval came from the model:

```fsharp
let onEverySecond model dispatch =
    ...
    let intervalId = Browser.Dom.window.setInterval(everySecond, model.Interval)
    ...
```

But nothing happens to the subscription if `model.Interval` changes. Let's fix that.

```fsharp
let subscriptions model =
    [
        if model.Enabled then
            [ "everySecond"; string model.Interval ], onEverySecond model
    ]
```

Now that `model.Interval` is part of the ID, the timer will stop the old interval then start with the new interval whenever the interval changes.

How does it work? It is taking advantage of ID uniqueness. Let's say that `model.Interval` is initially 1000. The sub ID is `[ "everySecond"; "1000" ]`, 
Elmish Land starts the subscription. Then `model.Interval` changes to 2000. The sub ID becomes `[ "everySecond"; "2000" ]`. Elmish Land sees that ["everySecond"; "1000"] is no longer 
active and stops it. Then it starts the "new" subscription [ "everySecond"; "2000" ].

To Elmish Land each interval is a different subscription. But to `subscriptions` this is a single timer that changes intervals.

## API Reference

For complete API documentation on subscription functions:

- **[Page Module API Reference](/docs/api-reference/page-module)** - Details on `Page.withSubscriptions`
- **[Layout Module API Reference](/docs/api-reference/layout-module)** - Details on `Layout.withSubscriptions`
