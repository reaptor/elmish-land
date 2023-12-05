---
sidebar_position: 3
draft: true
---

# Pages and Routes

Pages are the basic building blocks of your app. In a typical Elmish application, you have to manually manage your routing.

Elmish Land has a file-based routing convention that automatically generates that code for you. So if you want a new page at /hello, you can create a new file at src/Pages/Hello/Page.fs. Elmish Land handles the rest!

As you add more features to your app, you pages folder will grow to match all the URLs you care about. After a while, it might look something like this:

```bash
src/
└── Pages/
    ├── Home
    │   └── Page.fs
    ├── Settings
    │   └── Page.fs
    └── People/
        └── Page.fs
```
