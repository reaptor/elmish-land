---
sidebar_position: 3
---

# Project structure

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

You'll also find common files like .gitignore.

## Project files
Here's a breakdown of what those files and folders are:

* `/package.json` – Keeps track of your javascript package dependencies.
* `/global.json` - Specify which version of .NET to use.
* `/MyProject.fsproj` - The F# project file
* `/src/Routes.fs` - An auto generated file with the routes of your application.
* `/src/Shared.fs` - TODO
* `/src/Pages/` – The home for your page files, which correspond to URLs in your app.
* `/src/Layouts/` – Layouts allow you to nest pages within common UI.
* `/src/App.fs` – An auto generated file with the entry point of your application.
