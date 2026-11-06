# Shrine quests, blessings and dreams

A plan for a new way to earn deity points and a new use for the shrines. Trophy tributes go away.
Points come from deeds in the world instead: rare luck on hard kills, crafts and harvests, and a sure
point for the first kill of each boss and miniboss. Each shrine gives its biome's boss as a quest,
and once that is done, a short daily blessing. A deity a player is devoted to can also send a dream
that lends a stronger boon for a while. All seven build steps are built and compile; none has been tried
in game yet. Where the build differs from or adds to the plan, the section says so.

## Decisions taken

| Question | Decision |
| --- | --- |
| Tributes | Removed. Shrines give quests and blessings, and keep the star map reset and Ascension. |
| A run | The time since the player last Ascended. Everything "per run" is cleared on Ascension, as Soul of Power claims are today. |
| Quest cycle | Repeatable: a deity's next quest can be taken a few in-game days after the last (default 5), each worth 1 point. The first one done in a run unlocks that shrine's daily blessing, and both stay on offer after. |
| "Current biome level" | The biome after the last boss the player has beaten. |
| Lucky limit | A setting, `LuckyPointsPerDeity`: 5 lucky points per deity per run by default, shared by kills, crafts and harvests. |
| Lucky craft's deity | A random deity whose biome the player has discovered. |
| First boss kill | The Soul of Power delivers it, worth 1 point. Claiming it sets a player key for that boss. A soul rises only when a player nearby lacks that key, and only players without it can claim it. |
| Boons | Blessings and dreams lend existing star map powers for a while, through `PowerSet`, so they need no new power code. |

## Points at a glance

| Source | Deity | Points | Limit |
| --- | --- | --- | --- |
| Starred kill (see below) | Of the biome it died in | 5% chance of 1 | Lucky limit |
| Craft or upgrade | Random, among discovered biomes | 1% chance of 1 | Lucky limit |
| Harvest: picking, or felling a tree or breaking a rock or deposit | Of the biome it was in | 0.2% chance of 1 | Lucky limit |
| First kill of a miniboss | The miniboss's | 1 | Once per miniboss per run |
| First kill of a boss | The boss's, by its Soul of Power | 1 | Once per boss per run |
| Shrine quest | The shrine's | 1 | One per deity every 5 in-game days |
| Ascension | The deity Ascended with | 3 | Unchanged |

Tributes gave 8 to 10 points per deity per run. The new sources give about 1 from the soul, up to 5
from luck, one from each quest done, and sometimes 1 from a miniboss, so a run that keeps going back
to the shrines comes out close. Every number is a setting and a first guess, to tune after play.

## Deeds

### Who gets the credit

- **Kills** are seen only on the creature's owner, in `Character.OnDeath`. The owner sends a new
  routed RPC, `AL_Deed`, to every player who damaged it. Vanilla already marks each player who hits a
  creature on its ZDO (`ZDOVars.s_attackers` plus their name), which is how it registers a kill for
  every attacker, so the same marks say who took part. For bosses and minibosses the owner also sends
  it to every player within `SoulReclaimRange`. Each player then applies the rules with their own save.
- **Picking** happens on the picker's machine, in `Pickable.Interact`, with the same one-roll-per-plant
  guard `GatheringPowers` uses.
- **Trees, logs, rocks and deposits** break on their owner. The owner sends `AL_Deed` to whoever landed
  the breaking hit. It counts once per whole tree, log, rock or deposit, not per piece.
- **Crafting** happens on the crafter's machine, inside `InventoryGui.DoCrafting`, which `CraftPowers`
  already brackets.

`KillPowers` keeps its own `AL_Kill` RPC; the two don't need to merge.

- Only kills that can matter are sent: bosses, listed minibosses, and starred kills that pass the rules
  below. Breaking hits go out as a second RPC, `AL_Harvest`, carrying only where it happened.
- A player killed in the fight still counts. While their body lies there, the game saves it with what
  the kill wrote. After that, until they respawn, kills are held for a few minutes and counted when they
  are back.
