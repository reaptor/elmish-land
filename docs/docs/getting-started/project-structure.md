---
sidebar_position: 3
---
import AddedIn from '@site/src/components/AddedIn';

# Project structure

​Understanding the structure of an Elmish Land project is essential for efficient development and maintenance. A typical Elmish Land application is organized as follows:

```
MyProject
├── global.json
├── MyProject.fsproj
├── elmish-land.json
├── MyProject.sln 
└── src
    ├── Shared.fs
    └── Pages
        ├── Layout.fs
        ├── Page.fs
        └── route.json
```

You'll also find common files like .gitignore.

### Overview of files and directories

#### Root Level Files

* **`/global.json`** - Specifies the .NET SDK version to ensure consistency across development environments.
* **`/MyProject.sln`** - The solution file for managing your project in IDEs like Visual Studio and Rider. Automatically generated during project initialization. <AddedIn version="1.1.0-beta.1" />
* **`/MyProject.fsproj`** - The F# project file containing build configurations and dependencies. Since version 1.1, Elmish Land automatically manages file entries in this file when you add pages or layouts.
* **`/elmish-land.json`** - Configuration file for Elmish Land-specific settings. See the [App Config Reference](/docs/api-reference/app-config) for all available options.

#### Source Directory (`/src/`)

* **`/src/Shared.fs`** - Contains shared state and logic accessible across all pages. This is where you define your `SharedModel` and `SharedMsg` types, along with their `init` and `update` functions. See [Shared state](/docs/core-concepts/shared) for more information.

* **`/src/Pages/`** - Directory containing all your pages and layouts. The folder structure here determines your application's URL routing. See [Pages](/docs/core-concepts/pages) and [Page Module](/docs/api-reference/page-module) for more information.

  - **`NotFound.fs`** - Special page displayed when a route doesn't match any defined pages. See [Not Found Page](/docs/core-concepts/not-found) for details.

  - **`Layout.fs`** - The root layout that wraps all pages by default. Layouts can contain their own state and are preserved during navigation. See [Layouts](/docs/core-concepts/layouts) and [Layout Module](/docs/api-reference/layout-module) for more information.

  - **`Page.fs`** - The home page of your application, served at the root URL (`/#`). See [Pages](/docs/core-concepts/pages) and [Page Module](/docs/api-reference/page-module) for more information.

  - **`route.json`** - Optional configuration file for defining typed route and query parameters. Place this file in any page folder to add type safety to URL parameters. See [Route Config](/docs/api-reference/route-config) for details.

#### Generated Directory (`/.elmish-land/`)

Elmish Land automatically generates code in this directory. You should:
- **Never edit** files in this directory manually
- **Add to `.gitignore`** to avoid committing generated code

This directory contains:
- Generated routing code
- Type definitions for routes
- Application bootstrap code

#### Build Outputs

* **`/dist/`** - Production build output created by `dotnet elmish-land build`. Contains optimized static files ready for deployment.
* **`/node_modules/`** - NPM dependencies required for building and running your application. Managed by npm/yarn. Should be added to **`.gitignore`**.
