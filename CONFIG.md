# New Loop Plus settings

Settings live in `BepInEx\config\com.dev.survivallog.newloopplus.cfg`, created the first time the game starts with the mod. Change it in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Points, Endless and Loop settings apply at once, except `EndlessRewardsEverywhere`; Costs settings apply at the next game start. Delete the file to get the defaults back. Every multiplier goes from 0 to 100, and 1 is the game's own amount.

## Points

| Setting | Default | What it does |
| --- | --- | --- |
| `OverallMultiplier` | `1` | Multiplies every Planning Point a run earns, on top of the three settings below. |
| `SurvivalMultiplier` | `1` | Multiplies the Planning Points that survival points turn into when a run ends. |
| `CrisisMultiplier` | `1` | Multiplies the Planning Points that crises and events reward, both during the run and at its end. Penalties stay as they are. |
| `RecordMultiplier` | `1` | Multiplies the Planning Points paid for each survived day. |

Each part uses its own setting times `OverallMultiplier`, so `OverallMultiplier = 2` with `SurvivalMultiplier = 1.5` pays 3 times the survival part.

## Endless

| Setting | Default | What it does |
| --- | --- | --- |
| `StartingPointsMultiplier` | `1` | Multiplies the Planning Points a new endless game starts with (500 in the game). 0 starts with none. `OverallMultiplier` does not apply to it. |

## Loop

| Setting | Default | What it does |
| --- | --- | --- |
| `StartingBonus` | `0` | Extra Planning Points paid at the start of each new loop, as the loop reward screen opens after a run, so they can be spent there. Goes from 0 to 100000. 0 pays none. |
| `EndlessRewardsEverywhere` | `false` | Offers the endless mode loop rewards in every mode, and makes them work there as they do in endless. The camp rewards (Camp Rescuer, Liaison, Radio Station and the rest of the camp list) and Cartographer stay endless only. Applies at the next game start. |

## Costs

| Setting | Default | What it does |
| --- | --- | --- |
| `CostMultiplier` | `1` | Multiplies what every loop reward upgrade costs in Planning Points. 0 makes upgrades free. |
| `TalentCostOverrides` | (empty) | Gives single loop rewards their own cost multiplier in place of `CostMultiplier`, as `talentId:multiplier` pairs separated by commas, such as `1001:0.5, 2017:2`. An entry that cannot be read is skipped and named in `BepInEx\new-loop-plus.log`. |

Survival plans, bought with survival points during a run, are not changed by these settings.

## Talents file

With the Mod Manager 0.7.0 or newer, the Talents tab on New Loop Plus's page edits this file for you, and SAVE writes it. Saving drops the comments; the file as it was is kept as `NewLoopPlus.Talents.json.bak`. Saved changes apply at the next game start.

`BepInEx\config\NewLoopPlus.Talents.json` is written the first time the game starts with the mod. It is read once at game start, so edit it while the game is closed. Delete it to get the defaults back. Comments (`//`) and trailing commas are allowed. An entry that cannot be read is skipped and named in `BepInEx\new-loop-plus.log`; the rest still apply. A file that is not valid JSON applies nothing.

Every talent in the default file starts switched off. Set `"enabled": true` on one to add it to the loop rewards.

