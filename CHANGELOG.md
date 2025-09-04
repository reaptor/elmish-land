# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2025-08-12

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
