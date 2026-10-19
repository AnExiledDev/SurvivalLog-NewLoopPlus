# New Loop Plus

End a run whenever you like and go straight to the death screen, so you can start a new loop without waiting for your character to die.

## Features

- **End Run button**: the pause menu gets an **End Run** button, right above Save & Exit.
- **Always asks first**: clicking it opens a confirm dialog. Nothing ends until you click **End Run** in that dialog; Cancel or a click outside it closes the dialog and leaves the run alone.
- **Shows what you will get**: in a normal run the dialog shows the Planning Points the death screen will pay, split into survival points, crises and days survived. In Casual mode it says the run gives no Planning Points. In endless and challenge runs it says the run is settled on that mode's own results screen.
- **The game's own death**: on confirm the pause menu closes, and as soon as the game runs again the run ends exactly as a death does. The death screen, the settlement and the save are the game's own.
- **Log file**: every confirm, the points preview and how the end went are written to `BepInEx\new-loop-plus.log`.

The button only shows in a loaded run past the tutorial that has not already ended. The button and the dialog follow the game's language, English or Simplified Chinese.

New Loop Plus adds no data of its own to your save and has no config options. You can remove it at any time.

## Known issues and limits

- If the game is still paused five seconds after the pause menu closes (another window holding the game paused, for example), the run is not ended and a short message says so. Close that window and try again.
- The points preview is worked out the way the game settles a death. If a value it needs cannot be read, the dialog says the death screen will show the points instead.

## Requirements

- Survival Log 1.1.18902. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Copy `NewLoopPlus.dll` into the `BepInEx\plugins` folder inside the game folder (the folder with `SurvivalLog.exe`).

## Uninstall

Delete `NewLoopPlus.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\new-loop-plus.log`.

## Troubleshooting

Check `BepInEx\LogOutput.log` for `===== New Loop Plus 0.1.0 started`. The same line, plus every confirm, the points preview and how the end went, is in `BepInEx\new-loop-plus.log`, which survives restarts.

If End Run does not show in the pause menu, make sure you are in a loaded run past the tutorial. If the run did not end after you confirmed, the message on screen and the log say why.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\new-loop-plus.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/NewLoopPlus -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\NewLoopPlus\bin\Release\NewLoopPlus.dll`.

The unit tests cover the pure rules (the points preview, the confirm-then-end flow, the page messages and the texts) and need nothing from the game:

```
dotnet test tests/NewLoopPlus.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
