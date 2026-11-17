# Changelog

All notable changes to Sky Glance are listed here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [1.4.1] - 2026-11-17

### Changed
- The version line at the top of the widget's menu reads "Sky Glance v1.4.0".

## [1.4.0] - 2026-11-17

### Added
- A sky for every weather behind the widget: blue with a warm glow on a sunny day, deep blue with stars at night, slate with rain streaks, icy blue with snowflakes, misty grey for fog and violet for a storm, with white text. With animation on, the rain, snow and stars move and the clouds drift. On by default; Settings can bring back the plain style.
- The sunrise and sunset tiles in the flyout have a picture: the sun half over the horizon, with an arrow up or down.

### Fixed
- The golden hour lines up with the forecast's sunset, so the two tiles always agree.

## [1.3.2] - 2026-11-17

### Changed
- The animation setting also brings the flyout's header to life while it is open: the big picture moves, the cloud pattern drifts, and rain streaks, snowflakes or stars move behind it.
- Animations move more and faster: the sun's rays turn while its glow breathes, clouds and fog travel twice as far, and the slow motions repeat every 6 seconds instead of 14.

## [1.3.1] - 2026-11-17

### Changed
- The widget animation covers every kind of weather: clouds drift, fog slides, the sun's glow breathes, stars twinkle by the moon and lightning flashes now and then, as well as falling rain and snow. Still off by default.

## [1.3.0] - 2026-11-17

### Added
- Rain alerts: a notification when rain is about to start or stop at your location, from Open-Meteo's 15-minute forecast. It names the place and the time, and one shower raises one alert.
- A hover card: rest the mouse on the widget for the next six hours, the wind and the air.
- Air quality: the European air quality index and its band in the flyout, the pollen level in Europe, and a coloured dot on the widget when the air turns poor.
- Moon and golden hour tiles: tonight's moon as it looks and how much of it is lit, and when the next golden hour starts, worked out on your PC.
- Compared with yesterday: the flyout says how much warmer or colder it is than at this hour yesterday.
- The calendar shows a day hour by hour, with rain chances for forecast days and the rain that fell for past ones.
- Rain and snow can drift in the widget's picture (off by default).
- A 12-hour clock with AM and PM, on by default when Windows uses one.

## [1.2.4] - 2026-11-17

### Fixed
- Menu text sits in the middle of its row, with each icon centred on it, so the space above and below every separator is even. WinForms placed the text at the top of the padded row.

### Changed
- The version line at the top of the widget menu shows the Sky Glance icon.

## [1.2.3] - 2026-11-17

### Changed
- The widget's right-click menu is drawn in the app style, like Data Tide's: an icon on every row, lined up with its text, rounded rows and corners, roomier spacing and an accent check. Its first line shows the app name and version.

## [1.2.2] - 2026-11-17

### Added
- While an update is checked for or installed, a slim progress line runs along the bottom of the taskbar widget, so a check started from the right-click menu shows it is working. The menu item says what the update is doing and can't be clicked twice.

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
