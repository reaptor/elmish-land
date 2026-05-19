# Elmish Land

> Build reliable, type-safe web applications with the power of F# and the Elm Architecture.

Elmish Land is a framework for building F# browser apps on top of [Fable](https://fable.io), [Feliz](https://fable-hub.github.io/Feliz/), [Elmish](https://elmish.github.io/elmish/), [React](https://react.dev), and [Vite](https://vitejs.dev). It ships as a `dotnet` CLI that scaffolds your project, generates type-safe routes from your file system, runs a dev server with hot reload, and produces a production build — so you can focus on writing pages and your application's model/update/view code instead of wiring up the toolchain.

Full documentation: **[elmish.land](https://elmish.land)**

## Getting started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/) 10.0 or higher
- [Node.js](https://nodejs.org/) 20.19 or above

### Create a new project

```bash
mkdir MyProject
cd MyProject
dotnet tool install elmish-land
dotnet elmish-land init
dotnet elmish-land server
```

`init` will prompt you to pick between **hash routing** (URLs like `/#/about` — works everywhere, no server config) and **path routing** (URLs like `/about` — needs server-side fallback to `index.html`). If you're unsure, pick hash routing; you can change it later in `elmish-land.json`.

The dev server starts at `http://localhost:5173`. The scaffolded `package.json` also exposes `npm start` and `npm run build` as shortcuts.

See the full guide: [Creating a project](https://elmish.land/docs/getting-started/creating-a-project).

## What's in the box

- **Pages with the Elm Architecture.** Each page is a self-contained module with its own `Model`, `Msg`, `init`, `update`, and `view` — simple, predictable, easy to reason about.
- **File-system routing.** Create files in `src/Pages/` and Elmish Land generates the routes for you. No manual route table to maintain.
- **Type-safe routes.** Declare path and query parameters in a small `route.json` file and get a typed `Route` value in your page function — no string parsing, no runtime errors.
- **Layouts.** Share UI between routes; layouts preserve state across navigation.
- **Shared model.** A single `Shared` module gives every page and layout access to app-wide state, commands, and subscriptions.
- **One-command upgrades.** `dotnet elmish-land upgrade` brings an existing project up to the framework's current dependency set and applies mechanical source rewrites where it can.

## A page in 20 lines of F#

```fsharp
// src/Pages/Page.fs
type Model = { Count: int }

type Msg =
    | Increment
    | Decrement

let init () =
    { Count = 0 }, Command.none

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }, Command.none
    | Decrement -> { model with Count = model.Count - 1 }, Command.none

let view model dispatch =
    Html.div [
        Html.button [ prop.onClick (fun _ -> dispatch Decrement); prop.text "-" ]
        Html.span [ prop.text (string model.Count) ]
        Html.button [ prop.onClick (fun _ -> dispatch Increment); prop.text "+" ]
    ]
```

## Community

- [Discord](https://discord.gg/jQ26cZH3fU)
- [Blog / release notes](https://elmish.land/blog)
- [Issue tracker](https://github.com/reaptor/elmish-land/issues)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on how to develop Elmish Land locally.
