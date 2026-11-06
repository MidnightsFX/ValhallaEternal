# Víðarr, the eighth deity

A plan for the Deep North's deity. It is built, and "As built" at the end says where the build
differs from the plan and what is still missing. Nothing has been tried in game yet.

## Why Víðarr

- **His myth fits the last biome.** Víðarr is the silent god, Odin's son by the giantess Gríðr. At
  Ragnarök he avenges his father: he sets his thick shoe on Fenrir's lower jaw and tears the jaws
  apart. He is one of the few gods who live on into the world after.
- **That gives the Deep North its threads.** The long winter before the end, the last fight, and
  starting over in a renewed world, which is what Ascension is.
- **The shoe gives two things no deity has yet.** Unarmed fighting, and a use for the Deep North's
  deep snow. The shoe is made of every scrap of leather people cut from their own shoes.
- **Týr was the other choice.** Most of what he stands for is already taken: law and oaths are
  Syn's, and battle courage is Freyja's War arm. His Fenrir story is losing a hand, not winning.
  Swapping to Týr would rename four of the six arms; the shrine, tributes and code steps below stay
  the same.

## At a glance

| | |
| --- | --- |
| Name | Víðarr, the Silent |
| Biome and shrine | Deep North, location `ve_tribute_vidarr` |
| Soul of Power | Kall Fimbulbringer, the Deep North's final boss (his last stage, `FrozenKing_p3`) |
| Rune | Eihwaz, the yew: endurance, and life through death |
| Colours | Evergreen `#7FB59A`, frost `#D8E6EE`, dawn `#FFE7A8` |
| Effects | Piloto's Wind kit (wind orb, gale aura, wind wings, gale bursts), not used by any other deity |
| Portrait | New art: a tall, silent figure in a winter cloak, one huge iron-shod boot, a wolf's jaw at his feet |

## Star map

The same shape as the other seven: six arms, six bridges and six lattices, 121 stars, making 968
across the eight maps. Every arm is new, so there is no "today's big stars" table, and the small
stars start at the halved scale.

**Arms**, anticlockwise from the top: **Thick Shoe** · **Fimbulwinter** · **Wolf-slayer** ·
**Vengeance** · **Silence** · **The World After**.

Neighbours share a thread:

- The shoe crosses the winter's snow.
- The wolf comes at the winter's end.
- Killing the wolf is the vengeance.
- The vengeance is silent.
- The silent god outlives the end.
- The world after mends its shoes.

A **great foe** is a boss, or any creature whose max health is at least twice yours. Several stars
use it.

### Thick Shoe (90°, apex at A3)

