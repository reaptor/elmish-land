---
sidebar_position: 10
---

# CLI commands

## Overview ​

The Elmish Land CLI tool is available [as a .NET tool](https://www.nuget.org/packages/elmish-land/). This one command-line tool has everything you need to create new projects, run your development server, and even build your application for production.

After installing, you can run `donet elmish-land --help` to see these commands at any time. This page is a more detailed breakdown of the documentation you'll see in your terminal.

## Init ​

```bash
dotnet elmish-land init <folder-name> --verbose
```

#### Description ​
This command creates a new Elmish Land project or initializes an existing Fable project to use Elmish Land.

See [Project structure](/docs/getting-started/project-structure) for more information on what files that will be created.

#### Arguments ​
`<folder-name>` – Optional name of the folder for the new or existing Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

## Server ​

```bash
dotnet elmish-land server <folder-name> --verbose
```

#### Description ​
This command starts a development server (powered by [Vite](https://vitejs.dev)) at http://localhost:5173. If port 5173 is already taken, the server will automatically find the next available port.

#### Arguments ​
`<folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

## Build ​

```bash
dotnet elmish-land build <folder-name> --verbose
```

#### Description ​
This command builds your Elmish Land app in production-mode. The result is a static site that is ready to be hosted from the ./dist folder.

#### Arguments ​
`<folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

## Add page ​

```bash
dotnet elmish-land add page <url> --project-dir <folder-name> --verbose
```

#### Description ​
This scaffolding command generates a new Elmish Land page in your src/Pages folder.

#### Arguments ​
`<url>` – the URL you want this page to be available on.

`--project-dir <folder-name>` – Optional name of the folder for your Elmish Land project.

`--verbose` - Optional argument to display more output for the command.

Here are some examples:
```bash
dotnet elmish-land add page /SignIn ........ Creates "src/Pages/SignIn/Page.fs"
dotnet elmish-land add page /Users/{id} .... Creates "src/Pages/Users/{id}/Page.fs"
```

:::warning

You need to manually add the new page to your project file.

:::
