# NOTICE

## Project

**BB Keybinds** — in-game keyboard & mouse rebinding for Bloodborne on the Windows bbport
(shadPS4-based) port.

- **Authors:** Swn555666 ([github.com/Swn555666](https://github.com/Swn555666)) and
  Claude (Anthropic), built with [Claude Code](https://claude.ai/code).
- **License:** GNU General Public License v2.0 or later (GPL-2.0-or-later). See `LICENSE`.

## Third-party components and references

### bbport / bloodborne_pc

This tool is written for **Supermedo's bbport for Windows** (the shadPS4-based Bloodborne
port), which is licensed under **GPL-2.0**:

- <https://github.com/Supermedo/bloodborne_pc>
- <https://github.com/deadinside28/bloodborne_pc> (the Linux port bbport is built on)

The `input.ini` syntax, the factory default bindings, the hotkey names (`F7` mouse toggle,
`F8` reload), and the parser/writer semantics implemented here come from bbport's
`launcher/bbport_input_config.py`, `src/runtime_input_config.c`, and `docs/INPUT.md`.
`examples/factory-default.ini` and the default layout constants are reproduced from that
project for compatibility.

### shadPS4

bbport is based on shadPS4, the PlayStation 4 emulator. shadPS4's input-config format is the
basis for `input.ini`:

- <https://github.com/shadps4-emu/shadPS4>

### Claude / Claude Code

Implementation and documentation assistance was provided by Claude (Anthropic) through
Claude Code. <https://claude.ai/code>

## Trademarks

Bloodborne, PlayStation, and PS4 are trademarks of Sony Interactive Entertainment.
FromSoftware is the developer of Bloodborne. This project is unofficial, non-commercial, and
not affiliated with or endorsed by FromSoftware, Sony Interactive Entertainment, or the
bbport author.

## Scope

BB Keybinds only reads and writes the `input.ini` file of the user's own bbport installation.
It does not include or distribute any game files, emulator binaries, or copyrighted assets.
Users must provide their own legally obtained copy of Bloodborne and bbport.
