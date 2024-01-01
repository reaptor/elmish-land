---
sidebar_position: 1
---

# Defining Routes

Elmish Land uses a file-system based router where folders are used to define routes.

Each folder represents a route segment that maps to a URL segment. To create a nested route, you can nest folders inside each other.

* `src/Pages/Home` is the root route
* `src/Pages/About` creates an `/about` route
* `src/Pages/Blog/_slug` creates a route with a parameter, slug, that can be used to load data dynamically when a user requests a page like `/blog/hello-world`

Each page folder contains a file called `Page.fs` that is used to make route segments publicly accessible.

```bash
src/
└── Pages/
    ├── Home
    │   └── Page.fs
    ├── About
    │   └── Page.fs
    └── Blog
        ├── Shared
        └── Page.fs
```

In this example, the `/blog/shared` URL path is not publicly accessible because it does not have a corresponding Page.fs file. This folder could be used to store components, stylesheets, images, or other colocated files.