The shoe made from every scrap of leather, thick enough to hold Fenrir's jaw: fists, stamps and deep
snow.
Minors: Hard Sole +1.5% unarmed damage · Thick Heel deep snow slows you 5% less (each twice) ·
Heel-strike +3% (feeds Heel-stamp) · Knuckle-iron +2.5% (feeds Gríðr's Gloves).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| The Thick Shoe | +15% unarmed damage; deep snow slows you 50% less | `UnarmedDamage`, `SnowStride` | game deep snow (`Character.m_deepSnowSlowMax`) |
| Heel-stamp | Landing from 4 m or higher stamps the ground: enemies within 4 m are staggered and take blunt damage that grows with the fall. You take 30% less fall damage | `HeelStamp` | new |
| Gríðr's Gloves | His mother's iron gloves: unarmed hits add 25% of your armor as blunt damage | `IronGloves` | Prose Edda (Járngreipr) |
| Shoe of Scraps (oath) | Leather scraps weigh nothing, and every 10 you carry give +1% unarmed damage, up to 25%. *Oath: 15% less damage with anything but fists* | `ShoeOfScraps`, oath `ArmedDamagePenalty` | Prose Edda |
| Foot on the Jaw (apex) | An unarmed secondary attack on a staggered enemy pins it under your foot for 3 s (1 s for bosses). A pinned enemy takes 25% more damage from everyone. *Oath: 15% less max health* | `FootOnTheJaw` | Prose Edda |

### Fimbulwinter (150°, between Thick Shoe and Wolf-slayer; apex at A5)

The three winters with no summer between them, before Ragnarök: enduring the cold, the storms and the
snow on the roof. Skaði's Winter deals frost; this arm outlasts it.
Minors: Fur-lined Cold and Freezing slow your regen 5% less · Rime-skin +2.5% frost resistance (each
twice) · Banked Fire +2 m (feeds Snow on the Roof) · Winter-worn +0.5% (feeds Three Winters).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Endure the Winter | Cold and Freezing slow your regen half as much; +15% frost resistance | `ColdEndurance`, `FrostResist` (exists) | new |
| Snow on the Roof | Snow doesn't pile up on buildings within 20 m of you, and fires near you melt snow twice as far | `SnowShed` | game snow load (`WearNTear.m_snowBuildup`, `Fireplace.m_snowMelter`) |
| Three Winters | Each minute you spend Cold or Freezing gives +3% damage and 2% less damage taken, up to five; warming at a fire clears them | `ThreeWinters` | new |
| Winter-bound (oath) | In the Mountains, the Deep North and anywhere freezing: +15% damage and stamina regen. *Oath: 15% less damage anywhere else* | `WinterBound`, oath `WarmDamagePenalty` | new |
| Fimbulvetr (apex) | A blizzard follows you: enemies within 8 m move and attack 20% slower, and allies inside it don't freeze. *Oath: 25% more fire damage taken* | `Fimbulvetr` | new |

### Wolf-slayer (210°, apex at A6)

At Ragnarök he avenges Odin on Fenrir: great foes, bosses, and slipping free of what holds you.
Minors: Giant-feller +1.5% damage to great foes · Fetter-proof 3% shorter holds (each twice) ·
Slipknot +5% (feeds Slip the Fetter) · Wolf-tooth +2% (feeds The Long Fight).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Fenrir's Bane | +15% damage to great foes | `GreatFoeDamage` | new |
| Slip the Fetter | Slows, roots, webs and tar on you wear off 40% sooner | `Unbound` | Gleipnir, reversed |
| The Long Fight | Each hit on a great foe gives +1% damage against it, up to 20%; it resets after 10 s without a hit | `LongFight` | new |
| Sworn against the Wolf (oath) | +30% damage to great foes. *Oath: 15% less damage to everything else* | `WolfSworn`, oath `LesserFoeDamagePenalty` | new |
| Tear the Jaws (apex) | A heavy attack on a staggered great foe tears its jaws for 10% of its max health (4% on bosses), once every 20 s per foe. *Oath: 25% more damage taken* | `TearTheJaws` | Prose Edda |

### Vengeance (270°, between Wolf-slayer and Silence; apex at A4)

He was born to avenge his father: what is done to you and yours is paid back. Vör's Grudge marks
whoever hurt you and Freyja's Longing strengthens you when an ally falls. Vengeance stores the hurt
and pays it out in one blow.
Minors: Unforgiving +1.5% damage for 5 s after you're hit · Hardened +1 armor (each twice) ·
Wound-price +2.5% (feeds Blood for Blood) · Kin-sworn +3% (feeds Kin-vengeance).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| Blood-debt | A quarter of the damage you take is stored as debt, up to half your max health, and added to your next heavy or secondary attack. The debt fades after 10 s | `BloodDebt` | new |
| Blood for Blood | Paying out your debt heals you for 25% of it | `DebtHeal` | new |
| Kin-vengeance | When a player or tame within 20 m dies, its killer is marked for 60 s: you deal it 30% more and it can't flee | `KinVengeance` | new |
| Unatoned (oath) | Blood-debt stores half the damage you take and no longer fades. *Oath: 15% more damage taken* | `Unatoned` | new |
| Avenger of the Allfather (apex) | When your debt is full, your next heavy or secondary attack strikes everything within 5 m for twice the debt and staggers it. *Oath: 30% less health regen* | `Avenger` | new |

### Silence (330°, apex at A2)

The silent god speaks no word and strikes once: patience and stillness between blows. Vör's Careful
arm is about sneak attacks; this arm rewards waiting, seen or unseen.
Minors: Bated Breath +2% Patience · Hush +2.5% stealth (each twice) · Long Breath +5% (feeds
Stillness) · Measured +5% (feeds One Word).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| The Silent God | Each second you go without attacking gives your next hit +2% damage, up to 20% | `Patience`, `Stealth` (exists) | new |
| Stillness | After 2 s standing still, you're 50% harder to notice and regen stamina 25% faster | `Stillness` | new |
| One Word | A hit with full Patience deals 60% more stagger, and always staggers a non-boss | `OneWord` | new |
| Vow of Silence (oath) | Patience builds twice as fast and goes up to 40%. *Oath: a hit within 2 s of your last deals 15% less* | `SilenceVow`, oath `ImpatiencePenalty` | new |
| The Single Blow (apex) | After 8 s without attacking, your next hit deals triple damage (double on bosses), then Patience starts over. *Oath: 20% less max stamina* | `SingleBlow` | new |

### The World After (30°, between Silence and Thick Shoe; apex at A1)

Víðarr lives on after Ragnarök, in the green world that rises from the sea: surviving, falling,
and starting over. Hel's set-aside Hall (kinder deaths) lands here instead. The arm adds no new
cheat-death; four deities already have one.
Minors: Second Spring +5% relearning speed · Mending +2.5% health regen (each twice) · Hold Fast
+1% (feeds Survivor) · Lesser Loss +5% (feeds Hoddmímir's Wood).

| Star | Effect | Power | From |
| --- | --- | --- | --- |
| The World Renewed | Skills below the best level you've ever reached train 50% faster, so ground lost to death or Ascension comes back quickly | `Relearn`, `HealthRegen` (exists) | ValheimMastery |
| Survivor | Below 30% health you take 20% less damage | `LastStand` | new |
| Hoddmímir's Wood | Dying loses half as much skill | `KindDeath` | Hel's Hall (set aside) |
| Outlast (oath) | Below 30% health you regenerate 2% of your max health a second and move 15% faster. *Oath: 15% less max health* | `Outlast` | new |
| Iðavöllr (apex) | The gods find their golden game pieces in the grass. Reaching your grave gives back the food you had eaten when you died, and 10 minutes of +20% experience and stamina regen. *Oath: 15% less max stamina* | `GoldenPieces` | Völuspá |

Survivor and Hoddmímir's Wood are named for Líf and Lífþrasir, who outlast Ragnarök hidden in
Hoddmímir's wood. Iðavöllr is the field where the surviving gods meet afterwards.

### Bridges and lattices

| Gap | Bridge, linking | Grants | Woven stars |
| --- | --- | --- | --- |
| 120° | Snow-shod: Thick Shoe and Fimbulwinter | Deep snow slows you 2.5% less | Trackless Snow |
| 180° | Wolf-winter: Fimbulwinter and Wolf-slayer | +1.5% frost resistance | Wolf-age |
| 240° | Odin's Son: Wolf-slayer and Vengeance | +1% damage to great foes | Allfather's Due |
| 300° | Cold Wrath: Vengeance and Silence | +1% damage for 5 s after you're hit | Quiet Feud |
| 0° | Long Wait: Silence and The World After | +1% Patience | Hoddmímir's Holt |
| 60° | Patched Sole: The World After and Thick Shoe | +2.5% relearning | New Leather |

Wolf-age is from Völuspá's count of the last days: "axe-age, sword-age … wind-age, wolf-age". The
lattice stars follow the same rules as the other maps: a star touching an arm repeats that arm's
star, and the woven stars carry half of each side's opening minor.

### Apex order

Utility first, as for every deity:

| Deity | A1 | A2 | A3 | A4 | A5 | A6 |
| --- | --- | --- | --- | --- | --- | --- |
| Víðarr | Iðavöllr | The Single Blow | Foot on the Jaw | Avenger of the Allfather | Fimbulvetr | Tear the Jaws |

- Iðavöllr opens first because it helps after every death.
- Fimbulvetr sits at A5 because it's a big aura.
- Tear the Jaws is last because it's the Fenrir moment and the strongest.
- Foot on the Jaw opens three levels before Tear the Jaws. The two halves of the myth then work
  together: pin the great foe under your foot, then tear.

### New powers

32 new powers and 4 new oaths: `ArmedDamagePenalty`, `WarmDamagePenalty`,
`LesserFoeDamagePenalty` and `ImpatiencePenalty`. The map also reuses `FrostResist`, `Armor`,
`Stealth`, `HealthRegen`, `MaxHealthPenalty`, `MaxStaminaPenalty`, `FireDamageTaken`,
`DamageTaken` and `HealthRegenPenalty`.

Riskiest to build, worth a spike first:

- **Fimbulvetr.** A following blizzard that other players see. Creatures run on their owner's
  machine, so the slow is published on your ZDO, as Hel's Gaze is.
- **Kin-vengeance.** A player or tame dies on its own machine, so the death has to be sent to
  players nearby, as `AL_Kill` already sends kills.
- **Foot on the Jaw.** Holding a creature in place on its owner's machine. Rot's Grasp of the Dead
  already roots, so it can share that code.
- **Iðavöllr.** Saving the food eaten at death and giving it back at the grave.
- **The World Renewed.** Keeping each skill's best level on the player, through deaths and
  Ascension.
- **Snow on the Roof.** Snow build-up runs on each building's owner, so the range is published on
  your ZDO for the owner to read.

## Shrine, tributes and soul

### Tribute steps

They climb from common Deep North trophies to the final boss, as the others do.

- **Order:** a guess from the game's own names and descriptions. The Elaking is a small creature
  ("mean little eyes"). The Krigen and Hexen are the Jötunn warrior and witch, the strongest
  regular foes. How common each creature is still needs checking in game.
- **Final boss:** Kall Fimbulbringer leaves no trophy and sets no global key. His last stage drops
  Sacrificial Blood ("the last essence of an end once foretold"), so the last step asks for that.

| Step | Tribute |
| --- | --- |
| 1 | Seal trophy ×3 (`TrophySeal`) |
| 2 | White deer trophy ×3 (`TrophyDeerWhite`) |
| 3 | Elaking trophy ×3 (`TrophyElaking`) |
| 4 | Frost blob trophy ×3 (`TrophyBlob_Frost`) |
| 5 | Moose trophy ×2 (`TrophyMoose`) |
| 6 | Eyeless One trophy ×2 and Writhan trophy ×2 (`TrophyMole`, `TrophyWrithan`) |
| 7 | Mørkhalla draugr ×2 and Pulp trophy ×2 (`TrophyDraugrFem`, `TrophyBlob_Morkhalla`) |
| 8 | Barka trophy ×1 (`TrophyBarka`) |
| 9 | Krigen trophy ×1 and Hexen trophy ×1 (`TrophyJotunWarrior`, `TrophyJotunWitch`) |
| 10 | Sacrificial Blood ×1 (`FrozenKingDrop`), worth one star point |

The final boss gates nothing beyond that one point:

- **Reset cost:** a Krigen trophy.
- **Ascension cost:** a Krigen and a Hexen trophy.
- **Boss keys:** Ascension still asks only for the seven older bosses' keys, since he has none.
- `TrophyKvastur` is the Bog Witch's and stays out.

### Soul of Power

Kall Fimbulbringer's last stage leaves Víðarr's Soul of Power, the same way the other bosses leave
theirs.

- **No defeat key:** `SoulBosses` now also matches a boss by its prefab name when it sets no key. The
  default lists `FrozenKing_p3=Vidarr`.
- **Earlier stages:** `FrozenKing` and `FrozenKing_p2` aren't listed, so they leave nothing.
- **Any listed creature:** the boss check is gone, so a server can list any creature by its prefab
  name.

### Shrine

- A new `VE_Tribute_Vidarr` prefab in Unity, copied from an existing shrine with a new statue and
  material.
- Placed with `AddLocationsToWorldGen(Heightmap.Biome.DeepNorth, "ve_tribute_vidarr", …)` and a new
  `MaxDeepNorthLocations` setting.
- Deep snow blocks most building, so the shrine needs to sit clear of it or allow it
  (`Piece.m_allowedInDeepSnow`).

## What changes in code

- **`Deity` enum:** `Vidarr` goes last. Souls and summons store their deity as a number, so
  inserting it anywhere else would move every saved deity after it.
- **`Deities.cs`:** name, title, description and portrait (`vidarr_nobackground.png`).
- **`DefaultStarMaps`:** a `Vidarr()` map with
  `NewMap(Deity.Vidarr, "eihwaz", "#2F5D50", "#D8E6EE", "#FFE7A8")`, six `Arm` calls, six `Bridge`
  calls.
- **`RuneArt`:** strokes for eihwaz.
- **`Power.cs`:** 32 powers and 4 oaths, with their `PowerInfo`. New code would most likely go in two
  new files, one for the shoe and unarmed and one for winter and snow. Patience, Blood-debt and great
  foes fit `StrikePowers`, and The World After fits `SurvivalPowers`.
- **`TributeData`:** the steps above. **`ValConfig`:** the `SoulBosses` default and
  `MaxDeepNorthLocations`.
- **Unity:** the shrine prefab and `al_vfx_vidarr_*` recipes from the Wind kit in `VfxBuilder`
  (orb, aura, wings, footstep, kill, proc, burst, and the Fimbulvetr blizzard), then rebuild the
  bundle.
- **Star map window:** an eighth tab. Eight tabs take 728 px, about 900 px with the header and
  footer. Check it at 1080p, and shrink `TabHeight` if it doesn't fit.
- **Text:** the deity's name, title and description; star names; power and oath text; tribute
  messages.
- **Offline checks:** expect eight maps and 968 stars, with apexes 1 to 6 on Víðarr's map too.
- Looks, the compendium, console commands and the Looks panel loop over every deity already, so
  they pick him up without changes.

## Still open

- **Týr or Víðarr:** Víðarr is the plan; Týr remains an option.
- **The boss:** settled. Kall Fimbulbringer's last stage leaves the soul, and his Sacrificial Blood
  is the last tribute step.
- **Trophies:** which Deep North creatures are common or rare. This sets the step order.
- **Statue art** for his own shrine. The portrait is done.
- **Unarmed balance:** fist weapons are few, so the Thick Shoe arm may need its numbers raised
  after testing.

## As built

Víðarr has his deity entry, star map, 32 powers and 4 oaths, tributes, shrine placement and English
text. The offline checks pass for all eight maps: every Harmony patch resolves (180), each map has
121 stars with apexes at levels 1 to 6, the click areas keep at least 54 units apart, no full map's
sums pass a cap on Víðarr's map, and the YAML round trip is identical.

### Code

- `Deity.Vidarr` is last in the enum.
- His map is `DefaultStarMaps.Vidarr()`.
- His powers live in five new files, one per theme:
  - `ShoePowers`: Thick Shoe.
  - `WinterPowers`: Fimbulwinter.
  - `WolfPowers`: Wolf-slayer and Vengeance.
  - `SilencePowers`: Silence.
  - `AfterPowers`: The World After.
- `StrikePowers` calls them from the same hit, hurt and kill hooks the other arms use, and
  `PowerTicker` runs and resets them.
- New status effects: Pinned, Chilled (the blizzard's slow), Three Winters, Blood-debt and Iðavöllr.
- `AllyPowers` publishes the blizzard and Snow on the Roof's range for other machines to read.

### Where the build differs from the plan

- **Heavy attack** means a secondary attack or the last blow of a combo. Foot on the Jaw takes any
  unarmed heavy attack, not only a secondary one.
- **Fimbulvetr** slows enemies by 20% and makes their hits 15% softer. A status effect can't slow
  attacks. "Allies" means other players: the caster still feels the cold, so Three Winters keeps
  working inside their own blizzard.
- **Snow on the Roof** keeps snow off buildings near you, so it can't break them. The part about
  fires melting snow further was dropped: the melt runs on the fire's owner from a shared prefab.
- **Kin-vengeance** marks the killer for more damage; it doesn't stop the killer fleeing. A dying
  player or tame tells everyone by a routed RPC (`AL_Fallen`), and players within 20 m with the
  star mark the killer.
- **Patience, One Word and The Single Blow** are taken when an attack starts and spent on its first
  blow: the hits landing in the same frame, so a sweep through a crowd counts as one blow. Every hit
  counts as attacking, so a held, looping attack never builds patience.
- **Heel-stamp** only stamps on a landing that would hurt, at most once every 5 seconds. A feather
  cape, Sure-footed or a glide makes a landing harmless, so it doesn't stamp.
- **Stillness** hides you only while sneaking: vanilla doesn't ask about stealth otherwise. The
  stamina part works standing upright.
- **The World Renewed** keeps each skill's best level on the character (`al_bestskills` in its
  custom data) whether or not the star is held, so it also helps with losses from before it was
  taken.
- **Iðavöllr** saves the food eaten at death (`al_gravefood`) and gives it back the first time you
  open your own grave afterwards.
- **Colours:** the evergreen is lighter than planned, `#7FB59A`. The darker one would have been
  muddy on the star map's dark background.

### Fixed after review

- Patience and The Single Blow could stay on: a looping attack, or holding one for 8 s, kept triple
  damage on every hit. Now they are spent on the first blow and every hit counts as attacking.
- Iðavöllr lost every food carried over from the last login. Foods are now saved by name.
- Heel-stamp fired off harmless landings, such as a feather cape off a cliff, with no cooldown.
- The World Renewed only noted levels on a level-up, so a death or Ascension before one went
  unrecorded. Levels are now noted just before any loss.
- A heavy attack could be read from the last attack's leftover state, for example by a shield's
  charged burst. It now needs an attack in progress.
- Leech effects counted the paid debt and the torn jaws; those now go in last.
- Shoe of Scraps (and Hoard) left the old carried weight until the inventory changed. Weight is now
  recounted when the stars change.
- A player sheltered by another's blizzard lost Cold as well as Freezing, and so stopped gaining Three
  Winters. They now count as out in the cold.
- The blizzard was shown to everyone every 6 s even with no enemies near.
- Blood for Blood healed from the Avenger's doubled payout.

### Placeholders and what is missing

- **The boss** is no longer a placeholder (see "Soul of Power" and "Tribute steps"). Sacrificial
  Blood is used up when it is offered. If the game wants it for anything else, a player keeps
  another drop for that.
- **Existing configs:** the `SoulBosses` default only reaches new config files. A server or profile
  that already has a config needs `FrozenKing_p3=Vidarr` added by hand. The Modtest profile is one.
- **Shrine.** There is no `VE_Tribute_Vidarr` prefab yet. Until there is one, the location copies
  Skaði's shrine and points it at Víðarr, under the name `ve_tribute_vidarr`. A real prefab in the
  bundle under `assets/locations/ve_tribute_vidarr.prefab` is used instead with no code change.
- **Portrait.** Now in the bundle as `Assets/Art/Vidarr_nobackground.png`, a 512×512 sprite like
  the others. His eihwaz rune only stands in if a bundle lacks it.
- **Effects.** Built from the Wind kit like the other deities' kits, at the same sizes: `Orb_Wind`
  for the soul, `WindAura`, `Wing_Wind`, `Impact_Wind` for footsteps and procs,
  `VFX_WindExplosion` for kills and `AOE_Explosion_Wind` for bursts.
- **Star map tabs** are 76 px tall, down from 84, so eight fit under the header at 1080 lines.
- **Text:** no Vietnamese yet, and the package README still lists the old boons.
- **Weightless scraps** also weigh nothing in a cart this machine weighs, as Hoard's coins already
  do.

### To check in game, riskiest first

1. The shrine copy appears in the Deep North, opens Víðarr's tributes, and isn't buried in deep
   snow.
2. Heavy attacks are recognised: a secondary attack and the third punch of an unarmed combo both
   pay Blood-debt and can pin.
3. Kin-vengeance marks the killer when another player or a tame dies nearby, on a server.
4. Iðavöllr gives the food back at the grave, with the right time left, and nothing on a second
   grave.
5. Snow on the Roof stops snow piling up on a Deep North base while you are there.
6. Fimbulvetr chills creatures another player owns, and other players inside it don't freeze.
7. Heel-stamp's damage and stagger from a 4 m and a 20 m fall.
8. Kall Fimbulbringer's last stage leaves Víðarr's soul (its death runs `Character.OnDeath` and
   its prefab is `FrozenKing_p3`), and the earlier stages leave nothing.
9. Sacrificial Blood shows its icon at the shrine and can be offered for the last step.
