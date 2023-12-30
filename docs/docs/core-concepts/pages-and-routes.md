---
sidebar_position: 1
---

# Pages and Routes

At the heart of Elmish Land is a filesystem-based router. The routes of your app — i.e. the URL paths that users can access — are defined by the directories in your codebase:

* `src/Pages/Home` is the root route
* `src/Pages/About` creates an `/about` route
* `src/Pages/Blog/{slug}` creates a route with a parameter, slug, that can be used to load data dynamically when a user requests a page like `/blog/hello-world`

Each page directory contains one page file called `Page.fs`.

As you add more features to your app, you pages folder will grow to match all the URLs you care about. After a while, it might look something like this:

```bash
src/
└── Pages/
    ├── Home
    │   └── Page.fs
    ├── About
    │   └── Page.fs
    └── Blog
        └── {slug}
            └── Page.fs
```

## The "About" page ​

To get started, let's start with a page that is displayed when a user visits the URL /about.

We can create our about page using the `elmish-land add page` command shown below:

```bash
dotnet elmish-land add page /about
```

```bash
    Elmish Land added a new page at /about
    --------------------------------------

    You can edit your new page here:
    ./src/Pages/About/Page.fs

    Please add the file to the project using an IDE or add the following line to an
    ItemGroup in the project file './MyProject.fsproj':
    <Compile Include="src/Pages/About/Page.fs" />
```

:::warning

You need to manually add the new page to your project file.

:::

## Dynamic routes

Some page directories have a trailing underscore, (like Id_ or User_). These are called "dynamic pages", because this page can handle multiple URLs matching the same pattern. Here are some examples:

| Page filename                     | URL              | Example URLs                                             |
| --------------------------------- | ---------------- | -------------------------------------------------------- |
| src/Pages/Blog/Id_/Page.fs        | /blog/:id        | /blog/1, /blog/2, /blog/xyz, ...                         |
| src/Pages/Users/Username_/Page.fs | /users/:username | /users/ryan, /users/2, /users/bob, ...                   |
| src/Pages/Settings/Tab_/Page.fs   | /settings/:tab   | /settings/account, /settings/general, /settings/api, ... |

The name of the directory (Id_, User_ or Tab_) will determine the names of the parameters passed into your page's init function:

```fsharp
-- /blog/123
let init ... (``id``) ... = ... // ``id`` = 123

-- /users/ryan
let init ... (username) = ... // username = "ryan"

-- /settings/account
let init ... (tab) = ... // tab = "account"
```

For example, if we renamed Settings/Tab_/Page.fs to Settings/Foo_/Page.fs, we'd access the dynamic route parameter via foo instead!

:::info

If this concept is already familiar to you, great! "Dynamic routes" aren't an Elmish Land idea, they come from popular frameworks like Next.js and Nuxt.js:

* Next.js uses the naming convention: blog/[id].js
* Nuxt.js uses the naming convention: blog/_id.vue

Because leading _ (underscore) in FSharp has special meaning, Elmish Land uses a trailing _ instead.

* Blog/Id/Page.fs is a static page that only handles /blog/id
* Blog/Id_/Page.fs is a dynamic page that can handle /blog/id, /blog/xyz, /blog/3000, etc

:::

## Query parameters

Every page's ``init`` function has a ``(query: list<string * string>)`` parameter. This parameter contains the query parameters for the current URL. The following url:

`/Blog?username=john`

will yield the following value for the `query` parameter:

```fsharp
[ "username", "john" ]
```

You can get a query parameter by using `List.tryFind`:

```fsharp
let username = query |> List.tryFind (fun x -> x = "username")
```