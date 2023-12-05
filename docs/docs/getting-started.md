---
sidebar_position: 2
---

# Getting started

## Installation

### Requirements
* [.NET Core SDK](https://dotnet.microsoft.com/en-us/) version 7.0 or above (which can be checked by running dotnet --version).
* [Node.js](https://nodejs.org/en) version 18.0 or above (which can be checked by running node -v). You can use nvm for managing multiple Node versions on a single machine installed.
  - When installing Node.js, you are recommended to check all checkboxes related to dependencies.

## Create a new project
Elmish Land comes with a single dotnet tool to help you create new projects, add features, run your dev server, and more.

#### Create a new directory for your project and navigate to it

```bash
mkdir MyProject
cd MyProject
```

#### Install Elmish Land as a local dotnet tool

```bash
  dotnet new tool-manifest
  dotnet tool install elmish-land --prerelease
```

#### Initialize the project

```bash
  dotnet elmish-land init
```

## Project structure

Every Elmish Land application has a folder structure that looks something like this:

```
MyProject
├── package.json
├── global.json
├── MyProject.fsproj
├── index.html
└── src
    ├── Routes.fs
    ├── Shared.fs
    ├── Pages
    │   └── Home
    │       └── Page.fs
    └── App.fs
```

### Project structure rundown
Here's a breakdown of what those files and folders are:

* `/package.json` – Keeps track of your javascript package dependencies.
* `/global.json` - Specify which version of .NET to use.
* `/MyProject.fsproj` - The F# project file
* `/src/Routes.fs` - An auto generated file with the routes of your application.
* `/src/Shared.fs` - TODO
* `/src/Pages/` – The home for your page files, which correspond to URLs in your app.
* `/src/Layouts/` – Layouts allow you to nest pages within common UI.
* `/src/App.fs` – An auto generated file with the entry point of your application.

## Running the development server

The Elmish Land dotnet tool comes with a built in development server. Here's how to run your new project in the web browser:

```bash
dotnet elmish-land server
```

You should see "Home" when you open `http://localhost:5173`

## Build for production

Here's how to build your project for production. The resulting site will be generated within the `/TODO` directory.

```bash
dotnet elmish-land build
```

## Editor setup

We recommend using VS Code with the Ionide plugin for the best experience. If you prefer to use another editor, check out these [other editors](/docs/advanced/other-editors).

1. Install [Visual Studio Code](https://code.visualstudio.com/)
2. Install the [Ionide](https://ionide.io/Editors/Code/overview.html) extension
