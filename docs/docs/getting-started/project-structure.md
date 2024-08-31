---
sidebar_position: 3
---

# Project structure

Every Elmish Land application has a folder structure that looks something like this:

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

## Project files
Here's a breakdown of what those files and folders are:

* `/global.json` - Specify which version of .NET to use.
* `/MyProject.sln` - The F# solution file for working from an IDE
* `/MyProject.fsproj` - The F# project file
* `/elmish-land.json` –  The configuration file for Elmish Land.
* `/src/Shared.fs` - Shared data between your pages. 
* `/src/Pages/` – The home for your page and layout files.
  - `Layout.fs` – A layout file that allow you to share common UI for your pages.
  - `Page.fs` – A page file, which correspond to URLs in your app.
  - `route.json` – Configuration for the route of the folder. Makes it possible to specify type safe route parameters and query parameters.
