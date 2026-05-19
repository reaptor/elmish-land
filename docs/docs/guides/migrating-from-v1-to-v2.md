---
sidebar_position: 100
title: Migrating from 1.x to 2.0
---

# Migrating from 1.x to 2.0

Elmish Land 2.0 moves the framework onto [Fable 5](https://fable.io) and [Feliz 3](https://fable-hub.github.io/Feliz/). The runtime gets a few breaking changes — most are mechanical renames, but four areas (`React.memo`, `React.lazy'`, the React context API, and any hand-written `Interop.reactApi.createElement` bindings) need a small restructuring you have to do by hand.

The `dotnet elmish-land upgrade` command does the mechanical work for you and prints a checklist of the manual edits, with a deep link into the Feliz upgrade docs for each one.

:::warning Upgrade to .NET 10 before upgrading Elmish Land

Elmish Land 2.0 requires the **.NET 10 SDK**. Move your existing project onto .NET 10 *first*, and only then run `dotnet elmish-land upgrade`. If you skip ahead and run `dotnet tool update elmish-land --prerelease` while .NET 10 isn't the active SDK (typically because your project's `global.json` pins an older version), the update fails with:

```
Unhandled exception: Settings file 'DotnetToolSettings.xml' was not found in the package.
```

Before running the commands below:

