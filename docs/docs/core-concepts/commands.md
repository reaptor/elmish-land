---
sidebar_position: 4
---

# Commands

Commands let us send messages, navigate to another page, run a promise and more.

## An example

Let's define a `Model` that will hold a value and a `Msg` that will tell us how to change that value:

```fsharp
type Model =
    {
        Value : int
    }

type Msg =
    | Increment
    | Decrement
```

Now we define the `init` function that will produce initial state once the program starts running.

```fsharp
let init () =
    {
        Value = 0
    }
    , Command.ofMsg Increment
```

Notice that we return a tuple. The first field of the tuple tells the page's the initial state. The second field holds the command to issue an `Increment` message.

The `update` function will receive the change required by `Msg`, and the current state. It will produce a new state and potentially new command(s).

```fsharp
let update msg model =
    match msg with
    | Increment when model.Value < 2 ->
        { model with
            Value = model.Value + 1
        }
        , Command.ofMsg Increment
    | Increment ->
        { model with
            Value = model.Value + 1
        }
        , Command.ofMsg Decrement
    | Decrement when model.Value > 1 ->
        { model with
            Value = model.Value - 1
        }
        , Command.ofMsg Decrement
    | Decrement ->
        { model with
            Value = model.Value - 1
        }
        , Command.ofMsg Increment
```

Again we return a tuple: new state, command.

If we goto this page, it will keep updating the model from 0 to 3 and back.

:::info

**Does this look familiar?**

The `Command<'msg, 'sharedMsg>` type in Elmish Land is an abstraction built on top of [Elmish's standard Cmd\<'msg\> type](https://elmish.github.io/elmish/#commands). The `Command` module contians convenience functions for using Elmish Cmd:s and functions to allow communication from pages to the Shared module.

:::

## Available commands

### Command.none
This tells Elmish Land not to run any commands.

### Command.batch
This allows you to send many commands at once.

### Command.ofCmd
Convert an Elmish Cmd to an Elmish Land Command.

### Command.ofMsg
Send a msg as a Command.

### Command.ofShared
Send a SharedMsg to the shared module from a page or layout.

### Command.ofLayout
Send a message to the layout of a page [Read more about layouts in the "Layouts" section](/docs/core-concepts/layouts).

### Command.navigate
Navigates to a specified route. [Read more in the "Linking and Navigating" section](/docs/core-concepts/linking-and-navigating#command-navigate).

### Command.ofPromise
Run a promise that will send the specified message when completed. Throws an exception if it fails.

### Command.tryOfPromise
Run a promise that will send the specified messages when succeeds or fails. Does not throw exceptions.
