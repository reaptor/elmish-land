---
sidebar_position: 0
---

# Overview

Elmish Land is a framework for building **web frontends in F#**. You write your application as ordinary F# — types, modules, pattern matching — and Elmish Land turns it into a website that runs in the browser, with a development server, hot reload, and a production build ready to deploy.

If you know F# but have done little frontend work, there are two things to picture: **how your F# code becomes a running web app** (Fable and npm packages), and **how that app is organized** (the Model-View-Update architecture). This page covers both.

## From F# to a web app

A browser can't run F# — it runs JavaScript, HTML, and CSS. Elmish Land bridges that gap with a small toolchain, all driven by a single `dotnet elmish-land` CLI so you never have to wire it together yourself.

```
   Your F# code
        │   Fable compiles F# → JavaScript
        ▼
   JavaScript  ──calls──►  npm packages (React, …)
        │   Elmish Land serves it in dev, bundles it for production
        ▼
   A web app running in the browser
```

### Fable — the F#-to-JavaScript compiler

[Fable](https://fable.io) translates your F# into JavaScript that the browser can run. You keep writing idiomatic F# — records, discriminated unions, the type checker watching your back — and Fable produces the equivalent JavaScript. This is what makes F# in the browser possible at all.

### npm packages — the JavaScript ecosystem your app builds on

Frontend apps rely on JavaScript libraries published to [npm](https://www.npmjs.com/). The `package.json` Elmish Land scaffolds for you pulls in [React](https://react.dev), which efficiently renders your UI to the page. You don't call these libraries as JavaScript, though: F# bindings like [Feliz](https://fable-hub.github.io/Feliz/) let you write the UI in F# (that's the `Html.div` / `prop` syntax you'll see below), and [Elmish](https://elmish.github.io/elmish/) provides the architecture on top of React.

### A dev server and a production build

While you develop, Elmish Land runs a local server at `http://localhost:5173` and hot-reloads the page as you change code, so you see edits instantly. For release, it bundles everything into optimized static files in `dist/` that you can host anywhere.

You drive all of this with the Elmish Land CLI — `dotnet elmish-land server` for development and `dotnet elmish-land build` for production. See [Creating a project](/docs/getting-started/creating-a-project) to get set up.

:::info Frontend only

Elmish Land and Fable are tools for **frontend** development — out of the box they produce the app that runs in the user's browser, nothing more. They don't include a backend or database. When your app needs server-side logic, it talks to a backend over HTTP, just like any other frontend.

Because F# runs on the server too, you can write that backend in F# with a library such as [ASP.NET](https://learn.microsoft.com/en-us/aspnet/core/), [Giraffe](https://giraffe.wiki), or [Saturn](https://saturnframework.org) — and even share types across the stack. See the [Fullstack guide](/docs/guides/fullstack-fsharp-with-elmish-land-and-asp-net) for a complete example.

:::

## The Model-View-Update architecture

Now that your F# runs in the browser, how do you organize it? The hard part of any frontend is managing state — what's on screen, what the user has done, what data has loaded — and keeping the UI in sync with it.

Elmish Land applications use the **Model-View-Update** (MVU) pattern, also known as The Elm Architecture (TEA). It originated in the [Elm programming language](https://guide.elm-lang.org/architecture/) and has since been adopted across many languages because of its simplicity and reliability. It keeps state management predictable through a single cycle — **unidirectional data flow**:

```
User Action → Message → Update → Model → View
     ↑                                     |
     └─────────────────────────────────────┘
```

1. The **View** renders the current **Model** as a UI
2. A user interaction (click, input, etc.) produces a **Message**
3. The **Update** function takes the message and current model, and returns a new model
4. The view re-renders with the new model

Every interaction flows through this same loop, so there are no surprise side effects or hidden state mutations — your application's behavior stays predictable and easy to debug. Under the hood, [Elmish](https://elmish.github.io/elmish/) runs this loop and React renders the result.

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

Elmish Land builds on top of Elmish by adding file-based [routing](/docs/core-concepts/routing), [layouts](/docs/core-concepts/layouts), [shared state](/docs/core-concepts/shared), and [CLI tooling](/docs/api-reference/cli) — so you get the Elm Architecture with the productivity of a full framework. It organizes your application into a few building blocks:

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
