# New Loop Plus

End a run whenever you like and go straight to the death screen, so you can start a new loop without waiting for your character to die. Tune how many Planning Points a run earns and what loop rewards cost.

## Features

- **End Run button**: the pause menu gets an **End Run** button, right above Save & Exit.
- **Always asks first**: clicking it opens a confirm dialog. Nothing ends until you click **End Run** in that dialog; Cancel or a click outside it closes the dialog and leaves the run alone.
- **Shows what you will get**: in a normal run the dialog shows the Planning Points the death screen will pay, split into survival points, crises and days survived. In Casual mode it says the run gives no Planning Points. In endless and challenge runs it says the run is settled on that mode's own results screen.
- **The game's own death**: on confirm the pause menu closes, and as soon as the game runs again the run ends exactly as a death does. The death screen, the settlement and the save are the game's own.
- **Planning Points multipliers**: one overall setting, plus one each for the three parts a run pays: survival points, crises and events, and days survived. The death screen, its breakdown and the End Run preview all show the scaled points.
- **Starting bonus**: extra Planning Points at the start of each new loop, paid as the loop reward screen opens after a run, so you can spend them right there.
- **Endless starting points**: a multiplier for the 500 Planning Points a new endless game starts with.
- **Change it in game**: with Exiled's Toolkit installed, the points multipliers and the starting bonus can be changed on the Mod Manager's Mods screen and apply at once.
- **Loop reward costs**: one multiplier for what every loop reward upgrade costs, plus your own multiplier for single talents by ID. The upgrade screen shows, checks and charges the new price, and its reset refunds what you paid.
- **Talents file**: `BepInEx\config\NewLoopPlus.Talents.json` lets you change loop rewards by hand: costs, buffs, text, extra levels, and new talents copied from existing ones. It ships with ten talents the game has in its data but never offers, all switched off. Five work as the game left them (Driver, No-Till Soil, Trailblazer, Liaison, Radio Station). Five were unfinished and get new effects from the mod (Well-Connected, Last-Minute Preparation, Resourceful, Yoga Master, Shut-In). See [CONFIG.md](CONFIG.md#talents-file).
- **Thirty-four new loop rewards**, also in the talents file and switched off. Eighteen use effects the game already has, such as Prepper, Insulated Pack, Field Kit, Old Friend (start with an HK-1 robot), Brewmaster, Hardened Defenses, Long-Range Scavenging, Trade Routes, Quick Hands and Generator Tuning. Sixteen have effects made by the mod: Interest, Safety Net, Extra Pick, Crisis Veteran, Second Wind, Lucky Finds, Muscle Memory, Scout's Notes, Tinkerer, Family Recipe, Drone Pilot, Good Neighbor (more affinity per basket), Seasoned Explorer (higher Max Exposure), Dealer Discount (cheaper vehicles) Shortcuts (faster travel before the disaster) and Fast Rotors (faster drone trips). The full list is in [CONFIG.md](CONFIG.md#talents-file).
- **Five more levels for seventeen loop rewards**, also in the talents file and switched off: Early Riser, Savings Account, Encumbrance, Clearance, Bulk Bargains, Hoarding Habit, Quick Fortification and Gratitude go to ten levels, and Resourceful Management, Interior Decorator, Energy Saver, Bargaining, Deep Freeze, Long Harvest and Cold-Resistant Constitution to eight. Mental Toughness goes to ten, and its new levels can be used two to four times. Mold Master goes to eight and is spread out to reach 100% at the top, so its levels 2 and 3 are weaker than the game's.
- **Endless rewards everywhere**: an option, off by default, that offers the endless mode loop rewards in every mode and makes them work there as they do in endless. The camp rewards and Cartographer stay endless only.
- **Talents tab**: with the Mod Manager 0.7.0 or newer, New Loop Plus's page gets a Talents tab that edits the talents file for you. Change what any loop reward costs, how many levels it has, its numbers and its text; switch the file's rewards on or off; or click NEW REWARD to build your own from the effects the game's rewards use and New Loop Plus's own, with any talent or item image and text that writes itself from the numbers. SAVE writes the file, and the changes apply at the next game start.
- **Log file**: every confirm, the points preview and how the end went are written to `BepInEx\new-loop-plus.log`.

The button only shows in a loaded run past the tutorial that has not already ended. The button and the dialog follow the game's language, English or Simplified Chinese.

With every talent in the talents file switched off, New Loop Plus adds no data of its own to your save. A switched-on talent can leave its levels, and with Last-Minute Preparation its door and window buffs, in the save; switch it off and play a run before removing the mod. You can remove it at any time; points and costs go back to the game's own at the next start. A starting bonus already paid stays paid. Settings are in [CONFIG.md](CONFIG.md).

## Known issues and limits

- If the game is still paused five seconds after the pause menu closes (another window holding the game paused, for example), the run is not ended and a short message says so. Close that window and try again.
- Points are whole numbers, so scaled values are rounded (halves round up). A small reward times a small multiplier can round to 0.
- Crisis and event penalties that take Planning Points away are not scaled.
- The talents file is read once at game start. Changes need a restart, including changes saved from the Talents tab.
- Saving from the Talents tab drops the comments in the talents file. The file as it was is kept next to it as `NewLoopPlus.Talents.json.bak`.
- The Talents tab can change the numbers of the five rewards New Loop Plus finished, but not which effects they give.
- The talents that were unfinished in the game are the mod's own design, and their text was rewritten to match what they now do.
- With Seasoned Explorer, a few screens that show Max Exposure before a trip may still show the game's own number; the trip itself uses the higher one.
- Endless rewards bought outside endless stay bought when EndlessRewardsEverywhere is switched off again, but most of them then do nothing outside endless.
- The points preview is worked out the way the game settles a death. If a value it needs cannot be read, the dialog says the death screen will show the points instead.

## Requirements

- Survival Log 1.1.19611. Built and tested on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- Optional: [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, the points multipliers and the starting bonus changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start. With 0.10.0 or newer, its UI size setting also sizes the mod's short messages on screen. Cost settings always apply at the next game start.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73) 0.7.0 or newer, for the Talents tab. Without it, edit the talents file by hand.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Optional, to change settings without a restart: install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Copy `NewLoopPlus.dll` into the `BepInEx\plugins` folder inside the game folder (the folder with `SurvivalLog.exe`).

## Uninstall

Delete `NewLoopPlus.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.newloopplus.cfg`, `BepInEx\config\NewLoopPlus.Talents.json`, `BepInEx\config\NewLoopPlus.MuscleMemory.txt` and `BepInEx\new-loop-plus.log`.

## Troubleshooting

Check `BepInEx\LogOutput.log` for `===== New Loop Plus 0.9.0 started`. The same line, plus every confirm, the points preview and how the end went, is in `BepInEx\new-loop-plus.log`, which survives restarts.

With any multiplier set, the log also says what was scaled, such as `survival points now convert at ...` or `... loop reward levels now cost ...`. A per-talent entry it could not read is named there too. The talents file adds a line such as `talents file: enabled 1006, 2017; 10 table rows written`, plus a line for each entry it skipped and why.

If End Run does not show in the pause menu, make sure you are in a loaded run past the tutorial. If the run did not end after you confirmed, the message on screen and the log say why.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\new-loop-plus.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/NewLoopPlus -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\NewLoopPlus\bin\Release\NewLoopPlus.dll`.

The unit tests cover the pure rules (the points preview, the confirm-then-end flow, the page messages, the texts, the points and cost multipliers, the starting bonus, and reading and applying the talents file) and need nothing from the game:

```
dotnet test tests/NewLoopPlus.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
