---
sidebar_position: 1
---

# Routing

Elmish Land employs a file-system-based routing approach, where the structure of your `src/Pages` directory determines the URL paths for your application. This intuitive setup eliminates the need for complex manual route configurations, making navigation management effortless.

## Defining Routes with Folders

Each folder within `src/Pages` represents a route segment, which maps directly to a corresponding URL segment. To create nested routes, simply nest folders inside one another.

#### Examples:

* **Root route** → `src/Pages` → `/#`
* **Static route** → `src/Pages/About` → `/#about`
* **Dynamic route with a parameter** → `src/Pages/Blog/_slug` → `/#blog/:slug`
  *(Allows loading dynamic content for URLs like `/#blog/hello-world`)*

Each page folder that contains a `Page.fs` file will be served on that specific URL.

#### Example Project Structure:

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

💡 **Note:** The Utils folder inside `Blog/` is not a routable page since it lacks a `Page.fs` file. Instead, this folder can store shared utilities, stylesheets, images, or other assets.


## Types of Routes in Elmish Land

Elmish Land supports multiple routing types, ordered below from most to least specific:

| Route Type       | Example URL      | Description                                     |
|------------------|------------------|-------------------------------------------------|
| Root page        | `/#`              | Handles requests to the top-level URL.          |
| Simple routes    | `/#people`        | Maps a URL directly to a page.                  |
| Route parameters | `/#people/:id`    | Dynamically maps multiple URLs to a page.       |
| Query parameters | `/#people?id=:id` | Passes arguments to a page via query strings.   |

:::info

At the moment Elmish Land only support routes with a hash sign (#) eg `/#about`.

:::

## Root page

The root page (`/#`) is created automatically when running:

```bash
dotnet elmish-land init
```

#### Example:

| Page File    	         | URL  |
|------------------------|------|
| `src/Pages/Page.fs`    | `/#`  |


## Simple Routes (Static Routing)

"Static routes" directly map a folder to a URL path. Elmish Land also automatically **adds dashes between capitalized words** in filenames.

#### Examples:

| Page File                                   | URL                        |
|---------------------------------------------|----------------------------|
| `src/Pages/Hello/Page.fs`                   | `/#hello`                   |
| `src/Pages/AboutUs/Page.fs`                 | `/#about-us`                |
| `src/Pages/Settings/Account/Page.fs`        | `/#settings/account`        |
| `src/Pages/Settings/General/Page.fs`        | `/#settings/general`        |
| `src/Pages/Something/Really/Nested/Page.fs` | `/#something/really/nested` |

## Route Parameters (Dynamic Routing)

Folders prefixed with **an underscore** (`_`) define **dynamic route parameters**. These allow the creation of flexible URLs that accept user-specific data.

#### Examples:

| Page filename                       | URL                 | Example URLs                               |
|-------------------------------------|---------------------|--------------------------------------------|
| `src/Pages/Blog/_Id/Page.fs`        | `/#blog/:id`         | `/#blog/1`, `/#blog/xyz`                     |
| `src/Pages/Users/_Username/Page.fs` | `/#users/:username`  | `/#users/ryan`, `/#users/bob`                |
| `src/Pages/Settings/_Tab/Page.fs`   | `/#settings/:tab`    | `/#settings/account`, `/#settings/general`   |

### Accessing Route Parameters

The name of the folder (`_Id`, `_User` or `_Tab`) will determine the names of the fields available on the Route value passed into your page function:

```fsharp
// /blog/123
route.Id = "123"

// /users/ryan
route.User = "ryan"

// /settings/account
route.Tab = "account"
```

If you rename `Settings/_Tab/Page.fs` to `Settings/_Foo/Page.fs`, you will access the route parameter using `route.Foo` instead.


💡 **Did you know?**

Dynamic routing is a common feature in modern frameworks like:

* **Next.js** → Uses `[id].js` for dynamic parameters.
* **Nuxt.js** → Uses `_id.vue` for dynamic segments.


## Query parameters
In addition to URL path parameters, Elmish Land supports query parameters through a [`route.json`](/docs/api-reference/route-config) file. This allows pages to define optional or required query string parameters.

#### Example:

```json
{
  "queryParameters": [
    {
      "module": "System",
      "name": "name",
      "required": true
    },
    {
      "module": "System",
      "name": "age"
      // Optional parameter
    }
  ]
}
```

🔗 **Example URL**: `/#user?name=john&age=23`

*(Allows passing parameters dynamically, like filtering a user list or handling search queries.)*


## Type-Safe Routing
Elmish Land enforces **type-safe route parameters** using `route.json` (see the [Route Configuration API Reference](/docs/api-reference/route-config) for more information). This ensures that path parameters and query strings conform to expected data types.

#### Example:
A `route.json` file inside `/Pages/User/_UserId/`:

```json
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

This generates the following **strongly typed** route structure:

```fsharp
module Routes =
    type User_UserIdRoute = { UserId: Guid; Age: int  }
```

Now, only **valid GUIDs** will be accepted as `UserId`, and `age` is strictly an integer. This prevents runtime errors and ensures safe navigation across your application.

### Built-in Types

Elmish Land includes built-in support for common route and query parameter types like `Guid` and `int`. For a complete reference of all built-in types and their configuration, see the [Built-in Types](/docs/api-reference/route-config#built-in-types) section in the [Route Configuration API Reference](/docs/api-reference/route-config).

### Custom Types

You can also define custom route and query parameter types for route and query parameters. See the [Custom Types](/docs/api-reference/route-config#custom-types) section in the [Route Configuration API Reference](/docs/api-reference/route-config) for details on using custom types.

## API References

For more detailed information about routing functionality:

- **[Route Module API Reference](/docs/api-reference/route-module)** - Functions like `Route.format` and route comparison utilities
- **[Route Configuration Reference](/docs/api-reference/route-config)** - Complete `route.json` configuration options
