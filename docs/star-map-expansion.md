# Star map expansion — six arms per deity

Each deity's star map grows from three arms to six. The new arms are paid for by halving what the
small stars give, and lattices of small stars then fill the gaps between neighbouring arms, joining
them in more places. A full map ends with four times as many small stars as before, each at half
strength.

First draft for review: names, numbers and which ideas make the cut are all open.

## Shape

### Six arms

Today the arms point up (90°), lower left (210°) and lower right (330°), with a bridge star halfway
between each pair. The new arms take the bridges' places, so every arm is 60° from the next:

| Angle | Arm | Branch look |
| --- | --- | --- |
| 90° | existing "Up" arm | Footsteps |
| 150° | new arm, upper left | Footsteps (was Crown; the crown is now hidden) |
| 210° | existing "Left" arm | WeaponGlow |
| 270° | new arm, down | Footsteps |
| 330° | existing "Right" arm | Footsteps (was Crown; the crown is now hidden) |
| 30° | new arm, upper right | WeaponGlow |

- Every arm keeps today's 11-star shape: two minors, a major, a split into two branches of minor,
  major, minor, the oath-bound star, then the apex. Reach is unchanged: 3 points to the first
  major, 7 to an oath.
- Each new arm sits between the two old arms it is closest to in theme (listed per deity below).
- Branch looks alternate around the circle so neighbouring arms never offer the same one. Aura,
  kill effect and wings stay on the first major, the oath-bound star and the apex.
- Six bridges instead of three, at 0°, 60°, 120°, 180°, 240° and 300°, each linking the second stars
  of the two arms beside it.
- Eight lattice stars in each of the six gaps between arms (see "Lattices" below).
- Per deity: root, 66 arm stars, 6 bridges and 48 lattice stars, 121 in all (37 before); 847 across
  the seven maps.

### Lattices

The small stars on a map doubled again with a lattice in every gap between two neighbouring arms.
Each lattice joins the two arms in more places than the bridge does, so a build can cross over at
the branches and near the branch tips, not only close to the centre.

- **Eight stars a gap**, 48 a map. The first leads out from the bridge, the next two reach the first
  branch star on each side, and the rest reach the outer branch stars.
- **Two shapes, alternating round the map.** The *weave* (gaps at 0°, 120° and 240°) is a diamond
  across the gap that closes beyond the branch tips, with one star further out. The *ladder* (60°,
  180° and 300°) runs a rail beside each arm, joined by a rung and closed at the top.
- **Second levels.** A lattice star that touches an arm repeats the arm star it touches, with the
  same name and value. The first star repeats the bridge. Where the touched star is a major (Vör's
  Eureka), the star repeats the arm's first small star instead.
- **Woven stars.** The stars in the middle of the gap, three in a weave and one in a ladder, carry
  half of an opening small star from each arm, under a name of their own: Grazing Field joins
  Gefjun's Furrow and Herd, Dairy her Herd and Table, and so on.
- The lattices are routes, not shortcuts: reaching a major through a neighbouring arm's lattice
  always costs more stars than its own arm does.

| Deity | Woven stars, anticlockwise from 120° |
| --- | --- |
| Gefjun | Grazing Field · Dairy · Longhouse · Hearth-ward · Fence-line · Ploughshare |
| Vör | Apprenticeship · Whetted Lore · Studied Strike · Shadow-sight · Secret Oath · Sworn Study |
| Baldur | Hall-light · Clean Hearth · Sea-washed · Gunwale · Sunlit Shield · Daystar |
| Skaði | Stalker · Fur-trapper · Ice-hunter · Crag-spear · High Pack · Running Hunt |
| Syn | Doorpost · Gate-hall · Pike-gate · Haft and Edge · Edge Denied · Denied Ground |
| Freyja | Witch-brew · War-mead · Shield-sister · Feathered Vow · Gilded Wing · Seiðr-gold |
| Hel | Fever · Wasting · Grave-tithe · Death-march · Night Reaper · Corpse-fire |

### Apexes and Ascension

**Six Ascension levels, one apex each.** The order follows one rule for every deity: utility and
support apexes open early, build-defining combat apexes in the middle, and the strongest (cheating
death, big auras, summons on most kills) last.

| Deity | A1 | A2 | A3 | A4 | A5 | A6 |
| --- | --- | --- | --- | --- | --- | --- |
| Gefjun | The Four Oxen | Hearth-Mother | Bearer of the Land | The Plough | The Swarm | Bloom of Zealand |
| Vör | Weapon-lore | Völuspá | Unveiling | Oath-ring | Read the Fight | Foresight |
| Baldur | Ship-Caller | Bright Retort | Dawnbringer | Radiant Judgment | All Things Wept | Invincible |
| Skaði | Þjazi's Eyes | Wolf-Sister | Endless Pack | Hang on the Wind | Winter Spear | Winter's Shatter |
| Syn | Ban the Kind | Stone Skin | Hold the Gate | Doors of Valhalla | Verdict | The Last Door |
| Freyja | Longing for Óðr | Elixir of Seiðr | Falcon Form | Brísingamen | Tears of Red Gold | Fólkvangr |
| Hel | Garm's Hunger | Terror of Niflhel | Rotting Ground | Náglfar's Crew | Soul Eater | Queen's Decree |

Some placements are on purpose:

- Weapon-lore opens first because it builds up over many kills.
- Völuspá sits at A2 because Ascending halves your skills, so level-ups come quickly afterwards.
- Ship-Caller opens first because it saves time on every playthrough.
- Wolf-Sister and Endless Pack sit next to each other, so the summoned wolves soon join the pack.

What changes in code:

- Today's apexes all move to new levels: the `ascension` argument of each `Arm` call in
  `DefaultStarMaps`.
- `MaxAscension` defaults to 6 (it already allows up to 10), and its description stops saying levels
  1 to 3.
- The looks kept per level: aura at 1, footprints at 2, weapon glow at 3, kill effect at 4, wings
  at 5, and none at 6 since the rune crown was hidden (`Ascension.LooksByLevel`).
- The star map tab draws at most five Ascension stars (`Mathf.Min(Ascension.Max, 5)` in
  `StarMapWindow`); it needs six.
- The data harness checks that each map's apexes need levels 1 to 6.
- Each Ascension still gives 3 points, so six of them give 18.

### Layout check

Small stars are clicked within 40 units, majors and oath-bound stars within 58, apexes within 80.
With the lattices in, the tightest pair on any map is a lattice star beside a branch major, with 54
units to spare between their click areas (60 before the expansion).

## Small stars

**Rule:** every minor and bridge value halves. Flat carry weight and eitr round up to whole numbers,
Rising Fury rounds down to 0.1, and Morning Light changes stat (half a spirit point is too small to
show).

- With twice as many arms, a full map's total of small stats stayed the same; the lattices then
  doubled it.
- A player walking one arm gets half the stats they did on the way to its majors. The majors, oaths
  and apexes keep their values.

