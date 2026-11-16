# Changelog

All notable changes to Sky Glance are listed here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

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
- Auto-updates from GitHub Releases with SHA-256 verification.

[1.0.0]: https://github.com/medovanx/sky-glance/releases/tag/v1.0.0
