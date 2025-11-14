---
sidebar_position: 3
---

# Linking and Navigating

There are two ways to navigate between pages in Elmish Land:

* Using the HTML Anchor element (\<a\>) with the `Route.format` function
* Using the `Command.navigate` command

This page will go through how to use `Route.format` and `Command.navigate`, and dive deeper into how navigation works.

## Routes

Elmish Land generates a discriminated union named `<MyProject>.Routes.Route` with all the routes for your project.

The file `src/Pages/Foo/_bar/_baz/Page.fs` will generate the following route:

```fsharp
type FooBarBazRoute =
    {
        Bar: string
        Baz: string
        Query: list<string * string>
    }

type Route =
    | FooBarBaz of FooBarBazRoute
```

You can refer to the above route by using the following:

```fsharp
Route.FooBarBaz { Bar = "bar"; Baz = "baz"; Query = ["name", "John"] }
```

which will point to the URL: `#/Foo/bar/baz?name=John`

## Anchor element (\<a\>)

The `Route.format` function is used to get the URL for a specific route.
```fsharp
Html.a [
    prop.text "Click me"
    prop.href (
        Route.FooBarBaz { Bar = "bar"; Baz = "baz"; Query = ["name", "John"] }
        |> Routes.Route.format
    )
]
```

## Command navigate

The `Command.navigate` function creates a command that navigates to a specified route.

```fsharp
open Elmish

let init (): Model * Command<Msg, SharedMsg> =
    (),
    Command.navigate(Route.FooBarBaz { Bar = "bar"; Baz = "baz"; Query = ["name", "John"] })
```

Will automatically redirect the page to the `#/Foo/bar/baz?name=John` when loaded.

:::info

These examples uses [route parameters](/docs/core-concepts/routing#route-parameters-dynamic-routing).

:::

## API Reference

For complete API documentation on navigation functions:

- **[Route Module API Reference](/docs/api-reference/route-module)** - Details on `Route.format` and other route utilities
- **[Command Module API Reference](/docs/api-reference/command-module)** - Details on `Command.navigate` and other commands