| Deity | Minors, now → new | Bridges, now → new |
| --- | --- | --- |
| Gefjun | Strong Back 20 → 10 carry · Tilled Earth 5 → 2.5% · Furrow 3 → 1.5% · Full Nets 4 → 2% · Hearty Bread 3 → 1.5% · Slow Embers 5 → 2.5% · Second Helping 5 → 2.5% · Shared Table 3 → 1.5% · Hide of the Ox 3 → 1.5 armor · Steady Arm 4 → 2% · Sheltered 5 → 2.5% | Harvest Home 2 → 1% food · Hearthstone 2 → 1 armor · Field Hand 15 → 8 carry |
| Vör | Curiosity 3 → 1.5% · Second Thought 3 → 1.5% · Idle Study 5 → 2.5% · Studious 1 → 0.5% crit · Keen Strike 5 → 2.5% · Lore of War 3 → 1.5% · Far Sight 10 → 5% · Quiet Step 5 → 2.5% · Deep Sense 10 → 5 m · Seeress's Gaze 20 → 10 m | Hidden Lore 1 → 0.5% crit · Watchful 3 → 1.5% · Open Eyes 2 → 1% |
| Baldur | Dawnlight 4 → 2% · Sun-touched 2 → 1 spirit · Noonlight 15 → 7.5% · Pyre-light 5 → 2.5% · Clean Blood 5 → 2.5% · Dry Cloak 10 → 5% · Untouched 5 → 2.5% · Steady Hand 5 → 2.5% · Shield Arm 4 → 2% · Steady Eye 5 → 2.5% · Mirror Shield 15 → 7.5% | Morning Light 1 spirit → 2% undead damage · Pure Heart 3 → 1.5% · Bright Shield 3 → 1.5% |
| Skaði | Keen Eye 3 → 1.5% · Quick Draw 4 → 2% · Tracker 3 → 1.5% · Cold-hardy 5 → 2.5% · Rime 2 → 1 frost · Night Runner 2 → 1% · Splinters 5 → 2.5% · Light Step 5 → 2.5% · Long Stride 4 → 2% · Downhill 10 → 5% · Spring Leap 10 → 5% | Cold Aim 2 → 1% · Snow Stride 3 → 1.5% · Steady Draw 3 → 1.5% |
| Syn | Firm Stance 5 → 2.5% · Watchful 3 → 1.5% · Steady Guard 3 → 1.5% · Gatewarden 4 → 2% · Battering Shield 15 → 7.5% · Barred Door 4 → 2% · Keen Edge 3 → 1.5% · Hoarder 2 → 1% · Valkyrie's Lesson 3 → 1.5% | Braced 3 → 1.5% · Gate-iron 2 → 1 armor · Shoulder Blow 2 → 1% |
| Freyja | Eitr Well 5 → 3 eitr · Flowing Seiðr 5 → 2.5% · Sharp Seiðr 3 → 1.5% · Resonance 4 → 2% · Battle Breath 4 → 2% · Light Swing 3 → 1.5% · Precision 10 → 5% · Rising Fury 0.25 → 0.1% per stack · Feather Step 4 → 2% · Featherweight 5 → 2.5% · Gilded 2 → 1 armor · Gentle Hands 0.5 → 0.25 hp/s | War-song 2 → 1% · Light Feet 3 → 1.5% · Golden Tears 3 → 2 eitr |
| Hel | Ash-skin 5 → 2.5% · Ember-proof 5 → 2.5% · Smoulder 2 → 1 fire/s · Spreading Flame 5 → 2.5% · Jötunn's Breath 2 → 1 · Grave Thirst 0.5 → 0.25% · Aesir's Breath 2 → 1 · Executioner 4 → 2% · Reaper's Step 3 → 1.5% · Deep Cuts 3 → 1.5% | Cinder Blood 3 → 1.5% · Last Breath 1 → 0.5 stamina · Grave Edge 2 → 1% |

New arms' minors are written at the new scale from the start.

## Deities

For each deity: the big stars it has today, then three new arms. Each new arm lists its minors (the
pair it opens with, which repeats on the two branches, and the two feeders at the branch ends),
three majors, an oath-bound star and an apex. "From" names the mod an idea was taken from, where it was; as with Path of Valheiman, only
the idea carries over and the code is written fresh.

### Gefjun, the Plough-Mother

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Furrow (90°) | Bounty of the Lands: 10% extra harvest · Seeds of the Gods: crops grow in any biome · Fishing Prosperity: 10% bonus find per fish | Yoke of the Four Oxen: +150 carry, 15% extra piece from trees, logs and ore. *Oath: 15% less damage* | Bloom of Zealand (A6): cheat death every 10 min and root enemies within 6 m. *Oath: 20% less damage* |
| Table (210°) | Quality Nourishment: +8% food stats · Hearty Meal: strongest food +35% health · Gefjun's Feast: eating heals players within 6 m for 10% of the food | Feastbound: +25% food stats. *Oath: food burns 25% faster* | Hearth-Mother (A2): players within 8 m gain 15% of your food health. *Oath: 15% more damage taken* |
| Ward (330°) | Basic Protection: +6 armor · Shelter: 15% less damage below 35% health · Hearth Guard: +6 armor, 8% cheaper blocks | Mother's Ward: blocks build Fortification. *Oath: 10% slower* | The Plough (A4): a two-hander's secondary cuts an 8 m furrow for 120% damage. *Oath: 25% less stamina regen* |

**New arms.** Anticlockwise from the top: Furrow · **Herd** · Table · **Homestead** · Ward · **Land-taker**.
Gefjun stays the economy goddess: two of the three are non-combat, and Land-taker turns hauling into
strength.

**Herd** (150°, between Furrow and Table; apex at A1). Her four ox-sons: taming, breeding and draught beasts.
Minors: Gentle Hand +4% taming speed · Good Pasture +3% tame health (each twice) · Fruitful +5% (feeds Fruitful Herd) · Broad Yoke +5% (feeds Draught Ox).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Ox-Mother | Tame 25% faster; your tames have 15% more health | `TamingSpeed`, `TameHealth` | ValheimOne, PoV |
| Fruitful Herd | Tames near you breed 25% sooner and their young grow up 25% faster | `FruitfulHerd` | PoV, ValheimOne |
| Draught Ox | Carts are 30% lighter, and a tame following you can be harnessed to one | `DraughtOx` | PoV (Beast of Burden) |
| Never Hungry (oath) | Tames within 30 m stay fed. *Oath: your food burns 20% faster* | `HerdFed` | PoV |
| The Four Oxen (apex) | Each tame within 20 m, up to four, gives +50 carry weight and 4% less damage taken. *Oath: 15% less damage* | `FourOxen` | new |

**Homestead** (270°, between Table and Ward; apex at A5). The hearth, the working household and the hives.
Minors: Warm Hall +5% rested duration · Busy Hands +3% station speed (each twice) · Hearthside +0.25% per comfort (feeds Hearth Strength) · Humming +5% (feeds The Swarm).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Well-Rested | +1 comfort; rested lasts 25% longer | `Comfort`, `RestedDuration` | Jewelcrafting, PoV |
| Hearth Strength | While rested, each comfort level gives +0.5% damage and stamina regen | `HearthStrength` | PoV |
| Busy Household | Smelters, kilns, ovens, fermenters and cooking stations within 15 m work 20% faster | `StationSpeed` | Seasons |
| Hermit (oath) | +2 comfort; hives and crops within 30 m of your bed yield 25% more. *Oath: 10% slower* | `Hermit` | PoV (Hermit keystone) |
| The Swarm (apex) | Bees follow you and sting enemies within 4 m for poison each second, twice as hard near your hives. *Oath: 20% less max stamina* | `Swarm` | PoV (Guard Bees) |

**Land-taker** (30°, between Ward and Furrow; apex at A3). Tearing land from the earth: axes, pickaxes and the load you carry.
Minors: Woodsman +2% damage to trees · Stonebreaker +2% damage to rock and ore (each twice) · Rich Vein +1.5% (feeds Rich Seam) · Trail +1% (feeds Furrow Stride).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Clear the Land | +15% damage to trees, rock and ore; chopping and mining cost 15% less stamina | `GatherDamage`, `ToolStamina` | PassivePowers, Almanac |
| Rich Seam | Switches on the extra-piece chance the Yoke grants, at 8% | `GatherYield` (exists) | — |
| Furrow Stride | +10% speed and 20% cheaper running on paths, roads and tilled ground | `PathSpeed` | PoV, VentureValheim |
| Ox-strength (oath) | Go over your carry limit without slowing; each kilo over costs stamina instead. *Oath: 25% less stamina regen* | `OverCarry` | RuneMagic |
| Bearer of the Land (apex) | +1% damage for every 25 kg you carry, up to 20%. *Oath: 15% slower* | `LoadStrength` | new |

