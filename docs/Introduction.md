---
title: Introduction
category: Documentation
categoryindex: 1
index: 2
---
# Introduction

## What is Elmish Land?

In the JavaScript ecosystem, the idea of an "application framework" is pretty common. In the React community, one popular framework is called Next.js. In the Vue.js community, you'll find a similar framework called Nuxt.

These frameworks help take care of the common questions you might encounter when getting started with a new project. They also include helpful guides and learning resources throughout your personal journey.

Elmish Land is no different! But instead of building apps in Javascript, you'll be using something different: F# and Elmish!

## Project structure

Every Elmish Land application has a folder structure that looks something like this:

```bash
MyProject/
    |- package.json
    |- global.json
    |- MyProject.fsproj
    |- index.html
    |- src/
        |- Routes.fs
        |- Shared.fs 
        |- Pages/
            |- Home
                |- Page.fs
        |- App.fs
```

Here's a breakdown of what those files and folders are:

1. package.json – Keeps track of your javascript package dependencies.
2. global.json - Specify which version of .NET to use.
3. MyProject.fsproj - The F# project file
4. src/Routes.fs - An auto generated file with the routes of your application.
5. src/Shared.fs - ---
6. src/Pages – The home for your page files, which correspond to URLs in your app.
7. src/Layouts – Layouts allow you to nest pages within common UI.
8. src/App.fs – An auto generated file with the entry point of your application.

## Pages

Pages are the basic building blocks of your app. In a typical Elmish application, you have to manually manage your routing.

Elmish Land has a file-based routing convention that automatically generates that code for you. So if you want a new page at /hello, you can create a new file at src/Pages/Hello/Page.fs. Elmish Land handles the rest!

As you add more features to your app, you pages folder will grow to match all the URLs you care about. After a while, it might look something like this:

```bash
src/
└── Pages/
    ├── Home
    │   └── Page.fs
    ├── Settings
    │   └── Page.fs
    └── People/
        └── Page.elm
```

In the Pages and routes section, you'll learn more about the naming conventions for files.

## Layouts

TODO
