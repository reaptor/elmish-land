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

type Model = unit

type Msg = | NoOp

let init (): Model * Command<Msg, SharedMsg, Msg> =
    (),
    Command.none

let update (msg: Msg) (model: Model): Model * Command<Msg, SharedMsg, Msg> =
    match msg with
    | NoOp -> model, Command.none

let view (model: Model) (content: Feliz.ReactElement) (dispatch: Msg -> unit): Feliz.ReactElement =
    Html.div [
        Html.text "User Layout"
        Html.div [
            content
        ]
    ]

let layout (shared: SharedModel) =
    Layout.from init update view
```

Layouts have the same structure as pages, goto [Understanding pages](/docs/core-concepts/pages#understanding-pages) to read more. 

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


## Root Layout (Required)

The root layout is defined at the top level of the `/src/Pages` folder and is automatically create when you initialize a new Elmish Land project.
