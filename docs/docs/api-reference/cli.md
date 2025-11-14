---
sidebar_position: 10
---

# CLI

## Overview

The Elmish Land CLI tool is available [as a .NET tool](https://www.nuget.org/packages/elmish-land/). This one command-line tool has everything you need to create new projects, run your development server, and even build your application for production.

After installing, you can run `dotnet elmish-land --help` to see these commands at any time. This page is a more detailed breakdown of the documentation you'll see in your terminal.

## Init

```bash
dotnet elmish-land init --project-dir <folder-name> --verbose
```

#### Description
This command creates a new Elmish Land project or initializes an existing Fable project to use Elmish Land.

See [Project structure](/docs/getting-started/project-structure) for more information on what files that will be created.

#### Arguments
`--project-dir <folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

## Server

```bash
dotnet elmish-land server --project-dir <folder-name> --verbose
```

#### Description
This command starts a development server (powered by [Vite](https://vitejs.dev)) at http://localhost:5173. If port 5173 is already taken, the server will automatically find the next available port.

#### Arguments
`--project-dir <folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

## Build

```bash
dotnet elmish-land build --project-dir <folder-name> --verbose
```

#### Description
This command builds your Elmish Land app in production-mode. The result is a static site that is ready to be hosted from the ./dist folder.

#### Arguments
`--project-dir <folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

## Add page

```bash
dotnet elmish-land add page <page> --project-dir <folder-name> --verbose
```

#### Description
This scaffolding command generates a new Elmish Land page and automatically adds it to your project file.

#### Arguments
`<page>` – the folder path for the page.

`--project-dir <folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

Here are some examples:
```bash
dotnet elmish-land add page "/SignIn"      # Creates "src/Pages/SignIn/Page.fs"
dotnet elmish-land add page "/Users/_Id"   # Creates "src/Pages/Users/_Id/Page.fs"
```

The page file will be automatically added to your `.fsproj` file in the correct compilation order.

## Add layout

```bash
dotnet elmish-land add layout <layout> --project-dir <folder-name> --verbose
```

#### Description
This scaffolding command generates a new Elmish Land layout and automatically adds it to your project file. If you're adding a layout to a folder that already has pages, those pages will be automatically updated to reference the new layout.

#### Arguments
`<layout>` – the folder path for the layout.

`--project-dir <folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

Here are some examples:
```bash
dotnet elmish-land add layout "/SignIn"      # Creates "src/Pages/SignIn/Layout.fs"
dotnet elmish-land add layout "/Users/_Id"   # Creates "src/Pages/Users/_Id/Layout.fs"
```

The layout file will be automatically added to your `.fsproj` file in the correct compilation order.
