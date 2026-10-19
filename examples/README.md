# Example layouts

These are complete `input.ini` files you can use as a starting point. They follow bbport's
shadPS4 input-config syntax.

| File | Style |
|---|---|
| `factory-default.ini` | bbport's factory layout: WASD move, arrows camera, Space Cross, Left Shift Circle, E Square, V Triangle, 1/3 L1/R1, R/F L2/R2, Z L3, Q/C R3, I/K/J/L d-pad, Enter Options, Tab/Backspace touchpad. Mouse look off. |
| `dark-souls-3.ini` | Dark Souls III-style keyboard + mouse: mouse look on, direct camera, left click light attack, right click strong attack, side buttons weapon/firearm, mouse wheel/Shift combos for the d-pad, E/Enter confirm. |
| `elden-ring-style.ini` | Elden Ring-style example used by the project author: `E` or **left click** confirms/interacts, **ESC** opens the game menu (Enter also works), left click is also R1 attack, right click R2, side buttons L1/L2, middle click R3 lock-on, `Z` L3, direct 1:1 mouse camera, `Left Alt` walk. |

## How to use an example

1. Close the game (or be ready to press `F8`).
2. Back up your current file:

   ```bat
   copy input.ini input.ini.backup
   ```

3. Copy the example over your port's `input.ini` (next to `bbport.ini`).
4. Start the game, or press **F8** in game to reload.
5. If you want the layout to become the tool's **Restore defaults** target, open the tool,
   load/verify the bindings, and press **Set as default**. Alternatively copy the file to:

   ```
   %APPDATA%\BB Keybinds\default-layout.ini
   ```

## Left click confirms menus

Bloodborne confirms with **Cross**. If you want left click to confirm menu options (like
Elden Ring), add:

```ini
cross = leftbutton
```

Because left click is also commonly `r1` (attack), the tool will show a conflict warning.
That is intentional: left click attacks during gameplay and confirms in menus. Note that
Cross is also the interact button, so left click will interact with nearby items as well.
