# Elmish Land Contributing Guide

## Support
Ask your questions in our Discord https://discord.gg/jQ26cZH3fU

## Preparing

Elmish Land requires the use of dotnet. You can [download the SDK here](https://dotnet.microsoft.com/en-us/download)

`dotnet` commands run in the project's root directory. You can checkout the code and install the dependencies with:

```bash
git clone https://github.com/reaptor/elmish-land.git
cd elmish-land
dotnet tool restore
dotnet restore
```

## Running locally

Run elmish-land locally in development with the following commands:

```bash
dotnet run --framework net8.0 -- init <project_dir>
```

```bash
dotnet run --framework net8.0 -- server <project_dir>
```

```bash
dotnet run --framework net8.0 -- build <project_dir>
```

`<project_dir>` can be a relative path eg. `../TestProject`

### Building a local nuget
Increment the version in `<project-root>\src\elmish-land.fsproj` if needed:
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        ...
        <Version>1.1.0-beta.5</Version>
    </PropertyGroup>
```

Build and create nuget package:
```bash
dotnet pack
```

The built nuget is placed in `<project-root>\src\nupkg`

Example: `C:\Projects\elmish-land\src\nupkg`

### Installing a local version (not on nuget.org) in you own project
Run the following from you project that already has elmish land installed:
```bash
dotnet tool update elmish-land --version <you-built-version> --add-source <folder-of-built-elmish-land-nuget>
```
Example:
```bash
dotnet tool update elmish-land --version 1.1.0-beta.5 --add-source "C:\Projects\elmish-land\src\nupkg"
```

## Sending PRs

### Coding style

All code is must be with [Fantomas](https://fsprojects.github.io/fantomas) with rules specified in `.editorconfig`

```bash
dotnet fantomas .
```
