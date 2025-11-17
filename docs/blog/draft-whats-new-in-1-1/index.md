---
slug: whats-new-in-1-1
title: What's New in v1.1
authors: [klofberg]
draft: true
---

# What's New in v1.1

Version 1.1 brings significant improvements to the developer experience, making Elmish Land more productive and easier to use. This guide highlights the major new features and improvements.

## Automatic Project File Management

The biggest improvement in v1.1 is automatic project file management. You no longer need to manually add pages and layouts to your `.fsproj` file.

The CLI automatically:
- Adds new pages and layouts to your `.fsproj`
- Maintains correct compilation order
- Preserves existing project file entries
- Updates page files when adding layouts to existing folders

## Solution File Generation

When you create a new project with `dotnet elmish-land init`, a `.sln` solution file is automatically generated. This improves the IDE experience in:

- Visual Studio
- JetBrains Rider
- Visual Studio Code with Ionide

No more manual solution file creation needed for better IDE integration.

## Interactive Error Resolution

v1.1 introduces helpful interactive prompts when errors occur during build or server commands.

### Layout Reference Validation

The CLI now validates that pages reference the correct layouts. If a mismatch is detected, you'll see a clear error message identifying the problem:

```
Error: Page 'src/Pages/Dashboard/Stats/Page.fs' uses 'Layout.Msg' but should use 'Dashboard.Layout.Msg'
```

An interactive prompt offers to fix the issue automatically:

```
Would you like to automatically fix this layout reference? [y/n]
```

Choose 'yes' and the CLI updates your page file with the correct layout reference.

### File Ordering Prompts

If pages need to be reordered in your project file for correct compilation order, you'll see:

```
The following files need to be reordered:
  • src/Pages/Users/Layout.fs (should come before src/Pages/Users/Settings/Page.fs)

Would you like to reorder these files automatically? [y/n]
```

The CLI shows a preview of changes before applying them.

## Improved Loading Indicators

Commands that take time to complete now show a loading indicator so you know the CLI is working:

```bash
dotnet elmish-land build
⣾ Building project...
```

Use the `--verbose` flag to see full build output when debugging:

```bash
dotnet elmish-land build --verbose
```

This shows all build logs and server output for troubleshooting.

## Configurable Render Method

You can now configure how React renders your application via `elmish-land.json`:

```js
{
  "program": {
    "renderMethod": "synchronous"
  }
}
```

Two options are available:

- **`synchronous`** (default) - Immediate renders after each update. Best for most apps, especially with controlled inputs.
- **`batched`** - Batches renders for smoother frame rates. May have issues with React controlled inputs.

See the [Configuration Reference](/docs/api-reference/app-config#programrendermethod) for details.

## Configurable Render Target

You can also customize which HTML element your app mounts to:

```js
{
  "program": {
    "renderTargetElementId": "root"
  }
}
```

The default is `"app"`, but you can change it to match your HTML structure.

See the [Configuration Reference](/docs/api-reference/app-config#programrendertargetelementid) for details.

## Configurable Routing Mode

Elmish Land now supports two routing modes, allowing you to choose how URLs are handled in your application.

### Hash Mode (default)

Traditional hash-based routing that works everywhere without server configuration:

```js
{
  "program": {
    "routeMode": "hash"
  }
}
```

URLs look like: `https://example.com/#/about`

This is the default mode and is fully backwards compatible with existing applications.

### Path Mode

Clean URLs without hash signs for a more modern feel:

```js
{
  "program": {
    "routeMode": "path"
  }
}
```

URLs look like: `https://example.com/about`

**Note:** Path mode requires server configuration to handle client-side routing (e.g., redirecting all routes to index.html).

### Interactive Setup

When running `dotnet elmish-land init`, you'll be prompted to choose your preferred routing mode:

```
Which routing mode would you like to use?
(1) Hash [default] - URLs with hash sign (example.com/#about)
(2) Path - Clean URLs without hash (example.com/about)
Enter choice [1]:
```

See the [Configuration Reference](/docs/api-reference/app-config#programroutemode) for details.

## Command.ofAsync and Command.tryOfAsync

v1.1 adds support for F# async workflows in the Command module, providing native F# alternatives to the promise-based commands.

These new functions work seamlessly with Fable.Remoting for type-safe server communication and any F# libraries that return `Async<'T>` types.

**Example:**

```fsharp
let update msg model =
    match msg with
    | LoadWeather ->
        { model with Loading = true },
        Command.tryOfAsync
            weatherApi.getWeatherForecast
            ()
            WeatherLoaded
            LoadError
```

For detailed examples and guidance on when to use async vs promise commands, see the [Command Module Reference](/docs/api-reference/command-module#commandofasync).

## Bug Fixes

### Commands from Shared Module

Fixed an issue where commands dispatched from the Shared module weren't working properly. Now `Command.ofShared` works reliably across all scenarios.

### Server Command Stability

The `dotnet elmish-land server` command no longer hangs in certain situations, providing a more stable development experience.

### F# Keywords in Routes

Route parameters that use F# keywords (like "new", "type", "module") now work correctly:

```
src/Pages/Products/_New/Page.fs  # Works in v1.1+
```

Previously this would cause compilation errors.

### Project File Management

- Mixed path separators (forward and backslash) are now handled correctly
- Content entries in project files are preserved during updates
- File ordering maintains dependencies correctly

## Migration from v1.0.x

Upgrading to v1.1 is straightforward:

1. Update the package:
   ```bash
   dotnet tool update elmish-land
   ```

2. Your existing projects continue to work as-is

3. Future page and layout additions will be automatic

4. (Optional) Add new configuration options to `elmish-land.json` if desired

No breaking changes - all v1.0.x projects are fully compatible with v1.1.

## What's Next?

Stay tuned for future updates by:

- Following the [GitHub repository](https://github.com/reaptor/elmish-land)
- Joining the [Discord community](https://discord.gg/jQ26cZH3fU)
- Watching the [CHANGELOG](https://github.com/reaptor/elmish-land/blob/main/CHANGELOG.md)

Have feedback or feature requests? Open an issue on GitHub!
