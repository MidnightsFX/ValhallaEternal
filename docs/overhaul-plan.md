# Aesir Lineage — overhaul plan

Aesir Lineage replaces Valhalla Eternal. The boon shop (pay trophies, raise one boon a level at a
time) becomes a star map per deity: the player spends that deity's points on connected nodes, and
an effect gets stronger by linking more of the nodes that grant it. Early nodes are weak; the strong
ones carry oaths. The first kill of each boss leaves a Soul of Power that everyone nearby can claim
for a point in that deity's map. New Game+ prestige stays, as Ascension.

Status: phases 0 (rename), 1 (foundation), 2 (star map screen) and 3 (tributes and Souls of Power)
are built; none of 1 to 3 has been tested in game yet. See the "Phase N as built" sections near the end.

## Decisions taken

| Question | Decision |
| --- | --- |
| Identity | New mod: GUID `MidnightsFX.AesirLineage`, new Thunderstore package `AesirLineage`. Valhalla Eternal is deprecated. No save migration, so data formats, enum names and keys can change freely. |
| Rename depth | Everything: solution, project, namespace, assembly, manifest, README, config folder, Unity project folder, asset bundle name, `$al_` localization prefix, save/ZDO keys, RPC and console command names. |
| Point sources | Boss souls plus trophy tributes at each deity's shrine. |
| New Game+ | Kept, as Ascension: it opens a map's outer ring and its top cosmetics, and lets the player claim boss souls again. |

## Core loop

1. Explore a biome, find its deity's shrine, offer trophies: each tribute grants that deity points.
2. Kill the biome's boss: the first kill in the world leaves that deity's Soul of Power. Every
   player nearby claims one point from it.
3. Spend points in the deity's star map. The first ring is small, safe bonuses; the arms lead out
   to named major nodes, then to oath-bound nodes that are strong but cost something.
4. With every boss down, Ascend with a deity: lose part of your skills, start over at the spawn,
   and gain that deity's outer ring, an Ascension cosmetic and a fresh claim on every boss soul.

## Star maps

Each deity has one constellation. The deity's portrait is the centre; three arms radiate out, one
per aspect of the deity, and an outer ring (Ascension only) joins the arm ends.

### Node tiers

| Tier | Look | What it gives | Typical cost |
| --- | --- | --- | --- |
| Root | The deity portrait | Nothing; every path starts here | free |
| Minor star | Small star | One small stat (+2% harvest bounty, +3 armor) | 1 |
| Major star | Larger star with icon | A named mechanic (the old boons live here) plus a base value of its stat | 1 |
| Oath-bound star | Star in a red rune ring | A strong effect and an oath (a drawback that stays while allocated) | 1 |
| Apex star | The constellation's brightest star | The deity's signature power and visual, with the heaviest oath; Ascension only | 1 |

Costs are data, not code, so a server can price nodes differently.

### How effects stack

Every node grants one or more **powers** (named, summed values). A major node switches a mechanic
on; minors on the same arm feed that mechanic's power. "Bounty of the Lands" gives the extra-yield
mechanic and +5% chance; each linked "Furrow" minor adds +2%. The tooltip shows the running total
("Harvest bounty 9% → 11%"). This replaces raising one boon level by level.

Powers have caps in data, so stacking every node a server adds can't run away.

### Allocation rules

- A node can be taken when it links to the root or to an allocated node, and the player has a
  point for that deity.
- Refunding is free while the rest stays connected to the root; a full reset happens at the
  deity's shrine (cost configurable, default one boss trophy of that biome).
- Oaths are active while their node is allocated, and go when it is refunded.

### Size

37 nodes per deity: root, 3 arms of 11 (6 minors, 3 majors, 1 oath-bound star and the arm's apex
on the Ascension ring), and 3 bridge stars on the inner ring (see "Phase 4 as built"). With the default economy a
player fills about half of one map per playthrough, so each map asks for choices.

## Points

Points belong to a deity and are spent only in that deity's map.

| Source | Points | Notes |
| --- | --- | --- |
| Tribute at the deity's shrine | 1 per tribute | Tributes escalate (later points cost rarer trophies); the list is data, seeded from today's sacrifices. |
| Soul of Power | 1 (configurable) | First kill of the deity's boss in this world, or again after the player Ascends. |
| Ascension | 3 (configurable) | Granted to the deity you Ascend with. |

The shrine UI keeps the current tribute layout (item icons, counts) but each entry now reads "+1
point for Gefjun" instead of a boon.

## Oaths

Today's oaths (more damage taken, less damage dealt, slower skill gain, lower health, stamina,
eitr or carry weight) stay as the oath pool, plus new deity-themed ones. Differences from today:

- An oath is attached to a node, not bought separately, and goes away with the node.
- Stronger nodes carry heavier oaths: minors and majors none, oath-bound nodes one moderate oath,
  apex nodes a heavy one.
- Oaths are thematic where possible: Baldur's apex makes you invulnerable for a moment but you take
  far more piercing damage (the mistletoe dart); Gefjun's plough-oxen node raises carry weight and
  gathering but lowers damage dealt.
- The compendium lists active oaths with the node that imposed them.

## Soul of Power

A glowing orb with the deity's rune inside, a pillar of light above it and the deity's aura
around it (see the reference image: a red orb holding a rune, beam rising from it).

| Boss | Biome | Soul belongs to |
| --- | --- | --- |
| Eikthyr | Meadows | Gefjun |
| The Elder | Black Forest | Vör |
| Bonemass | Swamp | Baldur |
| Moder | Mountains | Skaði |
| Yagluth | Plains | Syn |
| The Queen | Mistlands | Freyja |
| Fader | Ashlands | Hel |
| Deep North boss | Deep North | open question (see the end) |

The mapping is data, so modded bosses can be added.

**Spawning.** Patch `Character.OnDeath` on the boss's owner, before vanilla sets the boss's
`m_defeatSetGlobalKey`. If the world key `al_soul_<bosskey>` is not set, spawn the soul above the
death position and set the key. Our own key is used instead of `defeated_*` because Ascension and
progression mods remove the vanilla keys. A soul also spawns when any player within range is
eligible again after Ascending.

**Claiming.** Hover text: "Soul of Hel — [E] Accept Hel's favour". Each player claims once; claims
live in the player's save as a list of boss keys, cleared on Ascension. On claim: the orb streams
into the player, a burst in the deity's colours, "+1 point for Hel" in the centre of the screen.
After you claim, the orb dims for you only, so others can still see it is theirs to take.

**Lifetime.** The soul stays for a configurable time (default 30 minutes, persistent across zone
unloads through its ZDO), then fades.

**Visual build.** Prefab per deity from the Piloto Elemental VFX pack already in the Unity project:
`Orb_<element>` for the body, `Lootbeam_<element>` for the pillar, a magic ring decal on the ground,
a rune sprite billboarded inside the orb, a pulsing point light and an ambient hum. Elements per
deity are listed under visual identity.

**Config.** Points per soul, lifetime, claim radius for re-spawns after Ascension, boss-to-deity map.

## Ascension (New Game+)

Kept from today's T4 prestige sacrifices, with one meaning per deity:

- Requires every boss defeated (same key list as today).
- Costs: skills lowered (default 50%), recipes forgotten (configurable), teleport to the start
  temple, boss keys removed so bosses must be killed again.
- Gives: +1 Ascension with that deity, which unlocks one apex node per level (three levels), +3
  points for that deity, that deity's Ascension cosmetic, and fresh claims on every boss soul.
- The prestige number shown next to names becomes the total Ascensions across deities.

## Visual identity

Every deity has a palette, an element family from the Piloto pack, a rune, and visual perks that
the star map unlocks. The existing portraits (`AesirLineageUnity/Assets/Art/*_nobackground.png`)
are the centre of each map and the art on the shrine UI.

| Deity | Aspect | Palette | Piloto family | Rune | Visual perks |
| --- | --- | --- | --- | --- | --- |
| Gefjun | Plough, harvest, the household | wheat gold, meadow green | Nature, Bees | Gebo ᚷ | flowers bloom in your footprints, vine wings (existing model), golden pollen aura, leaf bursts on bonus yield |
| Vör | Wisdom, curiosity, certainty | indigo, silver, cyan | Arcane, Universe | Ansuz ᚨ | rune motes orbiting the head, an eye sigil, glyph trail, flash of insight on skill-ups |
| Baldur | Light, purity, invincibility | white gold, sun gold | Holy, Light | Sowilo ᛊ | halo (Guardian Halo), radiant wings, sunburst on block, light footprints |
| Skaði | Winter, the hunt, mountains | ice blue, white, steel | Frost, Wind | Isa ᛁ | frost breath, snowflake trail, frost wings, frost-rimed arrows |
| Syn | Refusal, the shut door, guardianship | bronze, ember red, stone | Shields, Rocky VFX | Algiz ᛉ | hex shield shimmer on block, rune circle underfoot while guarding, stone skin |
| Freyja | Seiðr, war, love | rose, amber gold, violet | Fairy, Psychic | Fehu ᚠ | falcon-feather wings, Brísingamen glow at the throat, lotus aura, valkyrie motes |
| Hel | Death, the underworld | bone white, corpse blue, void black | Dark, Void | Hagalaz ᚺ | dark wings, soul wisps orbiting, ash footprints, a death mark on kills |

