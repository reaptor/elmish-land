---
sidebar_position: 1
---

# Introduction

Elmish Land is a modern framework for building robust, maintainable web applications with [F#](https://fsharp.org), [Elmish](https://elmish.github.io/elmish/) and [React](https://reactjs.org/). Whether you're new to functional programming or a seasoned F# developer, Elmish Land provides the structure and tools to help you build applications that are a joy to create and maintain.

## Why Elmish Land?

- **Type-safe development** that catches errors at compile time
- **Clean architecture** that scales from small apps to complex systems
- **Productive developer experience** with hot reloading and intuitive CLI tools
- **Predictable state management** using the battle-tested Elm architecture
- **Fast performance** using React under the hood

## How It Works

Elmish Land follows the [Elm architecture](https://guide.elm-lang.org/architecture/) (TEA), a pattern that has proven its worth across different languages and frameworks:

- **Model**: Your application's state as immutable F# types
- **View**: A pure function that renders your model as a UI
- **Update**: A pure function that evolves your model in response to messages

This creates a unidirectional data flow that makes applications predictable and easy to reason about. When a user interacts with your UI, a message is dispatched, the update function computes the new state, and the view re-renders to reflect that state.

## What Elmish Land Provides

### Smart Project Structure

Your application code is organized into pages, layouts, and shared components, making it easy to navigate as your project grows. The framework gives you a clean separation of concerns with directories for pages and shared code.

```
my-app/
└── src/
    └── Pages/         # Each page has its own folder
        ├── Home/
        └── About/
````

### Automatic Routing

Elmish Land automatically connects your file structure to URL paths with type safety. Define your pages in the file system, and the framework generates all the routing code for you, including typed parameters for dynamic routes.

```fsharp
// src/Pages/Users/Id_.fs becomes /users/:id
let page (shared: SharedModel) (route: UsersIdRoute) =
    // route.id gives you the URL parameter with proper typing
    Page.from init update view () LayoutMsg
````

### Layouts and Shared UI

Structure your UI with reusable layouts while maintaining type safety between components. Layouts make it easy to share common UI elements across multiple pages without duplicating code.

### Modern Development Tools

The `elmish-land` CLI makes common tasks simple, from creating new projects to adding pages and running the development server. Everything works out of the box with [hot module reloading](https://vitejs.dev/guide/features.html#hot-module-replacement) for rapid development.

## Ready to Try It?

Getting started takes just minutes with the Elmish Land CLI. Install the tool, create your first project, and start the development server to see your application running in the browser.