---
sidebar_position: 4
---

# Linking and Navigating

There are two ways to navigate between routes in Elmish Land:

* Using the HTML Anchor element (\<a\>) with the `Route.format` function
* Using the `Command.navigate` command

This page will go through how to use `Route.format` and `Command.navigate`, and dive deeper into how navigation works.

## Routes

Elmish Land generates a discriminated union called `<MyProject>.Routes.Route` with all the routes in your project.

The file `src/Pages/Foo/_bar/_baz/Page.fs` will generate the route entry:

```fsharp
| Foo_bar_baz of bar: string * baz: string * query: list<string * string>`
```

You can refer to routes by using the relevant route entry:

```fsharp
Route.Foo_bar_baz ("bar", "baz", ["name", "John"])
```

will point to the URL: `#/Foo/bar/baz?name=John`

## Anchor element (\<a\>)

The `Route.format` function is used to get the URL for a specified route. 
```fsharp
Html.a [
    prop.text "Click me"
    prop.href (Route.Foo_bar_baz ("bar", "baz", ["name", "John"]) |> Routes.Route.format)
]
```

## Command

The `Elmish.Command.navigate` function creates a command that navigates to a specified route.

```fsharp
let init (shared: SharedModel) (bar: string, baz: string) (query: list<string * string>): Model * Command<Msg, SharedMsg> =
    (),
    Command.navigate(Route.Foo_bar_baz ("bar", "baz", ["name", "John"]))
```

Will automatically redirect the page to the `#/Foo/bar/baz?name=John` when loaded.

:::info

These examples uses dynamic routes. [Click here to read more](/docs/core-concepts/dynamic-routes)

:::