**Alternates:** Reaper's Sweep (picking one crop picks every ripe one nearby and replants), Quickening
Soil (crops grow faster), Unspoiled Larder (food keeps full strength until it runs out), Skål (a toast
that buffs everyone who joins it), Isle-Plougher (Gefjun's sea-plough: faster ship, fair wind).

### Vör, the Watchful

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Insight (90°) | Wisdom of the Ages: 10% chance of double skill experience · Bladebound Knowledge: 10% of combat experience also trains a non-combat skill · Hunger for Knowledge: 25% chance a new recipe trains a skill · Eureka (in a minor's slot): 10% chance a level-up brings a second | Certainty: +40% skill experience. *Oath: lose twice the skill on death* | Völuspá (A2): a level-up gives +20% damage and experience for 10 s. *Oath: food burns 15% faster* |
| Lore (210°) | Knowledge Is Power: combat skills borrow 10% of your best non-combat skill · Learned from the Fallen: a starred kill gives +5% damage per star for 20 s · *(third slot is a minor)* | Certain Blow: every hit is critical, crits only +25% | Foresight (A6): an enemy's first hit misses, again after 20 s without a hit. *Oath: 20% less health* |
| Hidden (330°) | Ore Sense: ore within 40 m on the map · Tracks: last creature hit marked for a minute · The Seeress's Map: chests, dungeons and 2-star creatures within 80 m on the map | Raven's Eye: the map reveals 150 m around you each minute. *Oath: 25% less combat experience* | Unveiling (A3): hits mark, +2% damage per mark up to 5. *Oath: 25% less experience* |

**Change to Lore:** its right-branch major slot, a Lore of War small star today, becomes a major,
and the small star after it feeds the new major.

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Perfect Timing (major) | After a critical hit, attacks and movement are 10% faster for 4 s | `PerfectTiming` | PoV |
| Quickened (small star, replaces the outer Lore of War) | +2%, feeding Perfect Timing | `PerfectTiming` | — |

Faster attacks mean speeding up the attack animation, a stat the mod doesn't have yet (Jewelcrafting's
Berserk and Magic Supremacy both do it).

**New arms.** Anticlockwise from the top: Insight · **Careful Hand** · Lore · **Weak Points** · Hidden · **Vows**.

**Careful Hand** (150°, between Insight and Lore; apex at A1). Wisdom put to work: crafting and keeping gear.
Minors: Mindful 2.5% less wear · Sure Hands +1% max durability (each twice) · Thrift 1.5% (feeds Thrifty Craft) · Whetted 2% (feeds Masterwork).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Lasting Make | Gear wears 25% slower and has 10% more max durability | `DurabilityLoss`, `MaxDurability` | ValheimOne, Jewelcrafting |
| Thrifty Craft | 10% chance crafting or upgrading costs nothing | `CraftRefund` | CacoFFF |
| Masterwork | 10% chance a crafted or upgraded item is a masterwork: +10% damage or armor, kept on the item | `Masterwork` | new (item custom data) |
| Exacting (oath) | Everything you craft is a masterwork. *Oath: crafting and upgrading cost 50% more* | `Masterwork`, oath `CraftCostPenalty` | new |
| Weapon-lore (apex) | A weapon remembers its kills: +1% damage per 50, up to 20%. *Oath: gear someone else crafted does 20% less* | `WeaponLore`, oath `ForeignGearPenalty` | new |

**Weak Points** (270°, between Lore and Hidden; apex at A5). Nothing is hidden from her, least of all where to strike.
Minors: Quiet Blade +5% sneak attack damage · Studied +2% damage to weaknesses (each twice) · Soft Tread +5% stealth after a kill (feeds Keep the Secret) · Close Study +2% (feeds Know Thy Foe).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Unseen Edge | Sneak attacks on unaware enemies do 50% more | `SneakAttack` | BetterArchery |
| Know Thy Foe | +15% to damage types a creature is weak to; hovering a creature shows its weaknesses | `Weakness` | CreatureLevelControl |
| Keep the Secret | Killing an unaware enemy doesn't alert the others nearby | `QuietKill` | PoV |
| First Cut (oath) | Sneak attacks are always critical. *Oath: 15% less damage to enemies already fighting you* | `FirstCut`, oath `AlertedDamagePenalty` | new |
| Read the Fight (apex) | Hits on a staggered enemy count as sneak attacks. *Oath: 20% less max health* | `ReadTheFight` | new |

**Vows** (30°, between Hidden and Insight; apex at A4). Vár hears every oath sworn: an arm that pays you for the oaths you carry, in any deity's map.
Minors: Pledge +0.5% per oath, feeding Keeper of Vows · Bound 2%, feeding Bound Word (each twice) · Grudge-mark +5% (feeds Grudge) · Oath-tally +0.5% per oath (feeds Keeper of Vows).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Keeper of Vows | +3% damage and skill experience for each oath you carry, up to six | `VowKeeper` | new |
| Bound Word | The oaths you carry are 15% lighter | `OathEase` | new |
| Grudge | An enemy that hurts you takes 20% more from you for 20 s | `Grudge` | new |
| Vár's Witness (oath) | Players and tames within 10 m get half your Keeper of Vows bonus. *Oath: 10% less max health* | `VowWitness` | new |
| Oath-ring (apex) | Each oath you carry is a Fortification stack that never fades. *Oath: lose 50% more skill on death* | `OathRing` | new |

Vows stretches Vör toward Vár, the goddess of oaths. **Teacher** (nearby players earn a share of
your experience and have their skills raised toward yours) is set aside.

**Alternates:** Danger Sense (an icon when creatures have noticed you), Misdirection (a decoy rune
enemies target), Hoard-Omen (a treasure pin every few days), Seeker's Bones (the Wishbone finds
graves, chests and dungeons), Far-Sight (spyglass zoom), Weather Sense, Way Out (dungeon exit
direction), Forager's Sense, shorter Forsaken power cooldown.

### Baldur, the Shining

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Radiance (90°) | Light of Baldur: +15% damage to undead · Sunstrike: crits on undead add 50% as spirit · Cleansing Flame: undead burst on death for 20% of their health | Sunsworn: +6 spirit per hit. *Oath: 20% less damage at night* | Dawnbringer (A3): undead within 6 m take 8 spirit a second. *Oath: can't sneak* |
| Purity (210°) | Purify: 15% less burning and poison · Cleanse Wet: wet wears off 40% sooner · Clean Parry: a parry removes one ailment | Untainted: poison immunity. *Oath: your hits deal no poison* | Bright Retort (A2): a parry staggers enemies within 4 m. *Oath: 25% less stamina regen* |
| Shield (330°) | Shield Wall: +15% block power · Arrow Catcher: every fifth projectile does 40% less · Returned Light: parried projectiles strike back for 50% as spirit | Oath of All Things: the first hit every 20 s is ignored. *Oath: 50% more pierce damage taken* | Invincible (A6): below 20% health, 3 s of invulnerability every 5 min. *Oath: 20% less health* |

**New arms.** Anticlockwise from the top: Radiance · **Breiðablik** · Purity · **Hringhorni** · Shield · **Dawn**.