- `al-deeds` shows a player's biome level, lucky points per deity and minibosses this run.
  `al-deeds chance <percent>` tries every lucky chance at that percent until `al-deeds chance off`.

### Starred kills

A kill can give a point when all of these hold:

- It has at least `StarredKillMinStars` stars (default 2, which is vanilla's most). With a star mod,
  more stars count the same.
- `Character.IsMonsterFaction` is true, which leaves out passive animals, Dvergr and anything tamed.
- It has no `Tameable`, so wild wolves, boars, lox and asksvin don't count either.
- It isn't a boss or a listed miniboss, which have their own rewards.
- The biome where it died is at or past the player's biome level.

| Bosses beaten | Biome level |
| --- | --- |
| None | Meadows |
| Eikthyr | Black Forest |
| The Elder | Swamp |
| Bonemass | Mountains |
| Moder | Plains |
| Yagluth | Mistlands |
| The Queen | Ashlands |
| Fader | Deep North |

- A boss counts as beaten when its key is held, as a world key or the player's own (VentureValheim's
  private keys). This is the check `SacrificeUI.HasKeys` already makes.
- Ascension takes the keys away, so the level drops back to Meadows with the new run.
- Dungeon interiors use the biome on the surface above them.
- The ocean has no deity, so sea kills give nothing (see open questions).

### Crafting and harvesting

- Each craft or upgrade rolls `CraftChance` (1%). The deity is picked at random from those whose
  biome the player has discovered (`Player.m_knownBiome`) and that can still give lucky points this
  run. A craft the game marks as cheated, as with `nocost`, brings no luck.
- Each harvest rolls `HarvestChance` (0.2%), for the deity of the biome it was in. Picking covers
  crops, berries, mushrooms, herbs and flowers, but not treasure or loose materials such as flint,
  stone and branches (the same list Bounty of the Lands skips). A tree counts once when it is felled,
  and a rock or ore deposit once when its last piece breaks; logs, stumps and other breakables don't.
  Harvests are far more common than crafts, so their chance is lower.
- A crafted item counts at the end of `InventoryGui.DoCrafting`, where the game counts its
  `CraftsOrUpgrades` stat.

### The lucky limit

- Each deity can give `LuckyPointsPerDeity` lucky points per run, from the three sources together.
  It defaults to 5, and 0 turns lucky points off. Once it's reached, that deity's rolls stop until the
  next run.
- A lucky point shows a centre message ("Gefjun favours your harvest: +1 point") and the deity's burst.
- The shrine header shows how many the player has had ("Gefjun has rewarded 2 of 5 lucky deeds this run").

### Minibosses

A new `Minibosses` setting, in the same shape as `SoulBosses`: `prefab=Deity` pairs, where `+` joins
creatures that count as one.

| Miniboss | Prefab | Deity |
| --- | --- | --- |
| Brenna | `Skeleton_Hildir` | Vör |
| Geirrhafa | `Fenring_Cultist_Hildir` | Skaði |
| Zil and Thungr | `GoblinShaman_Hildir+GoblinBruteBros` | Syn |
| Fallen Valkyrie | `FallenValkyrie` | Hel |

- The first kill of each one in a run gives `MinibossPoints` (1) to its deity, with a message and burst.
- Vanilla has nothing that fits for the Meadows, Swamp or Mistlands. The Deep North is open until its
  creatures are known.

### Bosses

The Soul of Power stays as the first-kill reward, and each player can earn it once per boss per run.

- **The record is a player key.** Claiming a boss's soul gives the player the key `al_claimed_<bosskey>`
  (for example `al_claimed_defeated_eikthyr`) with `Player.AddUniqueKey`. The game saves it with the
  character, so it replaces the `ClaimedSouls` list in `LineageSave`. Admins can see and change it with
  the game's own `listkeys`, `setkeyplayer` and `removekeyplayer` commands.
- **Other machines can't see player keys**, since the game keeps them on the player's own machine. Each
  player publishes the soul keys they hold on their ZDO (`ClaimsZKey`, as `ClaimedSouls` is today).
  The published list follows the keys whenever they change, including through the game's key commands.
