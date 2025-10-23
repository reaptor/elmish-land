# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