1. Install the .NET 10 SDK from [dot.net/download](https://dot.net/download).
2. Update your project's `global.json` so the `sdk.version` is a `10.x` value you have installed — for example:

   ```json
   {
     "sdk": {
       "version": "10.0.100",
       "rollForward": "latestFeature"
     }
   }
   ```

   (If your project has no `global.json`, you can skip this step — `dotnet` will pick up the newest installed SDK on its own.) `dotnet elmish-land upgrade` does not touch `global.json`; flip the SDK version yourself before running the upgrade.
3. Bump the `<TargetFramework>` in every `.fsproj` file in your project from `net9.0` (or older) to `net10.0`:

   ```xml
   <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
   </PropertyGroup>
   ```

   `dotnet elmish-land upgrade` does not edit your project files' target framework — it only regenerates the `.elmish-land/` projects. You need to flip this in your own `.fsproj` files by hand.
4. Verify your project still compiles on .NET 10 by running `dotnet build` from the project root. Fix any SDK-related errors before continuing, so you have a clean baseline before introducing the elmish-land upgrade on top.

Note: once the active SDK is .NET 10, the elmish-land 1.1 tool can no longer run `dotnet elmish-land build` or `dotnet elmish-land server` — those commands were built against the older Fable and SDK combination. Plan to run the steps above and then immediately continue with `dotnet tool update elmish-land --prerelease` and `dotnet elmish-land upgrade`; don't stop in the middle expecting the 1.1 commands to keep working.

:::

## TL;DR

```bash
# 1. Update the elmish-land tool to 2.x
dotnet tool update elmish-land --prerelease

# 2. From inside your project directory, run the upgrade command
dotnet elmish-land upgrade
```

Then read the manual-migration list the upgrade command printed, and apply each fix using the linked Feliz docs as a reference.

## What `upgrade` changes for you

### Dependency versions

The command rewrites these files, if they exist, to match what 2.0 ships:

- **`Directory.Packages.props`** — re-pins the `<PackageVersion>` for every package elmish-land manages (notably `Feliz` to 3.x and `Fable.Core` to 5.x). The `Feliz.Router` entry is also removed: 2.0 vendors a small Router at `.elmish-land/Base/Router.fs` instead of taking the NuGet package as a runtime dependency.
- **`package.json`** — bumps `react` and `react-dom` to **19.x** and `vite` to **8.x**.
- **`.config/dotnet-tools.json`** — bumps the `fable` tool to 5.x (prerelease).
- **User `.fsproj` files** — strips any leftover `<PackageReference Include="Feliz.Router" />` entries. The default v1 scaffold kept this reference inside `.elmish-land/Base/...fsproj`, which is regenerated anyway; this cleanup catches projects that copied the reference up into a hand-edited project file.

If a file isn't found, the command prints an instruction telling you what to do manually instead.

### Source-level renames

For every `.fs` file under `src/` (and outside the auto-generated and tooling directories), these whole-identifier renames are applied:

| Feliz 2                          | Feliz 3                            |
| -------------------------------- | ---------------------------------- |
| `React.fragment`                 | `React.Fragment`                   |
| `React.keyedFragment`            | `React.KeyedFragment`              |
| `React.imported`                 | `React.Imported`                   |
| `React.dynamicImported`          | `React.DynamicImported`            |
| `React.strictMode`               | `React.StrictMode`                 |
| `React.suspense`                 | `React.Suspense`                   |
| `React.createDisposable`         | `FsReact.createDisposable`         |
| `React.useDisposable`            | `FsReact.useDisposable`            |
| `React.useCancellationToken`     | `FsReact.useCancellationToken`    |

The matcher uses identifier boundaries on both sides, so it won't touch:

- a longer name that starts with the old one (e.g. `React.fragmentExtra`)
- code that's already been upgraded (re-running `upgrade` is a no-op)

## What you have to migrate by hand

These three call sites can't be safely auto-rewritten because Feliz 3 splits one expression into a *definition* and a *renderer*. The upgrade command finds each occurrence and prints something like:

```text
Manual migration required:
  • src/Pages/Home.fs:42 — React.memo now requires explicit React.memoRenderer call sites at usage points
      see https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactmemo
```

Below is a quick reference for each pattern so you don't have to context-switch to the Feliz docs unless you want the full explanation.

### `React.memo` → `React.memoRenderer`

The wrapper no longer renders itself. Define the memoized function once, then call `React.memoRenderer` at each use site.

**Before (Feliz 2):**

```fsharp
let MemoFunction = React.memo<{| text: string |}> (fun props ->
    Html.div [ prop.text props.text ])

[<ReactComponent>]
let Main () =
    Html.div [ MemoFunction {| text = "hi" |} ]
```

**After (Feliz 3):**

```fsharp
let MemoFunction = React.memo<{| text: string |}> (fun props ->
    Html.div [ prop.text props.text ])

[<ReactComponent>]
let Main () =
    Html.div [ React.memoRenderer (MemoFunction, {| text = "hi" |}) ]
```

Full details: [Feliz upgrade — `React.memo`](https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactmemo).

### `React.lazy'` → `React.lazyRender`

Same shape: the lazy definition stays, the call site uses `React.lazyRender`. In typical use it lives inside a `React.Suspense`.

**Before (Feliz 2):**

```fsharp
let LazyHello = React.lazy' (fun () -> JsInterop.importDynamic "./Hello")

[<ReactComponent>]
let SuspenseDemo () =
    React.Suspense (
        [ LazyHello () ],
        Html.div [ prop.text "Loading..." ]
    )
```

**After (Feliz 3):**

```fsharp
let LazyHello : LazyComponent<unit> =
    React.lazy' (fun () -> JsInterop.importDynamic "./Hello")

[<ReactComponent>]
let SuspenseDemo () =
    React.Suspense (
        [ React.lazyRender (LazyHello, ()) ],
        Html.div [ prop.text "Loading..." ]
    )
```

Full details: [Feliz upgrade — `React.lazy'`](https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactlazy).

### `React.context` redesign

`React.contextProvider` and `React.contextConsumer` are gone. The provider/consumer now live as members on the context value itself, much closer to how the React JS API reads.

**Before (Feliz 2):**

```fsharp
let CounterContext = React.createContext (None: (int * (int -> unit)) option)

[<ReactComponent>]
let UseContext () =
    let count, setCount = React.useState 0
    React.contextProvider (CounterContext, Some (count, setCount), CounterDisplay ())
```

**After (Feliz 3):**

```fsharp
let CounterContext = React.createContext (None: (int * (int -> unit)) option)

[<ReactComponent>]
let CounterDisplay () =
    let ctx = React.useContext CounterContext
    match ctx with
    | Some (count, _) -> Html.p [ prop.text $"Current count: {count}" ]
    | None            -> Html.p [ prop.text "No context available" ]

[<ReactComponent(true)>]
let UseContext () =
    let count, setCount = React.useState 0
    CounterContext.Provider ((Some (count, setCount)), CounterDisplay ())
```

Full details: [Feliz upgrade — `React.context`](https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactcontext).

### Hand-written React bindings

If your project wraps a JavaScript React component by hand (a common pattern when binding to libraries like Radix or Floating UI), the call into Feliz has moved. See the [Feliz writing-bindings guide](https://fable-hub.github.io/Feliz/api-docs/guides/writing-bindings) for the rewrite (in short: `Interop.reactApi.createElement` becomes `ReactLegacy.createElement` and the component argument needs an `unbox<ReactElement>` cast).

## After the upgrade

```bash
dotnet tool restore        # picks up Fable 5
npm install                # installs React 19 / vite 8
dotnet elmish-land restore # regenerates the .elmish-land/ framework files
dotnet elmish-land build   # sanity-check that everything compiles
```

If `build` fails on something other than the patterns above, that's almost always a third-party package that hasn't been released against Feliz 3 yet — check its release notes.

## Tips

- **Commit before running `upgrade`.** It's the easiest way to review exactly what changed.
- **Re-run `upgrade` after manually editing.** It's a no-op on already-upgraded code, and any new manual-migration warnings will surface again.
- **Pass `-y` for non-interactive runs** (e.g. CI scripts that just want to bump versions): `dotnet elmish-land upgrade -y`.
