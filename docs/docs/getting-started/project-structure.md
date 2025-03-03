---
sidebar_position: 3
---

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

* `/global.json` - Specifies the .NET SDK version to ensure consistency across development environments.
* `/MyProject.sln` - The solution file facilitating project management within Integrated Development Environments (IDEs).
* `/MyProject.fsproj` - The F# project file containing build configurations and dependencies.
* `/elmish-land.json` –  Holds Elmish Land-specific configurations, allowing customization of framework behaviors.
* `/src/Shared.fs` - Contains shared data and utilities accessible across multiple pages.
* `/src/Pages/` – Directory dedicated to page and layout files.
  - `Layout.fs` – Defines common UI elements and structures shared among various pages, ensuring a consistent user experience.
  - `Page.fs` – Represents individual pages, each corresponding to specific URLs within the application.​
  - `route.json` – Configures routing parameters, enabling type-safe route and query parameter definitions.

This structured approach ensures that Elmish Land projects are organized, maintainable, and scalable, facilitating a seamless development process.