**Cosmetic slots.** Aura, wings, footprints, crown/halo, weapon glow, kill effect. Major and apex
nodes and Ascension unlock entries; the player picks one per slot (today's cosmetics panel, more
slots). Other players see them through the player's ZDO.

**Proc effects.** Mechanics that fire (bonus yield, arrow returned, perfect-form crit) play a small
effect in the deity's palette, so a build reads at a glance. Today's four shared effects become one
set per deity.

## Star map UI

- Opens from a hotkey and from the shrine. Points can be spent anywhere; full resets and Ascension
  happen only at a shrine.
- Full-screen panel: starfield with a nebula tinted to the deity, the portrait glowing at the
  centre, nodes as star sprites by tier, links as glowing lines. Allocated links flow (UV-scrolling
  shader), available nodes pulse, locked ones are dim. Pan by dragging, zoom with the wheel.
- A column of seven rune sigils switches deity; each shows points available.
- Tooltip: name, powers with the running total before and after, oath in red, cost, lore line.
- Built in code on Jotunn's GUIManager. All link lines are drawn by one UI `Graphic` that builds a
  single mesh, and nodes are pooled `Image`s, so a 34-node map is a few draw calls. Glow and flow
  shaders come from the bundle (Amplify Shader Editor is in the Unity project).
- Node positions are polar (ring, angle) in data, so maps stay tidy and admins can add nodes
  without hand-placing pixels.

## Data and code layout

**Data (server-synced YAML in `BepInEx/config/AesirLineage/`):**

```yaml
# StarMaps.yaml (shape, not final)
Gefjun:
  palette: { primary: "#E8C35A", secondary: "#7DBE5A", glow: "#FFF2B0" }
  rune: gebo
  nodes:
    root:            { tier: root }
    furrow_1:        { tier: minor, ring: 1, angle: 0,  links: [root],     powers: { HarvestBounty: 2 } }
    bounty_of_lands: { tier: major, ring: 3, angle: 6,  links: [furrow_2], powers: { HarvestBounty: 5, HarvestBountyBurst: 1 } }
    oxen_yoke:       { tier: oath,  ring: 5, angle: 0,  links: [bounty_of_lands],
                       powers: { CarryWeight: 150, GatherYield: 20 }, oaths: { DealLessAllDamage: 15 } }
    plough_bloom:    { tier: apex,  ring: 7, angle: 0,  ascension: 1, links: [oxen_yoke], visual: gefjun_bloom }
```

Also `Tributes.yaml` (tribute steps, reset cost and Ascension per deity). The soul settings (boss-to-deity
map, points, lifetime) are server config entries rather than a file, since Jotunn already syncs those.

**Player save (`m_customData["AL_DATA"]`):** allocated node ids per deity, points earned and
spent per deity, claimed souls, Ascension per deity, chosen cosmetics.

**Code:**

| Folder | Holds |
| --- | --- |
| `starmap/` | Data schema and loading, allocation and connectivity checks, the `PowerSet` (summed powers, rebuilt only when allocation changes) |
| `powers/` | Power and oath handlers, grouped by the game system they hook (damage, defence, skills, food, regen, gathering, kills, movement, weapon bans); oaths are powers with an oath flag |
| `souls/` | Soul prefab setup, spawn patch and boss map (`SoulBosses`), interaction, visuals, lifetime (`SoulOfPower`) |
| `visuals/` | Deity visual sets, cosmetic slots, remote-player sync |
| `ui/` | Star map, shrine (tribute, reset, Ascension), compendium |
| `shrines/` | Today's location setup |

Damage-related powers share one `RPC_Damage` prefix and postfix that dispatch to each handler,
rather than one patch per boon (today there are three separate `RPC_Damage` prefixes).

## Review of today's boons and oaths

Every boon becomes a power fed by minors and switched on or boosted by a major. Many of today's
formulas are broken, and several boons never run at all, so each gets rewritten as it moves.

| Boon (today) | Deity | What the code actually does | New home |
| --- | --- | --- | --- |
| Bounty of the Lands (`IncreasePickableYields`) | Gefjun | Fires on about 90% of picks (the chance check is inverted), and more boon levels give fewer extra drops | Gefjun harvest arm: minors "Furrow" + major "Bounty of the Lands" |
| Quality Nourishment | Gefjun | Works: food health, stamina and eitr scaled by value% | Gefjun hearth arm |
| Fishing Prosperity | Gefjun | Works; the top reward count can never roll (exclusive max) | Gefjun harvest arm, major |
| Basic Protection | Gefjun | Works: flat armor | Gefjun shield arm, minors |
| Wisdom of the Ages (`RandomXPBonus`) | Vör | Two implementations: a working Player.RaiseSkill re-raise, and a Skills.RaiseSkill prefix that edits a by-value argument and does nothing | Vör insight arm, major |
| Bladebound Knowledge | Vör | Inverted: non-combat skill gains trigger it, combat gains don't | Vör insight arm, major |
| Hunger for Knowledge | Vör | Works, but the random skill can be `None` | Vör insight arm, major |
| Knowledge Is Power | Vör | Not granted by any sacrifice; formula multiplies combat skill level by ~50× at skill 50 | Vör lore-of-war arm, rewritten with a cap |
| Shield Wall (`PowerfulShield`) | Baldur | Block power ×2.03 at level 3 (formula `1 + (100 + v)/100`) | Baldur invincibility arm |
| Light of Baldur (`DamageAgainstUndead`) | Baldur | ×2.02 damage to undead at level 2, same formula | Baldur radiance arm |
| Purify (`ReduceFirePoison`) | Baldur | Reduction shrinks as the level rises (10% at 3, 2% at 80) | Baldur purity arm |
| Arrow Catcher | Baldur | Works but not granted by any sacrifice | Baldur invincibility arm |
| Guardian of the Gates (`StrongerTowerBlock`) | Syn | Not granted by any sacrifice; same doubling formula as Shield Wall | Syn gate arm |
| Swift is the Shadow | Skaði | Never runs: patches `Character.GetRunSpeedFactor`, which `Player` overrides | Skaði winter arm |
| Stormbound Rage | Skaði | Fires at night, not in storms, and adds lightning equal to the whole hit (about double damage) | Skaði winter arm, as storm-only frost/lightning |
| Huntress Prowess (`HuntressArrowReturn`) | Skaði | Runs for whoever owns the creature, whoever killed it | Skaði hunt arm |
| Built Different | Syn | Never runs: the patch class has no target type | Syn refusal arm |
| Wealth of the Ages | Syn | Never drops: uses the arrow chance and looks the reward up in the arrow table | Syn blade arm |
| Dedication to the Blade | Syn | Melee ×2.025, ranged ×0.95; the bundled `IncreaseMeleeDamage` boon has no code | Syn blade arm, as an oath-bound node |
| Ever Watchful | Syn | Works but not granted by any sacrifice | Syn refusal arm |
| Seiðr of Plenty | Freyja | Base eitr works; the eitr regen part grants `IncreaseEitrRegen`, which no code reads | Freyja seiðr arm |
| Goddess of War | Freyja | Grants `IncreaseStaminaRegen`, which no code reads (the handler reads `GoddessOfWar`) | Freyja war arm |
| Perfect Form | Freyja | Works: roughly every 500 damage dealt, one hit does ×3.2 | Freyja war arm, major |
| Hellfire Adaptation | Hel | Any level adds ~1.03 to heat resistance, which means full immunity to Ashlands heat | Hel hellfire arm, as a capped percentage |
| Balance of the Jotunn / of the Aesir | Hel | Never run: they grant `ReturnStaminaOnDamage` / `ReturnEitrOnDamage`, but the handler reads other names; descriptions are swapped | Hel balance arm |
| Hasten the Inevitable | Hel | Never runs: grants `MovementSpeedOnKill`, handler reads `HastenTheInevitable` | Hel inevitable arm |
| Seeds of the Gods, Gefjun's Feasts, Strength of the Valkyries, Brutal Defiance, Strike Now!, Death Control | various | Planned in the README, never built | Majors and oath-bound nodes in the maps below |

