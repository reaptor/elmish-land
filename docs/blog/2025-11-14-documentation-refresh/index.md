---
slug: documentation-refresh
title: Documentation Refresh and new API Reference
authors: [klofberg]
draft: false
---

# Documentation Refresh and new API Reference

The Elmish Land documentation has been refreshed with a new API Reference section, improved organization, and enhanced content.

## API Reference

The [API Reference](/docs/api-reference/app-config) section documents all modules, functions, and configuration options with:

- Function signatures and parameter descriptions
- Examples showing common usage patterns
- Best practices for error handling and edge cases
- Guidance on when to use each feature

The API Reference includes:

- **[Page Module](/docs/api-reference/page-module)** - `Page.from`, `Page.withSubscriptions`, and page lifecycle functions
- **[Layout Module](/docs/api-reference/layout-module)** - `Layout.from`, `Layout.withSubscriptions`, `routeChanged` and more
- **[Command Module](/docs/api-reference/command-module)** - All command functions
- **[Route Module](/docs/api-reference/route-module)** - `Route.format` for type-safe navigation, `Route.isEqualWithoutPathAndQuery` for active link styling and more
- **[Route Configuration](/docs/api-reference/route-config)** - `route.json` configuration with path parameters, query parameters, built-in types, and custom types
- **[App Configuration](/docs/api-reference/app-config)** - `elmish-land.json` configuration options
- **[CLI Reference](/docs/api-reference/cli)** - Command-line interface documentation

## JSON Schema for Configuration files

A new JSON Schema for `elmish-land.json` and `route.json` files provides:
- IntelliSense in supported editors
- Validation for configuration errors
- Documentation for all options

The schemas can be referenced in `elmish-land.json` and `route.json`:

```js
// elmish-land.json
{
  "$schema": "https://elmish.land/schemas/v1.1/elmish-land.schema.json",
  ...
}
```
and
```js
// route.json
{
  "$schema": "https://elmish.land/schemas/v1/route.schema.json",
  ...
}
```

## Core concepts

The Core concept guides have been updated with new documentation:
- **[Subscriptions](/docs/core-concepts/subscriptions)** 
- **[Not Found Page](/docs/core-concepts/not-found)** 

