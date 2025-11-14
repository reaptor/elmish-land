---
sidebar_position: 7
---

# Not Found Page

When a user navigates to a URL that doesn't match any of your defined routes, Elmish Land displays a special Not Found page. This page is automatically created when you initialize a new project.

## Default Not Found Page

The Not Found page is located at `src/Pages/NotFound.fs` and has a simpler structure than regular pages:

```fsharp
module MyProject.Pages.NotFound

open Feliz

let view () = Html.text "Page not found"
```

Unlike regular pages, the Not Found page:
- Does not have a Model or Msg type
- Does not have init or update functions
- Only requires a simple view function that takes no parameters
- Cannot use layouts

## Customizing the Not Found Page

You can customize the Not Found page to provide a better user experience. Here's an example with more styling and helpful navigation:

```fsharp
module MyProject.Pages.NotFound

open Feliz
open ElmishLand

let view () =
    Html.div [
        prop.className "not-found-container"
        prop.children [
            Html.h1 "404 - Page Not Found"
            Html.p "The page you're looking for doesn't exist."
            Html.a [
                prop.text "Go to Home"
                prop.href (Route.format (Route.Home ()))
            ]
        ]
    ]

```

## When is the Not Found Page Shown?

The Not Found page is displayed in the following scenarios:

1. **Invalid URL path** - When the URL doesn't match any page in your `src/Pages` directory
2. **Type mismatch** - When route parameters don't match the expected type (e.g., expecting a GUID but receiving "abc")
3. **Missing required query parameters** - When required query parameters defined in `route.json` are absent
