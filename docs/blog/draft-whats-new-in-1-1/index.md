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

### Before (v1.0.x)

```bash
# Create a new page
dotnet elmish-land add page "/Users/_Id"

# Manually edit MyProject.fsproj to add:
# <Compile Include="src/Pages/Users/_Id/Page.fs" />
```

### Now (v1.1+)

```bash
# Create a new page - that's it!
dotnet elmish-land add page "/Users/_Id"
# The page is automatically added to your project file
```

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

New in beta 6, you can now configure how React renders your application via `elmish-land.json`:

```json
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

Also new in beta 6, customize which HTML element your app mounts to:

```json
{
  "program": {
    "renderTargetElementId": "root"
  }
}
```

The default is `"app"`, but you can change it to match your HTML structure.

See the [Configuration Reference](/docs/api-reference/app-config#programrendertargetelementid) for details.

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
