---
sidebar_position: 2
---

# Creating a project

## Prerequisites
Before initiating a project, ensure that your development environment includes:

* **[.NET SDK](https://dotnet.microsoft.com/)**: Version 8.0 or higher is required, with version 10.0 recommended.​
* **[Node.js](https://nodejs.org/)**: Version 18.0 or above is necessary. Utilizing a version manager like nvm can assist in handling multiple Node.js versions on a single machine.​


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

The init command will prompt you to choose between **hash routing** (default) and **path routing** for your project.

### 4. Launch the Development Server:
```bash
dotnet elmish-land server
```

Executing `dotnet elmish-land init` scaffolds a new project within the `MyProject` directory. Subsequently, running `dotnet elmish-land server` initiates the development server, accessible at `http://localhost:5173`.​

You can also use `npm start` as a shortcut for `dotnet elmish-land server`, and `npm run build` for `dotnet elmish-land build`, since the scaffolded `package.json` includes these scripts.

For community support, collaboration, or to seek assistance, consider joining the [Elmish Land Discord](https://discord.gg/jQ26cZH3fU). We are eager to see the innovative applications you'll develop with Elmish Land!​

## Editor Configuration

For an optimal development experience, we recommend using one of the following editors: 

* [Visual Studio Code](https://code.visualstudio.com/) with the [Ionide plugin](https://ionide.io/Editors/Code/overview.html)
* [Visual Studio](https://visualstudio.microsoft.com/)
* [JetBrains Rider](https://www.jetbrains.com/rider/)
