# Changelog

## 0.5.0

- A Talents tab on New Loop Plus's page in the Mod Manager (0.7.0 or newer). It lists every loop reward, and you can change what each one costs, how many levels it has, its numbers, its text, and switch the file's rewards on or off. NEW REWARD makes a reward of your own from the effects the game's rewards use, with an icon, a name, a column and text that writes itself from the numbers. SAVE writes the talents file; changes apply at the next game start.
- The talents file takes `gives` on a talent: the effects each level gives, by the name of a game effect script. The tab writes these for you.
- Saving from the tab drops the comments in the talents file. The file as it was before is kept next to it as `NewLoopPlus.Talents.json.bak`.

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.

## 0.4.0

- A talents file, `BepInEx\config\NewLoopPlus.Talents.json`, for changing loop rewards by hand: costs, buffs, text, extra levels and new talents. Read once at game start.
- Ten talents the game never offers are in the file, all switched off: Driver, No-Till Soil, Trailblazer, Liaison and Radio Station as the game left them, and Well-Connected, Last-Minute Preparation, Resourceful, Yoga Master and Shut-In with new effects in place of the unfinished ones.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, the points multipliers and the starting bonus changed on the Mods screen take effect without a restart; without it, at the next game start.
- The Mods screen links this changelog.

## 0.3.0

- A starting bonus for each new loop: extra Planning Points paid as the loop reward screen opens after a run, so you can spend them there.
- A multiplier for the Planning Points a new endless game starts with.
- The Planning Points multipliers and the starting bonus can be changed on the Mod Manager's Mods screen without restarting the game. Cost settings still apply at the next game start.
- The Mods screen shows the mod's name, description, category and links.
- Needs Exiled's Toolkit 0.4.0 or newer.

## 0.2.0

Planning Points multipliers: one overall, plus one each for survival points, crises and events, and days survived. A cost multiplier for loop reward upgrades, with your own multiplier for single talents by ID. Resetting the upgrade screen refunds what you paid.

## 0.1.0

First build. An End Run button in the pause menu ends the run as a death after you confirm, and the confirm dialog shows the Planning Points the death screen will pay.
