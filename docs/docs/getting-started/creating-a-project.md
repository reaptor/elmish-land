---
sidebar_position: 2
---

# Creating a project

## Prerequisites
Before initiating a project, ensure that your development environment includes:

* **[.NET SDK](https://dotnet.microsoft.com/)**: Version 6.0 or higher is required, with version 9.0 recommended.​
* **[Node.js](https://nodejs.org/)**: Version 18.0 or above is necessary. Utilizing a version manager like nvm can assist in handling multiple Node.js versions on a single machine.​

When installing Node.js, it's advisable to select all checkboxes related to dependencies to ensure a comprehensive setup.


## Setting Up a New Project
Elmish Land offers a dedicated .NET tool to facilitate project creation, feature addition, and development server management. Follow these steps to establish a new project:​

### 1. Initialize the Project Directory:
```bash
mkdir MyProject
cd MyProject
```

### 2. Install the Elmish Land Tool:
```bash
dotnet tool install elmish-land --create-manifest-if-needed
```

### 3. Initialize the Elmish Land Project:
```bash
dotnet elmish-land init
```

### 4. Launch the Development Server:
```bash
dotnet elmish-land server
```

Executing `dotnet elmish-land init` scaffolds a new project within the `MyProject` directory. Subsequently, running `dotnetn elmish-land server` initiates the development server, accessible at `http://localhost:5173`.​

For community support, collaboration, or to seek assistance, consider joining the [Elmish Land Discord](https://discord.gg/jQ26cZH3fU). We are eager to see the innovative applications you'll develop with Elmish Land!​

## Editor Configuration

For an optimal development experience, we recommend using [Visual Studio Code](https://code.visualstudio.com/) accompanied by the [Ionide plugin](https://ionide.io/Editors/Code/overview.html). If you prefer alternative editors, explore [other available options](/docs/advanced/other-editors).​

### To set up VS Code:

1. Install [Visual Studio Code](https://code.visualstudio.com/)
2. Add the [Ionide](https://ionide.io/Editors/Code/overview.html) extension

This setup provides a robust environment tailored for F# and Elmish Land development, enhancing productivity and code management.​

By following these guidelines, you are well-equipped to embark on your Elmish Land development journey, crafting scalable and maintainable web applications with F# and Elmish.