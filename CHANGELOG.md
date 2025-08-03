# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.9] - 2025-08-03

### Fixed

- NRE with item query cache
- Try/Catch GetApi calls

## [2.0.8] - 2025-07-21

### Changed

- Backend stuff for android compat
- Add close button on panel view

## [2.0.7] - 2025-06-30

### Changed

- Performance improvements with better item caching.

## [2.0.6] - 2025-06-25

### Fixed

- Context tags should not be case sensitive

## [2.0.5] - 2025-04-22

### Added

- New console command `mcp-dump-savedata` to help with debugging.
- Improve performance by moving item context tag cache to lazy.

## [2.0.4] - 2025-04-18

### Added

- Added pagination to help with extremely large machine rule lists.

## [2.0.3] - 2025-04-02

### Fixed

- Machine rule cache was not invalidated with Data/Machines

## [2.0.2] - 2025-03-29

### Fixed

- Handle EMC GetExtraRequirements correctly
- Add a little bit more whitespace on the dividers

## [2.0.1] - 2025-03-27

### Fixed

- Issue with SeedInfo sigh
- Populate context tag cache on save loaded
- Incorrect description in mod config menu

## [2.0.0] - 2025-03-26

### Added

- Rewrote this entire mod to use SML instead of legacy shared library
- Now supports per location rules
- Added searching for rules/inputs
- Various performance improvements

### Changed

- Prefetch Caches no longer supported
- Save on Change is now the only option, there are no longer buttons for saving

## [1.3.4] - 2025-01-18

### Fixed

- Update for StardewUI 0.6.0
- Add a null check for Img, hopefully stop some crashes

## [1.3.3] - 2025-01-18

### Fixed

- Empty cond string

## [1.3.2] - 2025-01-18

### Fixed

- Invalid output method probes throwing error

## [1.3.1] - 2025-01-17

### Fixed

- Performance improvements, new prefetch option to preload all machine rules data on save loaded/invalidate

## [1.3.0] - 2024-12-24

### Added

- Modal for context tag repr items, to see what items belong under that tag

### Fixed

- Weedses

## [1.2.1] - 2024-12-11

- Update for StardewUI HEAD (changed GridItemLayout.Length ctor)
- Byproducts heading not appearing on modal

## [1.2.0] - 2024-12-07

### Added

- New "Progression Mode" setting

### Fixed

- Text wrapping problem on the tab buttons

## [1.1.2] - 2024-11-22

### Added

- Update for StardewUI 0.4.0, change to the new item tooltips.

## [1.1.1] - 2024-11-14

### Added

- Fill in zh translations.

### Fixed

- Fix bug with infinite recursion on EMC extra byproducts

## [1.0.4] - 2024-10-04

### Fixed

- Fixed assumption that people have EMC oops

## [1.0.3] - 2024-10-02

### Added

- Toggle (enable/disable all) buttons.
- Support for EMC extra outputs.

### Fixed

- Display problem when too many outputs exist for 1 rule.

## [1.0.2] - 2024-10-02

### Fixed

- Issue with GMCM child menu hiding toolbar/daytimemoneybox.
- Catch error with null machine output and trigger Ids, cause unknown

## [1.0.0] - 2024-10-01

### Added

- Initial release.
