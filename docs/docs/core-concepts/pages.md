---
sidebar_position: 1
---

# Pages

## Overview

Pages are the basic building blocks of your Elmish Land application. When a user visits a URL, Elmish Land will use the names of the folders
in your src/Pages folder to decide which page to render. See [Routing](#routing) for more information.

:::info

**Already familiar with Elmish?**

In a standard Elmish project, all URL requests often go to one big routing file. In Elmish Land, this file is generated for you automatically.

The big difference in Elmish Land is that all pages share data with via `SharedModel` and access type-safe URL information using the `Route` type.

No need to write your URL parsers by hand!

:::

## Adding pages

When you run the `elmish-land add page` command, a new page is created.

```bash
dotnet elmish-land add page "/about"
```

:::warning

You need to manually add the new page to your project file by using an IDE or by adding the following line to an ItemGroup in the project file `./MyProject.fsproj`:

`<Compile Include="src/Pages/About/Page.fs" />`

:::

The "add page" command generates `src/Pages/About/Page.fs` with the following content:

```fsharp
module MyProject.Pages.About.Page

open System
open Feliz
open Elmish
open ElmishLand.Page
open ElmishLand.Routes
open MyProject
open MyProject.Shared

type Model = unit

type Msg = | NoOp

let init (): Model * Command<Msg, SharedMsg> =
    (),
    Command.none

let update (msg: Msg) (model: Model): Model * Command<Msg, SharedMsg> =
    match msg with
    | NoOp -> model, Command.none

let view (model: Model) (dispatch: Msg -> unit): ReactElement =
    Html.text "About"

let page (shared: SharedModel) (route: AboutRoute) =
    Page.create init update view
```

### Understanding pages

#### `Model`
The `Model` contains the state of or page. Everytime a value change on the page we need to update the `Model`. An example
of this is typing text in a HTML `ìnput` element. For every change we need update the `Model` with the new value from the
`onChange` event of the element.

#### `Msg`
The `Msg` contains all the possible events of our page. Examples of events can be:
* The user click a button
* An API response from the server is received
* A timer event is triggered by the browser.

#### `page`
The `page` function is our starting point for the page. From this function we need to call the `Page.create` function to setup our page.
`Page.create` takes in three smaller functions. Together, they tell Elmish Land how your page should look and behave. Here's an overview of each function:

#### `init`

This function is called anytime your page loads.

```fsharp
let init (): Model * Command<Msg, SharedMsg> =
    (),
    Command.none
```

:::info

You can read more about `Command.none` on the [Commands](/docs/core-concepts/commands) page.

:::

#### `update`

This function is called whenever a message is sent. An example of this is a user clicking a button.

```fsharp
let update (msg: Msg) (model: Model): Model * Command<Msg, SharedMsg> =
    match msg with
    | NoOp -> model, Command.none
```

#### `view`

This function converts the current model to the HTML you want to show to the user.

```fsharp
let view (model: Model) (dispatch: Msg -> unit): ReactElement =
    Html.text "About"
```

### Working with `shared` and `route`

You may have noticed that every `page` is a function that receive two arguments, `shared` and `route`:

```fsharp
let page (shared: SharedModel) (route: AboutRoute) =
    Page.create init update view
```

But what are these arguments for?

* `shared` – Stores any data you want to share across all your pages.
  * In [the Shared section](/), you'll learn how to customize what data should be available.
* `route` – Stores URL information, including things like URL parameters and query.
  * In [the Route section](/), you'll learn more about the other values on the route field.

Both of these values are available to any function within `page`. That means `init`, `update` and `view` all can get access to shared and route.

In the code example below, note how we pass the `shared` value as the first argument of the `view` function:

```fsharp
let page (shared: SharedModel) (route: AboutRoute) =
    Page.create
        init
        update
        (view shared)
```

After we pass in the shared argument, we can update our view function to get access to shared in our view code:

```fsharp
let view (shared: SharedModel) (model: Model) (dispatch: Msg -> unit): ReactElement =
    Html.text "About"
```

The same concept applies to `init`, `update`, and `subscriptions`.

For example, you might want your `init` function to use a URL parameter to decide what API endpoint to call. In this case, we can pass `route` into our `init` function using the same process as before:

```fsharp
let page (shared: SharedModel) (route: AboutRoute) =
    Page.create
        (init route)
        update
        view
```

After we pass in the `route` argument, we can update our `init` function to get access to `route` in our view code:

```fsharp
let init (route: AboutRoute): Model * Command<Msg, SharedMsg> =
    (),
    Command.none
```

## Routing

Elmish Land uses a file-system based router where folders are used to define routes.

Each folder represents a route segment that maps to a URL segment. To create a nested route, you can nest folders inside each other.

* `src/Pages/Home` is the root route
* `src/Pages/About` creates an `/about` route
* `src/Pages/Blog/_slug` creates a route with a parameter, slug, that can be used to load data dynamically when a user requests a page like `/blog/hello-world`

Each page folder contains a file called `Page.fs` that contains the page code.

```bash
src/
└── Pages/
    ├── Home
    │   └── Page.fs
    ├── About
    │   └── Page.fs
    └── Blog
        ├── Shared
        └── Page.fs
```

In this example, the `/blog/shared` URL path is not accessible as a page because it does not have the name Page.fs. This folder could be used to store components, stylesheets, images, or other colocated files.

Here are the categories of routes you'll find in every Elmish Land project, ordered from most to least specific:

| Route             | URL example   | Description                                        |
| ----------------- |---------------|----------------------------------------------------|
| Homepage          | /             | Handles requests to the top-level URL (/).         |
| Static routes     | /people       | Directly maps one URL to a page.                   |
| Dynamic routes    | /people/:id   | Maps many URLs with a similar structure to a page. |
| Not found page    | /*	         | Handles any URL that can't find a matching page.   |

### Homepage

This file is created automatically for you with the `elmish-land init` command.

| Page filename	           | URL  |
|--------------------------|------|
| `src/Pages/Home/Page.fs` | `/`  |

### Static routes

Let's start by talking about "static routes". These routes directly map one URL to a page file.

You can use capitalization in your filename to add a dash (`-`) between words.

| Page filename                               | URL                        |
|---------------------------------------------|----------------------------|
| `src/Pages/Hello/Page.fs`                   | `/hello`                   |
| `src/Pages/AboutUs/Page.fs`                 | `/about-us`                |
| `src/Pages/Settings/Account/Page.fs`        | `/settings/account`        |
| `src/Pages/Settings/General/Page.fs`        | `/settings/general`        |
| `src/Pages/Something/Really/Nested/Page.fs` | `/something/really/nested` |

### Dynamic routes

Some page folders have a leading underscore, (like `_Id` or `_User`). These are called "dynamic pages", because this page can handle multiple URLs matching the same pattern. Here are some examples:

| Page filename                       | URL                 | Example URLs                                                   |
|-------------------------------------|---------------------|----------------------------------------------------------------|
| `src/Pages/Blog/_Id/Page.fs`        | `/blog/:id`         | `/blog/1`, `/blog/2`, `/blog/xyz`, ...                         |
| `src/Pages/Users/_Username/Page.fs` | `/users/:username`  | `/users/ryan`, `/users/2`, `/users/bob`, ...                   |
| `src/Pages/Settings/_Tab/Page.fs`   | `/settings/:tab`    | `/settings/account`, `/settings/general`, `/settings/api`, ... |

The name of the folder (`_Id`, `_User` or `_Tab`) will determine the names of the fields available on the `Route` value passed into your page function:

```fsharp
// /blog/123
route.Id = "123"

// /users/ryan
route.User = "ryan"

// /settings/account
route.Tab = "account"
```

For example, if we renamed `Settings/_Tab/Page.fs` to `Settings/_Foo/Page.fs`, we'd access the dynamic route parameter with `route.Foo` instead.

:::info

If this concept is already familiar to you, great! "Dynamic routes" aren't an Elmish Land idea, they come from popular frameworks like Next.js and Nuxt.js:

* Next.js uses the naming convention: blog/[id].js
* Nuxt.js uses the naming convention: blog/_id.vue

:::

### Query parameters

Every page's `Route` type has a `Query` field. This field contains the query parameters for the current URL. The following URL:

`/Blog?username=john`

will yield the following value for the `Query` field:

```fsharp
[ "username", "john" ]
```

You can get a query parameter by using the `tryGetQueryParam` function:

```fsharp
open MyProject.Routes

let username = query |> tryGetQueryParam "username"
```
