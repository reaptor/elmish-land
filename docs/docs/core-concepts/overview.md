---
sidebar_position: 0
---

# Overview

Elmish Land applications follow the **Model-View-Update** (MVU) pattern, also known as The Elm Architecture (TEA). This pattern creates a simple, predictable cycle for managing state and UI.

## What is the Elm Architecture?

The Elm Architecture originated in the [Elm programming language](https://guide.elm-lang.org/architecture/) as a way to structure interactive applications. It has since been adopted across many languages and frameworks because of its simplicity and reliability.

The core idea is a **unidirectional data flow** — state changes always follow the same predictable cycle:

```
User Action → Message → Update → Model → View
     ↑                                     |
     └─────────────────────────────────────┘
```

1. The **View** renders the current **Model** as a UI
2. A user interaction (click, input, etc.) produces a **Message**
3. The **Update** function takes the message and current model, and returns a new model
4. The view re-renders with the new model

This cycle repeats for every interaction, making your application's behavior predictable and easy to debug. There are no surprise side effects or hidden state mutations — everything flows through the same loop.

## What is Elmish?

[Elmish](https://elmish.github.io/elmish/) is the F# implementation of the Elm Architecture, built on top of [React](https://react.dev/). It brings the MVU pattern to the .NET ecosystem while leveraging React's efficient rendering under the hood.

Elmish Land builds on top of Elmish by providing file-based [routing](/docs/core-concepts/routing), [layouts](/docs/core-concepts/layouts), [shared state](/docs/core-concepts/shared), and [CLI tooling](/docs/api-reference/cli) — so you get the benefits of the Elm Architecture with the productivity of a full framework.

## Model, Update, View in Practice

Here's a simple counter to show how the pieces work together.

### Model

The **Model** is a plain F# type that represents your application state. It is immutable — you never mutate it directly.

```fsharp
type Model = { Count: int }
```

### Msg

A **Msg** (message) is a discriminated union that describes everything that can happen in your application:

```fsharp
type Msg =
    | Increment
    | Decrement
```

### init

The **init** function returns the initial model and an optional [command](/docs/core-concepts/commands) to run at startup:

```fsharp
let init () =
    { Count = 0 }, Command.none
```

### update

The **update** function takes a message and the current model, and returns a new model along with any [commands](/docs/core-concepts/commands) to execute:

```fsharp
let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, Command.none
    | Decrement -> { model with Count = model.Count - 1 }, Command.none
```

Because `update` is a pure function, it's easy to test and reason about. Given the same message and model, it always produces the same result.

### view

The **view** function takes the model and a `dispatch` function, and returns the UI. Calling `dispatch` with a message starts the cycle again:

```fsharp
let view model dispatch =
    Html.div [
        Html.button [ prop.onClick (fun _ -> dispatch Decrement); prop.text "-" ]
        Html.span [ prop.text (string model.Count) ]
        Html.button [ prop.onClick (fun _ -> dispatch Increment); prop.text "+" ]
    ]
```

When the user clicks "+" the view dispatches `Increment`, the update function produces a new model with `Count + 1`, and the view re-renders with the updated count.

## Building blocks of an Elmish Land application

Elmish Land organizes your application into different building blocks:

### Pages
The fundamental building block. Each page has its own Model, Msg, init, update, and view. Pages correspond to URL [routes](/docs/core-concepts/routing) and are created using file-based conventions.

[Learn about Pages →](/docs/core-concepts/pages)

### Layouts
Shared UI that wraps pages. Layouts preserve their state across page navigation, making them ideal for navigation bars, sidebars, and other persistent UI elements.

[Learn about Layouts →](/docs/core-concepts/layouts)

### Shared State
Global state available to every page and layout. Use it for cross-cutting concerns like authentication status or user preferences.

[Learn about Shared State →](/docs/core-concepts/shared)

## Data Flow

Here's how data flows between the layers:

```
┌──────────────────────────────────┐
│           Shared State           │
│  (available to all pages/layouts)│
└──────────┬───────────────────────┘
           │
     ┌─────▼─────┐
     │   Layout   │
     │ (shared UI)│
     └─────┬──────┘
           │
     ┌─────▼─────┐
     │    Page    │
     │  (route)   │
     └───────────┘
```

- **Shared** state flows down to layouts and pages
- [**Pages**](/docs/core-concepts/pages) can send messages up to [Shared](/docs/core-concepts/shared) via [`Command.ofShared`](/docs/api-reference/command-module)
- [**Pages**](/docs/core-concepts/pages) can send messages to their [Layout](/docs/core-concepts/layouts) via [`Command.ofLayout`](/docs/api-reference/command-module)
- [**Layouts**](/docs/core-concepts/layouts) can send messages down to pages via `LayoutMsg`
