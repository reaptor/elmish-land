---
sidebar_position: 6
---

import AddedIn from '@site/src/components/AddedIn';

# Route Config (route.json)

Route configuration files allow you to add type-safe path parameters and query parameters to your pages. Each `route.json` file defines how URL segments and query strings should be parsed and formatted.

## File Location

Place a `route.json` file in the same directory as your dynamic route or page folders:

```
src/Pages/
├── Users/
│   └── _Id/            # Dynamic route
│       └── route.json  # Route configuration with dynamic path parameter
│       └── Page.fs     # Page
└── Blog/
    ├── Page.fs         # Page 
    └── route.json      # Route configuration with query parameters
```

## Configuration Options

### pathParameter

Configures how the dynamic path parameter (e.g., `_Id`, `_Slug`) should be parsed and formatted.

#### Properties

- **`module`** (string, required): Fully qualified module name containing the type and functions
- **`type`** (string, required): Type name for the path parameter
- **`parse`** (string, optional): Function name to parse the URL path value to the type
- **`format`** (string, optional): Function name to format the type to a string used in the URL path

#### Example

```js
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "pathParameter": {
    "module": "System",
    "type": "Guid",
    "parse": "parseGuid",
    "format": "formatGuid"
  }
}
```

### queryParameters

Array of typed query parameters that can be read from the URL query string.

#### Properties

- **`name`** (string, required): Name as it appears in the URL (will be converted to PascalCase in generated code)
- **`module`** (string, required): Fully qualified module name containing the type and functions
- **`type`** (string, optional): Type name for the parameter (defaults to "string")
- **`parse`** (string, optional): Function name to parse string to the type
- **`format`** (string, optional): Function name to format the type to string
- **`required`** (boolean, optional): Whether the parameter is required (defaults to false)

#### Example

```js
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "queryParameters": [
    {
      "name": "page",
      "module": "System",
      "type": "int",
      "parse": "parseInt",
      "format": "formatInt"
    },
    {
      "name": "sort-by",
      "module": "System",
      "type": "string",
      "required": false
    }
  ]
}
```

:::info
Query parameter names are converted to PascalCase in the generated code. For example, `"sort-by"` becomes `SortBy` in your F# code.
:::

## Built-in Types

Elmish Land provides built-in parse and format functions for common types:

| Type | Parse Function | Format Function |
|------|----------------|-----------------|
| `Guid` | `parseGuid` | `formatGuid` |
| `int` | `parseInt` | `formatInt` |
| `int64` | `parseInt64` | `formatInt64` |
| `bool` | `parseBool` | `formatBool` |
| `float` | `parseFloat` | `formatFloat` |
| `decimal` | `parseDecimal` | `formatDecimal` |

## Custom Types

Define custom types for route and query parameters using shared project references. This enables domain-specific types with custom parsing and formatting logic.

#### Setup

Configure a project reference in `elmish-land.json`:

```js
{
  "projectReferences": [
    "src/Common/Common.fsproj"
  ]
}
```

#### Defining the Custom Types

Create types with `parse` and `format` functions in your shared library:

```fsharp
module MyProject.Common

open System

type Age = Age of int

module Age =
    let parse (value: string) =
        match Int32.TryParse value with
        | true, i when i >= 0 && i <= 150 -> Some (Age i)
        | _ -> None

    let format (Age age) = string age
```

Parse functions must have signature `string -> 'T option`. Format functions must have signature `'T -> string`.

## Examples

### One Path Parameter Only

For a page at `src/Pages/Users/_Id/Page.fs`:

```js
// src/Pages/Users/_Id/route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "pathParameter": {
    "module": "System",
    "type": "Guid",
    "parse": "parseGuid",
    "format": "formatGuid"
  }
}
```

This allows you to use `Guid` directly in your page:

```fsharp
type Model = { userId: Guid }

let init (routeParams: Route.Path) =
    // routeParams.Id is a Guid, not a string
    { userId = routeParams.Id }, Cmd.none
```

### Multiple Path Parameters

For a page at `src/Pages/Users/_Active/_Id/Page.fs`:

```js
// src/Pages/Vehicles/_Active/route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "pathParameter": {
    "module": "System",
    "type": "bool",
    "parse": "parseBool",
    "format": "formatBool"
  }
}
```

```js
// src/Pages/Vehicles/_Active/_Id/route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "pathParameter": {
    "module": "System",
    "type": "Guid",
    "parse": "parseGuid",
    "format": "formatGuid"
  }
}
```

This allows you to use `bool` and `Guid` directly in your page:

```fsharp
type Model = { 
  isActive: bool
  userId: Guid 
}

let init (routeParams: Route.Path) =
    // routeParams.Active is a bool, not a string
    // routeParams.Id is a Guid, not a string
    { 
      isActive = routeParams.active
      userId = routeParams.Id
    }, Cmd.none
```

### Query Parameters Only

For a page at `src/Pages/Search/Page.fs`:

```js
// src/Pages/Search/route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "queryParameters": [
    {
      "name": "page",
      "module": "System",
      "type": "int",
      "parse": "parseInt",
      "format": "formatInt"
    },
    {
      "name": "sort-by",
      "module": "System",
      "type": "string"
    }
  ]
}
```

Access query parameters in your page:

```fsharp
let init (routeParams: Route.Path) =
    let page = routeParams.Query.Page |> Option.defaultValue 1
    let sortBy = routeParams.Query.SortBy |> Option.defaultValue "name"
    // ...
```

### Combined Path and Query Parameters

For a page at `src/Pages/Posts/_Id/Page.fs`:

```js
// src/Pages/Posts/_Id/route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "pathParameter": {
    "module": "System",
    "type": "int",
    "parse": "parseInt",
    "format": "formatInt"
  },
  "queryParameters": [
    {
      "name": "age",
      "module": "System",
      "type": "int",
      "parse": "parseInt",
      "format": "formatInt",
      "required": false
    },
    {
      "name": "highlight",
      "module": "System",
      "type": "string"
    }
  ]
}
```

### Custom Types

For a page at `src/Pages/User/Page.fs`:


```js
// src/Pages/User/route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  "queryParameters": [
    {
      "module": "MyProject.Common",
      "name": "age",
      "type": "Age",
      "required": false,
      "parse": "Age.parse",
      "format": "Age.format"
    }
  ]
}
```

This allows you to use `Age` directly in your page:

```fsharp
type Model = { age: Age }

let init (routeParams: Route.Path) =
    // routeParams.Age is a type Age, not a string
    { age = routeParams.Age }, Cmd.none
```

## Related Documentation

- [Routing](/docs/core-concepts/routing) - Learn about Elmish.Land's routing system
- [Project Structure](/docs/getting-started/project-structure) - Understanding the project layout
- [Pages](/docs/core-concepts/pages) - Working with page modules
