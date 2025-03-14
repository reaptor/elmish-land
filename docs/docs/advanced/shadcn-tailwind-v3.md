---
sidebar_position: 3
---

# Using Tailwind v3 with shadcn/ui
## Introduction

At the moment of writing, using Tailwind CSS v4 breaks Vite's Hot Module Replacement (HMR).

This guide walks you through setting up Tailwind CSS v3 and shadcn/ui in an Elmish Land project,.

## Setting Up Tailwind CSS v3 in an Elmish Land Project

To use Tailwind CSS with Elmish Land, follow these steps:

### 1. Install Tailwind CSS v3

Install Tailwind CSS and its dependencies to your Elmish Land project 
(See [Creating a project](/docs/getting-started/creating-a-project) for information on how to create a new Elmish Land project), following the [official docs](https://v3.tailwindcss.com/docs/guides/vite):

```bash
npm install -D tailwindcss@3 postcss autoprefixer
npx tailwindcss init -p
```

### 2. Configure tailwind.config.js

Add the paths to all of your template files in your `tailwind.config.js` file:

```javascript
/** @type {import('tailwindcss').Config} */
export default {
    // highlight-start    
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
    // highlight-end
  theme: {
    extend: {},
  },
  plugins: [],
}
```

### 3. Add the Tailwind directives to your CSS

Create a file named `styles.css` in the root folder of your project and add Tailwind directives.

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

### 4. Add your CSS file to index.html

Add a link to your `styles.css` in the `<head>` section of your `index.html`.

```html
<!DOCTYPE html>
<html lang="en">
    <head>
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <meta http-equiv="X-UA-Compatible" content="IE=edge">
        <meta charset="UTF-8">
        // highlight-start
        <link href="/styles.css" rel="stylesheet">
        // highlight-end
        <title>TailwindElmishLand</title>
    </head>
    <body>
        <div id="app"></div>
        <script type="module" src=".elmish-land/App/App.fs.js"></script>
    </body>
</html>
```
This will setup Tailwind CSS v3. Follow the next step if you want to setup shadcn/ui.

## Using shadcn/ui with Tailwind CSS v3

Following the [official guide](https://ui.shadcn.com/docs/installation/vite) we need to use `shadcn@2.3.0`.

### 1. Configure import alias in tsconfig

Create a file named `tsconfig.json` in the root folder of your project and add the following:

```json
{
    "files": [],
    "compilerOptions": {
        "baseUrl": ".",
        "paths": {
            "@/*": [
                "./src/*"
            ]
        }
    }
}
```

### 2. Configure Vite

Add the `path` import and shadcn's component alias to your Vite configuration `vite.config.js`:

```javascript
import { defineConfig } from 'vite'
// highlight-start
import path from "path"
// highlight-end

export default defineConfig({
    build: {
        outDir: "dist"
    },
    // highlight-start
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
        },
    },
    // highlight-end
})
```


### 3. Install shadcn/ui

```bash
npx shadcn@2.3.0 init
```

You will be asked a few questions to configure components.json.

### 4. Add Feliz.Shadcn
```bash
dotnet add package Feliz.Shadcn
```

### 5. That's it

You can now start adding components to your project.

```bash
npx shadcn@2.3.0 add button
```

The command above will add the Button component to your project. You can then use it in your pages like this:

```fsharp
module FelizShadcnIntro.Pages.Page

open Feliz
open ElmishLand
open FelizShadcnIntro.Shared
open FelizShadcnIntro.Pages
// highlight-start
open Feliz.Shadcn
// highlight-end

// highlight-start
type Model = {
    Count: int
}
// highlight-end

type Msg =
    | LayoutMsg of Layout.Msg
// highlight-start
    | Increment
// highlight-end

let init () =    
// highlight-start    
    {
        Count = 0
    },
// highlight-end
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
// highlight-start    
    | Increment -> { model with Count = model.Count + 1}, Command.none
// highlight-end

let view (_model: Model) (_dispatch: Msg -> unit) =
    // highlight-start
    Html.div [
        Html.div $"Count: {_model.Count}"
        Shadcn.button [
            prop.text "Increment"
            prop.onClick (fun _ -> _dispatch Increment)
        ]
    ]
    // highlight-end

let page (_shared: SharedModel) (_route: HomeRoute) =
    Page.from init update view () LayoutMsg
```

Run:

```bash
dotnet elmish-land server
```

to start your application.

Increment the counter by clicking a few times on the button. Change the button text to e.g. `Increment (+1)` and save the file. Button's text should update,  `Count` should not reset to 0 (HMR works properly). 