| Oath (today) | What the code does | New home |
| --- | --- | --- |
| Damage taken increase | Works | Oath pool |
| Deal less blunt / pierce / slash / all damage | Works (blunt and all floor at 20%; slash and pierce have no floor) | Oath pool, with a floor on all four |
| Lower skill gain (12 skills) | Squares the gain (`value *= value * mod`) | Oath pool, fixed |
| Lower health / stamina / eitr percent | Works | Oath pool |
| Reduce carry weight | Works | Oath pool |
| No bows / no crossbows | Declared, no code | Oath pool, implemented |

Other problems found while reviewing (all fixed in phase 1):

- **Security:** other players' cosmetics are read with `BinaryFormatter.Deserialize` on a byte array
  from their ZDO (`PrestigeDisplays.SyncPrestigeVisuals`). Any player on a server can put bytes
  there, and BinaryFormatter on untrusted data can run code. It also runs on every
  `VisEquipment.UpdateVisuals` call. Replace with the plain `Aura=x|Wings=y` string the code
  already has (`PackActiveEffectsForZDO`), read only when the ZDO revision changes. The shipped
  Valhalla Eternal has the same code.
- **Config sync:** the server sends `Sacrifices.yaml` to clients, but the client handler drops it,
  so each client uses its own file.
- `PrestigeLevelConfigData` and `OathUI` are unused.

## Borrowed from Path of Valheiman

Path of Valheiman (PoV below) 5.1.2 has about 150 distinct player mechanics, a 4,069-node passive
tree and a polished tree window. Its licence forbids redistributing modified versions of its code,
and our copy is a decompile, so only mechanics and techniques carry over. Everything is written
fresh here, and every borrowed mechanic is cut down to fit one deity's theme.

**Mechanics.** Marked "PoV" in the maps below. PoV-derived mechanics are always major, oath-bound
or apex nodes; minors stay plain stats. Most need their own patches, so phase 4 budgets them one at
a time.

**Power vocabulary.** PoV gives every stat an optional condition (night, raining, wet, sheltered,
low or full health, airborne, sprinting, in a biome) and an optional trigger window ("for N s
after a block, parry, kill, dodge or crit"). Aesir Lineage powers take the same two optional
fields, which covers today's hand-written special cases (Swift is the Shadow at night or in the
mountains, Stormbound Rage in storms) and lets most new minors be data only.

**Star map UI techniques** (`PassiveTree/UI/TreeWindow.cs` and friends in the PoV checkout):

- Links are one `MaskableGraphic` per tile that builds ribbon quads in `OnPopulateMesh`: a wide
  soft halo pass for taken links, then a thin body pass coloured from a small state atlas (locked,
  available, taken). Links can bend as quadratic Béziers. This is the plan for our link renderer.
- Nodes are stacked images (shadow, glow, frame by tier and state, icon, "available" crown), hover
  scales to 1.06, available nodes pulse.
- Allocation queues nodes, walks back to the allocated set to fill the path, and commits on
  "Apply". Refund safety is a BFS from the root before and after removal.
- Allocating plays bursts that travel along the incoming links. We use it for every allocation,
  in the deity's colours.
- Pan and zoom ease toward the cursor; detail (glow, icons) drops at low zoom.
- Tooltips show the before and after of every stat.
- Allocations are saved by node id in `m_customData`, and the tree definition is sent host to
  client with a hash check, which is the shape our `StarMaps.yaml` sync takes.

**VFX techniques:**

- A glowing outline on a character, made by duplicating each skinned mesh renderer on the same
  bones with an outline shader and a pulsing width. This becomes the deity-glow cosmetic.
- Rune particle auras built in code (texture-sheet of runes, upward drift, colour gradient), and
  ground rings from line renderers (rim, band, orbiting arcs, expanding wave) synced through the
  ZDO. Used for Vör's and Syn's auras.
- Helix beams between two points with flowing motes, for the soul streaming into a player.
- Loot beam from a line renderer, a light and rune particles: the fallback soul pillar if a Piloto
  beam looks wrong in Valheim's lighting.
