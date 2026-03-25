# WeightHUD

> HUD that displays the carry weight

**Author:** JordiXIII  
**Version:** 0.1.0  
**SPT Version:** 4.0.13  
**License:** MIT

---

## What This Mod Does

Describe what your mod does here.

This template includes BepInEx Configuration examples. All settings are editable in-game via the **F12** config manager or by editing the config file at `BepInEx/config/JordiXIII.WeightHUD.cfg`.

### Included Config Entries

| Setting | Type | Default | Description |
|---|---|---|---|
| Enable Mod | `bool` | `true` | Enables or disables the mod |
| Example Integer | `int` | `5` | Integer with slider (0–100) |
| Example Float | `float` | `1.0` | Float with slider (0–10) |
| Example String | `string` | `Hello SPT` | Simple string setting |
| Enum Selection | `enum` | `OptionA` | Dropdown selection |
| Action Key | `KeyboardShortcut` | `F10` | Remappable keybind |
| Overlay Color | `Color` | `red` | Color picker |

---

## Requirements

- [SPT](https://www.sp-tarkov.com/) installed (BepInEx is included)
- .NET SDK (`netstandard2.1`-compatible) for building from source

---

## Building

```sh
cd WeightHUD
dotnet build -c Release
```

The PostBuild target automatically copies the compiled DLL to your SPT installation's `BepInEx\plugins\` folder.
The project also packages the mod into a distributable `WeightHUD.zip` ready to be uploaded.

---

## Installation

1. Build the project (see above) **or** download the latest release DLL.
2. Copy `WeightHUD.dll` to `<SPT install>\BepInEx\plugins\`.
3. Launch SPT as usual.
4. Press **F12** in-game to open the config manager and adjust settings.

---

## Project Structure

```
WeightHUD/
├── WeightHUD.csproj   ← project file with DLL references
├── Plugin.cs              ← BepInEx plugin with config entries
├── README.md
└── .gitignore
```

---

## Learning Resources

| Resource | URL |
|---|---|
| BepInEx Configuration Docs | https://github.com/BepInEx/BepInEx.ConfigurationManager/blob/master/README.md|
| SPT Client Mod Examples | https://github.com/Jehree/SPTClientModExamples |
| SPT Wiki Modding Resources | https://wiki.sp-tarkov.com/modding/Modding_Resources |

---
