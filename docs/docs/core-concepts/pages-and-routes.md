---
sidebar_position: 3
draft: true
---

# Pages and Routes

At the heart of Elmish Land is a filesystem-based router. The routes of your app — i.e. the URL paths that users can access — are defined by the directories in your codebase:

* `src/Pages/Home` is the root route
* `src/Pages/About` creates an `/about` route
* `src/Pages/Blog/[slug]` creates a route with a parameter, slug, that can be used to load data dynamically when a user requests a page like `/blog/hello-world`

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
        └── Page.fs
```