- Cloning vanilla effects (Eikthyr's stomp, Dverger nova, chain lightning) into an inactive
  template with network parts removed, for proc effects that match the game's look.

**Boss rewards.** PoV gives every character five tree points per boss key, with no object in the
world. The Soul of Power differs on purpose: it is a thing in the world, it belongs to one deity,
and you have to be there.

**Damage hooks.** One prefix and one postfix on `RPC_Damage` dispatching to handlers in a fixed
order, with a per-build profile that skips handlers the build cannot trigger, as PoV's rebuild
does.

## Deity star maps

First drafts: names and numbers are for review, not final. Each map is root, three arms, and an
Ascension ring of three apex nodes (one per Ascension level). Minors are listed once with how many
copies the arm has. "Oath:" is the drawback that comes with the node.

### Gefjun, the Plough-Mother

Meadows · Eikthyr · Gebo ᚷ · wheat gold and meadow green. Gefjun ploughed Zealand out of Sweden
with her four sons turned to oxen. Abundance, the hearth, protection of the household.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| The Furrow (gathering) | Harvest bounty +2% ×3; farming XP +5% ×2; fishing bounty +2% | **Bounty of the Lands** (extra yields with a leaf burst); **Seeds of the Gods** (plant crops in any biome, PoV); **Fishing Prosperity** | **Yoke of the Four Oxen**: +150 carry weight, +20% wood and ore yield. Oath: deal 15% less damage. |
| The Long Table (food) | Food health, stamina and eitr +3% ×3; food duration +5% ×2 | **Quality Nourishment**; **Hearty Meal** (your strongest food counts 1.35×, PoV); **Gefjun's Feast** (eating heals allies within 6 m a little) | **Feastbound**: food values +25%. Oath: food lasts 25% less long. |
| Zealand's Ward (protection) | Armor +3 ×3; block stamina −4% ×2 | **Basic Protection**; **Shelter** (below 35% health, 15% of each hit is prevented, PoV Endurance) | **Mother's Ward**: blocks build Fortification, 3% less damage taken per stack, 5 stacks (PoV). Oath: move 10% slower. |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Bloom of Zealand | Once every 10 minutes a killing blow leaves you at 1 health and roots nearby enemies in vines for 3 s (PoV Immobilize) | Deal 20% less damage | flowers bloom in your footprints, vine wings |
| The Plough | Two-handed alternate attacks cut a furrow that throws up a ridge of earth (PoV Fault Line, Ridge) | Stamina regen −25% | earth spray in gold light |
| Hearth-Mother | Allies within 8 m get 15% of your food health | Take 15% more damage | golden pollen aura |

### Vör, the Watchful

Black Forest · The Elder · Ansuz ᚨ · indigo, silver and cyan. Nothing can be hidden from Vör.
Wisdom, curiosity, certainty.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| Insight (skills) | Skill XP +3% ×4; non-combat XP +5% ×2 | **Wisdom of the Ages**; **Hunger for Knowledge**; **Bladebound Knowledge**; **Eureka** (a level-up sometimes grants a second, PoV) | **Certainty**: skill XP +40%. Oath: skills lost on death doubled. |
| Nothing Hidden (perception) | Explore radius +10% ×2; sneak +5% ×2; detection range −5% ×2 | **Ore Sense** (nearby ore pings on the minimap, PoV); **The Seeress's Map** (chests, dungeons and rare creatures marked, PoV MapSight); **Tracks** (follow a hunted creature's trail, PoV) | **Raven's Eye**: look down on the area from above for 8 s, once a minute (PoV Hugin's Flight). Oath: combat skill gains −25%. |
| Lore of War (precision) | Crit chance +1% ×3; crit damage +5% ×2 | **Knowledge Is Power** (combat skills gain 10% of your best non-combat skill, capped at +10 levels); **Learned from the Fallen** (killing a starred creature lends you one of its traits for 20 s, PoV Hugin's Memory) | **Certain Blow**: every hit crits, crits deal ×1.25 instead of their full bonus (PoV Precise Calculation). Oath: built in. |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Foresight | The first hit from each enemy in a 20 s window misses you (PoV evasion) | Max health −20% | an open eye sigil above your head |
| Unveiling | Enemies you hit are marked for 6 s and take 2% more damage from everyone per stack, 5 stacks (PoV Impale) | Skill gains −25% | glyphs flare on marked enemies |
| Völuspá | A skill level-up gives 10 s of +20% damage and XP | Food duration −15% | rune motes orbit your head |

### Baldur, the Shining

Swamp · Bonemass · Sowilo ᛊ · white gold and sun gold. Everything swore not to harm Baldur except
the mistletoe. Light, purity, invincibility.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| Radiance (against undead) | Damage to undead +4% ×3; spirit damage +2 ×2 | **Light of Baldur**; **Sunstrike** (crits against undead call a bolt of light, PoV Smite); **Cleansing Flame** (undead you kill burst in light, damaging undead nearby, PoV kill explosions) | **Sunsworn**: +6 spirit damage on every hit. Oath: deal 20% less damage at night. |
| Purity (cleansing) | Poison and fire duration −5% ×3; wet duration −10% ×2 | **Purify**; **Cleanse Wet**; **Clean Parry** (a parry removes one ailment, PoV) | **Untainted**: immune to poison. Oath: your own poison damage is removed. |
| Invincibility (shields) | Block power +4% ×3; parry window +5% ×2 | **Shield Wall**; **Arrow Catcher**; **Returned Light** (parried projectiles fly back at the shooter, PoV) | **Oath of All Things**: the first hit every 20 s is ignored. Oath: take 50% more pierce damage (the mistletoe). |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Invincible | Below 20% health, 3 s of invulnerability, once every 5 minutes | Max health −20% | halo and radiant wings |
| Dawnbringer | An aura burns undead within 6 m and lights the dark (PoV aura ring) | You can't sneak | light footprints |
| Bright Retort | A parry releases a light nova that staggers enemies within 4 m (PoV parry burst) | Stamina regen −25% | sunburst on block |

### Skaði, the Huntress

Mountains · Moder · Isa ᛁ · ice blue, white and steel. Goddess of winter, the hunt, skis and the
mountains of Þrymheimr.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| The Hunt (bows) | Bow damage +3% ×3; draw speed +4% ×2 | **Huntress Prowess** (arrows from your own kills); **Piercing Shot** (arrows pass through one enemy, PoV); **Field Dressing** (trophies and hides from your kills go straight to your bag, PoV) | **Rain of the North**: +2 arrows per shot. Oath: draw speed and arrow damage −50% (PoV Rain of Arrows). |
| Winter (cold) | Frost resistance +5% ×3; frost damage +2 ×2 | **Swift is the Shadow** (faster at night, in mountains and the Deep North); **Blizzard-born** (in snowstorms, hits add frost damage); **Shatter** (frozen and cold-killed enemies grow ice that explodes, PoV) | **Child of Þrymheimr**: immune to cold and freezing. Oath: take 50% more fire damage. |
| The Peaks (movement) | Jump height +5% ×2; sprint stamina −4% ×2 | **Sure-footed** (no fall damage, PoV safe fall); **Skis of Skaði** (slide down snowy slopes, PoV Slide); **Leap** (hold jump to jump higher, PoV) | **Mountain-born**: double jump. Oath: −100 carry weight (PoV Feather). |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Hang on the Wind | Drawing a bow in mid-air holds you there (PoV Hang on the String) | Melee damage −20% | frost wings |
| Winter's Shatter | Bow kills burst into a frost nova | Take 30% more fire damage | frost-rimed arrows, snowflake trail |
| Wolf-Sister | After three bow kills in 20 s, a spectral wolf hunts beside you for 60 s (PoV Bottled Essences) | Max health −15% | frost breath |

### Syn, the Gatekeeper

Plains · Yagluth · Algiz ᛉ · bronze, ember red and stone. Syn guards the doors of the hall and
refuses entry to those she will not admit. Refusal, guardianship.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| Refusal (poise) | Stagger threshold +5% ×3; damage taken while staggered −3% ×2 | **Built Different**; **Ever Watchful**; **Footing** (a block's stagger is paid with stamina instead, PoV) | **The Unmoving Door**: you can't be staggered. Oath: move 25% slower (PoV Unshakeable). |
| The Gate (shields) | Tower shield block +4% ×3; block stamina −3% ×2 | **Guardian of the Gates**; **Brutal Defiance** (shield bash; three blocks in a row release a burst, PoV); **Sealed Wall** (tower shields block spells, PoV) | **The Shut Door**: blocking reflects 30% of the blocked damage (PoV thorns). Oath: deal 15% less damage. |
| Blade and Hoard (melee and wealth) | Melee damage +3% ×3; loot from kills +2% ×2 | **Wealth of the Ages**; **Strength of the Valkyries** (other weapon skills gain 10% of your best one); **Challenge** (hits taunt enemies for 15 s, PoV Provoke) | **Dedication to the Blade**: melee damage +25%. Oath: no bows or crossbows. |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Doors of Valhalla | While blocking, a rune dome covers you; allies inside take 20% less damage (PoV RunicDome) | Deal 20% less damage | rune circle underfoot |
| Stone Skin | Standing still for 1 s grants full Fortification (PoV) | Move 15% slower | stone skin |
| The Last Door | Once every 5 minutes, the killing blow on an ally within 10 m is taken by you instead (PoV ally links) | Take 15% more damage | hex shield shimmer |

### Freyja, the Seiðkona

Mistlands · The Queen · Fehu ᚠ · rose, amber gold and violet. Mistress of seiðr, who chooses half
the slain for Fólkvangr, wears Brísingamen and flies in a falcon cloak.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| Seiðr (magic) | Max eitr +5 ×3; eitr regen +5% ×2; magic damage +3% ×2 | **Seiðr of Plenty**; **Echo** (a spell sometimes casts twice, PoV Spell Echo); **Storm-weave** (lightning arcs between wet enemies, PoV) | **Völva's Price**: when out of eitr, spells cost health. Oath: max health −20%. |
| Fólkvangr (war) | Stamina regen +4% ×3; attack stamina −3% ×2 | **Goddess of War**; **Perfect Form**; **Battle Fury** (hits build fury, 1% more damage per stack, up to 20, PoV Rage) | **Chooser of the Slain**: kills heal 8% of max health. Oath: take 15% more damage. |
| The Falcon Cloak (grace) | Dodge stamina −4% ×2; jump stamina −5% ×2; armor +2 ×2 | **Falcon Glide** (hold jump while falling to glide, PoV Glide); **Beloved** (your mead effects pulse to allies within 6 m, PoV trinket aura); **Battle Medic** (shield alternate attack heals allies, PoV) | **Charged Grace**: a dodge or parry charges your next attack, +25% and a small splash (PoV Charged Attacks). Oath: armor −20%. |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Fólkvangr | Kills have a 50% chance to raise a spectral einherjar who fights for 20 s (PoV minion inheritance) | Max health −20% | valkyrie motes |
| Brísingamen | While eitr is above half, eitr takes damage before health (PoV Soul Vessel, cut down) | Eitr regen −30% | Brísingamen glow at the throat, lotus aura |
| Falcon Form | Hold jump to hover (PoV Levitation) | Stamina regen −30% | falcon-feather wings |

### Hel, Queen of the Dead

Ashlands · Fader · Hagalaz ᚺ · bone white, corpse blue and void black. Hel is half living, half
dead, and rules everyone who did not die in battle.

| Arm | Minor stars | Major stars | Oath-bound star |
| --- | --- | --- | --- |
| Hellfire (heat) | Heat resistance +5% ×3; fire resistance +5% ×2 | **Hellfire Adaptation** (capped percentage, not immunity); **Retort** (taking fire damage wraps you in a fire aura for 5 s, PoV Ashen Crown); **Wildfire** (burning spreads from a slain enemy to the next, PoV) | **Pyre of Helheim**: you burn, losing 1% health per second; your fire damage ×1.5 and Ashlands heat can't hurt you (PoV Eternal Bonfire). Oath: built in. |
| Balance (sustain) | Life leech +0.5% ×3; stamina when hit +2 ×2 | **Balance of the Jotunn** (stamina when hit); **Balance of the Aesir** (eitr when hit); **Stored for Later** (overhealing becomes Fortification, PoV) | **Death's Bargain**: once an hour, lethal damage leaves you at 1 health instead; kill something within 15 s or die. Oath: built in. |
| The Inevitable (death) | Damage to enemies under 30% health +4% ×3; move speed after a kill +3% ×2 | **Hasten the Inevitable**; **Silent Passing** (a dodge right after a kill makes you silent, PoV Vanish in Motion); **Open Wound** (hits make enemies bleed; bleeding enemies burst when they die, PoV) | **Half-Dead**: immune to poison and spirit damage, +25% damage. Oath: max health −30% (PoV Soul Vessel, cut down). |

| Apex (Ascension) | Power | Oath | Visual |
| --- | --- | --- | --- |
| Garm's Hunger | Life leech 3% of all damage | No natural health regen | ash footprints, soul wisps orbit |
| Náglfar's Crew | Enemies you kill rise as spectral thralls for 20 s and explode at low health (PoV Unstable Minions) | Max health −25% | dark wings |
| Queen's Decree | Enemies under 15% health that you hit die (bosses excluded) | Take 25% more damage | a death mark burns on your kills |

## Roadmap

| Phase | Scope | Done when |
| --- | --- | --- |
| 0. Rename | Everything renamed to Aesir Lineage | Builds (done); asset bundle rebuilt in Unity (to do) |
| 1. Foundation (built, untested in game) | Star map data schema and loading, `PowerSet`, allocation and connectivity, new save format, server config sync applied on clients, safe cosmetic sync, today's boons ported to powers with fixed formulas, debug console commands (`al-points`, `al-allocate`, `al-reset`) | A node can be allocated from the console and its power works in game |
| 2. Star map UI (built, untested in game) | Constellation rendering, pan and zoom, deity tabs, tooltips with running totals, allocate and refund, portrait centre | All seven maps browsable and allocatable |
| 3. Points (built, untested in game) | Shrine tributes grant points, shrine reset, Soul of Power (spawn, claim, persistence, placeholder orb) | A boss kill gives each nearby player a point |
| 4. Content (built, untested in game) | Full node sets for all seven deities, Path of Valheiman-derived mechanics, oaths on strong nodes | Every node does what its tooltip says |
| 5. Visuals (built, untested in game) | Soul orb per deity, star map art and shaders, cosmetic slots and per-deity sets, proc effects | Each deity is recognisable from its effects alone |
| 6. Ascension (built, untested in game) | Prestige flow per deity, apex ring, Ascension cosmetics, soul re-claims | One full Ascension loop played through |
| 7. Release | Balance pass, localization (English, Vietnamese), README and Thunderstore page, Valhalla Eternal deprecation notice | First Thunderstore release |

## Phase 1 as built

**What is there.** `starmap/` holds the schema (`StarMapData.cs`), the registry and YAML loading
(`StarMaps.cs`), the built-in maps (`DefaultStarMaps.cs`), the 51 powers with kind and cap
(`Power.cs`), the summed powers (`PowerSet.cs`) and the character save (`LineageSave.cs`).
`modules/PlayerData.cs` owns points, allocation, refunds and cosmetics. `powers/` holds every
handler. The old `boons/`, `oaths/`, `PrestigeLevelConfigData` and `OathUI` are gone.

**Rules as implemented.**

- Mechanic powers (harvest bounty, fishing, skill echo, perfect form and the like) only work once a
  major, oath-bound or apex node grants them; minors on their own leave the value dormant, and the
  compendium shows it as dormant. Stat powers (armor, damage, regen) work from any node.
- Every power has a cap on its summed value (`PowerInfo`); damage-dealt oaths never take a hit
  below 20%.
- Spent points are worked out from the allocated nodes, never stored. Allocated nodes that don't
  exist in the current maps, or no longer link back to the root, cost nothing and do nothing, but
  stay in the save until the player changes that deity, so joining a server with different maps
  doesn't wipe anything.
- The built-in maps hold only implemented powers (99 nodes across seven deities, every arm the plan
  lists that phase 1 could fill). Arms that need new mechanics (Vör's Nothing Hidden, Skaði's Peaks,
  Freyja's Falcon Cloak) arrive in phase 4.

**Config.** `UseStarMapsFile` (off by default) switches from the built-in maps to
`BepInEx/config/AesirLineage/StarMaps.yaml`, which is written with the current built-in maps when
missing. The server sends its tributes and star maps to clients on join and again when either file
changes; clients apply them and go back to their own when they leave.

**Tribute bridge** (replaced by the tribute steps in phase 3). Each shrine tribute granted star points for its deity: ordinary
tributes 1 point, up to three times per character; devotion (prestige) tributes 3 points, with no
limit beyond needing every boss key, which they take away. Requirement checks now
count items (they only checked the item was present).

**Console commands.** `al-list [deity]`, `al-allocate <deity> <node>`, `al-refund <deity> <node>`,
`al-powers`; admin only: `al-points <deity> <amount>`, `al-reset <deity|all>`,
`al-give-points <player> <deity> <amount>`, `al-remove-points <player> <deity> <amount>`. The last two
reach another player through the server (`modules/AdminPoints.cs`), which checks the admin against its
own adminlist.txt by the connection the request came on, as the game's kick and ban do. Tab completes
every argument, not only the first as the game does (`common/ArgumentCompletion.cs`): deities, online
players, the nodes that can be taken or given back, and each command's keywords.

**Fixed while porting**, beyond the formulas in the boon review: outgoing damage boons now apply
where the attacker is (`Character.Damage`), not on whoever owns the target; regen boons no longer
speed up creatures the player owns; "when hit" powers ignore dodged and fully blocked hits; tower
shields are detected by their parry bonus (no shield had the 0 the old check wanted); Fishing
Prosperity asked for a "Coin" prefab that doesn't exist (it is "Coins"); the cosmetics description
skipped effects the player didn't have instead of ones they did.

**Known limits.**

- Kill powers (arrow return, wealth, speed and heal on kill) fire only for creatures this machine
  owns, which is usually what the player is fighting.
- New strings have English only; Vietnamese falls back to English for them.

**Checked offline, not in game.** A Cecil check resolves all 44 Harmony patch methods against
`assembly_valheim.dll` (target exists and is unambiguous, every injected parameter matches by name
and type). A .NET harness runs the data layer from the built DLL: the built-in maps round-trip
through YAML unchanged, a hand-written map with a bad link, an unknown power and an orphan node
loads with exactly those three warnings, a map with two roots is refused, `PowerSet` sums, caps and
gates as above, the allocation and refund rules hold, and the character save and the tribute
defaults round-trip. Both tools live in the session scratchpad, not the repository.

## Phase 2 as built

**Opening it.** `K` (client config `StarMapKey`), a star button at the end of the inventory's row of
round buttons (compendium, skills, trophies, achievements, PvP and any Clans buttons, spread out
again to fit it on the board; under Auga, a link in its row under the player name), or the Star Map
button on any shrine, which opens that shrine's deity. Esc, `K` or the X closes it. The hotkey
does nothing while typing or while another screen has the cursor.

**What is on screen** (`ui/StarMapWindow.cs`):

- The deity's portrait in the centre with a slow glow, ring 1 drawn just outside it.
- Stars sized by tier, with a ring for majors (palette colour), oath-bound stars (red) and apex
  stars, in four states:
  - taken: bright palette glow;
  - available: pulsing;
  - reachable but unaffordable: dim;
  - locked: grey.
- Links in one mesh with a soft halo: taken–taken links glow in the palette, links reaching out
  from what you have are fainter, the rest are grey.
- A starfield and two palette-tinted nebulae that drift and slide a little with the map, and a vignette.
- Deity tabs down the left use the portraits, not rune glyphs, since the game fonts may not have
  the runic block. Each tab shows points to spend.
- The header shows points to spend and spent, and the footer lists the controls.

**Controls.** Click to take a star, right-click to give one back (only if nothing else depends on
it), drag to pan, wheel to zoom towards the cursor. The map opens fitted to its bounding box, so
lopsided maps still fill the view.

**Tooltips.** Name, tier and cost, then every power the star grants, shown as it would be after
taking or giving it back, with the current value beside it. Mechanics left dormant say so, and a
power that would end says so. Oaths are red. The last line says what a click does, or why it can't.
`PowerSet.Preview` computes this without touching the live powers.

**Art.** Stars, glows, rings, link falloff, starfield, nebula and vignette are generated in code
(`ui/StarMapArt.cs`), since the bundle can't be rebuilt from here. Phase 5 can replace any of them
with authored art from the bundle without changing the window.

**Not in phase 2.** Flowing links, bursts travelling along links when a star is taken (a star
flares instead), sounds, gamepad control, a full-map overview of all seven deities.

**Checked offline, not in game.**

- It builds, and the patch check resolves all 45 Harmony patches.
- The data tests still pass after `PowerSet` gained `Preview`.
- An offline render used the window's geometry, palettes, real portraits and the built-in maps, on
  a sample allocation. No two stars' click areas overlap on any map (the tightest pair has 66 units
  to spare), and the nearest star edge sits 38 units outside the portrait.

**To check in game:**

- The shrine button's placement.
- Whether the Norse header font has "ð" and "ö" (Unity falls back to another font if not).
- That Esc closes only the map, both on its own and when opened over a shrine.

## Phase 3 as built

**Tributes** (`common/TributeData.cs`, `BepInEx/config/AesirLineage/Tributes.yaml`).

- Each deity has an ordered list of steps; a character offers them in order, once each, and each
  grants its points (1 by default).
- The steps climb from common trophies of the deity's biome to its boss trophy: Gefjun has 10, with
  crops from later biomes (raspberries to barley) since she is the harvest goddess; the others have 8.
- Every trophy name was checked against a loot-table dump taken from the running game (the Star Level
  System dump in the frostforged profile). The four that aren't creature drops (`Fish1`, `Carrot`,
  `Turnip`, `Raspberry`) appear throughout other mods' configs, and `Fish1` was already used by
  Valhalla Eternal.
- `UseTributesFile` (off by default) switches to the file, which is written with the built-in lists
  when missing. The server sends its tributes with the star maps.
- The phase 1 bridge (`Sacrifices.yaml`, per-tribute limits) is gone.

**Shrine** (`modules/SacrificeUI.cs`, same panel). It lists:

- the next step, as "Tribute 3 of 8", with the points it grants, any keys it waits for, and each item
  as held/needed, red while short; an unknown item shows its name and logs a warning, so a typo in
  the file is visible;
- "Reset <deity>'s star map" while the player holds any of that deity's stars. It costs the biome's
  boss trophy, gives every point back, and keeps the shrine open;
- the devotion (prestige) where the deity has one, as before, until Ascension.

Fixed in the panel while there:

- It took and released input blocks unpaired, so closing the inventory could hand control back
  while the star map was open.
- Escape went through legacy `Input` in `Update`, before the game menu's check. It now uses `ZInput`
  in `LateUpdate`, skipping the frame the star map closed on.

**Soul of Power** (`souls/`).

- **Spawn.** When a boss dies, its owner checks the boss's defeat key against the `SoulBosses` config
  (`defeated_eikthyr=Gefjun,…`). If our world key `al_soul_<key>` isn't set, the owner sets it and
  raises a soul 1.5 m above where the boss fell. Players within 60 m see "A Soul of <deity> rises…".
- **Claim.** Use it within reach: the boss key goes on the character's claimed list, and the deity
  grants `PointsPerSoul` points (default 1). The soul dims for players who have claimed it.
- **Persistence.** It is a persistent networked object built from a Jotunn empty prefab (the cube
  removed). Deity, boss key and birth time live in its ZDO, so it survives zone unloads and
  restarts. Its owner removes it after `SoulLifetimeMinutes` (default 30; 0 keeps it).
- **Placeholder look**, all code:
  - an outer glow in the deity's main colour and an inner glow near white;
  - a star core with the deity's rune inside, the runes drawn as glowing strokes (`ui/RuneArt.cs`),
    as in the reference image;
  - a 14 m beam, a turning ring on the ground, and a pulsing point light;
  - the closest of the five aura effects already in the bundle.

  Phase 5 replaces this with an authored prefab.
- **Not yet.** Souls rising again for players who have Ascended (phase 6, which also clears claims),
  and the Deep North boss's deity (open question; add it to `SoulBosses` once decided).

**Checked offline, not in game.**

- It builds, and all 46 Harmony patches resolve.
- No class overrides `Character.OnDeath` except `Player`, so the spawn hook sees every boss.
- The data harness now also covers the tribute defaults (7 deities, 58 steps, each with items and
  points; 7 reset costs, 5 devotions) and a refused tributes file that keeps the old ones loaded.
- It also covers the `SoulBosses` parsing (defaults, spaces, case, bad pairs warned and skipped), and
  the new save fields round-tripping.

**To check in game:**

- The orb's look: which shader the game provides, and whether the rune reads well.
- That the soul can be hovered and used.
- That a second kill of the same boss leaves nothing.
- That the soul is gone after its lifetime, including after a server restart.
- That several players can each claim once.

## Phase 4 as built

**Maps** (`starmap/DefaultStarMaps.cs`). Every deity has the same shape, built by one `Arm` helper:

- Root, three arms of 11 stars, and three bridge stars: 37 per deity, 259 in all.
- An arm runs two minors out from the portrait to its first major. It then splits into two
  branches of minor, major, minor, which meet again at the oath-bound star. The apex lies beyond it.
- A bridge star sits on the inner ring between each pair of arms, linking their second minors, so
  a build can cross over early.
- Each map's three apexes need Ascension 1, 2 and 3, one per arm.
- Every star costs 1 point, as the tier table proposes.
- Ids: majors, oath-bound stars, apexes and bridges are named (`seeds_of_the_gods`); minors are
  `<arm>_<n>`, numbered outwards (`furrow_1` to `furrow_6`).

**Where the maps differ from the draft tables above:**

- An arm has three majors. Where the draft had two, the third is new (Gefjun's Hearth Guard: armor
  and block stamina), or a minor takes its place (Vör's Lore of War).
- Vör's Eureka is a major in a minor's slot, because a mechanic only works from a major.
- Skaði's Blizzard-born stays Stormbound Rage, the old boon. It already adds frost in snowstorms.
- Yoke of the Four Oxen adds a chance per hit on trees, logs and ore to throw off an extra piece,
  not a flat 20% more wood and ore.
- Raven's Eye reveals the map around you once a minute. There is no bird's-eye camera.
- Unveiling's marks raise only your own damage, not everyone's.
- Learned from the Fallen gives damage per star of the creature killed, not one of its traits.
- Field Dressing collects everything a bow or crossbow kill drops, for 15 seconds, not only trophies
  and hides.
- Sealed Wall turns aside a share of elemental, poison and spirit damage while blocking with a tower
  shield. Tower shields still can't block spells outright.
- Brutal Defiance has no shield bash. Every third block within 6 s releases the burst.
- Battle Medic heals while blocking, not on the shield's secondary attack.
- Skis of Skaði make running on snow cheaper and a little faster. There is no sliding.
- Leap raises jumps made while running. Jumps are not charged by holding.
- Fólkvangr and Náglfar's Crew are chances per kill. A player has at most three summons at once,
  and Náglfar's thralls last 20 s.
- Mother's Ward, Stored for Later and Stone Skin all feed one Fortification pool. Its strength per
  stack is summed from every star that grants it, and stacks never cut damage by more than half.

**Mechanics and where they came from.** Plain stats (damage, armor, regen, stamina costs,
resistances) are left out of this table.

| Deity | Power | From PoV | Hook |
| --- | --- | --- | --- |
| Gefjun | Seeds of the Gods | crops in any biome | `Piece.SetCreator` flags the plant's ZDO; `Plant.UpdateHealth` lifts biome, heat and cold limits; the build ghost drops `m_onlyInBiome` |
| | Yoke (extra yield) | — | `Damage` prefixes on trees, logs, rocks, deposits, mined breakables |
| | Hearty Meal, Hearth-Mother | food scaling, ally aura | `Player.GetTotalFoodValue`; the share is published on the player's ZDO |
| | Gefjun's Feast, Beloved | trinket aura | `Player.ConsumeItem` heals allies or re-applies the mead |
| | Shelter, Fortification, Mother's Ward | Endurance, Fortify | `Character.ApplyDamage` prefix; `Humanoid.BlockAttack` |
| | Bloom of Zealand | Immobilize | lethal blow left at 1 health, enemies rooted |
| | The Plough | Fault Line | `Humanoid.StartAttack` postfix, delayed line strike |
| Vör | Eureka, Völuspá | — | `Player.OnSkillLevelup` |
| | Ore Sense, Seeress's Map, Tracks, Raven's Eye | MapSight, Hugin's Flight | temporary unsaved minimap pins, rescanned every few seconds; `Minimap.Explore` |
| | Critical hits, Certain Blow | Precise Calculation | outgoing multiplier |
| | Foresight | evasion | first hit from each enemy in 20 s is cancelled |
| | Unveiling | Impale | marks per target |
| | Learned from the Fallen | Hugin's Memory | damage window after a starred kill |
| Baldur | Sunstrike | Smite | crit on undead adds a spirit strike |
| | Cleansing Flame | kill explosions | `Character.OnDeath` burst |
| | Clean Parry, Returned Light, Bright Retort | parry burst | `Humanoid.BlockAttack` postfix on a parry |
| | Parry window | — | block timer nudged back into vanilla's window |
| | Untainted, wet resist | — | `SEMan.AddStatusEffect` refuses poison; a fresh Wet is shortened |
| | Oath of All Things, Invincible | — | incoming hit cancelled; `ApplyDamage` window |
| | Dawnbringer | aura ring | 1 s pulse and a point light |
| Skaði | Piercing Shot | pierce | the nearest enemy behind the target takes part of the hit |
| | Rain of the North | Rain of Arrows | `Attack.FireProjectileBurst` gets extra projectiles and spread |
| | Draw speed | — | `Humanoid.GetAttackDrawPercentage` worked out again from the draw time |
| | Shatter, Winter's Shatter | kill explosions | `Character.OnDeath` burst |
| | Child of Þrymheimr | — | refuses Cold and Freezing |
| | Double jump, Leap, Sure-footed | — | `Character.Jump`; `SEMan.ModifyFallDamage` |
| | Hang on the Wind | Hang on the String | `FixedUpdate` holds the fall while drawing |
| | Wolf-Sister | Bottled Essences | three bow kills summon a tamed wolf |
| Syn | Footing, the Unmoving Door | Unshakeable | `Character.AddStaggerDamage` |
| | Brutal Defiance, the Shut Door | thorns | `Humanoid.BlockAttack` postfix |
| | Sealed Wall | — | incoming elemental damage cut while blocking with a tower shield |
| | Strength of the Valkyries | — | `SEMan.ModifySkillLevel` |
| | Challenge | Provoke | `MonsterAI.OnDamaged` on the creature's owner, read from the player's ZDO |
| | Doors of Valhalla | RunicDome | dome strength on the player's ZDO, read by every player within 4 m |
| | Stone Skin | — | standing still fills Fortification |
| | The Last Door | ally links | routed RPC moves the killing blow to the guardian |
| Freyja | Echo | Spell Echo | `Attack.FireProjectileBurst` fires again without paying |
| | Storm-weave | chain lightning | lightning arcs between wet enemies |
| | Völva's Price | Blood Magic cost | `Player.HaveEitr` and `Player.UseEitr` take the shortfall from health |
| | Battle Fury | Rage | stacks per hit |
| | Falcon Glide, Falcon Form | Glide, Levitation | `FixedUpdate` caps the fall or hovers |
| | Charged Grace | Charged Attacks | a dodge or parry charges the next hit |
| | Battle Medic | — | 1 s pulse while blocking |
| | Fólkvangr | minion inheritance | summoned einherjar (`Skeleton_Friendly`) |
| | Brísingamen | Soul Vessel | eitr above half absorbs damage first |
| Hel | Retort | Ashen Crown | fire taken starts a 5 s fire aura |
| | Wildfire, Open Wound | kill explosions, bleed | poison bleed on hit, burst on death |
| | Pyre of Helheim | Eternal Bonfire | self-burn pulse, fire ×1.5, heat immunity |
| | Stored for Later | — | `Character.RPC_Heal` overheal feeds Fortification |
| | Death's Bargain | — | lethal blow left at 1 health, a kill within 15 s or the debt is paid |
| | Silent Passing | Vanish in Motion | `Player.Dodge` after a kill; stealth and noise mods |
| | Half-Dead | Soul Vessel | poison and spirit immunity |
| | Náglfar's Crew | Unstable Minions | thralls that burst into fire when badly hurt |
| | Queen's Decree | — | non-boss enemies below the threshold die to the hit |

**How it runs.**

- `powers/PowerTicker.cs` drives the timed powers from the plugin's `Update` and `FixedUpdate`. If a
  power throws, it is skipped for that frame and the error is logged once per kind.
- `PlayerData`'s load prefix resets every power's timers, stacks and pins along with the power set.
- Shared powers (dome, Last Door, Challenge, Hearth-Mother) are small values on the player's own ZDO,
  written only when they change.
- `PowerFx` shows timed powers as status effects with the deity's rune as the icon, and roots enemies
  with one that zeroes their speed.

**Fixed during review:**

- Food duration scaled `UpdateFood`'s `dt`, which also paces food regen. Only the burn timer is
  nudged now.
- A slower bow draw never showed, because vanilla clamps the draw at full. The percentage is now
  worked out again from the draw time.
- Field Dressing would have tried, and complained, every frame with a full bag.
- The double jump would also have fired while swimming, dodging, staggered, or when the jump button
  pulls a grappling hook.
- The Plough counted bows, which vanilla treats as two-handed.
- The shrine's starting text asked for two keys that never existed (carried over from Valhalla
  Eternal).

**Checked offline, not in game.**

- It builds, and all 88 Harmony patch methods resolve, with argument names checked against the game.
- The data harness checks:
  - all 259 stars survive the YAML round trip and load without warnings;
  - every power has its info, and every map's minors only feed mechanics that a stronger star in
    that map can switch on;
  - oaths sit only on oath-bound and apex stars;
  - each map's apexes need Ascension 1, 2 and 3;
  - no map can push a power past its cap;
  - sample totals: the Yoke, Dedication, Stone Skin with the Unmoving Door, Mother's Ward, Eureka.
- Every power, status effect, pin and message has English text, and every `$al_` key in the code
  exists.
- In the layout preview no two stars' click areas overlap. The tightest pair has 60 units to spare,
  and the window opens zoomed out to about half.

**To check in game, riskiest first:**

- The Last Door's RPC, the rune dome and Hearth-Mother between two players.
- Echo with summoning staffs, and Rain of the North's extra arrows (they should not use ammo).
- That summons follow, fight and leave on time, including Náglfar's burst.
- How the double jump, glide, hover and Hang on the Wind feel. Also that the fall-damage reset
  doesn't let a player skip real falls.
- That Seeds of the Gods places and grows crops outside their biome.
- Field Dressing pickups when another player owns the drops.
- That the status-effect icons show, and that rooted enemies stop.
- Balance in general: every number in the maps is a first draft.

**Not yet:**

- Vietnamese text for the new powers (it falls back to English).
- The plan's per-power condition and trigger-window fields. Minors are plain stats.
- Effects beyond the four effects already in the bundle (phase 5).
- Six older powers no built-in star grants (`RangedDamage`, the three per-type damage oaths, and the
  stamina and eitr oaths). They stay available to server maps.

## Phase 5 as built

**The Unity project.**

- Opened and upgraded to Unity 6000.0.75f1.
- Shader Graph added. Piloto's materials use its `UberFXSG` graph, which has a Built-In pipeline target but compiles only with Shader Graph installed. Before this, every Piloto material was Unity's pink error shader.
- 57 Piloto materials put back on Piloto's shader. Valhalla Eternal had switched them to `Legacy Shaders/Particles/Alpha Blended`, which drops their alpha masks, so they drew as squares. Their original settings were still saved in each material.
- Three stray `shared/...` bundle labels on Piloto materials cleared, so nothing the mod uses lands in a bundle it never loads.

**Building the effects** (`AesirLineageUnity/Assets/AesirLineage/Editor/VfxBuilder.cs`, menu *Aesir Lineage*).

- A recipe table turns Piloto prefabs into 53 prefabs named `al_vfx_<deity>_<role>`, the name the mod asks the bundle for.
- Each one is unpacked, has Piloto's scripts stripped (the game doesn't have them) and shadows turned off, and is scaled and placed.
- The builder deletes prefabs whose recipe is gone and caps the Windows size of every Piloto texture they use at 512.
- *Build asset bundle* writes `AssetBundles/StandaloneWindows/aesirlineage`, which the mod's build embeds as before.
- The bundle is 9.5 MB, up from 2.6 MB; it was 16.2 MB before the texture cap.
- `VfxPreview.cs` lays effects out against a dark backdrop with a player-sized figure, for comparing them in the Scene view.

