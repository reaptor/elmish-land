# Elmish Land Contributing Guide

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

## Sending PRs

### Coding style

All code is must be with [Fantomas](https://fsprojects.github.io/fantomas) with rules specified in `.editorconfig`

```bash
dotnet fantomas .
```