- Claims are matched exactly with the game's `HaveUniqueKey`, not the prefix match the shrine uses
  for boss keys.
- **A soul rises only when someone needs it.** When a mapped boss dies, its owner looks at every player
  within `SoulReclaimRange`. If all of them hold the boss's key, no soul rises. If any of them lacks it,
  one does. This replaces today's rule, which raises a soul on the boss's first death in the world or
  for a nearby Ascended player who is owed one. The `al_soul_*` world key is no longer needed.
- **Only players without the key can claim it.** A soul that rose for one player gives nothing to
  another player who already holds the key. It shows dimmed for them, as a claimed soul does today.
- **Ascension removes the player's `al_claimed_*` keys**, so every soul can be earned again in the next run.
- `PointsPerSoul` goes from 2 to 1 by default, to match "a guaranteed point".
- A player who wasn't there for the world's first kill can now earn the point by killing the boss again.

## Shrine quests

- A shrine's quest is to slay its biome's boss: the boss `SoulBosses` maps to that deity. Gefjun asks
  for Eikthyr, Vör for the Elder, and so on, and Víðarr for Kall Fimbulbringer's last stage.
- The player accepts it at the shrine ("Gefjun asks: slay Eikthyr"). It has no time limit.
- It is done when the boss dies while the player has hit it or is within `SoulReclaimRange`, which the
  `AL_Deed` boss report covers. That gives `QuestPoints` (1), a message and the deity's burst.
- A boss killed before the quest was accepted doesn't count.
- A player has at most one quest per deity, but quests from different deities can run together.
- The next quest for a deity can be taken `QuestCooldownDays` (5) in-game days after the last one was
  taken. An in-game day is 20 real minutes. The clock starts at accepting, so when a quest takes longer
  than that, the next is ready as soon as it's done.
- A status icon with no timer shows each quest under way, so the player remembers it.
- A player killed in the fight still completes it. While their body lies there the game saves it with
  the quest done. After that, until they respawn, the kill is held for a few minutes and counted when they are back.
- `AL_Deed` carries the creature's prefab, stars, where it fell and its boss key.
- The shrine shows the boss by the game's name for it, found from its defeat key or prefab name.
- `QuestCooldownDays` can be 0, which gives the next quest as soon as the last is done.
- If no boss is mapped to a deity, its shrine has no quest and its blessing is open from the start.
- Days are `EnvMan.instance.GetDay()`, saved with the world's id. A character who moves to another
  world finds every cooldown open there, rather than locked by a day number from elsewhere.

## Blessings

- A deity's blessing opens once its quest has been done this run.
- It can be taken once per in-game day per deity, and lasts `BlessingMinutes` (10).
- Blessings from different shrines can be active together.
- It is free by default. The data has an optional `offering` per deity for a server that wants a cost.
- By the time a blessing opens its boss is dead, so each one helps with going back to that biome: silver
  runs, obsidian, black metal, sap and the like. Each is worth about one major star of its powers.

| Deity · biome | Blessing | Powers |
| --- | --- | --- |
| Gefjun · Meadows | Blessing of the Plough | +100 carry weight; 10% chance of extra wood, stone and ore; 10% chance of a double harvest |
| Vör · Black Forest | Vör's Sight | Ore within 50 m shows on the map; +30% explore radius; +10% sneak |
| Baldur · Swamp | Baldur's Purity | +20% fire and poison resistance; wet lasts 50% less; +15% damage to undead |
| Skaði · Mountains | Skaði's Mantle | Immune to cold and freezing; +15% frost resistance; sprinting costs 10% less stamina |
| Syn · Plains | Syn's Ward | +15% block power; +20% stagger threshold; +6 armor |
| Freyja · Mistlands | The Falcon Cloak | Glide while falling; jumping costs 25% less stamina; +15% eitr regen |
| Hel · Ashlands | Hel's Embrace | +25% heat resistance; +20% fire resistance |
| Víðarr · Deep North | Víðarr's Stride | Deep snow slows you 50% less; cold slows your regen 50% less; +15% frost resistance |

