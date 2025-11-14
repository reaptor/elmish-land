---
sidebar_position: 1
---
import AddedIn from '@site/src/components/AddedIn';

# App Config (elmish-land.json)

The `elmish-land.json` file in your project root configures various aspects of your Elmish Land application. This file is automatically created when you run `dotnet elmish-land init`.

## Example Configuration

```js
{
  "$schema": "https://elmish.land/schemas/v1.1/elmish-land.schema.json",
  "program": {
    "renderMethod": "synchronous",
    "renderTargetElementId": "app"
  },
  "view": {
    "module": "Feliz",
    "type": "ReactElement",
    "textElement": "Html.text"
  },
  "projectReferences": []
}
```

## Configuration Options

### program <AddedIn version="1.1.0-beta.6" />

Controls how your Elmish Land application renders and behaves at runtime.

#### program.renderMethod

**Type:** `"synchronous"` | `"batched"`
**Default:** `"synchronous"`

Configures the React render method used by your application:

- **`synchronous`** - New renders are triggered immediately after an update. This is the default and recommended for most applications, especially those with controlled inputs.
- **`batched`** - Renders are batched for smoother frame rates. This can provide better performance but may have unexpected effects with React controlled inputs. See [elmish/react#12](https://github.com/elmish/react/issues/12) for more details.

```js
{
  "program": {
    "renderMethod": "synchronous"
  }
}
```

#### program.renderTargetElementId

**Type:** `string`
**Default:** `"app"`

Specifies the HTML element ID where your application will be mounted. The element must exist in your `index.html` file.

```js
{
  "program": {
    "renderTargetElementId": "app"
  }
}
```

When using a custom element ID, ensure your HTML contains the matching element:

```html
<body>
  <div id="app"></div>
</body>
```

### view

Configures the view layer and UI library used by your application.

#### view.module

**Type:** `string`
**Default:** `"Feliz"`

The F# module that contains your view types and elements. By default, Elmish Land uses [Feliz](https://zaid-ajaj.github.io/Feliz/) for building React UIs.

#### view.type

**Type:** `string`
**Default:** `"ReactElement"`

The type used for view elements in your application. This corresponds to the return type of your view functions.

#### view.textElement

**Type:** `string`
**Default:** `"Html.text"`

This function is used by the `add page` command when scaffolding new pages.

#### Custom View Type

If you want to use a custom UI library instead of Feliz, you can configure the view settings:

```json
{
  "view": {
    "module": "MyCustomUI",
    "type": "IElement",
    "textElement": "UI.text"
  }
}
```
:::info

Elmish Land only supports view types that can render to `ReactElement` ([Source](https://github.com/fable-compiler/fable-react/blob/main/src/Fable.React.Types/Fable.React.fs), [Package](https://www.nuget.org/packages/Fable.React.Types)). 

```
let inline (|Renderable|) (element: 'a when 'a: (member Render: unit -> ReactElement)) = 
  element
```

Example: 
```
type IElement =
  abstract member Render: unit -> ReactElement
```

:::

and then add a project reference to the library containing the UI element definition:

```json
{
  "projectReferences": [
    "src/UI/UI.fsproj"
  ]
}
```

### projectReferences

**Type:** `string[]`
**Default:** `[]`

An array of relative paths to additional F# project files (.fsproj) that should be referenced by your Elmish Land application. This is useful for sharing code across multiple projects or using custom types in your routes.

```json
{
  "projectReferences": [
    "src/Common/Common.fsproj",
    "../SharedLibrary/Shared.fsproj"
  ]
}
```

Common use cases include:
- Creating shared libraries for business logic
- Defining custom types for [route and query parameters](/docs/api-reference/route-config#custom-types)
- Using a custom view type
