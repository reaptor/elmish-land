---
sidebar_position: 2
---

# Pages

## Pages

A page is UI that is unique to a route. You can define pages by running the CLI command `elmish-land add page`.

To get started, let's create a page that is displayed when a user visits the URL `/about`.

```bash
dotnet elmish-land add page "/about"
```

```bash
    Elmish Land added a new page at /about
    --------------------------------------

    You can edit your new page here:
    ./src/Pages/About/Page.fs

    Please add the file to the project using an IDE or add the following line to an
    ItemGroup in the project file './MyProject.fsproj':
    <Compile Include="src/Pages/About/Page.fs" />
```

:::warning

You need to manually add the new page to your project file.

:::

### Query parameters

Every page's ``init`` function has a ``(query: list<string * string>)`` parameter. This parameter contains the query parameters for the current URL. The following url:

`/Blog?username=john`

will yield the following value for the `query` parameter:

```fsharp
[ "username", "john" ]
```

You can get a query parameter by using `List.tryFind`:

```fsharp
let username = query |> List.tryFind (fun x -> x = "username")
```