| Deity | Piloto kit | Notes |
| --- | --- | --- |
| Gefjun | Nature | Leaf Spray for harvest, yield and the Plough's furrow |
| Vör | Universe | Arcane level-up flash for Eureka and Völuspá |
| Baldur | Holy | |
| Skaði | Frost | Frostmist Orb for the soul; Orb_Frost is a white blob |
| Syn | Fire | the Sunfire ring as aura and soul ground, Solar Protection Dome's rune circle as the rune dome |
| Freyja | Fairy | the Celestial Lotus as aura |
| Hel | Mist | teal suits her corpse-blue palette; Dark is red, Void purple |

Roles per deity:

- soul orb;
- aura and wings;
- footstep, proc, burst and kill.

The loot beams were dropped: they are hair-thin lines. The `EnchantedGround` rings were dropped too, because they draw nothing without a depth texture. Projectile weapon glows and the Guardian Halo were also tried and dropped: one is invisible at rest, the other a dim smudge.

**Three shaders of our own** (`Assets/AesirLineage/Shaders`):

- `UI Additive`: star, portrait and tab glows add light.
- `UI Link Flow`: pulses travel outward along taken links. It reads the distance from the root out of the links' second UV channel.
- `World Additive`: for effects drawn in code; it fades with fog.

All three handle `RectMask2D` or fog like the stock shaders.

