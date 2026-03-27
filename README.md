# WeightHUD

HUD overlay for Escape from Tarkov / SPT that shows the player carry weight, live carry thresholds, and a category breakdown for equipment, weapons, and backpack.

**Author:** JordiXIII  
**Version:** 0.1.1  
**SPT Version:** 4.0.13  
**License:** MIT

## Features

- Circular weight gauge with threshold markers and optional numeric labels on the ring.
- Compact live weight display with safe / overweight / slow walk / max carry coloring.
- Weight breakdown for equipment, weapons, and backpack.
- Configurable breakdown placement: right, bottom, left, or top of the gauge.
- PMC / SCAV badge, with optional main-menu HUD display.
- F12-configurable position, scale, refresh rate, shortcut, and colors.
- Prefers loaded in-game fonts by default.

## Configuration

The config file is created at `BepInEx/config/JordiXIII.WeightHUD.cfg`.

Main settings:

- `Enable HUD`
- `Show In Main Menu`
- `Toggle HUD Shortcut`
- `Refresh Interval (ms)`
- `Anchor X`
- `Anchor Y`
- `Scale`
- `Breakdown Placement`
- `Show Gauge Threshold Labels`
- threshold and panel colors
- `Preferred Font Name`

All settings are editable through the BepInEx Configuration Manager (`F12`) if it is installed.

## Building

```powershell
dotnet build -c Release
```

The project copies the compiled DLL to the configured SPT install after a successful build and also creates `WeightHUD.zip`.

## Installation

1. Build the project or download a release.
2. Copy `WeightHUD.dll` into `<SPT>\BepInEx\plugins\`.
3. Launch the game.
4. Adjust the HUD from `F12` if needed.
