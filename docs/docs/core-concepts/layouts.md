---
sidebar_position: 2
---

# Layouts

## Overview

A layout is UI that is shared between multiple routes. On navigation, layouts preserve state and remain interactive.

## Adding layouts

You can create a layout by running the following command:
```bash
dotnet elmish-land add layout "/User"
```

:::warning

You need to manually add the new layout to your project file by using an IDE or by adding the following line to an ItemGroup in the project file `./MyProject.fsproj`:

`<Compile Include="src/Pages/User/Layout.fs" />`

:::

The "add layout" command generates src/Pages/User/Layout.fs with the following content:

```fsharp
module MyProject.Pages.User.Layout

open Feliz
open ElmishLand
open MyProject.Shared

type Props = unit

type Model = unit

type Msg = | NoOp

let init () =
    (),
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | NoOp -> model, Command.none

let routeChanged (_route: Route) (model: Model) =
    model, Command.none

let view (_model: Model) (content: Feliz.ReactElement) (_dispatch: Msg -> unit) =
    Html.div [
        Html.text "User Layout"
        Html.div [
            content
        ]
    ]

let layout (_props: Props) (_shared: SharedModel) =
    Layout.from init update routeChanged view
```

Layouts have the same [structure as pages](/docs/core-concepts/pages#understanding-pages) except for the `Props` type and the `layout` and `routeChanged` functions.

### Understanding layouts

#### `layout`
The `layout` function is our starting point for the layout. From this function we need to call the `Layout.from` function to setup our layout.
```fsharp
let layout (_props: Props) (_route: Route) (_shared: SharedModel) =
    Layout.from init update routeChanged view
```

#### `Props`
The `Props` type is used to enable pages to pass initial data to a layout.
The `Props` is passed to the `layout` function so it's available to `init`, `update`, `routeChanged` and `view.`

```fsharp
type Props = unit
```

#### `routeChanged`
The `routeChanged` function is called every time the user navigates with [commands or links](/docs/core-concepts/linking-and-navigating).
This function is NOT called the first time the layout loads. Use the `init` function for this.

```fsharp
let routeChanged (model: Model) =
    model, Command.none
```

If you need to access the current route from this function, you can pass it from the `layout` function.

```fsharp
let routeChanged (route: Route) (model: Model) =
    model, Command.none
    
let layout (_props: Props) (route: Route) (_shared: SharedModel) =
    Layout.from init update (routeChanged route) view
```

## Layout selection

All pages that are in a sub-folder of a layout will use that layout.

In the following example : 
```
MyProject
└── src
    └── Pages
        ├── Layout.fs
        └── User
            └── Page.fs
```
the page `/src/Pages/User/Page.fs` will use the layout `/src/Pages/Layout.fs`.


## Root Layout

The root layout is defined at the top level of the `/src/Pages` folder and is automatically 
created when you initialize a new Elmish Land project.

## Sending messages to and from pages

### Sending messages to pages
When you need to send messages from a layout to it's current page you will use the `Command.ofMsg` function from the layout
and handle the `LayoutMsg` on the page. The message will be sent both to the layout and the page.

```fsharp
// A Layout.fs file
type Msg =
    | LoadUser of string
    
let routeChanged (_route: Route) (model: Model) =
    model, Command.ofMsg (LoadUser "John Doe")
    
// A Page.fs file
let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg (Layout.LoadUser name) -> model, Command.none    
```

In [the Commands section](/docs/core-concepts/commands), you'll learn more about commands.

### Sending messages from pages
When you need to send messages from a page to it's layout you will use the `Command.ofLayout` function.

```fsharp
// A Page.fs file
let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
    | SignOut -> model, Command.ofLayout Layout.SignOutClicked    
```

In [the Commands section](/docs/core-concepts/commands), you'll learn more about commands.