**In the mod** (`visuals/`).

- **`DeityVfx`** loads the prefabs and the shaders.
  - It plays a one-shot locally, or shares it: a routed RPC to everyone, played within 80 m.
  - Without a role's prefab it falls back to that deity's general proc or burst, then to a burst of glows in the deity's colours drawn in code.
  - If a shader reports unsupported on some machine, its materials get the World Additive shader instead of going pink.
  - It doesn't load on a dedicated server, where every shader reports unsupported.
- **Power effects.** `PowerFx.Effect(power, role, …)` plays in the colours of the deity whose star granted the power (several deities share some powers). The four shared Valhalla Eternal effects are gone.
  - New effects mark Perfect Form, Charged Grace, Foresight and Oath of All Things dodges, Retort, Stone Skin, Death's Bargain, the Last Door and Völuspá.
  - Area bursts, summons, kill effects and the shrine and soul rewards are shared. Small procs stay local.
- **The Soul of Power** is the deity's Piloto orb with the rune glowing inside. The drawn beam stays, the ground ring is now a turning ring of the deity's runes (Syn: the Sunfire ring), and the deity's aura plays at its base. Claiming it bursts in the deity's colours for everyone nearby.
- **Looks** (cosmetic slots): aura, wings, footprints, weapon glow and kill effect. Each deity has one look per slot. The rune crown is still in code but hidden: it is left out of `Cosmetics.Slots`, so no star offers it, the Looks panel doesn't list it and no one shows it.
  - A star's `cosmetics` list offers its deity's look while the star is held.
  - By default, every arm's first major offers the aura, and its two branch stars offer the arm's own look: the weapon glow on the Left and Upper Right arms, footprints on the rest. The oath-bound star offers the kill effect and the apex the wings.
  - Devotions unlock their deity's looks for good (Ascension will too).
  - A slot with no choice shows the deity the player has given most stars, so an aura shows as soon as its star is taken.
  - The **Looks** panel on the star map steps each slot through the deities available, or None. Star tooltips name the look a star offers.
  - **Sync and parts.** Every player carries a `PlayerVisuals` component.
    - The local player publishes `Aura=Gefjun|Wings=Hel` to their ZDO, and other clients parse it with unknown slots and deities dropped.
    - Wings sit on the back, turned to face Valheim's +Z.
    - The rune crown is code: the deity's rune circling above the head over a halo ring.
    - The weapon glow is code: sparks from the held weapon's bounding box, since the game's meshes can't be read at runtime.
    - Footprints come from the game's own step RPC, which reaches every client. Each step plays the deity's step effect and a glowing print that fades.
  - The old cosmetics panel and Valhalla Eternal's wings and auras are gone, and those prefabs are out of the bundle.
