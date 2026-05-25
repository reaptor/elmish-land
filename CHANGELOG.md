# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0 beta 2] - 2026-05-25

### Changed
- Minimum required .NET SDK raised from `6.0.100` to `10.0.100`, matching the .NET 10 requirement for scaffolded 2.0 projects

### Fixed
- SDK version check no longer fails when the latest installed .NET SDK is a preview release (e.g. `10.0.300-preview.0.26177.108`), see https://github.com/reaptor/elmish-land/issues/40

## [2.0.0 beta 1] - 2026-05-19

> **Breaking change release.** The dependency upgrades below — in particular Feliz 3, Fable 5, and vite 8 — contain breaking changes that may require updates to your application code. See the [2.0 preview blog post](https://elmish.land/blog/announcing-elmish-land-2-0-preview) and the [migration guide](https://elmish.land/docs/guides/migrating-from-v1-to-v2) for upstream breaking changes and step-by-step migration instructions.

### Changed
- Scaffolded projects (created by `init`) now target Fable 5 and the matching ecosystem majors. Requires the .NET 10 SDK and Node.js 20.19+.
  - `fable` (dotnet tool): `4` → `5`
  - `FSharp.Core`: now pinned at `10`
  - `Fable.Core`: now pinned at `5` (previously implicit)
  - `Fable.Promise`: now pinned at `3`
  - `Fable.Elmish`: now pinned at `5`
  - `Fable.Elmish.HMR`: `8` → `9`
  - `Fable.Elmish.React`: now pinned at `5`
  - `Feliz`: `2` → `3`
  - `Feliz.Router`: removed — replaced by a vendored copy at `.elmish-land/Base/Router.fs`
  - `react` / `react-dom`: now pinned at `19`
  - `vite`: `7` → `8`
- URL change handling in scaffolded apps moved from a `React.router` element to a subscription (`Router.subscribeToUrlChanges`), so the router no longer wraps the view tree. Application code that assumed the view was rendered inside a `React.router` element will need adjustment.
- Scaffolded projects now include a vendored `Router.fs` (copy of Feliz Router) under `.elmish-land/Base/`, used by the new subscription-based URL change handling. The `Feliz.Router` NuGet dependency has been removed.

### Added
- Dynamic resolution of NuGet and npm package versions during `init` and `upgrade` — the CLI now queries the NuGet and npm registries for the latest version matching each pinned major instead of hardcoding exact versions, so newly created and upgraded projects always pick up the latest patch and minor releases without waiting for a new Elmish Land release.
- New `upgrade` command (`dotnet elmish-land upgrade`) that brings an existing project up to the 2.0 dependency set and applies the mechanical parts of the Feliz 2 → 3 migration:
  - Updates `Directory.Packages.props` (NuGet `PackageVersion` entries), preserving any user-added entries, and removes the `Feliz.Router` entry
  - Updates `package.json` dependencies and devDependencies, preserving user-added packages
  - Updates `.config/dotnet-tools.json` (or root `dotnet-tools.json`) tool versions, preserving user-added tools
  - Strips any leftover `<PackageReference Include="Feliz.Router" />` entries from user `.fsproj` files
  - Applies whole-identifier renames across user `.fs` files for the Feliz 2 → 3 PascalCase components (`React.fragment` → `React.Fragment`, `React.suspense` → `React.Suspense`, etc.) and the `FsReact` namespace move (`React.createDisposable` → `FsReact.createDisposable`, etc.)
  - Prints a checklist of manual migrations with deep links into the Feliz 3 upgrade docs for patterns that cannot be auto-rewritten: `React.memo`, `React.lazy'`, the redesigned `React.context` API, and hand-written bindings using `Interop.reactApi.createElement`
  - Walks the project tree for `elmish-land.json` files so a solution root containing multiple Elmish Land apps upgrades all of them in one invocation
  - Regenerates files under `.elmish-land/` for each app
  - Does **not** edit `global.json` or your `.fsproj` `<TargetFramework>` — those .NET 10 prerequisites are documented in the migration guide and must be set up by hand before running `upgrade`. After `upgrade` completes, run `dotnet restore` and `npm install` yourself.

### Fixed
- The interactive route-mode prompt during `init` is no longer interrupted by spinner output — the prompt now runs before the spinner starts
- Removed F# compiler warning about unused query parameter binding for routes that don't define query parameters
- Removed F# compiler warning about unused `msg` binding in the generated `sharedCommandToCmd` function

## [1.1.0] - 2026-02-21
- Includes everything from 1.1.0 beta

## [1.1.0 beta 11] - 2026-02-21
### Added
- Configurable Fable `noCache` option in `elmish-land.json` via `fable.server.noCache` and `fable.build.noCache`

### Fixed
- `init` command now also detects `dotnet-tools.json` in project root directory (new default for .NET SDK 10.0.102, https://github.com/dotnet/sdk/issues/53103#issuecomment-3937607643)

## [1.1.0 beta 10] - 2026-02-19
### Fixed
- Support slnx solution file format, see https://github.com/reaptor/elmish-land/issues/38

## [1.1.0 beta 9] - 2026-02-18
### Fixed
- Update function in page is not receiving the latest shared model value, see https://github.com/reaptor/elmish-land/issues/37

## [1.1.0 beta 8] - 2025-11-17
### Added
- Upgraded nuget dependencies for scaffolded projects (by `init` command) to:
  - `FSharp.Core` version 10.0.100
  - `Fable.Elmish.HMR` version 8.0.0
  - `Fable.Elmish.React` version 5.0.0
- Replaced nuget dependency `Elmish` with `Fable.Elmish` version 5.0.2
- Upgraded npmjs dependencies for scaffolded projects (by `init` command) to:
  - `vite` version 7

## [1.1.0 beta 7] - 2025-11-16

### Added
- Configurable routing mode in `elmish-land.json` via `program.routeMode`
  - `hash` (default): Traditional hash-based URLs (example.com/#about)
  - `path`: Clean URLs without hash signs (example.com/about)
- Interactive prompt during `init` command to choose between path and hash routing modes
- `Command.ofAsync` and `Command.tryOfAsync` functions for executing F# async workflows

## [1.1.0 beta 6] - 2025-11-10

### Added
- Configurable React render methods in `elmish-land.json` via `program.renderMethod`
  - `synchronous` (default): New renders are triggered immediately after an update.
  - `batched`: Smoother frame rates. (NOTE: This may have unexpected effects in React controlled inputs, see https://github.com/elmish/react/issues/12)
- Configurable render target element ID in `elmish-land.json` via `program.renderTargetElementId` (defaults to `"app"`)

### Fixed
- Static routes now correctly take precedence over dynamic routes ([#35](https://github.com/reaptor/elmish-land/issues/35))

## [1.1.0 beta 5] - 2025-10-26

### Fixed
- `dotnet elmish-land server` command sometimes hangs

## [1.1.0 beta 4] - 2025-10-24

### Added
- Interactive prompts for automatic page file reordering in project files with preview

### Fixed
- Project file management now preserves Content entries and handles mixed path separators
- Commands from Shared.init now work properly ([#34](https://github.com/reaptor/elmish-land/issues/34))

## [1.1.0 beta 3] - 2025-10-22

### Added
- Replace cryptic type mismatch errors with clear messages identifying which pages have incorrect layout references during build and server commands
- Interactive prompt during build failures to automatically fix layout reference mismatches in page files (e.g., when a page uses `Layout.Msg` but should use `About.Layout.Msg`)

## [1.1.0 beta 2] - 2025-09-04

### Fixed
- Fixed validation to only check layout references for pages that are included in the project file, preventing spurious errors for pages that exist on disk but aren't part of the project

## [1.1.0 beta 1] - 2025-09-04

### Added
- Automatic project file management - pages and layouts are now automatically added to the project file ([#29](https://github.com/reaptor/elmish-land/issues/29))
- Automatic layout reference updates when adding new layouts to existing pages ([#30](https://github.com/reaptor/elmish-land/issues/30))
- Solution file generation during project initialization to improve IDE experience in Visual Studio and Rider ([#22](https://github.com/reaptor/elmish-land/issues/22))
- Loading indicator when running commands (init, build, restore, server). Use --verbose to show the ful build and server output.

### Fixed
- Improved error messaging when pages use incorrect layout references, with validation checks in `build`, `restore`, and `server` commands ([#28](https://github.com/reaptor/elmish-land/issues/28))
- Resolved issue where adding pages with nested layouts caused incorrect layout assignment ([#21](https://github.com/reaptor/elmish-land/issues/21))
- Fixed compilation errors when route path parameters contain F# keywords like "new" ([#25](https://github.com/reaptor/elmish-land/issues/25))
- Resolved issue preventing Commands from Shared module from working properly ([#27](https://github.com/reaptor/elmish-land/issues/27))

## [1.0.4] - 2025-03-07

### Fixed
- Support for dashes (-) in folder and application names ([#20](https://github.com/reaptor/elmish-land/issues/20))

## [1.0.3] - 2024-11-15

### Fixed
- Fixed add page command throwing exception

## [1.0.0] - 2024-11-14

### Added
- Full stable release of Elmish Land framework
- Support for .NET 9
- Support for subscriptions in Shared module  
- Support for cascading query parameters in route.json files
- NotFound page support
- View module support in config
- Route.isEqualWithoutPathAndQuery method for route comparison
- Ability to set view type to scaffold when creating new pages
- Layout props and ability to send messages from layouts to pages
- Support for typed path parameters and query parameters with custom parsers and formatters

### Changed
- Improved layout system based on naming conventions
- Layout init now only called when layout changes

### Fixed
- Query parameters handling ([#19](https://github.com/reaptor/elmish-land/issues/19))
- Route.format when using reserved names for query params and multiple query params
- Layout models now support structural comparison
- Page init no longer called twice
- Pages now correctly receive layout messages
- App.fs compilation when using page messages from layout
- App.fs no longer always initializes HomePage