**Breiðablik** (150°, between Radiance and Purity; apex at A5). His hall, where nothing impure may enter: healing and keeping others whole.
Minors: Mending Light +2.5% healing received · Bright Hall +1% regen for players and tames within 8 m (each twice) · Gentle Light +2% (feeds Mercy) · Haven +1% (feeds Safe Haven).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Breiðablik | You, and players and tames within 8 m, regenerate 15% faster; burning and poison on them end 25% sooner | `Sanctuary` | new |
| Mercy | 10% of your hits heal the most hurt player or tame within 10 m, you included, for 30% of the damage | `Mercy` | Jewelcrafting (Fleeting Life) |
| Safe Haven | Other players and your tames within 6 m take 10% less damage | `SafeHaven` | Jewelcrafting |
| Selfless (oath) | Breiðablik also heals other players and tames 1% of their health a second. *Oath: 50% less health regen for you* | `Selfless` | new |
| All Things Wept (apex) | A player or tame within 15 m who drops below 20% health is healed to half and cleansed; once every 3 min each. *Oath: 20% less max health* | `AllThingsWept` | new |

**Hringhorni** (270°, between Purity and Shield; apex at A1). The greatest of all ships: sailing and the sea.
Minors: Fair Wind +2% sail speed · Sea-legs 2.5% cheaper swimming (each twice) · Tarred Hull 5% (feeds Steady Keel) · Strong Stroke +2.5% (feeds Sea-born).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Hringhorni | A ship you steer sails 15% faster and turns quicker | `SailSpeed` | PassivePowers, RuneMagic |
| Steady Keel | The ship you steer takes 50% less from waves, rocks and capsizing | `SteadyKeel` | ProtectiveWards |
| Sea-born | Swim 25% faster for 30% less stamina | `SwimSpeed`, `SwimStamina` | StaminaExtended |
| Fair Wind Always (oath) | The wind always fills the sail of a ship you steer. *Oath: 25% more fire damage taken (his ship was his pyre)* | `FairWind` | CacoFFF, PassivePowers |
| Ship-Caller (apex) | At the shore, call the last ship you steered and it sails itself to you. *Oath: 20% less max health* | `ShipCaller` | Historical Heritage |

**Dawn** (30°, between Shield and Radiance; apex at A4). The bright god: sunlight, torches, mist burned away.
Minors: Sunlit +1.5% damage in daylight · Daybreak +2.5% regen in daylight (each twice) · Pitch +10% (feeds Torchbearer) · Clear Sight +2 m (feeds Mist-burner).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Sun-kissed | In direct sunlight, +15% health and stamina regen and +5% damage | `SunKissed` | Seasons |
| Torchbearer | A held torch counts as a fire for warmth and rest, never burns out, and hits like a club with 50% more fire | `Torchbearer` | VentureValheim, PoV |
| Mist-burner | The Mistlands mist clears 20 m around you, as if a wisp were with you | `MistBurner` | Jewelcrafting (Wisplight) |
| Bifröst (oath) | When rain stops near dawn or dusk, a rainbow gives players within 30 m +30% stamina and +20% health regen for 5 min. *Oath: 25% less eitr regen* | `Bifrost` | HardHeadHacker (Rainbows) |
| Radiant Judgment (apex) | Once every 2 min, a heavy attack calls down light on every enemy within 20 m for 50% of the hit as spirit. *Oath: 20% more damage taken at night* | `RadiantJudgment`, oath `NightDamageTaken` | ProtectiveWards |

**Alternates:** Glancing Blow (a hit sometimes lands weak, and the next hit you deal gains what was
prevented), Restoring Parry (a parry heals part of the health lost in the last 5 s), Untouched (a
hit over 30% of your health makes you briefly immune), Lingering Light (rested survives logging out).

### Skaði, the Huntress

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Hunt (90°) | Huntress Prowess: 10% of bow kills return an arrow · Piercing Shot: arrows hit the next enemy behind for 40% · Field Dressing: bow kills drop into your bag | Rain of the North: +2 arrows per shot. *Oath: draw 50% slower, 50% less bow damage* | Hang on the Wind (A4): drawing in mid-air stops your fall. *Oath: 20% less melee damage* |
| Winter (210°) | Swift is the Shadow: +8% run speed at night, in the mountains and the Deep North · Stormbound Rage: 10% of hits as lightning or frost in storms · Shatter: frost kills burst for 20% of their health | Child of Þrymheimr: cold immunity. *Oath: 50% more fire taken* | Winter's Shatter (A6): bow kills burst into frost for 30%. *Oath: 30% more fire taken* |
| Peaks (330°) | Sure-footed: no fall damage · Skis of Skaði: 30% cheaper, faster running on snow · Leap: running jumps 25% higher | Mountain-born: double jump. *Oath: −100 carry weight* | Wolf-Sister (A2): three bow kills in 20 s call a wolf for a minute. *Oath: 15% less health* |

**New arms.** Anticlockwise from the top: Hunt · **Quarry** · Winter · **Spear** · Peaks · **Pack**.

**Quarry** (150°, between Hunt and Winter; apex at A1). The prize of the hunt: hides, meat and trophies.
Minors: Skinner +3% extra hide or meat · Hunter's Fare +1.5% from meat dishes (each twice) · Trophy-taker +5% (feeds Trophy Hunter) · Bone-broth +2.5% (feeds Hunter's Fare).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Skinner | 15% chance a kill drops an extra hide, pelt or meat | `HideYield` | new |
| Trophy Hunter | Trophies drop 25% more often, and a one-hit kill always drops one | `TrophyChance` | TalentForge, PoV |
| Hunter's Fare | Meat dishes give 15% more health and stamina | `MeatFood` | new |
| Call of the Hunt (oath) | Creatures within 30 m of you are one star higher: more loot and experience. *Oath: they fight at that star too* | `CallOfTheHunt` | Jewelcrafting (Daring) |
| Þjazi's Eyes (apex) | Each kind of trophy in your bag gives +5% damage against that creature. *Oath: 15% less max health* | `TrophyLore` | new |

Trophy Hunter and Þjazi's Eyes touch the point economy, since trophies are what shrines take. Þjazi's
Eyes makes a player choose between carrying a trophy and offering it, which is the point.

**Spear** (270°, between Winter and Peaks; apex at A5). The hunting spear, thrown.
Minors: Spear-arm +1.5% spear damage · Long Throw +2.5% throw speed (each twice) · Lanyard +5% (feeds Returning Spear) · Barbed +0.2 s (feeds Pinning Throw).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Huntress's Spear | Thrown spears fly 25% faster and hit 15% harder | `ThrowPower` | BetterArchery |
| Returning Spear | A thrown spear that hits comes back to your hand | `ReturningSpear` | Jewelcrafting (Magnetic) |
| Pinning Throw | A thrown spear roots what it hits for 2 s | `PinningThrow` | PoV (Immobilize) |
| Long Hunt (oath) | Thrown spears pass through every enemy in their path. *Oath: 25% less bow damage* | `SpearPierce` | new |
| Winter Spear (apex) | A thrown spear bursts into ice where it lands, hitting enemies within 4 m for 60% of the throw as frost. *Oath: you can't block* | `WinterSpear`, oath `NoBlock` | new |