Powers used: `CarryWeight`, `GatherYield`, `HarvestBounty` · `OreSense`, `ExploreRadius`, `Stealth` ·
`FireAndPoisonResist`, `WetResist`, `UndeadDamage` · `ColdImmunity`, `FrostResist`, `RunStamina` ·
`BlockPower`, `StaggerThreshold`, `Armor` · `FalconGlide`, `JumpStamina`, `EitrRegen` ·
`HeatResistance`, `FireResist` · `SnowStride`, `ColdEndurance`, `FrostResist`.

## Dreams

- A deity can send a dream once the player has earned more than 3 of its points.
- Each time the player sleeps with no dream active, there is an `InspirationChance` (15%) chance of
  one. The deity is picked from those eligible, weighted by points earned, so the one a player is most
  devoted to comes most often.
- The deity's dream replaces the vanilla dream text that night, from a prefix on
  `SleepText.ShowDreamText`, with a line such as "A falcon carries you above the mist. Freyja's fury
  stays with you."
- On waking, the player has that deity's inspiration for `InspirationMinutes` (30). Only one can be
  active at a time.
- An inspiration is moderately strong: about an oath-bound star's worth of one of the deity's aspects,
  without the oath.
- The dream is chosen after the game has picked its own dream text, so a boss's dream cinematic still
  plays and no dream is chosen that night. The inspiration starts in `Player.SetSleeping` on waking.
- Each inspiration has a `dream` line, shown as `name` is. One without a line, or whose translation is
  missing, shows "Freyja comes to you in a dream."
- `al-dream` makes the next sleep bring a dream whatever the chance, and lists the deities that can send one.

| Deity | Dream | Powers |
| --- | --- | --- |
| Gefjun | The Long Table | +15% food health, stamina and eitr; 20% chance of a double harvest |
| Vör | The Knowing | +30% skill XP; +5% crit chance |
| Baldur | The Shining | +20% block power; +20% health regen; +20% damage to undead |
| Skaði | The Huntress | +15% bow damage; +15% draw speed; sprinting costs 15% less stamina |
| Syn | The Threshold | +40% stagger threshold; +12% melee damage |
| Freyja | Sessrúmnir | +20% stamina regen; +20% eitr regen; +12% magic damage |
| Hel | Hel's Harvest | 2% life leech; +20% damage to enemies under 30% health |
| Víðarr | The Silent Vow | +20% unarmed damage; cold slows your regen 30% less |

Each deity's inspirations are a list in the data, and one is picked at random, so a server can add more.
No boon shares a name with a star, so the compendium's list of where a power comes from stays clear.

## How boons work

Blessings and inspirations are both boons: a deity, an id, and a set of powers with values, named as
in the star maps.

- `PowerSet.Rebuild` takes the active boons' powers along with the allocated stars. A boon's grants
  count as a major star's, so they switch mechanics on. They add to what the player's stars give, and
  the usual caps still apply.
- `PowerSet.Preview` takes them too, so the star map's before and after totals match what the player has.
- Active boons are saved in `LineageSave` with the time left. They count down only while playing, so
  logging out pauses them, and they last through death.
