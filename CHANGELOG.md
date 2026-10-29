# Changelog

## 0.10.0

- New: `UiScale` under Interface sets the size of New Loop Plus's short messages on screen on their own, from 75% to 200%, whatever Exiled's Toolkit's UI size is. At 0, the default, they follow the Toolkit's. Needs Exiled's Toolkit 0.11.0 or newer.
- Made for Survival Log 1.1.19611.

## 0.9.2

- Less work when the game opens a screen: End Run now only checks the pause menu when the pause menu itself opens, instead of every time any game screen loads.
- Made for Survival Log 1.1.19611.

## 0.9.1

- Fixed: a drone back from a very short trip (with Drone Pilot or Fast Rotors) stayed invisible on its hub and showed no flights after that. It now shows when it lands. A drone that is invisible now shows again after its next trip, or when you load the save.

## 0.9.0

- Eleven new loop rewards in the talents file, all switched off. Switch them on in the Talents tab or with `"enabled": true` in the file.
- Long-Range Scavenging and Trade Routes: the drone can scavenge or trade up to five more times a day. Five levels each.
- Good Neighbor: each basket sent to the neighbor or your companion gives up to 25 more affinity. Five levels.
- Packed Fresh, Quick Hands, Solar Tuning and Generator Tuning: backpack food keeps up to twice as long, crafting takes up to 50% less time, and solar panels and generators make up to 50% more power. Ten levels each.
- Fast Rotors: every drone trip takes up to 75% less time. Five levels.
- Seasoned Explorer: up to 500 more Max Exposure while exploring. Ten levels.
- Dealer Discount: vehicles cost up to 50% less. Ten levels.
- Shortcuts: travel before the disaster takes up to 50% less time, with or without a vehicle. Ten levels.
- Resourceful Management past level 3: the phone's three bigger loans now go up with your level, up to 3800 / 4600 / 5500 at level 8. Before, they stopped at 1800.
- These stack with the game's survival plans and the mod's other rewards that do the same thing.
- The Talents tab's NEW REWARD can use these effects too.
- Made for Survival Log 1.1.19611.

## 0.8.0

- Five more levels for seventeen loop rewards in the talents file, all switched off. Switch them on in the Talents tab or with `"enabled": true` in the file.
- Early Riser, Savings Account, Encumbrance, Clearance, Bulk Bargains, Hoarding Habit, Quick Fortification and Gratitude go from five levels to ten.
- Resourceful Management, Interior Decorator, Energy Saver, Bargaining, Deep Freeze, Long Harvest and Cold-Resistant Constitution go from three levels to eight.
- Mental Toughness goes to ten levels. Its new levels give 80 to 100 Morale and can be used two to four times.
- Mold Master goes to eight levels, from 15% to 100%. The game's level 3 already reaches 100%, so levels 2 and 3 are weaker than before.
- A loop reward can now have up to ten levels, in the talents file and the Talents tab.
- New setting `EndlessRewardsEverywhere` under Loop, off by default: offers the endless mode loop rewards in every mode, and makes them work there as they do in endless. The camp rewards and Cartographer stay endless only. Applies at the next game start.
- Made for Survival Log 1.1.19611.

## 0.7.0

- Five new loop rewards in the talents file, all switched off. Switch them on in the Talents tab or with `"enabled": true` in the file. Each has five levels.
- Tinkerer: crafting at a workbench has a 5% to 30% chance to give one of the materials back.
- Family Recipe: fresh ingredients and exact recipes add up to 15 more cooking quality. From level 3, multi-pot cooking unlocks at Cooking Level 1 instead of 3.
- Brewmaster: brewing takes up to 50% less time. From level 3, each bottle needs less Satiety, so a brew gives more bottles.
- Drone Pilot: trade drone trips take as little as 30 minutes instead of 60. Level 3 gives one more trade a day, level 5 two more.
- Hardened Defenses: zombies do up to 50% less bite damage to doors, windows and defenses.
- Brewmaster, Drone Pilot and Hardened Defenses stack with the game's Cellar Craft, Not a Drop Wasted, Endurance Drone and Iron Defense survival plans.
- The Talents tab's NEW REWARD can use these effects too.
- Made for Survival Log 1.1.19611.

## 0.6.3

- The short messages New Loop Plus shows on screen can be drawn bigger or smaller with the UI size setting of Exiled's Toolkit 0.10.0 or newer. A change shows at once.
- The Talents tab takes the size of the Mod Manager window it sits in (Mod Manager 0.8.3 or newer). End Run and its dialog keep the game's size, like the rest of the pause menu.
- Made for Survival Log 1.1.19611.

## 0.6.2

- Less stutter: the talent and reward tables are only rewritten when the game reloads them, and the background checks take turns instead of all running in the same frame.

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