- **The star map.** Glows are additive, and light flows along the stars you hold. Two rings of the deity's rune turn around the portrait in opposite directions, and taking a star sends sparks along its links into it.

**Checked offline, not in game.**

- It builds, and all 86 Harmony patches resolve.
- The data harness also checks:
  - every map offers each look from the expected stars;
  - an unknown look in a map is warned about and skipped;
  - the new save fields round-trip;
  - the default look, None, falling back from a look no longer held, permanent unlocks, and the ZDO string;
  - a hostile looks string from another client.
- The built bundle was opened in Unity: all 53 effects and the 3 shaders load by name, and the shrine prefabs now resolve their `SacrificeUI` script.
- Every effect was looked at in the Scene view. The rune rings were rendered by the mod's own code inside the editor.
- `$al_` keys: none missing.

**To check in game:**

- That Shader Graph's Built-In output renders in Valheim. If it doesn't, the log names each shader, and materials fall back to a plain glow.
- The scale and placement of wings, auras and the rune crown on the real player model, and the rune dome's reach.
- That the weapon glow follows weapon swaps, and that footprints and kill effects show for other players.
- How busy combat gets with procs on. Every number here is a first pass.
- The bundle's memory cost in a full play session.

**Not yet:** the Ascension looks (phase 6), Vietnamese text for the new strings, and sounds.

