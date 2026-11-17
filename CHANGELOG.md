# Changelog

All notable changes to Sky Glance are listed here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [1.2.1] - 2026-11-17

### Added
- A Language submenu in the widget's right-click menu switches between English and Arabic.

### Fixed
- In Arabic, the condition line in the taskbar widget now lines up on the right like the temperature above it.

## [1.2.0] - 2026-11-17

### Changed
- Checking for and installing an update now shows a progress bar where Check now was, with the download percentage and size, then the check of the download and the restart.
- The Updates page has GitHub page and Release notes buttons in place of the plain link.

## [1.1.0] - 2026-11-17

### Changed
- No more icon in the notification area. The widget's right-click menu has every command, running the exe again opens the flyout, and update messages arrive as Windows notifications.

## [1.0.1] - 2026-11-16

### Fixed
- The city list in the flyout and the right-click menu now follow the app's light or dark theme.
- Disabled buttons in Settings no longer show their text in near-black on the dark theme.
- The place name in the flyout always opens the city list, with "Manage cities", even before any city is saved.

## [1.0.0] - 2026-11-16

### Added
- Taskbar widget with the weather picture, the temperature, today's high and low (each can be hidden) and the condition, beside the notification area or at the left end of the taskbar. On either side it takes the nearest free spot, so it never overlaps other taskbar widgets such as Data Tide or Salaty Reminder, and it survives Explorer restarts.
- Forecast flyout: conditions now with "feels like" and today's high and low, six detail tiles, a 24-hour temperature chart with rain chances and a 7-day forecast.
- Automatic location through Windows location with an IP fallback, re-checked every 6 hours, or any city found by search.
- Temperature in Celsius or Fahrenheit, wind in km/h, m/s, mph, knots or Beaufort, precipitation in millimetres or inches.
- Several cities: save any number, switch from the flyout, the right-click menu, the arrow keys or the mouse wheel over the widget.
- Weather on any date from 1 January 1940 up to 15 days ahead, in a calendar window, from the Open-Meteo forecast and historical archive.
- The season at the place shown and the days left in it.
- The hottest and coldest of 32 watched places around the world right now.
- The last forecast is cached on disk and shown while offline; failed refreshes retry with a growing delay.
- English and Arabic with a full right-to-left layout; light and dark theme following Windows.
- Settings > Updates links to the project page on GitHub.
- Auto-updates from GitHub Releases with SHA-256 verification.

[1.1.0]: https://github.com/medovanx/sky-glance/releases/tag/v1.1.0
[1.0.1]: https://github.com/medovanx/sky-glance/releases/tag/v1.0.1
[1.0.0]: https://github.com/medovanx/sky-glance/releases/tag/v1.0.0
