---
sidebar_position: 3
---

# Dynamic routes

Some page directories have a trailing underscore, (like _Id or _User). These are called "dynamic pages", because this page can handle multiple URLs matching the same pattern. Here are some examples:

| Page filename                      | URL              | Example URLs                                             |
|------------------------------------| ---------------- | -------------------------------------------------------- |
| src/Pages/Blog/_Id/Page.fs         | /blog/:id        | /blog/1, /blog/2, /blog/xyz, ...                         |
| src/Pages/Users/_Username/Page.fs  | /users/:username | /users/ryan, /users/2, /users/bob, ...                   |
| src/Pages/Settings/_Tab/Page.fs    | /settings/:tab   | /settings/account, /settings/general, /settings/api, ... |

The name of the directory (_Id, _User or _Tab) will determine the names of the parameters passed into your page's init function:

```fsharp
-- /blog/123
let init ... (``id``) ... = ... // ``id`` = 123

-- /users/ryan
let init ... (username) = ... // username = "ryan"

-- /settings/account
let init ... (tab) = ... // tab = "account"
```

For example, if we renamed Settings/_Tab/Page.fs to Settings/_Foo/Page.fs, we'd access the dynamic route parameter via foo instead!

:::info

If this concept is already familiar to you, great! "Dynamic routes" aren't an Elmish Land idea, they come from popular frameworks like Next.js and Nuxt.js:

* Next.js uses the naming convention: blog/[id].js
* Nuxt.js uses the naming convention: blog/_id.vue

:::
