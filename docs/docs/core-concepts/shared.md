---
sidebar_position: 5
---

# Shared state

## Overview 
The Shared module is designed to enable us to share state between pages. To understand why it exists, it might first be helpful to understand how Elmish Land stores information. Under the hood, Elmish Land generates a bit of code for making a standard F#/Elmish application.

Normally, Elmish applications have a single, top-level Model that represents the entire state of our application. Elmish Land is no different, except it generates that Model for us. This allows you to focus on creating smaller models for each page.

Here's a high-level overview of the Model that is used internally by Elmish Land to store our web application state:

```fsharp
type Model = {
    Shared: SharedModel
    CurrentPage: Page
}
```

Every Elmish Land application has these two fields:

* Shared - stores data that is available to every page
* CurrentPage - stores data for the current page

### A quick example
For example, if we have an application with a Dashboard, Settings, and SignIn page, this would be the generated Page type:

```fsharp
type Page =
    | Dashboard of Pages.Dashboard.Page.Model
    | Settings of Pages.Settings.Page.Model
    | SignIn of Pages.SignIn.Page.Model
    | NotFound
```

When a user visits a URL like /dashboard, Elmish Land calls the Pages.Dashboard.Page.init function to determine the initial state of Pages.Dashboard.Page.Model. Each time the user navigates to a different page, the model.CurrentPage field is cleared out, and replaced with the Model for the new page.

#### 1. The user visits `/dashboard` (`Pages.Dashboard.Page.init` is called)
```fsharp
model =
    { 
        Shared = {}
        CurrentPage = Dashboard { ... }
    }
```

#### 2. The user clicks a link to `/settings` (`Pages.Settings.Page.init` is called)
```fsharp
model =
    { 
        Shared = {}
        CurrentPage = Settings { ... }
    }
```

#### 3. The user refreshes the page (`Pages.Settings.Page.init` is called again)
```fsharp
model =
    { 
        Shared = {}
        CurrentPage = Settings { ... }
    }
```

The important thing to understand is that as the user changes the URL, the entire model.CurrentPage field is replaced with a new one. This behavior helps make Elmish Land pages easier to understand, but introduces a new challenge: "How do we share information like a signed-in user across pages?"

## Adding custom state to `SharedModel`
In order to create state that should be available across pages, we'll want to customize our `SharedModel` so it can store whatever information is important for our specific app.

## SharedModel
The `SharedModel` type in `src/Shared.fs` defines what data is available on every page.

For example, you might add a field to track the username of the current user:

```fsharp
// Shared.fs
type SharedModel = {
    Username: string option
}
``` 

## Shared.init
The `init` function in the `src/Shared.fs` file defines the initial state of our SharedModel. This function is called anytime the web application loads for the first time or is refreshed.

For example, if you have edited the SharedModel, you'll see a compiler error message saying that this function needs to be updated.

```fsharp
// Shared.fs
let init (): SharedModel * Command<'msg, SharedMsg, 'layoutMsg> =
    {
        Username = None
    }
    , Command.none
``` 

## SharedMsg
The `SharedMsg` type in `src/Shared.fs` defines what shared messages is available for all pages.

This type works just like page or layout's Msg value: it defines all the possible events that can be handled by the `Shared.update` function.

For example, you might define SharedMsg values like these:

```fsharp
type SharedMsg = 
    | SignedIn of User
    | SignOutClicked
```

In the next section, we'll see how Shared.update uses these values to make changes to the SharedModel!

## Shared.update
The update function handles how our SharedModel should change in response to events across our application. This concept should be familar to you if you have already read the Pages or Layouts guides.

Here's a visual example of a Shared.update function responding from message you might find in a real world application:

```fsharp
// Shared.fs
let update (msg: SharedMsg) (model: SharedModel): SharedModel * Command<'msg, SharedMsg, 'layoutMsg> =
    match msg with
    | SignedIn user -> { model with user = Some user }, Command.none
    | SignOutClicked -> { model with user = None }, Command.none
```

## Reading Shared data from pages
Often you need to read data from Shared from pages. Every page's `page` function receives the `shared` model as an argument.

```fsharp
// A Page.fs file
let page (shared: SharedModel) (route: AboutRoute) =
    Page.create init update view
```

In [the "Working with shared and route" section](/docs/core-concepts/pages#utilizing-shared-and-route), you'll learn more about how to use `shared` from a page.

## Sending messages from pages to Shared
When you need to send messages from a page to Shared you will use the `Command.ofShared` function. 

```fsharp
// A Page.fs file
let update (msg: Msg) (model: Model): Model * Command<Msg, SharedMsg, MyProject.Pages.Layout.Msg> =
    match msg with
    | SignOut -> model, Command.ofShared SharedMsg.SignOutClicked
```

In [the Commands section](/docs/core-concepts/commands), you'll learn more about commands.
