# Changelog

## 0.6.1

- End Run shows the moment the Esc menu opens, instead of popping in a moment later.

## 0.6.0

- Eighteen new loop rewards in the talents file, all switched off. Switch them on in the Talents tab or with `"enabled": true` in the file.
- Ten use effects the game already has: Prepper (more time before the disaster, more shopping weight), Insulated Pack (backpack items keep longer), Quick Rest (faster stamina from resting), Every Crumb Counts (the Ration Press keeps more nutrition), Field Kit (start with random materials, seeds and medicine), Solar Engineer (more solar power), Trapper (traps catch more often), Quick Learner (faster Production skill), Old Friend (start with an HK-1 sorting robot and its blueprints) and Light Footed (exposure builds slower).
- Eight have effects made by New Loop Plus: Interest (Planning Points on what you kept from the last loop), Safety Net (a run that pays too few Planning Points is topped up), Extra Pick (more rewards to choose from at the end of each day), Crisis Veteran (more Planning Points from crises and events), Second Wind (survive running out of Life once or twice per run), Lucky Finds (a chance to find one more of an item when searching a container), Muscle Memory (keep part of the skill levels the last run gained) and Scout's Notes (start with drone trading posts already found).
- Muscle Memory keeps the last run's skill levels in `BepInEx\config\NewLoopPlus.MuscleMemory.txt`. Delete it to start over.
- The Talents tab's NEW REWARD can use all of these effects, plus robots at the start of a run.
- The icon picker in the Talents tab offers every talent and item image in the game, grouped by kind.

## 0.5.0

- A Talents tab on New Loop Plus's page in the Mod Manager (0.7.0 or newer). It lists every loop reward, and you can change what each one costs, how many levels it has, its numbers, its text, and switch the file's rewards on or off. NEW REWARD makes a reward of your own from the effects the game's rewards use, with an icon, a name, a column and text that writes itself from the numbers. SAVE writes the talents file; changes apply at the next game start.
- The talents file takes `gives` on a talent: the effects each level gives, by the name of a game effect script. The tab writes these for you.
- Saving from the tab drops the comments in the talents file. The file as it was before is kept next to it as `NewLoopPlus.Talents.json.bak`.

- After a game update, Mod Manager can tell you if this mod may need an update.

## 0.4.0

- A talents file, `BepInEx\config\NewLoopPlus.Talents.json`, for changing loop rewards by hand: costs, buffs, text, extra levels and new talents. Read once at game start.
- Ten talents the game never offers are in the file, all switched off: Driver, No-Till Soil, Trailblazer, Liaison and Radio Station as the game left them, and Well-Connected, Last-Minute Preparation, Resourceful, Yoga Master and Shut-In with new effects in place of the unfinished ones.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, the points multipliers and the starting bonus you change on the Mods screen take effect right away. Without it, they take effect the next time you start the game.
- Mod Manager's Mods screen links to this changelog.

## 0.3.0

- A starting bonus for each new loop: extra Planning Points paid as the loop reward screen opens after a run, so you can spend them there.
- A multiplier for the Planning Points a new endless game starts with.
- The Planning Points multipliers and the starting bonus can be changed on the Mod Manager's Mods screen without restarting the game. Cost settings still apply at the next game start.
- Mod Manager's Mods screen shows the mod's name, description, category and links.
- Needs Exiled's Toolkit 0.4.0 or newer.

## 0.2.0

- Planning Points multipliers: one overall, plus one each for survival points, crises and events, and days survived.
- A cost multiplier for loop reward upgrades, with your own multiplier for single talents by ID.
- Resetting the upgrade screen refunds what you paid.

## 0.1.0

- First release.
- An End Run button in the pause menu ends the run as a death after you confirm.
- The confirm dialog shows the Planning Points the death screen will pay.