- Each shows a status effect icon (the deity's rune) with the time left and its powers in the tooltip.
  The icon is only for show; the effect comes from `PowerSet`, rebuilt when a boon starts or ends.

## Data

`Tributes.yaml` becomes `Shrines.yaml`, `TributeData` becomes `ShrineData`, and `UseTributesFile`
becomes `UseShrinesFile`. Aesir Lineage hasn't shipped, so nothing needs migrating. Tribute steps are
removed. Per deity:

```yaml
Gefjun:
  resetCost:
    TrophyEikthyr: 1
  ascension:
    items:
      TrophyEikthyr: 1
    requiredKeys: [defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen, defeated_fader]
    points: 3
  blessing:
    id: gefjun_plough
    name: $al_boon_gefjun_plough
    offering: {}
    powers:
      CarryWeight: 100
      GatherYield: 10
      HarvestBounty: 10
  inspirations:
  - id: gefjun_long_table
    name: $al_boon_gefjun_long_table
    powers:
      FoodBonus: 15
      HarvestBounty: 20
```

- A boon's `id` is saved on the character while it is active, so it must not change.
- A boon's `name` is shown as written, or localized when it starts with `$`, as star names are, so a
  server can add boons without a translation file. The built-in ones use `$al_boon_<id>`.
- Unknown power names are skipped with a warning, as in the star maps.
- An inspiration's `dream` is the line shown that night, shown as `name` is. The built-in ones use
  `$al_boon_<id>_dream`.
- The quest's boss comes from `SoulBosses`, so it isn't listed twice.

## Settings

New server settings, synced by Jotunn like the others:

| Section | Setting | Default | What it does |
| --- | --- | --- | --- |
| Deeds | `LuckyPointsPerDeity` | 5 | Lucky points each deity can give per run; 0 turns lucky points off |
| Deeds | `StarredKillChance` | 5 | Percent chance a qualifying starred kill gives a point |
| Deeds | `StarredKillMinStars` | 2 | Fewest stars a kill needs |
| Deeds | `CraftChance` | 1 | Percent chance per craft or upgrade |
| Deeds | `HarvestChance` | 0.2 | Percent chance per harvest |
| Deeds | `BiomeDeities` | `Meadows=Gefjun,BlackForest=Vor,Swamp=Baldur,Mountain=Skaldi,Plains=Syn,Mistlands=Freya,AshLands=Hel,DeepNorth=Vidarr` | Which deity hears deeds in each biome |
| Deeds | `Minibosses` | the table above | Which creatures are minibosses, and whose |
| Deeds | `MinibossPoints` | 1 | Points for each miniboss's first kill per run |
| Quests | `QuestCooldownDays` | 5 | In-game days between taking a deity's quests |
| Quests | `QuestPoints` | 1 | Points a quest gives |
| Blessings | `BlessingMinutes` | 10 | How long a blessing lasts |
| Blessings | `InspirationChance` | 15 | Percent chance of a dream when sleeping |
| Blessings | `InspirationMinPoints` | 4 | Points earned with a deity before it can send a dream |
| Blessings | `InspirationMinutes` | 30 | How long an inspiration lasts |

Changed: `PointsPerSoul` defaults to 1, and `UseTributesFile` is renamed `UseShrinesFile`.

## Save

`LineageSave` gains:

- **`Run`**, cleared on Ascension: lucky points per deity, the minibosses killed, quests done per deity,
  the quests under way, the day each deity's last quest was taken and the day of its last blessing.
  Days are saved with the world's id.
- **`Boons`**: active blessings and the inspiration, with the time each has left.

`DeityProgress.TributeSteps` goes. `ClaimedSouls` goes too, since the `al_claimed_*` player keys hold
the soul claims now. Character saves are read with a reader that skips fields it doesn't know, so a
removed field doesn't make a character's lineage unreadable. The config files keep the strict reader, so
typos there still show. `PlayerData.ClearSoulClaims` becomes `StartNewRun`, which removes those keys and
clears `Run` together.

## Shrine screen

`SacrificeUI.BuildChoices` lists for the shrine's deity:

1. **The quest.** "Accept: slay Eikthyr" when one can be taken. Otherwise an entry that can't be
   chosen: "Slay Eikthyr (under way)" or "Gefjun's next quest in 3 days".
2. **The blessing.** "Receive the Blessing of the Plough", with its powers and how long it lasts, when
   open. Otherwise an entry that can't be chosen: "Complete Gefjun's quest to earn her blessing",
   "Active, 7 minutes left" or "Gefjun blesses you again tomorrow".
3. **Reset and Ascension**, unchanged.

- The header shows the lucky deeds and quests done this run.
- `ShrineChoice` gains a flag for entries that only inform, which hide their toggle.
- The "Sacrifice" wording on the panel and its button becomes shrine wording. Hovering a shrine shows
  whose it is ("Shrine of Gefjun").
- `SacrificeUI` keeps its name: the shrine prefabs in the asset bundle point at the component by name.

## Code

| File | Change |
| --- | --- |
| `common/TributeData.cs` → `common/ShrineData.cs` | Steps removed; blessing and inspirations added, with the defaults above |
| `common/ValConfig.cs` | The new settings, `PointsPerSoul`'s default, the file rename |
| `starmap/LineageSave.cs` | `Run` and `Boons`; `TributeSteps` and `ClaimedSouls` removed |
| `starmap/PowerSet.cs` | `Rebuild` and `Preview` take boon grants, switched on |
| `modules/PlayerData.cs` | Run state, `Recompute` passes the boons, `StartNewRun`; soul claims read and set the `al_claimed_*` player keys, and `Publish` writes them to the ZDO; `OfferTribute` removed |
| `modules/Deeds.cs` (new) | `AL_Deed` and the `OnDeath` report on the owner; kill, craft and harvest rolls; minibosses; biome level; the lucky limit |
| `modules/Quests.cs` (new) | Taking and completing quests, cooldown days, the quest icon |
| `modules/Boons.cs` (new) | Starting, ending and saving boons; their icons; the dream on sleep |
| `modules/SacrificeUI.cs` | Quest and blessing entries, entries that only inform, the header |
| `modules/Ascension.cs` | Starts a new run |
| `souls/SoulBosses.cs` | A soul rises only when a player in range lacks the boss's key; boss deaths reach `Deeds` for quests |
| `souls/SoulOfPower.cs` | Claiming gives the player key; a player who holds it can't claim |
| `modules/Commands.cs` | Admin commands: `al-quest` (take or complete), `al-boon <id>`, `al-dream`, `al-deeds` (show the run, or force the luck chances for testing) |
| `localizations/English.json` | Quests, blessings, dreams and deed messages; tribute keys removed |
| `Package/README.md`, `Package/CHANGELOG.md` | The 0.1.0 tribute bullets rewritten, since 0.1.0 hasn't shipped |
| `docs/overhaul-plan.md` | Its points table points here |

## Build order

1. **Boons.** Data, save, the `PowerSet` overlay, the icon and `al-boon`. This can be tested alone.
2. **Shrine data.** The rename, tributes removed, and the blessing entry at the shrine (opened by
   command until quests exist).
3. **Quests.** Taking one, the boss report, completing it, the cooldown and the icon.
4. **Deeds.** `AL_Deed` with starred kills and minibosses, then crafting and picking, then trees,
   rocks and deposits.
5. **Souls.** The player keys, rising only when someone in range lacks the key, and 1 point.
6. **Dreams.**
7. **Text.** Localization, README and changelog.

## Testing in game

None of this can be checked without the game.

- **Starred kills:** count at and above the biome level and not below it; wolves, deer and tamed
  creatures never count; the limit stops rolls. Use `al-deeds` to force the chance to 100%.
- **Crafting and harvest:** a forced chance gives points to the right deity; a craft only picks
  discovered biomes.
- **Quest:** take, kill the boss, get the point, see the blessing open, and wait the cooldown out
  with the `skiptime` dev command.
- **Blessing:** its powers apply and show in the star map totals, end after 10 minutes, and survive
  logging out and death.
- **Dream:** sleep with 4 or more points in a deity, or force one with `al-dream`; the deity's line
  replaces the vanilla dream.
- **Multiplayer:** a creature owned by another player's machine credits everyone who hit it; a
  player in range who didn't hit the boss still completes the quest; a late joiner gets a soul.
- **Souls:** no soul rises when everyone in range holds the boss's key. A player who holds it gets
  nothing from a soul that rose for someone else. `listkeys` shows the key after a claim.
- **Ascension:** lucky counts, minibosses, quests, blessings and soul keys all start over.

## Open questions

- The miniboss prefab names want checking in game, and whether the Deep North has one.
- Two-star creatures are rare in vanilla, so 5% may give too few points without a star mod.
- Whether blessings should cost a small offering by default. They're free in this plan.
- The ocean has no deity. `Ocean=Baldur` in `BiomeDeities` would give sea kills and harvests to the
  deity whose star map has the sailing arm.