**Pack** (30°, between Peaks and Hunt; apex at A3). Wolves and the beasts that hunt beside you.
Minors: Pack-scent +2% companion damage · Thick Fur +2% companion health (each twice) · Howl +2% (feeds Pack Tactics) · Warm Den +0.5% (feeds Lick Wounds).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Pack-leader | Tames following you deal 15% more and take 15% less | `PackLeader` | CacoFFF |
| Pack Tactics | +10% damage to an enemy a companion is also fighting, and companions go for what you hit | `PackTactics` | Chebs Necromancy (Orb of Beckoning) |
| Lick Wounds | Following tames heal 3% of their health a second out of combat | `LickWounds` | new |
| Pack-bound (oath) | Companions take 40% less damage. *Oath: you take 15% more* | `PackBound` | new |
| Endless Pack (apex) | A following tame dealt a killing blow flees instead and returns 60 s later at half health; Wolf-Sister's wolves join the pack and stay. *Oath: 25% less stamina regen* | `EndlessPack` | new |

Herd (Gefjun) and Pack split taming: Gefjun breeds, feeds and harnesses; Skaði fights alongside.

**Alternates:** Ricochet (arrows bounce to a second enemy), Hawk's Focus (zoom and no spread at full
draw), Keep the Draw (a dodge keeps your bow drawn), Ice-skater or Ski Slide (an upgrade for Skis of
Skaði), Frozen Footfalls (ice forms under you on cold seas), Windwalk (faster running with the wind),
Great Hunt (a marked beast to track down), crossbow reload speed. A Crossbows arm was set aside in
favour of Spear.

### Syn, the Gatekeeper

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Refusal (90°) | Built Different: 20% harder to stagger · Ever Watchful: 25% less damage while staggered · Footing: stamina pays stagger while blocking | The Unmoving Door: can't be staggered. *Oath: 25% slower* | Stone Skin (A2): standing still fills Fortification. *Oath: 15% slower* |
| Gate (210°) | Guardian of the Gates: +15% tower shield block · Brutal Defiance: every third block bursts for 60% block power · Sealed Wall: tower blocks turn aside 50% of elemental damage | The Shut Door: blocking strikes back for 30%. *Oath: 15% less damage* | Doors of Valhalla (A4): blocking raises a dome, allies within 4 m take 20% less. *Oath: 20% less damage* |
| Blade (330°) | Wealth of the Ages: 5% of kills drop coins or valuables · Strength of the Valkyries: weapon skills borrow 10% of your best other · Challenge: creatures you hit turn on you | Dedication to the Blade: +25% melee. *Oath: no bows or crossbows* | The Last Door (A6): take a nearby player's killing blow every 5 min. *Oath: 15% more damage taken* |

**New arms.** Anticlockwise from the top: Refusal · **Warden** · Gate · **Polearm** · Blade · **Denial**.

**Warden** (150°, between Refusal and Gate; apex at A1). She guards the doors of the hall: your buildings and the raids against them.
Minors: Mortar 2.5% less damage to your buildings · Watchman +2% damage during raids (each twice) · Mason +1% (feeds Mend the Walls) · Rally +1.5% (feeds Defender of the Hall).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Hall-ward | Pieces you built take 20% less damage and don't rot in the rain | `HallWard` | ValheimOne, EpicMMO |
| Defender of the Hall | During a raid, within 40 m of your buildings: +15% damage, 15% less damage taken | `RaidDefender` | new |
| Mend the Walls | Kills near your damaged buildings repair them 10% each, and the hammer repairs everything within 8 m | `MendWalls` | PoV |
| The Barred Hall (oath) | Raiders come for you before your buildings, and you take 25% less from them. *Oath: 10% slower* | `BarredHall` | new |
| Ban the Kind (apex) | Hang a creature's trophy on an item stand in your base: that kind can't harm your buildings and takes 25% more from you everywhere; one kind at a time. *Oath: 10% less damage to every other kind* | `BanTheKind`, oath `UnbannedDamagePenalty` | ProtectiveWards |

**Polearm** (270°, between Gate and Blade; apex at A3). The gate-guard's atgeir: hold the line.
Minors: Long Haft +1.5% polearm damage · Set Stance 2% cheaper polearm attacks (each twice) · Brace 0.5 s (feeds Set Against the Charge) · Sweep 0.5% (feeds Sweeping Ward).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Gate-guard's Reach | Polearms hit 15% harder and knock back 25% further | `PolearmDamage` | new |
| Set Against the Charge | The first enemy to come within 3 m every 10 s is staggered on your levelled polearm | `SetAgainstCharge` | new |
| Sweeping Ward | Each enemy a polearm sweep hits gives a Fortification stack | `SweepFortify` | new |
| Wall of Spears (oath) | Polearm sweeps reach 25% wider and hit 25% harder. *Oath: dodging costs 50% more stamina* | `WallOfSpears`, oath `DodgeStaminaPenalty` | new |
| Hold the Gate (apex) | While you stand your ground with a polearm, enemies you hit are knocked back and slowed by half for 3 s. *Oath: 15% slower* | `HoldTheGate` | new |

**Denial** (30°, between Blade and Refusal; apex at A5). Refusal turned outward: what you deny the enemy.
Minors: Dull Their Blades 1.5% (feeds Refuse) · Crack +0.5% (feeds Armour Break) (each twice) · Cut Short +5% (feeds Interrupt) · Stand Firm +2.5% stagger threshold.

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Refuse | An enemy you hit deals 15% less damage to you for 5 s | `Refuse` | new |
| Armour Break | Blunt hits stack a break (+3% damage taken, up to five); a full break staggers | `ArmourBreak` | PoV (Corrosion) |
| Interrupt | Hits on an enemy that is winding up an attack do 50% more stagger | `Interrupt` | new |
| Mirror of Refusal (oath) | 15% chance to send half of any hit back at the attacker, ranged included. *Oath: 10% less max health* | `Reflect` | Jewelcrafting (Mirror) |
| Verdict (apex) | Every fifth hit on the same enemy passes judgment: for 10 s it takes 25% more from everyone. *Oath: 20% less max stamina* | `Verdict` | new |

**Alternates:** Finisher (the last swing of a combo hits and staggers harder), Spectral Blades (a hit
sometimes repeats on nearby enemies), Shield Rush (sprint behind your shield to charge), Shield Bash or Throw, Stamina Debt
(health pays for a block you lack stamina for), Stunning Blows, Gate-breaker (+damage after your first
hit on a boss), Refuse the Burden (encumbered but still jumping and regenerating), Toll of Greed.

### Freyja, the Seiðkona

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Seiðr (90°) | Seiðr of Plenty: +15 eitr, +15% eitr regen · Echo: 15% chance a spell casts again free · Storm-weave: lightning arcs to wet enemies for 40% | Völva's Price: health pays when eitr runs short. *Oath: 20% less health* | Brísingamen (A4): eitr above half takes damage first. *Oath: 30% less eitr regen* |
| War (210°) | Goddess of War: +15% stamina regen, +10% health regen · Perfect Form: about every 500 damage, one hit does 50% more · Battle Fury: +1% damage per hit, up to 20 | Chooser of the Slain: kills heal 8% of max health. *Oath: 15% more damage taken* | Fólkvangr (A6): 50% chance a kill calls an einherjar. *Oath: 20% less health* |
| Falcon (330°) | Falcon Glide: glide while falling · Beloved: your meads also affect players within 6 m · Battle Medic: blocking heals players within 5 m 2 a second | Charged Grace: a dodge or parry charges the next hit, +25% and a splash. *Oath: 20% less armor* | Falcon Form (A3): hover at a stamina cost. *Oath: 30% less stamina regen* |

**New arms.** Anticlockwise from the top: Seiðr · **Cauldron** · War · **Bond** · Falcon · **Tears of Gold**.

