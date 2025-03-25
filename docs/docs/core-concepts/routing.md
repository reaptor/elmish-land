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
In addition to URL path parameters, Elmish Land supports query parameters through a `route.json` file. This allows pages to define optional or required query string parameters.

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
Elmish Land enforces **type-safe route parameters** using `route.json`. This ensures that path parameters and query strings conform to expected data types.

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

### Included Types for Route and Query Parameters

The following parameter types for `pathParameter` and `queryParameters` in `route.json` can be used out of the box:

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

💡 **Note:** In the [Custom route and query parameters page](/docs/advanced/custom-route-and-query-parameters), you'll learn more about how to use your own types as route and query parameters.
