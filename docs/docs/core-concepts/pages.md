---
sidebar_position: 1.1
---

# Pages

​In Elmish Land, pages serve as the fundamental components of your application, each corresponding to a specific URL route. When a user navigates to a particular URL, Elmish Land determines the appropriate page to render based on the folder structure within your `src/Pages` directory.

:::info

**Already familiar with Elmish?**

In a standard Elmish project, all URL requests often go to one big routing file. In Elmish Land, this file is generated for you automatically.

The big difference in Elmish Land is that all pages share data via `SharedModel` and access type-safe URL information using the `Route` type.

No need to write your URL parsers by hand!

:::

## Creating a New Page

To add a new page to your Elmish Land project, follow these steps:

1. **Generate the Page**: Use the CLI command to create a new page. For example, to add an **`About`** page:
    ```bash
    dotnet elmish-land add page "/About"
    ```
    This command creates a new file at `src/Pages/About/Page.fs`.
   
    💡 **Note**: The page path should be in file system format `/MyProfile` and ***not*** URL format `/my-profile`.
    
1. **Include the Page in Your Project**: Manually add the new page to your project file (`MyProject.fsproj`) by inserting the following line within an `<ItemGroup>`:
    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
            <TargetFramework>net9.0</TargetFramework>
            <LangVersion>latest</LangVersion>
        </PropertyGroup>    
        <ItemGroup>
            <Compile Include="src/Shared.fs"/>
            <Compile Include="src/Pages/NotFound.fs"/>
            <Compile Include="src/Pages/Layout.fs"/>
            <Compile Include="src/Pages/Page.fs"/>
            // highlight-start
            <Compile Include="src/Pages/About/Page.fs" />
            // highlight-end
        </ItemGroup>    
        <ItemGroup>
          <ProjectReference Include=".elmish-land/Base/ElmishLand.MyProject.Base.fsproj" />
        </ItemGroup>
    </Project>
    ```
    F# projects require all sources to be listed **in compilation order** in an `.fsproj` file. This may look quite restrictive at first, but it does have [some advantages](https://fsharpforfunandprofit.com/posts/cyclic-dependencies/).
   
The "add page" command generates `src/Pages/About/Page.fs` with the following content:

```fsharp
module MyProject.Pages.About.Page

open Feliz
open ElmishLand
open MyProject.Shared
open MyProject.Pages

type Model = unit

type Msg =
    | LayoutMsg of Layout.Msg

let init () =
    (),
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none

let view (_model: Model) (_dispatch: Msg -> unit) =
    Html.text "About Page"

let page (_shared: SharedModel) (_route: HomeRoute) =
    Page.from init update view () LayoutMsg

```

## Understanding the Page Structure

A typical page file (`Page.fs`) includes the following components:

### **Model** (the state of the page)
The `Model` contains the state of our page. Everytime a value changes on the page we need to update the `Model`. An example
of this is typing text in a HTML `input` element. For every change we need update the `Model` with the new value from the
`onChange` event of the element.

```fsharp
type Model = unit
```

### **Messages** (events or actions on the page)
The `Msg` contains all the possible events of our page. Examples of events can be:
* The user clicks a button
* An API response from the server is received
* A timer event is triggered by the browser.

```fsharp
type Msg =
    | LayoutMsg of Layout.Msg
```

:::info

You can read more about layouts on the [Layouts](/docs/core-concepts/layouts) page and more about `LayoutMsg` on the [Sending messages to pages](/docs/core-concepts/layouts#sending-messages-to-pages) section.

:::

### **Initialization** (the initial state of the page)

This function is called anytime your page loads for the first time.

```fsharp
let init () =
    (),
    Command.none
```

:::info

You can read more about `Command.none` on the [Commands](/docs/core-concepts/commands) page.

:::

### **Updates** (state transitions in response to messages)

This function is called whenever a message is sent. An example of this is a user clicking a button.

```fsharp
let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
```

### **Rendering** (the UI representation of the page)

This function converts the current model to the HTML you want to show to the user.

```fsharp
let view (_model: Model) (_dispatch: Msg -> unit) =
    Html.text "About Page"
```

### **Page** (constructs the page for Elmish Land)
The `page` function is our starting point for the page. From this function we need to call the `Page.from` function to setup our page.

```fsharp
let page (shared: SharedModel) (layout: MyProject.Pages.Layout.Model) (route: AboutRoute) =
    Page.from init update view
```

## Utilizing `shared` and `route`

The `page` function receives two parameters: `shared` and `route`.

* **shared**: Provides access to the [`SharedModel`](/docs/core-concepts/shared), allowing data to be shared across all pages.

* **route**: Contains URL information, including parameters and query strings, enabling [type-safe routing](/docs/core-concepts/routing#type-safe-routing).

All of these values are available to any function within `page`. That means `init`, `update` and `view` all can get access to shared and route.

In the code example below, note how we pass the `shared` value as the first argument of the `view` function:

```fsharp
let page (shared: SharedModel) (layout: MyProject.Pages.Layout.Model) (route: AboutRoute) =
    Page.from init update (view shared)
```

After we pass in the shared argument, we can update our view function to get access to shared in our view code:

```fsharp
let view (shared: SharedModel) (model: Model) (dispatch: Msg -> unit) =
    Html.text "About Page"
```

The same concept applies to `init`, `update`, and `subscriptions`.

For example, you might want your `init` function to use a URL parameter to decide what API endpoint to call. In this case, we can pass `route` into our `init` function using the same process as before:

```fsharp
let page (shared: SharedModel) (route: AboutRoute) =
    Page.from (fun () -> init route) update view
```

After we pass in the `route` argument, we can update our `init` function to get access to `route` in our view code:

```fsharp
let init (route: AboutRoute) =
    (),
    Command.none
```