| Talent | ID | What it does |
| --- | --- | --- |
| Driver | 1006 | As the game left it: buy vehicles, shorter travel time. |
| No-Till Soil | 2017 | As the game left it: replant without tilling. |
| Trailblazer | 3076 | As the game left it: Planning Points for each crisis overcome. |
| Liaison | 4004 | Endless only, as the game left it: three camps call for aid once more. |
| Radio Station | 4006 | Endless only, as the game left it: aid requests are announced a day early. |
| Well-Connected | 1008 | Before the disaster, 1 / 1 / 2 / 2 / 3 rare medicines are left in your home's cabinets. |
| Last-Minute Preparation | 1010 | Your home's doors and windows get +100 / +200 / +300 / +400 / +500 Max Durability. |
| Resourceful | 2004 | Before the disaster, 3 / 5 / 7 / 9 / 12 raw materials salvaged from junk are left in your home's cabinets. |
| Yoga Master | 2006 | While you have Vim and Vigor, Satiety drops 10 / 15 / 20 / 25 / 30% slower. |
| Shut-In | 2007 | While you are Lonely, you recover 1 Morale every 5 / 4 / 3 / 2 / 1 hours. |
| Early Riser | 1001 | Five more levels: 8 / 9 / 10 / 12 / 14 more hours before the disaster. |
| Savings Account | 1002 | Five more levels: $700 / $800 / $900 / $1000 / $1200 more money before the disaster. |
| Encumbrance | 1003 | Five more levels: 3.5 / 4 / 4.5 / 5 / 6 kg more shopping weight. |
| Clearance | 1004 | Five more levels: 35 / 40 / 45 / 50 / 55% off supplies that keep less than 5 days. |
| Bulk Bargains | 1005 | Five more levels: 30 / 35 / 40 / 45 / 50% off when buying 6 or more. |
| Hoarding Habit | 1007 | Five more levels: 9 / 10 / 11 / 12 / 15 random food items left in your home's cabinets. |
| Resourceful Management | 1011 | Five more levels: a loan limit of 2400 / 3000 / 3800 / 4600 / 5500. |
| Interior Decorator | 1012 | Five more levels: 20 / 25 / 30 / 35 / 40% off furniture. |
| Quick Fortification | 2005 | Five more levels, each as much stronger as the game's levels are. |
| Gratitude | 2008 | Five more levels: a fulfilled wish's status lasts 15 / 18 / 21 / 24 / 30 hours longer. |
| Mental Toughness | 2010 | Five more levels, with more than one use: +80 Morale twice, +90 twice, +90 three times, +100 three times, +100 four times. Each use also gives Tranquility. |
| Energy Saver | 2011 | Five more levels: power use 25 / 30 / 35 / 40 / 50% lower. |
| Bargaining | 2013 | Five more levels: strangers value your goods 40 / 50 / 60 / 80 / 100% higher. |
| Deep Freeze | 2014 | Five more levels: food spoils 55 / 65 / 75 / 85 / 90% slower in the Refrigerator. |
| Mold Master | 2015 | Eight levels in all: 15 / 30 / 45 / 60 / 70 / 80 / 90 / 100% less quality penalty from spoiled ingredients. The game's level 3 already takes off all of it, so levels 2 and 3 are weaker than the game's. |
| Long Harvest | 2016 | Five more levels: the harvest window lasts 130 / 160 / 200 / 250 / 300% longer. |
| Cold-Resistant Constitution | 2018 | Five more levels: cold waves take 55 / 60 / 65 / 70 / 75% less Satiety, Morale and Vitality. |
| Prepper | 5101 | +1 / +2 / +3 hours before the disaster, +0.5 / +1 / +1.5 kg shopping weight. |
| Insulated Pack | 5102 | Backpack items keep 2 / 3 / 4 times as long. |
| Quick Rest | 5103 | Resting restores stamina 10 / 20 / 30% faster. |
| Every Crumb Counts | 5104 | The Ration Press keeps 5 / 10 / 20% more nutrition (60% in the game). |
| Field Kit | 5105 | Start each run with 6 / 10 / 14 random crafting materials, seeds and medicine. |
| Solar Engineer | 5106 | Solar panels make 15 / 30 / 45% more power. |
| Trapper | 5107 | Traps catch prey 10 / 20 / 30% more often. |
| Quick Learner | 5108 | Production XP +25 / +50 / +75%. |
| Old Friend | 5109 | Start with an HK-1 sorting robot and its full set of blueprints. |
| Light Footed | 5110 | Exposure builds 10 / 20 / 30% slower while exploring. |
| Interest | 5111 | As each new loop starts, 5 / 10 / 15% interest on the Planning Points kept from the last one. |
| Safety Net | 5112 | A run that pays fewer than 100 / 200 / 300 Planning Points is topped up to it as the next loop starts. |
| Extra Pick | 5113 | +1 / +2 rewards to choose from at the end of each day. |
| Crisis Veteran | 5114 | Crises and events reward 10 / 20 / 30% more Planning Points. |
| Second Wind | 5115 | Once / twice per run, running out of Life leaves you at 30 Life instead. |
| Lucky Finds | 5116 | Searching a container has a 10 / 20 / 30% chance to turn up one more of what you find. |
| Muscle Memory | 5117 | Each run starts with 25 / 50% of the skill levels the last run gained, kept in `BepInEx\config\NewLoopPlus.MuscleMemory.txt` (delete it to start over). |
| Scout's Notes | 5118 | Start each run with 1 / 2 / 3 drone trading posts already found. |
| Tinkerer | 5119 | Crafting at a workbench has a 5 / 10 / 15 / 20 / 30% chance to give one of the materials back. |
| Family Recipe | 5120 | Fresh ingredients and exact recipes add +3 / +6 / +9 / +12 / +15 more cooking quality (+10 each in the game). From level 3, multi-pot cooking unlocks at Cooking Level 1 instead of 3. |
| Brewmaster | 5121 | Brewing takes 10 / 20 / 30 / 40 / 50% less time. At levels 3 and 4 each bottle needs 10% less Satiety, at level 5 20% less, so a brew gives more bottles. |
| Drone Pilot | 5122 | Trade drone trips take 50 / 45 / 40 / 35 / 30 minutes (60 in the game). Level 3 gives one more trade a day, level 5 two more. |
| Hardened Defenses | 5123 | Zombies do 10 / 20 / 30 / 40 / 50% less bite damage to doors, windows and defenses. |
| Long-Range Scavenging | 5124 | The drone can go scavenging 1 / 2 / 3 / 4 / 5 more times a day (once in the game). |
| Trade Routes | 5125 | The trade drone can make 1 / 2 / 3 / 4 / 5 more trades a day (once in the game). Adds to Drone Pilot. |
| Packed Fresh | 5126 | Backpack food keeps 1.1 / 1.2 / 1.3 / 1.4 / 1.5 / 1.6 / 1.7 / 1.8 / 1.9 / 2.0 times as long. Adds to Insulated Pack and the Insulated Backpack survival plan. |
| Quick Hands | 5127 | Crafting at a workbench takes 5 / 10 / 15 / 20 / 25 / 30 / 35 / 40 / 45 / 50% less time. |
| Good Neighbor | 5128 | Each basket sent to the neighbor or your companion gives 1 / 2 / 3 / 4 / 5 more affinity (1 in the game). |
| Solar Tuning | 5129 | Solar panels make 5 / 10 / 15 / 20 / 25 / 30 / 35 / 40 / 45 / 50% more power. Adds to Solar Engineer. |
| Generator Tuning | 5130 | Generators make 5 / 10 / 15 / 20 / 25 / 30 / 35 / 40 / 45 / 50% more power. |
| Seasoned Explorer | 5131 | Max Exposure while exploring is 50 / 100 / 150 / 200 / 250 / 300 / 350 / 400 / 450 / 500 higher, so trips out can last longer. |
| Dealer Discount | 5132 | Vehicles cost 5 / 10 / 15 / 20 / 25 / 30 / 35 / 40 / 45 / 50% less (bicycle 150, car 500, SUV 900 in the game). Selling one back pays less to match. |
| Shortcuts | 5133 | Travel before the disaster takes 5 / 10 / 15 / 20 / 25 / 30 / 35 / 40 / 45 / 50% less time, with or without a vehicle. Adds to the vehicle's own cut. |

