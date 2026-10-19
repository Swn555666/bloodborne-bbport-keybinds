# Changelog

All notable changes to BB Keybinds are documented here.

## 1.0.0 — 2026-10-19

Initial public release.

- In-game overlay (default **F10**) for rebinding keyboard and mouse bindings.
- Up to 3 slots per action and 3 keys per binding, including mouse buttons, wheel and
  modifier combinations (`Shift + Left click`).
- Conflict detection and reserved-key protection (`Insert`, `F9`, `F7`, `F8`, panel hotkey).
- Mouse look settings: stick emulation or direct 1:1 camera, sensitivity, invert X/Y,
  dead-zone/speed parameters.
- Safe `input.ini` writer: preserves controller bindings, comments and unknown settings,
  automatic timestamped backups (last 10 kept), UTF-8 without BOM, LF line endings.
- Automatic focus + **F8** reload after applying.
- User-defined default layout (**Set as default** / **Restore defaults** /
  **Clear custom default**).
- Follows the running `bb-probe.exe` when several bbport installations exist.
- Single-file self-contained Windows executable (no .NET installation required).
- Headless `--selftest` suite covering parsing, round-trip preservation, conflicts, layout
  application, encoding and mouse settings.