**Cauldron** (150°, between Seiðr and War; apex at A2). Seiðr in a cup: meads and potions.
Minors: Steeped +4% mead duration · Strong Brew +2.5% mead strength (each twice) · Quick Hand 2.5% (feeds Quick Draught) · Dregs +2% (feeds Last Drop).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Völva's Brew | Meads last 25% longer and restore 15% more | `MeadDuration`, `MeadStrength` | ValheimMastery, PoV |
| Quick Draught | Mead cooldowns are 25% shorter | `MeadCooldown` | new |
| Last Drop | 15% chance a mead isn't used up | `MeadSave` | new |
| Twice-brewed (oath) | Fermenters you fill make one more mead per batch. *Oath: 20% less max eitr* | `TwiceBrewed` | new |
| Elixir of Seiðr (apex) | Every mead you drink also gives 10 s of +20% damage and clears one ailment. *Oath: food burns 15% faster* | `Elixir` | new |

**Bond** (270°, between War and Falcon; apex at A1). Love: the one at your side. A tame counts as your bonded one when no player is near, so the arm works solo.
Minors: Devotion +1% damage near your bonded one · Tender +2.5% regen near your bonded one (each twice) · Shared 2% (feeds Shared Burden) · Linked 5% (feeds Seiðr Link).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Bonded | The nearest player within 15 m, or else a tame, is your bonded one: both gain +10% stamina regen and speed | `Bonded` | Jewelcrafting (Together Forever) |
| Shared Burden | 20% of the damage your bonded one takes moves to you | `SharedBurden` | Star Levels (Life Link) |
| Seiðr Link | Your overhealing and mead healing also flow to your bonded one | `SeidrLink` | PoV (Link Warden) |
| Inseparable (oath) | Within 10 m of your bonded one, +15% damage and 10% less taken. *Oath: 15% less damage with no one bonded* | `Inseparable`, oath `AloneDamagePenalty` | new |
| Longing for Óðr (apex) | When your bonded one or another player near you falls below 20% or dies, you grieve: +40% damage and 5% life leech for 20 s. *Oath: 15% more damage taken* | `Longing` | new |

**Tears of Gold** (30°, between Falcon and Seiðr; apex at A5). She wept red gold and bought Brísingamen from four dwarves: trade, treasure and the hoard.
Minors: Haggler 1.5% better trader prices · Gleam +2.5% coins from everything you pick up (each twice) · Glint +2% (feeds Gersemi) · Hoard-heap +0.2 armor per 200 coins (feeds Hoard).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Dwarves' Bargain | Haldor, Hildir and the Bog Witch sell to you 15% cheaper and pay 15% more for valuables | `TradePrice` | new |
| Gersemi | The first time you open a chest, 15% chance it holds an extra valuable fitting its biome: amber, pearls, rubies, silver or coins | `Gersemi` | PoV (Second Sweep), Almanac (treasure hunts) |
| Hoard | Coins weigh nothing, and every 200 you carry give +1 armor, up to 10 | `Hoard` | Epic Loot (Coin Hoarder) |
| Gold-hunger (oath) | +1% damage for every 100 coins you carry, up to 25%. *Oath: coins don't go to your grave; dying loses them* | `GoldHunger`, oath `LoseCoinsOnDeath` | Epic Loot (Coin Hoarder) |
| Tears of Red Gold (apex) | While you carry coins, a quarter of the damage you take is paid in coins instead, at 5 coins a point. *Oath: 20% less armor* | `RedGoldTears` | new |

Gersemi is named for her daughter, whose name means "treasure". It finds valuables in chests;
Syn's Wealth of the Ages, which stays in Blade, finds them on kills.

**Alternates:** Twin Fury (dual-wield one-handers; build-defining but the heaviest to make, since it
needs new animations), Valkyrie's Flight (once a day, a Valkyrie carries you to a boss altar or a
trader), Gift of the Forsaken (shorter Forsaken power cooldown, shared with allies), Second Wind
(stamina refills faster the emptier it is), Storm-cloud Glide, Onslaught (a kill gives a burst of
speed), Phantom Strikes, Evasion, Air Dash. Hildisvíni (cheaper, faster and fiercer mounts) is set
aside.

### Hel, Queen of the Dead

**Today's big stars**

| Arm | Majors | Oath-bound star | Apex |
| --- | --- | --- | --- |
| Hellfire (90°) | Hellfire Adaptation: 15% Ashlands heat resistance · Retort: fire taken wraps you in a 6/s fire aura · Wildfire: enemies that die burning spread it for 20% | Pyre of Helheim: you burn down to 1 health; fire ×1.5 and heat immunity | Garm's Hunger (A1): heal for 3% of damage dealt. *Oath: no health regen* |
| Balance (210°) | Balance of the Jötunn: +6 stamina when hit · Balance of the Aesir: +6 eitr when hit · Stored for Later: overhealing builds Fortification | Death's Bargain: once an hour live at 1 health; kill within 15 s or die | Náglfar's Crew (A4): 30% chance a kill raises a thrall. *Oath: 25% less health* |
| Inevitable (330°) | Hasten the Inevitable: kills give +15% speed for 5 s · Silent Passing: a dodge after a kill hides you for 5 s · Open Wound: hits bleed for 10%, bleeding deaths burst | Half-Dead: +25% damage, poison and spirit immunity. *Oath: 30% less health* | Queen's Decree (A6): non-bosses below 15% die to your next hit. *Oath: 25% more damage taken* |

**New arms.** Anticlockwise from the top: Hellfire · **Rot** · Balance · **Host** · Inevitable · **Dread**.

**Rot** (150°, between Hellfire and Balance; apex at A3). Her plate is Hunger and her bed is Sick-bed: poison and sickness.
Minors: Bile +1 poison per hit · Lingering +5% poison duration (each twice) · Spore 2.5% (feeds Contagion) · Grasping 1% (feeds Grasp of the Dead).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Plague-touched | +15% poison damage; your poison lasts 25% longer | `PoisonDamage`, `PoisonDuration` | new |
| Contagion | An enemy that dies poisoned spreads its poison to enemies within 4 m | `Contagion` | new |
| Grasp of the Dead | 10% chance a hit roots the enemy for 2 s; rooted enemies burst into poison when they die | `GraspOfTheDead` | PoV (Immobilize) |
| Sick-bed (oath) | Poison you deal heals you for 15% of it. *Oath: food gives 15% less* | `PoisonDrain`, oath `FoodValuePenalty` | Jewelcrafting (Poisonous Drain) |
| Rotting Ground (apex) | Rot follows you: enemies within 5 m are poisoned each second and deal 10% less damage. *Oath: 15% more damage taken* | `RottingGround` | Jewelcrafting |