A talent entry takes:

| Key | What it does |
| --- | --- |
| `id` | The talent ID. Its level rows are `id * 10 + level`, so a talent has at most 10 levels (level 10 takes the next talent's unused level 0 row). |
| `enabled` | `true` (the default when left out) or `false`. A switched-off entry changes nothing, its buffs and effects included. |
| `copyFrom` | For a talent ID the game does not have: the talent whose rows the new one starts from. |
| `name`, `description`, `levelText` | Text, either `"plain"` or `{ "en": "...", "zh": "..." }`. A missing language shows the other one. |
| `fields` | Any talent table field for every level, such as `{ "ShowType": 2 }`. |
| `levels` | One `{ }` per level, each with optional `cost`, `buff`, `description`, `levelText` and `fields`. More levels than the game has adds levels copied from the highest one; fewer hides the rest. |
| `buffs`, `effects` | Buff and effect table rows this talent changes, or makes when given a `copyFrom`. Each has an `id`, an optional `copyFrom` and `fields`. |
| `reinforce` | `[{ "talentBuff": A, "doorWindowBuff": B }]`: while you have buff A, every door and window of your home gets buff B. |
| `gives` | What each level gives: a list of `{ "effect": "game:<script>", "choice": { }, "levels": [{ }, ...] }`. `effect` names an effect script one of the game's talents runs, or one of New Loop Plus's own effects as `nlp:<name>` (`interest`, `safety-net`, `extra-pick`, `crisis-veteran`, `second-wind`, `lucky-finds`, `muscle-memory`, `scouts-notes`, `tinkerer`, `family-recipe`, `drone-pilot`; the default file shows the fields each one reads); `choice` holds fields that stay the same at every level, and `levels` one `{ }` of fields per level (a level past the end uses the last). Each give gets its own buff and effect rows, so the talent ID must be 1000 to 9999 and a talent can have at most 10 gives. A level's `buff` is ignored when the talent has gives. |

The `buffs` and `effects` lists at the bottom of the file always apply. Field names are the game's own table names (`ConfigName`, `IntValue0`, `BuffSatietyDel`, `EffectConfigId` and so on). A cost set here is still scaled by `CostMultiplier` and `TalentCostOverrides`.
