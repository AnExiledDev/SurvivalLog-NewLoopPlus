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
