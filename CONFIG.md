# New Loop Plus settings

Settings live in `BepInEx\config\com.dev.survivallog.newloopplus.cfg`, created the first time the game starts with the mod. Change it in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Points, Endless and Loop settings apply at once; Costs settings apply at the next game start. Delete the file to get the defaults back. Every multiplier goes from 0 to 100, and 1 is the game's own amount.

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

## Costs

| Setting | Default | What it does |
| --- | --- | --- |
| `CostMultiplier` | `1` | Multiplies what every loop reward upgrade costs in Planning Points. 0 makes upgrades free. |
| `TalentCostOverrides` | (empty) | Gives single loop rewards their own cost multiplier in place of `CostMultiplier`, as `talentId:multiplier` pairs separated by commas, such as `1001:0.5, 2017:2`. An entry that cannot be read is skipped and named in `BepInEx\new-loop-plus.log`. |

Survival plans, bought with survival points during a run, are not changed by these settings.

## Talents file

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

A talent entry takes:

| Key | What it does |
| --- | --- |
| `id` | The talent ID. Its level rows are `id * 10 + level`, so a talent has at most 9 levels. |
| `enabled` | `true` (the default when left out) or `false`. A switched-off entry changes nothing, its buffs and effects included. |
| `copyFrom` | For a talent ID the game does not have: the talent whose rows the new one starts from. |
| `name`, `description`, `levelText` | Text, either `"plain"` or `{ "en": "...", "zh": "..." }`. A missing language shows the other one. |
| `fields` | Any talent table field for every level, such as `{ "ShowType": 2 }`. |
| `levels` | One `{ }` per level, each with optional `cost`, `buff`, `description`, `levelText` and `fields`. More levels than the game has adds levels copied from the highest one; fewer hides the rest. |
| `buffs`, `effects` | Buff and effect table rows this talent changes, or makes when given a `copyFrom`. Each has an `id`, an optional `copyFrom` and `fields`. |
| `reinforce` | `[{ "talentBuff": A, "doorWindowBuff": B }]`: while you have buff A, every door and window of your home gets buff B. |

The `buffs` and `effects` lists at the bottom of the file always apply. Field names are the game's own table names (`ConfigName`, `IntValue0`, `BuffSatietyDel`, `EffectConfigId` and so on). A cost set here is still scaled by `CostMultiplier` and `TalentCostOverrides`.
