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
dotnet elmish-land add page "/About"
```

:::warning

You need to manually add the new page to your project file by using an IDE or by adding the following line to an ItemGroup in the project file `./MyProject.fsproj`:

`<Compile Include="src/Pages/About/Page.fs" />`

:::

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

### Understanding pages

#### `Model`
The `Model` contains the state of or page. Everytime a value change on the page we need to update the `Model`. An example
of this is typing text in a HTML `ìnput` element. For every change we need update the `Model` with the new value from the
`onChange` event of the element.

```fsharp
type Model = unit
```

#### `Msg`
The `Msg` contains all the possible events of our page. Examples of events can be:
* The user clicks a button
* An API response from the server is received
* A timer event is triggered by the browser.

```fsharp
type Msg =
    | LayoutMsg of Layout.Msg
```

:::info

You can read more about `LayoutMsg` on the [Sending messages to pages](/docs/core-concepts/layouts#sending-messages-to-pages) section.

:::

#### `init`

This function is called anytime your page loads for the first time.

```fsharp
let init () =
    (),
    Command.none
```

:::info

You can read more about `Command.none` on the [Commands](/docs/core-concepts/commands) page.

:::

#### `update`

This function is called whenever a message is sent. An example of this is a user clicking a button.

```fsharp
let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
```

#### `view`

This function converts the current model to the HTML you want to show to the user.

```fsharp
let view (_model: Model) (_dispatch: Msg -> unit) =
    Html.text "About Page"
```

#### `page`
The `page` function is our starting point for the page. From this function we need to call the `Page.from` function to setup our page.

```fsharp
let page (_shared: SharedModel) (_route: HomeRoute) =
    Page.from init update view () LayoutMsg
```

### Working with `shared` and `route`

You may have noticed that every `page` is a function that receive two arguments, `shared` and `route`:

```fsharp
let page (shared: SharedModel) (layout: MyProject.Pages.Layout.Model) (route: AboutRoute) =
    Page.from init update view
```

But what are these arguments for?

* `shared` – Stores any data you want to share across all your pages.
  * On [the Shared state page](/docs/core-concepts/shared), you'll learn how to customize what data should be available.
* `route` – Stores URL information, including things like URL parameters and query.
  * In [the Routing section](#routing), you'll learn more about the other values on the route field.

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

## Routing

Elmish Land uses a file-system based router where folders are used to define routes.

Each folder represents a route segment that maps to a URL segment. To create a nested route, you can nest folders inside each other.

* `src/Pages` is the root route
* `src/Pages/About` creates an `/about` route
* `src/Pages/Blog/_slug` creates a route with a parameter, slug, that can be used to load data dynamically when a user requests a page like `/blog/hello-world`

Each page folder contains a file called `Page.fs` that contains the page code.

```bash
src/
└── Pages/
    ├── Page.fs
    ├── About
    │   └── Page.fs
    └── Blog
        ├── Utils
        │   └── SharedUtils.fs
        └── Page.fs
```

In this example, the `/blog/utils` URL path is not available as a page because it does not contain a file named Page.fs. This folder could be used to store components, stylesheets, images, or other colocated files.

Here are the categories of routes you'll find in an Elmish Land project, ordered from most to least specific:

| Route            | URL example    | Description                                        |
|------------------|----------------|----------------------------------------------------|
| Root page        | /              | Handles requests to the top-level URL (/).         |
| Simple routes    | /people        | Directly maps one URL to a page.                   |
| Route parameters | /people/:id    | Maps many URLs with a similar structure to a page. |
| Query parameters | /people?id=:id | Pass arguments to a page in the query string.      |

### Root page

This file is created automatically for you with the `elmish-land init` command.

| Page filename	      | URL  |
|------------------------|------|
| `src/Pages/Page.fs`    | `/`  |

### Simple routes

Let's start by talking about "static routes". These routes directly map one URL to a page file.

You can use capitalization in your filename to add a dash (`-`) between words.

| Page filename                               | URL                        |
|---------------------------------------------|----------------------------|
| `src/Pages/Hello/Page.fs`                   | `/hello`                   |
| `src/Pages/AboutUs/Page.fs`                 | `/about-us`                |
| `src/Pages/Settings/Account/Page.fs`        | `/settings/account`        |
| `src/Pages/Settings/General/Page.fs`        | `/settings/general`        |
| `src/Pages/Something/Really/Nested/Page.fs` | `/something/really/nested` |

### Route parameters

Some page folders have a leading underscore, (like `_Id` or `_User`). These are called "route parameters". Here are some examples:

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

If this concept is already familiar to you, great! They are called "Dynamic routes" in many popular frameworks like Next.js and Nuxt.js:

* Next.js uses the naming convention: blog/[id].js
* Nuxt.js uses the naming convention: blog/_id.vue

:::

Elmish Land supports type safe routing via the `route.json` file. In the [Type safe routing section](#type-safe-routing), you'll learn more about how this works.

### Query parameters
Page folders can contain an optional `route.json` file that is used for specifying the query parameters for that page.

The following file `/Pages/User/route.json` will add two query parameters `name` and `age` to the `User` page:

```javascript
{
  "queryParameters": [
    {
      "module": "System",
      "name": "name"
    },
    {
      "module": "System",
      "name": "age"
    }
  ]
}
```

that will yield the follow url: `/user?name=john&age=23`

### Type safe routing

Elmish Land supports type safe route and query parameters through the `route.json` file.

The following `/Pages/User/_UserId/route.json`:

```javascript
{
    "pathParameter": {
        "module": "System",
        "type": "Guid",
        "parse": "parseGuid",
        "format": "formatGuid"
    },
    "queryParameters": [
        {
            "module": "System",
            "name": "age",
            "type": "int",
            "required": true,
            "parse": "parseInt",
            "format": "formatInt"
        }
    ]
}
```

yields the following `Route` type for the page `/user/:id?age=:age`:

```fsharp
module Routes =
    type User_UserIdRoute = { UserId: Guid; Age: int  }
```

The following parameter types can be used out of the box: 

`Guid`
```javascript
{
    "module": "System",
    "type": "Guid",
    "parse": "parseGuid",
    "format": "formatGuid"
}
```

`Int32`
```javascript
{
    "module": "System",
    "type": "int",
    "parse": "parseInt",
    "format": "formatInt"
}
```

`Int64`
```javascript
{
    "module": "System",
    "type": "int64",
    "parse": "parseInt64",
    "format": "formatInt64"
}
```

`Bool`
```javascript
{
    "module": "System",
    "type": "bool",
    "parse": "parseBool",
    "format": "formatBool"
}
```

`Float`
```javascript
{
    "module": "System",
    "type": "float",
    "parse": "parseFloat",
    "format": "formatFloat"
}
```

`Decimal`
```javascript
{
    "module": "System",
    "type": "decimal",
    "parse": "parseDecimal",
    "format": "formatDecimal"
}
```

In the [Custom route and query parameters page](/docs/advanced/custom-route-and-query-parameters), you'll learn more about how to use your own types as route and query parameters.
