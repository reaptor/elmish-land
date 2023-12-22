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