## Phase 6 as built

**Ascension replaces the devotion** (`modules/Ascension.cs`, the shrine in `modules/SacrificeUI.cs`).

- Each deity's shrine offers "Ascend with <deity> (n of 3)" until the player has ascended with that deity `MaxAscension` times (default 3, up to 10). Levels 1 to 3 each open one apex star.
- **The deity's part of the price** is its `ascension` entry in the tributes:
  - items, every boss's key, and 3 points;
  - the old devotion costs are kept, and Vör (The Elder's trophy) and Freyja (the Queen's trophy) get one;
  - `Tributes.yaml` now has `ascension` where it had `devotion`.
- **The server's part of the price** comes from config: `AscensionSkillLoss` (50%), `AscensionForgetsRecipes` (on), `AscensionReturnsToStart` (on) and `AscensionRemovesBossKeys` (on).
  - Removing the boss keys is Valhalla Eternal's behaviour. On a shared world those are the world's keys, so one player's Ascension resets boss progress for everyone. The setting and the shrine text both say so.
- The shrine lists what the next level opens (the apex star by name), the looks it keeps, the souls, then every cost in red and the keys still missing. Since Ascension can't be undone, the offer must be made twice within 5 seconds.
- **What it gives:**
  - the level;
  - the deity's points;
  - looks kept for good, one per level: aura at 1, footprints at 2, weapon glow at 3, kill effect at 4, wings at 5;
  - fresh claims on every boss soul;
  - a shared burst at the shrine.
- The prestige number by a player's name is now the total of their Ascensions, worked out from the save. The VE devotions' own count is gone, along with the `AL-Set-Player-Level` command. `al-ascension <deity> <level>` sets a level for testing, without costs or rewards.
- Valhalla Eternal's devotion types (`Sacrifice`, `PlayerResetData`, `PrestigeEffectDetails`, `PrestigeEffect`) and their text are gone.

**Soul re-claims** (`souls/SoulBosses.cs`).

- A boss still leaves its soul the first time it dies in a world.
- It leaves one again when a player within `SoulReclaimRange` (60 m) has Ascended at least once and has not claimed that boss's soul since their last Ascension.
- The boss's owner can't read other players' saves. So each player publishes their Ascension total and their claimed boss keys on their ZDO (`ALLevel`, `AL_Claims`), written only when they change.
- Anyone who hasn't claimed it can take a re-claim soul, as with the first.

**Ascension looks** (`visuals/`).

- The looks string carries the wearer's Ascension with the look's deity (`Aura=Gefjun:2`). Levels from other clients are clamped to 0–10, and an entry with an unreadable level is dropped.
- Ascended forms grow with each level:
  - wings: a little larger;
  - weapon glow: more sparks;
  - kill effect: bigger.
- Auras and footprints have no ascended form: the rune ring under the aura and the rune footprints were removed.

**The apex ring** (`ui/StarMapWindow.cs`). A fine ring with tick marks runs through the apex stars and turns slowly. It is dim until the player ascends with the deity, then brighter with each level. Each deity tab has a star per Ascension level, lit as reached, and the header adds "Ascension n of 3".

**Checked offline, not in game.**

- It builds, and all 86 Harmony patches resolve.
- The data harness checks:
  - every deity's Ascension asks for items and all seven bosses, for 3 points;
  - Ascension 1 with Gefjun opens Bloom of Zealand;
  - an apex star counts only once its level is reached;
  - the looks kept per level;
  - the looks string with levels, including clamping and dropping a bad entry from another client.
- Every `$al_` key used exists; the devotion's text is removed.

**To check in game:**

- A full Ascension at a shrine: the confirmation, the costs, the return to the start, points and apex stars.
- That a re-claim soul appears for an Ascended player and not for others, on a server too.
- The ascended looks on other players.
- What removing the boss keys does with VentureValheim's private keys.

## Rename follow-ups

- ~~Rebuild the asset bundle in Unity.~~ Done in phase 5. The bundle is built as `aesirlineage`, and
  the shrine prefabs resolve `AesirLineage.modules.SacrificeUI`.
- ~~Upgrade the Unity project.~~ Done in phase 5: it is on 6000.0.75f1.
- Prefab and asset names inside the bundle keep their `VE_` names (`VE_Tribute_*`, `VELocalHud`,
  `VERemoteHud`); they get renamed when the shrines and HUD are rebuilt in phases 3 and 5.
- `origin` is still `MidnightsFX/ValhallaEternal`, and `release.yml` publishes to GitHub, Discord and
  Thunderstore. Create or rename the GitHub repository before the first Aesir Lineage release, or
  its release lands on the Valhalla Eternal repository.
- Valhalla Eternal deprecation: a last release with a README notice pointing to Aesir Lineage (and
  the BinaryFormatter fix) would come from git history, since its folder is now renamed.

## Open questions

- Deep North boss: give its soul to Skaði, or add an eighth deity (Ullr — winter, archery,
  skis — is the closest fit)?
- Points from tributes per deity per playthrough (default proposal: 12, escalating).
- Should star map points be spendable anywhere, or only at a shrine? (Proposal: anywhere; resets
  and Ascension at the shrine.)
- Code names: `Skaldi` should become `Skadi` and `Freya` `Freyja`, now that no save migration is
  needed.
