# New Loop Plus

End a run whenever you like and go straight to the death screen, so you can start a new loop without waiting for your character to die. You can also change how many Planning Points a run earns and what loop rewards cost.

## Features

- **End Run button**: the pause menu gets an **End Run** button near the bottom, below other mods' buttons and above Restart and Save & Exit.
- **Always asks first**: clicking it opens a confirm dialog. Nothing ends until you click **End Run** in that dialog. Cancel, or a click outside the dialog, closes it and leaves the run alone.
- **Shows what you will get**: in a normal run the dialog shows the Planning Points the death screen will pay, split into survival points, crises and days survived. In Casual mode it says the run gives no Planning Points. In endless and challenge runs it says the run ends on that mode's results screen.
- **Ends like a death**: when you confirm, the pause menu closes, and once the game is running again the run ends just as if your character died. You get the normal death screen, settlement and save.
- **Planning Points multipliers**: one overall setting, plus one each for the three parts a run pays: survival points, crises and events, and days survived. The death screen, its breakdown and the End Run preview all show the changed points.
- **Starting bonus**: extra Planning Points at the start of each new loop, paid as the loop reward screen opens after a run, so you can spend them right there.
- **Endless starting points**: a multiplier for the 500 Planning Points a new endless game starts with.
- **Change it in game**: the points multipliers and the starting bonus can be changed on the Mod Manager's Mods screen, and they apply right away.
- **Loop reward costs**: one multiplier for what every loop reward upgrade costs, plus your own multiplier for single talents by ID. The upgrade screen shows and charges the new price, and resetting it refunds what you paid.
- **Talents file**: `BepInEx\config\NewLoopPlus.Talents.json` lets you change loop rewards by hand: costs, buffs, text, extra levels, and new talents copied from existing ones. It comes with ten talents that are in the game's data but never offered, all switched off. Five work as the game left them (Driver, No-Till Soil, Trailblazer, Liaison, Radio Station). Five were unfinished, and the mod gives them new effects (Well-Connected, Last-Minute Preparation, Resourceful, Yoga Master, Shut-In). See [CONFIG.md](CONFIG.md#talents-file).
- **Thirty-six new loop rewards**, also in the talents file and switched off. Eighteen use effects the game already has, such as Prepper, Insulated Pack, Field Kit, Old Friend (start with an HK-1 robot), Brewmaster, Hardened Defenses, Long-Range Scavenging, Trade Routes, Quick Hands and Generator Tuning. Eighteen have effects made by the mod: Interest, Safety Net, Extra Pick, Crisis Veteran, Second Wind, Lucky Finds, Muscle Memory, Scout's Notes, Tinkerer, Family Recipe, Drone Pilot, Good Neighbor (more affinity per basket), Seasoned Explorer (higher Max Exposure), Dealer Discount (cheaper vehicles), Shortcuts (faster travel before the disaster), Fast Rotors (faster drone trips), and Wind Engineer and Wind Tuning (more power from Wind Power's turbines). The full list is in [CONFIG.md](CONFIG.md#talents-file).
- **Five more levels for seventeen loop rewards**, also in the talents file and switched off. Early Riser, Savings Account, Encumbrance, Clearance, Bulk Bargains, Hoarding Habit, Quick Fortification and Gratitude go to ten levels, and Resourceful Management, Interior Decorator, Energy Saver, Bargaining, Deep Freeze, Long Harvest and Cold-Resistant Constitution go to eight. Mental Toughness goes to ten, and its new levels can be used two to four times. Mold Master goes to eight and its levels are spread out to reach 100% at the top, so its levels 2 and 3 are weaker than before.
- **Endless rewards everywhere**: an option, off by default, that offers the endless mode loop rewards in every mode and makes them work there like they do in endless. The camp rewards and Cartographer stay endless only.
- **Talents tab**: with the Mod Manager 0.7.0 or newer, New Loop Plus's page gets a Talents tab that edits the talents file for you. You can change what any loop reward costs, how many levels it has, its numbers and its text, and switch the file's rewards on or off. Click NEW REWARD to build your own from the effects the game's rewards use plus the ones New Loop Plus adds. Pick any talent or item image for it, and its text writes itself from the numbers. The search box finds a reward by its name or description. SAVE writes the file, and the changes apply at the next game start.
- **Log file**: every confirm, the points preview and how the end went are written to `BepInEx\new-loop-plus.log`.

The button only shows in a loaded run past the tutorial that hasn't already ended. The button and the dialog follow the game's language, English or Simplified Chinese.

With every talent in the talents file switched off, New Loop Plus adds nothing of its own to your save, and you can remove it any time. Loop reward levels the game doesn't have would do nothing without the mod, so turn on `UninstallPrep` before you remove it (the steps are under Uninstall below). Last-Minute Preparation also leaves its door and window buffs, so switch it off and play a run first. Once it's removed, points and costs go back to normal at the next start. A starting bonus that was already paid stays paid. Settings are in [CONFIG.md](CONFIG.md).

## Known issues and limits

- If the game is still paused five seconds after the pause menu closes (another window holding the game paused, for example), the run is not ended and a short message says so. Close that window and try again.
- Points are whole numbers, so changed values are rounded (halves round up). A small reward times a small multiplier can round to 0.
- Crisis and event penalties that take Planning Points away are not changed by the multipliers.
- The talents file is read once at game start. Changes need a restart, including changes saved from the Talents tab.
- Saving from the Talents tab drops the comments in the talents file. The first save keeps the file as it was next to it as `NewLoopPlus.Talents.json.bak`, and later saves leave that copy alone.
- The Talents tab can change the numbers of the five rewards New Loop Plus finished, but not which effects they give.
- The effects of the five talents the game left unfinished were designed for this mod, and their text was rewritten to match what they do now.
- With Seasoned Explorer, a few screens that show Max Exposure before a trip may still show the normal number. The trip itself uses the higher one.
- Endless rewards bought outside endless stay bought when EndlessRewardsEverywhere is switched off again, but most of them then do nothing outside endless.
- The points preview uses the same math the game uses when your character dies. If a value it needs can't be read, the dialog says the death screen will show the points instead.

## Requirements

- Survival Log 1.1.21271. Built and tested on this version. A game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.30.0 or newer. New Loop Plus doesn't load without it. It shows New Loop Plus's messages as toasts, and it applies the points multipliers and the starting bonus you change on the Mod Manager's Mods screen without a restart. Cost settings always apply at the next game start.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73) 0.7.0 or newer, for the Talents tab. Without it, edit the talents file by hand.
- Optional: [Wind Power](https://www.nexusmods.com/survivallog/mods/136) 0.9.3 or newer, for the Wind Engineer and Wind Tuning rewards. Without it, those two rewards are left out of the game and the Talents tab doesn't offer the wind effect.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Copy `NewLoopPlus.dll` into the `BepInEx\plugins` folder inside the game folder (the folder with `SurvivalLog.exe`).

## Uninstall

Loop reward levels the game doesn't have, and the rewards New Loop Plus adds, would do nothing without the mod, and the game's reset wouldn't pay their points back. So do this first:

1. Turn on `UninstallPrep` (on the Mods screen, or in the config file while the game is closed).
2. Load a run. Within a few seconds every loop reward above the normal game's top level goes down to that level, each reward only the mod adds is taken off, and you get back the Planning Points they cost. `BepInEx\new-loop-plus.log` says what was changed.
3. Save the game and quit.

Then delete `NewLoopPlus.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.newloopplus.cfg`, `BepInEx\config\NewLoopPlus.Talents.json`, `BepInEx\config\NewLoopPlus.MuscleMemory.txt` and `BepInEx\new-loop-plus.log`.

## Troubleshooting

Check `BepInEx\LogOutput.log` for a line like `===== New Loop Plus 0.12.3 started`. The same line, plus every confirm, the points preview and how the end went, is in `BepInEx\new-loop-plus.log`, which is kept across restarts.

With any multiplier set, the log also says what changed, such as `survival points now convert at ...` or `... loop reward levels now cost ...`. A per-talent entry it couldn't read is named there too. The talents file adds a line such as `talents file: enabled 1006, 2017; 10 table rows written`, plus a line for each entry it skipped and why.

If End Run doesn't show in the pause menu, make sure you are in a loaded run past the tutorial and that Exiled's Toolkit 0.30.0 or newer is installed, since the Toolkit places the button. If the run didn't end after you confirmed, the message on screen and the log say why.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say hi in the [unofficial modding Discord for Survival Log](https://discord.gg/pDrQtqbZgb). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\new-loop-plus.log`.

To reach me directly, message `exiled.dev` on Discord. You can find me in the [unofficial modding Discord](https://discord.gg/pDrQtqbZgb) and the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so you need a game install with BepInEx set up and started once. Those assemblies come from the game and are not part of this repo. You also need the .NET 8 SDK.

```
dotnet build src/NewLoopPlus -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to change. The output DLL is at `src\NewLoopPlus\bin\Release\NewLoopPlus.dll`.

The unit tests cover the parts that don't need the game (the points preview, the confirm-then-end flow, the page messages, the texts, the points and cost multipliers, the starting bonus, and reading and applying the talents file):

```
dotnet test tests/NewLoopPlus.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
