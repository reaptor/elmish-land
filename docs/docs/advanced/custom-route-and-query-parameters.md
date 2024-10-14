---
sidebar_position: 1
---
# Custom route and query parameters

You can use your own types for route and query parameters by creating a shared dotnet library.
To get this working, first you need to add a project reference to the shared project in your `/elmish-land.json` configuration file for your project.

```javascript
// elmish-land.json
{
    "projectReferences": [
        "src/Common/Common.fsproj"
    ]
}
```

then you need to add your types and parser and formatter functions to the library:

```fsharp
// src/Common/RouteParams.fs
module MyProject.Common.RouteParams

open System

type Age = Age of int

module Age =
    let parse (value: string) =
        match Int32.Parse value with
        | true, i -> Some (Age i)
        | _ -> None

    let format (Age age) = string age
```

lastly you need to add it to the page's `route.json`:

```javascript
{
    "queryParameters": [
        {
            "module": "MyProject.Common.RouteParams",
            "name": "age",
            "type": "Age",
            "required": true,
            "parse": "Age.parse",
            "format": "Age.format"
        }
    ]
}
```
