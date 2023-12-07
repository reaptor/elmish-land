---
sidebar_position: 2
---

# Creating a project

## Requirements
* [.NET SDK](https://dotnet.microsoft.com/en-us/) version 7.0 or above (which can be checked by running dotnet --version).
* [Node.js](https://nodejs.org/en) version 18.0 or above (which can be checked by running node -v). You can use nvm for managing multiple Node versions on a single machine installed.
  - When installing Node.js, you are recommended to check all checkboxes related to dependencies.

## Create a new project
Elmish Land comes with a single dotnet tool to help you create new projects, add features, run your dev server, and more.

```bash
mkdir MyProject
cd MyProject
dotnet new tool-manifest
dotnet tool install elmish-land --prerelease
dotnet elmish-land init
dotnet elmish-land server
```

`elmish-land init` will scaffold a new project in the MyProject directory and `elmish-land server` will start the development server on `http://localhost:5173`.

You create pages by adding files to the src/Pages directory of your project. Try editing the files to get a feel for how everything works.

Be sure to join the [Elmish Land Discord](https://discord.gg/jQ26cZH3fU) to get help if you're stuck or to make new friends. We hope you have an awesome experience with Elmish Land, and can't wait to see what you build!

## Editor setup

We recommend using VS Code with the Ionide plugin for the best experience. If you prefer to use another editor, check out these [other editors](/docs/advanced/other-editors).

1. Install [Visual Studio Code](https://code.visualstudio.com/)
2. Install the [Ionide](https://ionide.io/Editors/Code/overview.html) extension