**Host** (270°, between Balance and Inevitable; apex at A5). Blood magic and the risen dead.
Minors: Grave-chill +2% summon damage · Long Watch +5% summon duration (each twice) · Blooded +1 kill (feeds Ascendant Thralls) · Bound Soul 2% (feeds Thrall's Burden).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Lord of the Host | Your summons (staff skeletons, thralls, einherjar) deal 15% more and last 25% longer | `SummonPower`, `SummonDuration` | new |
| Ascendant Thralls | A summon that kills three foes gains a star | `AscendantThralls` | Star Levels (Evolve) |
| Thrall's Burden | 20% of the damage you take passes to your nearest summon or tame | `ThrallsBurden` | Star Levels (Life Link) |
| Legion (oath) | Keep two more summons at once. *Oath: 15% less damage* | `Legion` | Chebs Necromancy |
| Soul Eater (apex) | Every creature that dies within 15 m, whoever killed it, gives +2% damage and heals 1% (up to 20 stacks, 10 s). *Oath: 20% less max stamina* | `SoulEater` | Star Levels (Soul Eater) |

**Dread** (30°, between Inevitable and Hellfire; apex at A2). Half her face is dark: night, mist and fear. The mirror of Baldur's Sunsworn.
Minors: Night-eyed +1.5% damage at night · Mist-cloak +2.5% stealth at night (each twice) · Shudder +1% (feeds Dread) · Long Shadow +1% (feeds Hel's Gaze).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Half-Dark | At night, in the Mistlands and in dungeons: +10% damage, and 15% harder to notice | `HalfDark` | PoV (Night Folk) |
| Dread | 8% chance a hit makes a non-boss enemy flee for 3 s | `Dread` | vanilla pheromone flee |
| Hel's Gaze | At night, enemies within 8 m deal 10% less damage | `HelsGaze` | new |
| Child of Night (oath) | At night, +20% damage and 10% more speed. *Oath: 20% less damage by day* | `ChildOfNight`, oath `DayDamagePenalty` | new |
| Terror of Niflhel (apex) | Each kill at night adds a stack of Terror for 10 s, up to 10 stacks, each timed on its own. Each stack gives +2% damage and +0.5% movement speed. *Oath: each stack also means 1% more damage taken* | `Terror` | new |

At 10 stacks Terror gives +20% damage, +5% speed and 10% more damage taken.

Valheim already makes monsters run: one flees from a player who carries a matching "pheromone" status
effect (`MonsterAI.PheromoneFleeCheck`). Dread can ride on that check. A creature's AI runs
on its owner's machine, so the fear is published on the player's ZDO, as Challenge already is.

**Alternates:** Doom Ledger (hits store damage on the target, and it dies once the store passes its
remaining health), Wound Transfer (a kill passes its bleed on), Cinder Crust (lava hardens under you),
Ash-born (smoke can't choke you), Kindled Retaliation, Earth-Delver (dig real tunnels; underground
counts as shelter), Oath-devourer. Hel's Hall (kinder deaths: less skill lost, food kept, worn gear
kept) is set aside.

## New powers

The new arms added 120 powers, about five mechanics and two minor stats an arm with a few shared,
plus these oaths: `CraftCostPenalty`, `ForeignGearPenalty`, `AlertedDamagePenalty`,
`NightDamageTaken`, `NoBlock`, `UnbannedDamagePenalty`, `DodgeStaminaPenalty`, `AloneDamagePenalty`,
`LoseCoinsOnDeath`, `FoodValuePenalty`, `DayDamagePenalty`. Two unused oaths come into play:
`MaxStaminaPenalty` and `MaxEitrPenalty`. Terror of Niflhel's extra damage taken is part of its own
stacks, not a separate oath.

Riskiest to build, worth a spike before committing to them:

- Ship-Caller (a ship steering itself to the shore).
- Draught Ox (harnessing a tame to a cart).
- Masterwork and Weapon-lore (data kept on items).
- The Swarm (a following bee cloud that other players see).
- Call of the Hunt (changing creature stars around a player, then putting them back).
- Endless Pack (tames that flee instead of dying).
- Gersemi (adding to a chest's loot the first time it is opened, on whichever machine owns it).
- Tears of Red Gold (coins taking damage in place of health).

## Decisions

| Question | Decision |
| --- | --- |
| Freyja's third new arm | Tears of Gold (trade, treasure, the hoard); riding is set aside |
| Hel's third new arm | Dread (night and fear); Hel's Hall is set aside |
| Skaði's third new arm | Spear; Crossbows is set aside |
| Vör's third new arm | Vows; Teacher is set aside |
| Terror of Niflhel | A stacking buff: each kill at night adds a 10 s stack, up to 10; each gives +2% damage, +0.5% speed and 1% more damage taken |
| Wealth of the Ages | Stays in Syn's Blade |
| Points | `PointsPerSoul` defaults to 2 (from 1): about 10 points per deity in a first playthrough (Gefjun 12), and 2 more from each re-claimed soul after Ascending |
| Apexes | Six Ascension levels, one apex each, ordered utility first and strongest last (table under "Apexes and Ascension") |
| Stars that help nearby players | They all reach your tames as well as players: the new ones (Breiðablik's stars, Vár's Witness, Bond) and today's (see below) |
| Vör's Lore arm | Its third major slot takes Perfect Timing |
| Eighth deity | Víðarr, for the Deep North; his plan is in `docs/vidarr.md` |

### Today's support stars reach tames

| Star | Deity | Change |
| --- | --- | --- |
| Gefjun's Feast | Gefjun | Eating also heals tames within 6 m |
| Hearth-Mother | Gefjun | Tames within 8 m gain the same share of your food health, as extra max health |
| Beloved | Freyja | Meads you drink also take effect on tames within 6 m |
| Battle Medic | Freyja | Blocking also heals tames within 5 m |
| Doors of Valhalla | Syn | The rune dome also covers tames within 4 m |
| The Last Door | Syn | You can also take the killing blow meant for a tame within 10 m |

A tame's damage and healing run on whichever machine owns it, which may not be yours. So these work
the way the dome and the Last Door already do between players: the star's strength goes on your ZDO,
and the tame's owner reads it.

## Still open

- **Trophies.** Trophy Hunter and Þjazi's Eyes speed up or compete with tributes. Check against the
  tribute steps once the numbers settle.

## As built

Every deity now has its six arms, six bridges and six lattices: 121 stars each, 847 in all.

### Layout, small stars and Ascension

- Six arm slots in `DefaultStarMaps`. An arm returns where it points, and each bridge places itself
  halfway between the two arms it links. Branch looks alternate round the six slots.
- Every small star and bridge on today's arms is halved as in the table above, with Morning Light
  changed to undead damage. The new arms were written at the new scale.
- Apexes open at the levels in the table under "Apexes and Ascension". `MaxAscension` defaults to
  6, Ascension keeps one look per level (aura, footprints, weapon glow, kill effect, wings),
  and the star map tab shows six Ascension stars.
- Ascended looks grow half as much per level, so a look at level 6 is the size a level-3 look was.
- `PointsPerSoul` defaults to 2.
- Power values show two decimals, since some small stars give quarters (0.25% life leech).
- Each bridge builds the lattice beyond it (`DefaultStarMaps.Lattice`). Lattice ids are both arms'
  names and a number (`furrow_herd_1`). Woven values are halved to steps of 0.05.

### Bridges

Three bridges per deity are today's, moved to a pair that still includes one of their arms; three
are new.

| Deity | Bridges, anticlockwise from 120° |
| --- | --- |
| Gefjun | Pasture (taming) · Harvest Home (food) · Hearth-smoke (rested) · Hearthstone (armor) · Field Hand (carry) · Cleared Field (chopping) |
| Vör | Open Eyes (skill experience) · Hidden Lore (crit) · Soft Spot (weakness) · Watchful (stealth) · Whispered Oath (Keeper of Vows) · Tally (skill experience) |
| Baldur | Morning Light (undead damage) · Pure Heart (fire and poison) · Salt Spray (wet) · Shield-rail (block) · Bright Shield (block) · First Light (sunlight damage) |
| Skaði | Cold Aim (bow damage) · Pelt-lined (frost resist) · Rimed Haft (spear damage) · Snow Stride (run stamina) · Den-mother (companion guard) · Steady Draw (draw speed) |
| Syn | Braced (stagger) · Gatehouse (Hall-ward) · Gate-iron (armor) · Long Reach (polearm damage) · Shoulder Blow (melee) · Turn Aside (Refuse) |
| Freyja | Herb-lore (mead strength) · War-song (magic damage) · Shield-maiden (stamina regen) · Light Feet (dodge stamina) · Golden Tears (eitr) · Gold-thread (trade prices) |
| Hel | Cinder Blood (fire resist) · Sickly Thirst (life leech) · Last Breath (stamina when hit) · Grave-guard (summon damage) · Grave Edge (execute) · Ember-dark (night damage) |

### Code

New files in `powers/`, by what they touch in the game:

| File | Holds |
| --- | --- |
| `TamePowers` | Taming, breeding, feeding, carts, companions in a fight, Endless Pack, Call of the Hunt, raiders turned by the Barred Hall |
| `HomePowers` | Comfort, rest, stations, the hermit's hives and the swarm, buildings (Hall-ward, Ban the Kind), Mend the Walls |
| `CraftPowers` | Wear, durability, crafting refunds and cost, masterworks, Weapon-lore |
| `TradePowers` | Trader prices, coins (Gleam, Hoard, Gold-hunger, Tears of Red Gold), Gersemi |
| `MeadPowers` | The Cauldron arm |
| `SeaPowers` | Sailing, Fair Wind, Steady Keel, swimming, Ship-Caller |
| `LightPowers` | Breiðablik and Dawn |
| `BondPowers` | Freyja's Bond |
| `HuntPowers` | Quarry and Spear |
| `StrikePowers` | The new arms' share of every hit: Weak Points, Vows, Polearm, Denial, Rot, Host, Dread, Land-taker's load and the coin and bond factors |

- `DamagePowers` calls `StrikePowers` for what changes a hit before it goes, the factors on damage
  dealt and taken, and what a hit, a hurt or a kill sets off.
- What other machines need from a player (taming, station and building wards, havens, bonds, dread,
  pack targets, the rainbow) is published on the player's ZDO by `AllyPowers.Publish`. Whoever owns
  the creature, station or building reads it.
- Bound Word is applied in `PowerSet.Compute`, so star tooltips show the lighter oaths too.
- A new client setting, `CallShipKey` (default Y), calls the ship for Ship-Caller.

### Where the build differs from the tables above

- **Ox-Mother** and Good Pasture make tames near you take less damage, rather than giving them
  more health.
- **The Swarm** is 10 poison a second at the apex; Humming adds 1.
- **Draught Ox** harnesses a tame only when you crouch as you take the cart's handle, so taking a
  cart yourself still works.
- **Know Thy Foe** doesn't show weaknesses on hover; it only adds the damage.
- **Keep the Secret** makes hits on unaware enemies silent, which is what keeps the others from
  being alerted.
- **Weak Points:** Soft Tread gives stealth rather than feeding Keep the Secret, which is on or off.
- **Oath-ring** also grants 2% Fortification per stack, since stacks do nothing without it.
- **Hringhorni** makes ships faster but doesn't turn them quicker.
- **Ship-Caller** moves the ship straight to the water nearest you, rather than sailing it there.
  The server moves it, so the server needs the mod.
- **Quarry:** the small stars are Clean Cut and Marrow, so they don't share the majors' names.
- **Pinning Throw** is a 50% chance (Barbed adds 5%) to root for 3 seconds.
- **Long Hunt** strikes every enemy in a 6 m line behind the target, rather than the spear flying
  on.
- **Endless Pack** leaves a tame at 1 health and unharmed for 10 seconds (once a minute each),
  rather than it fleeing and coming back. Wolf-Sister's wolves stay while their summoner is within
  100 m; ten seconds after they leave that range, the wolves leave too. All of a player's wolves count
  against the summon limit, following or not.
- **Hall-ward** and **Ban the Kind** protect buildings while their builder is near, since only a
  loaded player's values can be read.
- **Defender of the Hall** works during any raid on you, not only near your buildings.
- **Mend the Walls** fully mends the worst-hurt building of yours near a kill.
- **Polearm:** the outer small stars are plain Long Haft and Set Stance; Sweeping Ward also grants
  2% Fortification.
- **Denial:** the outer small stars are Crack and Cut Short. Verdict raises damage from you only,
  since status effects aren't shared between machines.
- **Twice-brewed** gives one more mead when you tap a fermenter, not when you fill it. Like the
  Hermit's extra honey, it needs access under a ward and counts once per batch.
- **Breiðablik** cuts burning and poison damage by a quarter, rather than making them end sooner:
  the game sets how long they last from their damage.
- **Gleam** only adds to coins nobody has carried yet, so dropping and picking up coins gives
  nothing more.
- **Bond:** a bonded player gets the speed and regen; a bonded tame gets nothing from it. Seiðr Link
  passes on overhealing only.
- **Gersemi** rolls from the same table as Wealth of the Ages, not by biome.
- **Plague-touched** adds flat poison to every hit, rather than making poison last longer, which
  only the target's owner could do.
- **Host:** the outer small stars are Grave-chill and Bound Soul.
- **Weapon-lore** counts kills only while you hold the star. Upgrading keeps a weapon's kills and
  masterwork.

### Checked offline

- It builds.
- A Cecil check resolves all 168 Harmony patch methods against the game, one target each, with
  every parameter name and type matching.
- The built maps load with 121 stars each, every one reachable from the root, no broken links,
  apexes at levels 1 to 6, bridges at 0°, 60°, 120°, 180°, 240° and 300°, and an unchanged YAML
  round trip.
- No two stars' click areas overlap; the tightest pair has 54 units to spare.
- Minors only feed mechanics a stronger star in the same map switches on, and oaths sit only on
  oath-bound and apex stars.
- Every power has English text, and every `$al_` key used in the code exists.

### Fixed after review

An independent read of the new code found these, all fixed:

- Mist-burner's ball was a networked copy that broke object unloading after respawn or turning the
  power off. It is now local to the player's machine.
- Mend the Walls' mass repair worked around the hammer's repair tool, which sits at the world's
  origin, rather than the piece being repaired.
- Gleam could be repeated without end by dropping and picking up coins.
- Twice-brewed and the Hermit's honey could be repeated by pressing use again before the station
  emptied, and ignored wards.
- Wolves that stay could be called without limit by telling them to stay or walking away.
- Fair Wind gave a tailwind, vanilla's weakest point of sail, so it slowed ships in a crosswind.
- Breiðablik's shorter ailments did nothing (see above).
- A kill on a creature another player's machine owns was never seen by the killer, so Death's
  Bargain could still kill them after it. The owner now tells the killer, and Trophy Hunter and
  Skinner read the killer's values wherever the creature was owned.
- Regen ticks at full health counted as overhealing for Stored for Later and Seiðr Link.
- Endless Pack misjudged lethal blows on worlds whose difficulty scales creature damage.
- Field Dressing looked over every drop in the world each frame; it now looks four times a second.
- Fields and flags changed for one call (breeding, growing, rest, swimming, throwing, sailing,
  drinking, trophy chance) are put back even if the game's own code throws.

### To check in game, riskiest first

- Ship-Caller on a server, with the ship far away and nearby.
- Draught Ox harnessing a tame to a cart.
- Call of the Hunt raising creatures, and Endless Pack sparing tames.
- Dread making creatures run.
- Masterwork and Weapon-lore surviving saves, chests and upgrades.
- The Swarm, Rotting Ground and Soul Eater in a real fight, for balance and effects.
- Torchbearer letting you rest by a held torch; Mist-burner clearing the mist.
- Values shared through the ZDO with two players: Bond, Shared Burden, Safe Haven, Vár's Witness and
  the rainbow.
- The six Ascension stars on the deity tabs, and how ascended looks read at their smaller steps.
- Balance in general: every number is a first draft.

### Not built yet

- Perfect Timing in Vör's Lore arm.
- Today's support stars reaching tames.
- Vietnamese text for the new strings, which fall back to English.
