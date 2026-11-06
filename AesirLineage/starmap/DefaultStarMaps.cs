using System;
using System.Collections.Generic;
using System.Linq;
using static AesirLineage.common.Deities;

namespace AesirLineage.starmap
{
    /// <summary>
    /// The built-in star maps: for each deity, six arms of one shape (see <see cref="Arm"/>), one per
    /// aspect of the deity, 60° apart, with a bridge star between each pair of neighbouring arms on the
    /// inner rings (docs/star-map-expansion.md).
    /// </summary>
    internal static class DefaultStarMaps
    {
        // The six arm slots, anticlockwise from the top. The original arms hold up, lower left and lower right.
        private const float Up = 90f;
        private const float UpperLeft = 150f;
        private const float Left = 210f;
        private const float Down = 270f;
        private const float Right = 330f;
        private const float UpperRight = 30f;

        internal static Dictionary<Deity, StarMapDefinition> Build()
        {
            return new Dictionary<Deity, StarMapDefinition>() {
                { Deity.Gefjun, Gefjun() },
                { Deity.Vor, Vor() },
                { Deity.Baldur, Baldur() },
                { Deity.Skaldi, Skadi() },
                { Deity.Syn, Syn() },
                { Deity.Freya, Freyja() },
                { Deity.Hel, Hel() },
                { Deity.Vidarr, Vidarr() },
            };
        }

        private static StarMapDefinition Gefjun()
        {
            StarMapDefinition map = NewMap(Deity.Gefjun, "gebo", "#E8C35A", "#7DBE5A", "#FFF2B0");
            ArmEnd furrow = Arm(map, "furrow", Up, 6,
                Minor("Strong Back", (Power.CarryWeight, 10)),
                Minor("Tilled Earth", (Power.FarmingXp, 2.5f)),
                Major("bounty_of_the_lands", "Bounty of the Lands", (Power.HarvestBounty, 10)),
                Minor("Furrow", (Power.HarvestBounty, 1.5f)),
                Minor("Tilled Earth", (Power.FarmingXp, 2.5f)),
                Major("seeds_of_the_gods", "Seeds of the Gods", (Power.SeedsOfTheGods, 1)),
                Major("fishing_prosperity", "Fishing Prosperity", (Power.FishingProsperity, 10)),
                Minor("Furrow", (Power.HarvestBounty, 1.5f)),
                Minor("Full Nets", (Power.FishingProsperity, 2)),
                Oath("yoke_of_the_four_oxen", "Yoke of the Four Oxen", (Power.CarryWeight, 150), (Power.GatherYield, 15), (Power.LessDamageDealt, 15)),
                Apex("bloom_of_zealand", "Bloom of Zealand", (Power.CheatDeath, 1), (Power.LessDamageDealt, 20)));
            ArmEnd table = Arm(map, "table", Left, 2,
                Minor("Hearty Bread", (Power.FoodBonus, 1.5f)),
                Minor("Slow Embers", (Power.FoodDuration, 2.5f)),
                Major("quality_nourishment", "Quality Nourishment", (Power.FoodBonus, 8)),
                Minor("Hearty Bread", (Power.FoodBonus, 1.5f)),
                Minor("Slow Embers", (Power.FoodDuration, 2.5f)),
                Major("hearty_meal", "Hearty Meal", (Power.HeartyMeal, 35)),
                Major("gefjuns_feast", "Gefjun's Feast", (Power.FeastShare, 10)),
                Minor("Second Helping", (Power.HeartyMeal, 2.5f)),
                Minor("Shared Table", (Power.FeastShare, 1.5f)),
                Oath("feastbound", "Feastbound", (Power.FoodBonus, 25), (Power.FoodDurationPenalty, 25)),
                Apex("hearth_mother", "Hearth-Mother", (Power.HearthAura, 15), (Power.DamageTaken, 15)));
            ArmEnd ward = Arm(map, "ward", Right, 4,
                Minor("Hide of the Ox", (Power.Armor, 1.5f)),
                Minor("Steady Arm", (Power.BlockStamina, 2)),
                Major("basic_protection", "Basic Protection", (Power.Armor, 6)),
                Minor("Hide of the Ox", (Power.Armor, 1.5f)),
                Minor("Steady Arm", (Power.BlockStamina, 2)),
                Major("shelter", "Shelter", (Power.Shelter, 15)),
                Major("hearth_guard", "Hearth Guard", (Power.Armor, 6), (Power.BlockStamina, 8)),
                Minor("Sheltered", (Power.Shelter, 2.5f)),
                Minor("Steady Arm", (Power.BlockStamina, 2)),
                Oath("mothers_ward", "Mother's Ward", (Power.Fortification, 3), (Power.MothersWard, 1), (Power.MoveSpeedPenalty, 10)),
                Apex("the_plough", "The Plough", (Power.FurrowSlam, 120), (Power.StaminaRegenPenalty, 25)));
            ArmEnd herd = Arm(map, "herd", UpperLeft, 1,
                Minor("Gentle Hand", (Power.TamingSpeed, 4)),
                Minor("Good Pasture", (Power.TameGuard, 3)),
                Major("ox_mother", "Ox-Mother", (Power.TamingSpeed, 25), (Power.TameGuard, 15)),
                Minor("Gentle Hand", (Power.TamingSpeed, 4)),
                Minor("Good Pasture", (Power.TameGuard, 3)),
                Major("fruitful_herd", "Fruitful Herd", (Power.FruitfulHerd, 25)),
                Major("draught_ox", "Draught Ox", (Power.DraughtOx, 30)),
                Minor("Fruitful", (Power.FruitfulHerd, 5)),
                Minor("Broad Yoke", (Power.DraughtOx, 5)),
                Oath("never_hungry", "Never Hungry", (Power.HerdFed, 1), (Power.FoodDurationPenalty, 20)),
                Apex("the_four_oxen", "The Four Oxen", (Power.FourOxen, 50), (Power.LessDamageDealt, 15)));
            ArmEnd homestead = Arm(map, "homestead", Down, 5,
                Minor("Warm Hall", (Power.RestedDuration, 5)),
                Minor("Busy Hands", (Power.StationSpeed, 3)),
                Major("well_rested", "Well-Rested", (Power.Comfort, 1), (Power.RestedDuration, 25)),
                Minor("Warm Hall", (Power.RestedDuration, 5)),
                Minor("Busy Hands", (Power.StationSpeed, 3)),
                Major("hearth_strength", "Hearth Strength", (Power.HearthStrength, 0.5f)),
                Major("busy_household", "Busy Household", (Power.StationSpeed, 20)),
                Minor("Hearthside", (Power.HearthStrength, 0.25f)),
                Minor("Humming", (Power.Swarm, 1)),
                Oath("hermit", "Hermit", (Power.Hermit, 1), (Power.MoveSpeedPenalty, 10)),
                Apex("the_swarm", "The Swarm", (Power.Swarm, 10), (Power.MaxStaminaPenalty, 20)));
            ArmEnd landtaker = Arm(map, "landtaker", UpperRight, 3,
                Minor("Woodsman", (Power.ChopDamage, 2)),
                Minor("Stonebreaker", (Power.MineDamage, 2)),
                Major("clear_the_land", "Clear the Land", (Power.ChopDamage, 15), (Power.MineDamage, 15), (Power.ToolStamina, 15)),
                Minor("Woodsman", (Power.ChopDamage, 2)),
                Minor("Stonebreaker", (Power.MineDamage, 2)),
                Major("rich_seam", "Rich Seam", (Power.GatherYield, 8)),
                Major("furrow_stride", "Furrow Stride", (Power.PathSpeed, 10)),
                Minor("Rich Vein", (Power.GatherYield, 1.5f)),
                Minor("Trail", (Power.PathSpeed, 1)),
                Oath("ox_strength", "Ox-strength", (Power.OverCarry, 1), (Power.StaminaRegenPenalty, 25)),
                Apex("bearer_of_the_land", "Bearer of the Land", (Power.LoadStrength, 1), (Power.MoveSpeedPenalty, 15)));
            Bridge(map, "pasture", "Pasture", furrow, herd, "Grazing Field", (Power.TamingSpeed, 2));
            Bridge(map, "harvest_home", "Harvest Home", herd, table, "Dairy", (Power.FoodBonus, 1));
            Bridge(map, "hearth_smoke", "Hearth-smoke", table, homestead, "Longhouse", (Power.RestedDuration, 2.5f));
            Bridge(map, "hearthstone", "Hearthstone", homestead, ward, "Hearth-ward", (Power.Armor, 1));
            Bridge(map, "field_hand", "Field Hand", ward, landtaker, "Fence-line", (Power.CarryWeight, 8));
            Bridge(map, "cleared_field", "Cleared Field", landtaker, furrow, "Ploughshare", (Power.ChopDamage, 1));
            return map;
        }

        private static StarMapDefinition Vor()
        {
            StarMapDefinition map = NewMap(Deity.Vor, "ansuz", "#3B3F8C", "#C9D3E8", "#7FE3FF");
            ArmEnd insight = Arm(map, "insight", Up, 2,
                Minor("Curiosity", (Power.SkillXp, 1.5f)),
                Minor("Curiosity", (Power.SkillXp, 1.5f)),
                Major("wisdom_of_the_ages", "Wisdom of the Ages", (Power.SkillEcho, 10)),
                Minor("Second Thought", (Power.SkillEcho, 1.5f)),
                Minor("Idle Study", (Power.NonCombatSkillXp, 2.5f)),
                Major("bladebound_knowledge", "Bladebound Knowledge", (Power.BladeboundKnowledge, 10)),
                Major("hunger_for_knowledge", "Hunger for Knowledge", (Power.RecipeInsight, 25)),
                Minor("Idle Study", (Power.NonCombatSkillXp, 2.5f)),
                Major("eureka", "Eureka", (Power.Eureka, 10)),
                Oath("certainty", "Certainty", (Power.SkillXp, 40), (Power.DeathSkillLoss, 100)),
                Apex("voluspa", "Völuspá", (Power.Voluspa, 20), (Power.FoodDurationPenalty, 15)));
            ArmEnd lore = Arm(map, "lore", Left, 6,
                Minor("Studious", (Power.CritChance, 0.5f)),
                Minor("Keen Strike", (Power.CritDamage, 2.5f)),
                Major("knowledge_is_power", "Knowledge Is Power", (Power.KnowledgeIsPower, 10)),
                Minor("Studious", (Power.CritChance, 0.5f)),
                Minor("Keen Strike", (Power.CritDamage, 2.5f)),
                Major("learned_from_the_fallen", "Learned from the Fallen", (Power.LearnedFromFallen, 5)),
                Minor("Lore of War", (Power.KnowledgeIsPower, 1.5f)),
                Minor("Studious", (Power.CritChance, 0.5f)),
                Minor("Lore of War", (Power.KnowledgeIsPower, 1.5f)),
                Oath("certain_blow", "Certain Blow", (Power.CertainBlow, 1)),
                Apex("foresight", "Foresight", (Power.Foresight, 1), (Power.MaxHealthPenalty, 20)));
            ArmEnd hidden = Arm(map, "hidden", Right, 3,
                Minor("Far Sight", (Power.ExploreRadius, 5)),
                Minor("Quiet Step", (Power.Stealth, 2.5f)),
                Major("ore_sense", "Ore Sense", (Power.OreSense, 40)),
                Minor("Far Sight", (Power.ExploreRadius, 5)),
                Minor("Quiet Step", (Power.Stealth, 2.5f)),
                Major("tracks", "Tracks", (Power.Tracks, 1)),
                Major("seeress_map", "The Seeress's Map", (Power.SeeressMap, 80)),
                Minor("Deep Sense", (Power.OreSense, 5)),
                Minor("Seeress's Gaze", (Power.SeeressMap, 10)),
                Oath("ravens_eye", "Raven's Eye", (Power.RavensEye, 150), (Power.CombatSkillGainPenalty, 25)),
                Apex("unveiling", "Unveiling", (Power.Unveiling, 2), (Power.SkillGainPenalty, 25)));
            ArmEnd careful = Arm(map, "careful", UpperLeft, 1,
                Minor("Mindful", (Power.DurabilityLoss, 2.5f)),
                Minor("Sure Hands", (Power.MaxDurability, 1)),
                Major("lasting_make", "Lasting Make", (Power.DurabilityLoss, 25), (Power.MaxDurability, 10)),
                Minor("Mindful", (Power.DurabilityLoss, 2.5f)),
                Minor("Sure Hands", (Power.MaxDurability, 1)),
                Major("thrifty_craft", "Thrifty Craft", (Power.CraftRefund, 10)),
                Major("masterwork", "Masterwork", (Power.Masterwork, 10)),
                Minor("Thrift", (Power.CraftRefund, 1.5f)),
                Minor("Whetted", (Power.Masterwork, 2)),
                Oath("exacting", "Exacting", (Power.Masterwork, 100), (Power.CraftCostPenalty, 50)),
                Apex("weapon_lore", "Weapon-lore", (Power.WeaponLore, 1), (Power.ForeignGearPenalty, 20)));
            ArmEnd weakpoints = Arm(map, "weakpoints", Down, 5,
                Minor("Quiet Blade", (Power.SneakAttack, 5)),
                Minor("Studied", (Power.Weakness, 2)),
                Major("unseen_edge", "Unseen Edge", (Power.SneakAttack, 50)),
                Minor("Quiet Blade", (Power.SneakAttack, 5)),
                Minor("Studied", (Power.Weakness, 2)),
                Major("know_thy_foe", "Know Thy Foe", (Power.Weakness, 15)),
                Major("keep_the_secret", "Keep the Secret", (Power.QuietKill, 1)),
                Minor("Close Study", (Power.Weakness, 2)),
                Minor("Soft Tread", (Power.Stealth, 2.5f)),
                Oath("first_cut", "First Cut", (Power.FirstCut, 1), (Power.AlertedDamagePenalty, 15)),
                Apex("read_the_fight", "Read the Fight", (Power.ReadTheFight, 1), (Power.MaxHealthPenalty, 20)));
            ArmEnd vows = Arm(map, "vows", UpperRight, 4,
                Minor("Pledge", (Power.VowKeeper, 0.5f)),
                Minor("Bound", (Power.OathEase, 2)),
                Major("keeper_of_vows", "Keeper of Vows", (Power.VowKeeper, 3)),
                Minor("Pledge", (Power.VowKeeper, 0.5f)),
                Minor("Bound", (Power.OathEase, 2)),
                Major("bound_word", "Bound Word", (Power.OathEase, 15)),
                Major("grudge", "Grudge", (Power.Grudge, 20)),
                Minor("Oath-tally", (Power.VowKeeper, 0.5f)),
                Minor("Grudge-mark", (Power.Grudge, 5)),
                Oath("vars_witness", "Vár's Witness", (Power.VowWitness, 1), (Power.MaxHealthPenalty, 10)),
                Apex("oath_ring", "Oath-ring", (Power.OathRing, 1), (Power.Fortification, 2), (Power.DeathSkillLoss, 50)));
            Bridge(map, "open_eyes", "Open Eyes", insight, careful, "Apprenticeship", (Power.SkillXp, 1));
            Bridge(map, "hidden_lore", "Hidden Lore", careful, lore, "Whetted Lore", (Power.CritChance, 0.5f));
            Bridge(map, "soft_spot", "Soft Spot", lore, weakpoints, "Studied Strike", (Power.Weakness, 1));
            Bridge(map, "watchful", "Watchful", weakpoints, hidden, "Shadow-sight", (Power.Stealth, 1.5f));
            Bridge(map, "whispered_oath", "Whispered Oath", hidden, vows, "Secret Oath", (Power.VowKeeper, 0.5f));
            Bridge(map, "tally", "Tally", vows, insight, "Sworn Study", (Power.SkillXp, 1));
            return map;
        }

        private static StarMapDefinition Baldur()
        {
            StarMapDefinition map = NewMap(Deity.Baldur, "sowilo", "#FFF4D6", "#F5B83D", "#FFFFFF");
            ArmEnd radiance = Arm(map, "radiance", Up, 3,
                Minor("Dawnlight", (Power.UndeadDamage, 2)),
                Minor("Sun-touched", (Power.SpiritDamage, 1)),
                Major("light_of_baldur", "Light of Baldur", (Power.UndeadDamage, 15)),
                Minor("Dawnlight", (Power.UndeadDamage, 2)),
                Minor("Sun-touched", (Power.SpiritDamage, 1)),
                Major("sunstrike", "Sunstrike", (Power.Sunstrike, 50)),
                Major("cleansing_flame", "Cleansing Flame", (Power.CleansingFlame, 20)),
                Minor("Noonlight", (Power.Sunstrike, 7.5f)),
                Minor("Pyre-light", (Power.CleansingFlame, 2.5f)),
                Oath("sunsworn", "Sunsworn", (Power.SpiritDamage, 6), (Power.NightDamagePenalty, 20)),
                Apex("dawnbringer", "Dawnbringer", (Power.Dawnbringer, 8), (Power.NoSneak, 1)));
            ArmEnd purity = Arm(map, "purity", Left, 2,
                Minor("Clean Blood", (Power.FireAndPoisonResist, 2.5f)),
                Minor("Dry Cloak", (Power.WetResist, 5)),
                Major("purify", "Purify", (Power.FireAndPoisonResist, 15)),
                Minor("Clean Blood", (Power.FireAndPoisonResist, 2.5f)),
                Minor("Dry Cloak", (Power.WetResist, 5)),
                Major("cleanse_wet", "Cleanse Wet", (Power.WetResist, 40)),
                Major("clean_parry", "Clean Parry", (Power.CleanParry, 1)),
                Minor("Untouched", (Power.FireAndPoisonResist, 2.5f)),
                Minor("Steady Hand", (Power.ParryWindow, 2.5f)),
                Oath("untainted", "Untainted", (Power.PoisonImmunity, 1), (Power.NoPoisonDealt, 1)),
                Apex("bright_retort", "Bright Retort", (Power.BrightRetort, 1), (Power.StaminaRegenPenalty, 25)));
            ArmEnd invincibility = Arm(map, "shield", Right, 6,
                Minor("Shield Arm", (Power.BlockPower, 2)),
                Minor("Steady Hand", (Power.ParryWindow, 2.5f)),
                Major("shield_wall", "Shield Wall", (Power.BlockPower, 15)),
                Minor("Shield Arm", (Power.BlockPower, 2)),
                Minor("Steady Hand", (Power.ParryWindow, 2.5f)),
                Major("arrow_catcher", "Arrow Catcher", (Power.ArrowCatcher, 40)),
                Major("returned_light", "Returned Light", (Power.ReturnedLight, 50)),
                Minor("Steady Eye", (Power.ArrowCatcher, 2.5f)),
                Minor("Mirror Shield", (Power.ReturnedLight, 7.5f)),
                Oath("oath_of_all_things", "Oath of All Things", (Power.AllThings, 1), (Power.PierceDamageTaken, 50)),
                Apex("invincible", "Invincible", (Power.Invincible, 1), (Power.MaxHealthPenalty, 20)));
            ArmEnd breidablik = Arm(map, "breidablik", UpperLeft, 5,
                Minor("Mending Light", (Power.HealingReceived, 2.5f)),
                Minor("Bright Hall", (Power.Sanctuary, 1)),
                Major("breidablik", "Breiðablik", (Power.Sanctuary, 15)),
                Minor("Mending Light", (Power.HealingReceived, 2.5f)),
                Minor("Bright Hall", (Power.Sanctuary, 1)),
                Major("mercy", "Mercy", (Power.Mercy, 10)),
                Major("safe_haven", "Safe Haven", (Power.SafeHaven, 10)),
                Minor("Gentle Light", (Power.Mercy, 2)),
                Minor("Haven", (Power.SafeHaven, 1)),
                Oath("selfless", "Selfless", (Power.Selfless, 1), (Power.HealthRegenPenalty, 50)),
                Apex("all_things_wept", "All Things Wept", (Power.AllThingsWept, 1), (Power.MaxHealthPenalty, 20)));
            ArmEnd hringhorni = Arm(map, "hringhorni", Down, 1,
                Minor("Fair Wind", (Power.SailSpeed, 2)),
                Minor("Sea-legs", (Power.SwimStamina, 2.5f)),
                Major("hringhorni", "Hringhorni", (Power.SailSpeed, 15)),
                Minor("Fair Wind", (Power.SailSpeed, 2)),
                Minor("Sea-legs", (Power.SwimStamina, 2.5f)),
                Major("steady_keel", "Steady Keel", (Power.SteadyKeel, 50)),
                Major("sea_born", "Sea-born", (Power.SwimSpeed, 25), (Power.SwimStamina, 30)),
                Minor("Tarred Hull", (Power.SteadyKeel, 5)),
                Minor("Strong Stroke", (Power.SwimSpeed, 2.5f)),
                Oath("fair_wind_always", "Fair Wind Always", (Power.FairWind, 1), (Power.FireDamageTaken, 25)),
                Apex("ship_caller", "Ship-Caller", (Power.ShipCaller, 1), (Power.MaxHealthPenalty, 20)));
            ArmEnd dawn = Arm(map, "dawn", UpperRight, 4,
                Minor("Sunlit", (Power.DaylightDamage, 1.5f)),
                Minor("Daybreak", (Power.DaylightRegen, 2.5f)),
                Major("sun_kissed", "Sun-kissed", (Power.DaylightRegen, 15), (Power.DaylightDamage, 5)),
                Minor("Sunlit", (Power.DaylightDamage, 1.5f)),
                Minor("Daybreak", (Power.DaylightRegen, 2.5f)),
                Major("torchbearer", "Torchbearer", (Power.Torchbearer, 50)),
                Major("mist_burner", "Mist-burner", (Power.MistBurner, 20)),
                Minor("Pitch", (Power.Torchbearer, 10)),
                Minor("Clear Sight", (Power.MistBurner, 2)),
                Oath("bifrost", "Bifröst", (Power.Bifrost, 1), (Power.EitrRegenPenalty, 25)),
                Apex("radiant_judgment", "Radiant Judgment", (Power.RadiantJudgment, 50), (Power.NightDamageTaken, 20)));
            Bridge(map, "morning_light", "Morning Light", radiance, breidablik, "Hall-light", (Power.UndeadDamage, 2));
            Bridge(map, "pure_heart", "Pure Heart", breidablik, purity, "Clean Hearth", (Power.FireAndPoisonResist, 1.5f));
            Bridge(map, "salt_spray", "Salt Spray", purity, hringhorni, "Sea-washed", (Power.WetResist, 2.5f));
            Bridge(map, "shield_rail", "Shield-rail", hringhorni, invincibility, "Gunwale", (Power.BlockPower, 1));
            Bridge(map, "bright_shield", "Bright Shield", invincibility, dawn, "Sunlit Shield", (Power.BlockPower, 1.5f));
            Bridge(map, "first_light", "First Light", dawn, radiance, "Daystar", (Power.DaylightDamage, 1));
            return map;
        }

        private static StarMapDefinition Skadi()
        {
            StarMapDefinition map = NewMap(Deity.Skaldi, "isa", "#8FD3FF", "#FFFFFF", "#CFEFFF");
            ArmEnd hunt = Arm(map, "hunt", Up, 4,
                Minor("Keen Eye", (Power.BowDamage, 1.5f)),
                Minor("Quick Draw", (Power.DrawSpeed, 2)),
                Major("huntress_prowess", "Huntress Prowess", (Power.ArrowScavenge, 10)),
                Minor("Keen Eye", (Power.BowDamage, 1.5f)),
                Minor("Quick Draw", (Power.DrawSpeed, 2)),
                Major("piercing_shot", "Piercing Shot", (Power.PiercingShot, 40)),
                Major("field_dressing", "Field Dressing", (Power.FieldDressing, 1)),
                Minor("Tracker", (Power.ArrowScavenge, 1.5f)),
                Minor("Keen Eye", (Power.BowDamage, 1.5f)),
                Oath("rain_of_the_north", "Rain of the North", (Power.ExtraArrows, 2), (Power.DrawSpeedPenalty, 50), (Power.BowDamagePenalty, 50)),
                Apex("hang_on_the_wind", "Hang on the Wind", (Power.HangOnTheWind, 1), (Power.MeleeDamagePenalty, 20)));
            ArmEnd winter = Arm(map, "winter", Left, 6,
                Minor("Cold-hardy", (Power.FrostResist, 2.5f)),
                Minor("Rime", (Power.FrostDamage, 1)),
                Major("swift_is_the_shadow", "Swift is the Shadow", (Power.ShadowSpeed, 8)),
                Minor("Night Runner", (Power.ShadowSpeed, 1)),
                Minor("Rime", (Power.FrostDamage, 1)),
                Major("stormbound_rage", "Stormbound Rage", (Power.StormRage, 10)),
                Major("shatter", "Shatter", (Power.Shatter, 20)),
                Minor("Cold-hardy", (Power.FrostResist, 2.5f)),
                Minor("Splinters", (Power.Shatter, 2.5f)),
                Oath("child_of_thrymheimr", "Child of Þrymheimr", (Power.ColdImmunity, 1), (Power.FireDamageTaken, 50)),
                Apex("winters_shatter", "Winter's Shatter", (Power.WintersShatter, 30), (Power.FireDamageTaken, 30)));
            ArmEnd peaks = Arm(map, "peaks", Right, 2,
                Minor("Light Step", (Power.JumpHeight, 2.5f)),
                Minor("Long Stride", (Power.RunStamina, 2)),
                Major("sure_footed", "Sure-footed", (Power.SureFooted, 1)),
                Minor("Light Step", (Power.JumpHeight, 2.5f)),
                Minor("Long Stride", (Power.RunStamina, 2)),
                Major("skis_of_skadi", "Skis of Skaði", (Power.Skis, 30)),
                Major("leap", "Leap", (Power.Leap, 25)),
                Minor("Downhill", (Power.Skis, 5)),
                Minor("Spring Leap", (Power.Leap, 5)),
                Oath("mountain_born", "Mountain-born", (Power.DoubleJump, 1), (Power.CarryWeightPenalty, 100)),
                Apex("wolf_sister", "Wolf-Sister", (Power.WolfSister, 1), (Power.MaxHealthPenalty, 15)));
            ArmEnd quarry = Arm(map, "quarry", UpperLeft, 1,
                Minor("Clean Cut", (Power.HideYield, 3)),
                Minor("Marrow", (Power.MeatFood, 1.5f)),
                Major("skinner", "Skinner", (Power.HideYield, 15)),
                Minor("Clean Cut", (Power.HideYield, 3)),
                Minor("Marrow", (Power.MeatFood, 1.5f)),
                Major("trophy_hunter", "Trophy Hunter", (Power.TrophyChance, 25)),
                Major("hunters_fare", "Hunter's Fare", (Power.MeatFood, 15)),
                Minor("Trophy-taker", (Power.TrophyChance, 5)),
                Minor("Bone-broth", (Power.MeatFood, 2.5f)),
                Oath("call_of_the_hunt", "Call of the Hunt", (Power.CallOfTheHunt, 1)),
                Apex("thjazis_eyes", "Þjazi's Eyes", (Power.TrophyLore, 5), (Power.MaxHealthPenalty, 15)));
            ArmEnd spear = Arm(map, "spear", Down, 5,
                Minor("Spear-arm", (Power.SpearDamage, 1.5f)),
                Minor("Long Throw", (Power.ThrowSpeed, 2.5f)),
                Major("huntress_spear", "Huntress's Spear", (Power.ThrowSpeed, 25), (Power.SpearDamage, 15)),
                Minor("Spear-arm", (Power.SpearDamage, 1.5f)),
                Minor("Long Throw", (Power.ThrowSpeed, 2.5f)),
                Major("returning_spear", "Returning Spear", (Power.ReturningSpear, 1)),
                Major("pinning_throw", "Pinning Throw", (Power.PinningThrow, 50)),
                Minor("Lanyard", (Power.ThrowSpeed, 2.5f)),
                Minor("Barbed", (Power.PinningThrow, 5)),
                Oath("long_hunt", "Long Hunt", (Power.SpearPierce, 1), (Power.BowDamagePenalty, 25)),
                Apex("winter_spear", "Winter Spear", (Power.WinterSpear, 60), (Power.NoBlock, 1)));
            ArmEnd pack = Arm(map, "pack", UpperRight, 3,
                Minor("Pack-scent", (Power.CompanionDamage, 2)),
                Minor("Thick Fur", (Power.CompanionGuard, 2)),
                Major("pack_leader", "Pack-leader", (Power.CompanionDamage, 15), (Power.CompanionGuard, 15)),
                Minor("Pack-scent", (Power.CompanionDamage, 2)),
                Minor("Thick Fur", (Power.CompanionGuard, 2)),
                Major("pack_tactics", "Pack Tactics", (Power.PackTactics, 10)),
                Major("lick_wounds", "Lick Wounds", (Power.LickWounds, 3)),
                Minor("Howl", (Power.PackTactics, 2)),
                Minor("Warm Den", (Power.LickWounds, 0.5f)),
                Oath("pack_bound", "Pack-bound", (Power.CompanionGuard, 40), (Power.DamageTaken, 15)),
                Apex("endless_pack", "Endless Pack", (Power.EndlessPack, 1), (Power.StaminaRegenPenalty, 25)));
            Bridge(map, "cold_aim", "Cold Aim", hunt, quarry, "Stalker", (Power.BowDamage, 1));
            Bridge(map, "pelt_lined", "Pelt-lined", quarry, winter, "Fur-trapper", (Power.FrostResist, 2.5f));
            Bridge(map, "rimed_haft", "Rimed Haft", winter, spear, "Ice-hunter", (Power.SpearDamage, 1));
            Bridge(map, "snow_stride", "Snow Stride", spear, peaks, "Crag-spear", (Power.RunStamina, 1.5f));
            Bridge(map, "den_mother", "Den-mother", peaks, pack, "High Pack", (Power.CompanionGuard, 1));
            Bridge(map, "steady_draw", "Steady Draw", pack, hunt, "Running Hunt", (Power.DrawSpeed, 1.5f));
            return map;
        }

        private static StarMapDefinition Syn()
        {
            StarMapDefinition map = NewMap(Deity.Syn, "algiz", "#B07A3A", "#C0392B", "#FFD9A0");
            ArmEnd refusal = Arm(map, "refusal", Up, 2,
                Minor("Firm Stance", (Power.StaggerThreshold, 2.5f)),
                Minor("Watchful", (Power.StaggeredDamageReduction, 1.5f)),
                Major("built_different", "Built Different", (Power.StaggerThreshold, 20)),
                Minor("Firm Stance", (Power.StaggerThreshold, 2.5f)),
                Minor("Watchful", (Power.StaggeredDamageReduction, 1.5f)),
                Major("ever_watchful", "Ever Watchful", (Power.StaggeredDamageReduction, 25)),
                Major("footing", "Footing", (Power.Footing, 1)),
                Minor("Firm Stance", (Power.StaggerThreshold, 2.5f)),
                Minor("Steady Guard", (Power.BlockStamina, 1.5f)),
                Oath("the_unmoving_door", "The Unmoving Door", (Power.Unstaggerable, 1), (Power.MoveSpeedPenalty, 25)),
                Apex("stone_skin", "Stone Skin", (Power.StoneSkin, 1), (Power.Fortification, 4), (Power.MoveSpeedPenalty, 15)));
            ArmEnd gate = Arm(map, "gate", Left, 4,
                Minor("Gatewarden", (Power.TowerBlockPower, 2)),
                Minor("Steady Guard", (Power.BlockStamina, 1.5f)),
                Major("guardian_of_the_gates", "Guardian of the Gates", (Power.TowerBlockPower, 15)),
                Minor("Gatewarden", (Power.TowerBlockPower, 2)),
                Minor("Steady Guard", (Power.BlockStamina, 1.5f)),
                Major("brutal_defiance", "Brutal Defiance", (Power.BrutalDefiance, 60)),
                Major("sealed_wall", "Sealed Wall", (Power.SealedWall, 50)),
                Minor("Battering Shield", (Power.BrutalDefiance, 7.5f)),
                Minor("Barred Door", (Power.TowerBlockPower, 2)),
                Oath("the_shut_door", "The Shut Door", (Power.ThornsOnBlock, 30), (Power.LessDamageDealt, 15)),
                Apex("doors_of_valhalla", "Doors of Valhalla", (Power.RuneDome, 20), (Power.LessDamageDealt, 20)));
            ArmEnd blade = Arm(map, "blade", Right, 6,
                Minor("Keen Edge", (Power.MeleeDamage, 1.5f)),
                Minor("Keen Edge", (Power.MeleeDamage, 1.5f)),
                Major("wealth_of_the_ages", "Wealth of the Ages", (Power.LootWealth, 5)),
                Minor("Hoarder", (Power.LootWealth, 1)),
                Minor("Keen Edge", (Power.MeleeDamage, 1.5f)),
                Major("strength_of_the_valkyries", "Strength of the Valkyries", (Power.StrengthOfValkyries, 10)),
                Major("challenge", "Challenge", (Power.Challenge, 1)),
                Minor("Valkyrie's Lesson", (Power.StrengthOfValkyries, 1.5f)),
                Minor("Keen Edge", (Power.MeleeDamage, 1.5f)),
                Oath("dedication_to_the_blade", "Dedication to the Blade", (Power.MeleeDamage, 25), (Power.NoBows, 1), (Power.NoCrossbows, 1)),
                Apex("the_last_door", "The Last Door", (Power.LastDoor, 1), (Power.DamageTaken, 15)));
            ArmEnd warden = Arm(map, "warden", UpperLeft, 1,
                Minor("Mortar", (Power.HallWard, 2.5f)),
                Minor("Watchman", (Power.RaidDefender, 2)),
                Major("hall_ward", "Hall-ward", (Power.HallWard, 20)),
                Minor("Mortar", (Power.HallWard, 2.5f)),
                Minor("Watchman", (Power.RaidDefender, 2)),
                Major("defender_of_the_hall", "Defender of the Hall", (Power.RaidDefender, 15)),
                Major("mend_the_walls", "Mend the Walls", (Power.MendWalls, 1)),
                Minor("Rally", (Power.RaidDefender, 1.5f)),
                Minor("Mason", (Power.HallWard, 2.5f)),
                Oath("the_barred_hall", "The Barred Hall", (Power.BarredHall, 1), (Power.MoveSpeedPenalty, 10)),
                Apex("ban_the_kind", "Ban the Kind", (Power.BanTheKind, 25), (Power.UnbannedDamagePenalty, 10)));
            ArmEnd polearm = Arm(map, "polearm", Down, 3,
                Minor("Long Haft", (Power.PolearmDamage, 1.5f)),
                Minor("Set Stance", (Power.PolearmStamina, 2)),
                Major("gate_guards_reach", "Gate-guard's Reach", (Power.PolearmDamage, 15)),
                Minor("Long Haft", (Power.PolearmDamage, 1.5f)),
                Minor("Set Stance", (Power.PolearmStamina, 2)),
                Major("set_against_the_charge", "Set Against the Charge", (Power.SetAgainstCharge, 1)),
                Major("sweeping_ward", "Sweeping Ward", (Power.SweepFortify, 1), (Power.Fortification, 2)),
                Minor("Long Haft", (Power.PolearmDamage, 1.5f)),
                Minor("Set Stance", (Power.PolearmStamina, 2)),
                Oath("wall_of_spears", "Wall of Spears", (Power.WallOfSpears, 25), (Power.DodgeStaminaPenalty, 50)),
                Apex("hold_the_gate", "Hold the Gate", (Power.HoldTheGate, 1), (Power.MoveSpeedPenalty, 15)));
            ArmEnd denial = Arm(map, "denial", UpperRight, 5,
                Minor("Dull Their Blades", (Power.Refuse, 1.5f)),
                Minor("Crack", (Power.ArmourBreak, 0.5f)),
                Major("refuse", "Refuse", (Power.Refuse, 15)),
                Minor("Dull Their Blades", (Power.Refuse, 1.5f)),
                Minor("Crack", (Power.ArmourBreak, 0.5f)),
                Major("armour_break", "Armour Break", (Power.ArmourBreak, 3)),
                Major("interrupt", "Interrupt", (Power.Interrupt, 50)),
                Minor("Crack", (Power.ArmourBreak, 0.5f)),
                Minor("Cut Short", (Power.Interrupt, 5)),
                Oath("mirror_of_refusal", "Mirror of Refusal", (Power.Reflect, 15), (Power.MaxHealthPenalty, 10)),
                Apex("verdict", "Verdict", (Power.Verdict, 25), (Power.MaxStaminaPenalty, 20)));
            Bridge(map, "braced", "Braced", refusal, warden, "Doorpost", (Power.StaggerThreshold, 1.5f));
            Bridge(map, "gatehouse", "Gatehouse", warden, gate, "Gate-hall", (Power.HallWard, 2.5f));
            Bridge(map, "gate_iron", "Gate-iron", gate, polearm, "Pike-gate", (Power.Armor, 1));
            Bridge(map, "long_reach", "Long Reach", polearm, blade, "Haft and Edge", (Power.PolearmDamage, 1));
            Bridge(map, "shoulder_blow", "Shoulder Blow", blade, denial, "Edge Denied", (Power.MeleeDamage, 1));
            Bridge(map, "turn_aside", "Turn Aside", denial, refusal, "Denied Ground", (Power.Refuse, 1.5f));
            return map;
        }

        private static StarMapDefinition Freyja()
        {
            StarMapDefinition map = NewMap(Deity.Freya, "fehu", "#E86A9A", "#F2B33D", "#C9A0FF");
            ArmEnd seidr = Arm(map, "seidr", Up, 4,
                Minor("Eitr Well", (Power.EitrBase, 3)),
                Minor("Flowing Seiðr", (Power.EitrRegen, 2.5f)),
                Major("seidr_of_plenty", "Seiðr of Plenty", (Power.EitrBase, 15), (Power.EitrRegen, 15)),
                Minor("Sharp Seiðr", (Power.MagicDamage, 1.5f)),
                Minor("Flowing Seiðr", (Power.EitrRegen, 2.5f)),
                Major("echo", "Echo", (Power.SpellEcho, 15)),
                Major("storm_weave", "Storm-weave", (Power.StormWeave, 40)),
                Minor("Resonance", (Power.SpellEcho, 2)),
                Minor("Sharp Seiðr", (Power.MagicDamage, 1.5f)),
                Oath("volvas_price", "Völva's Price", (Power.BloodSeidr, 1), (Power.MaxHealthPenalty, 20)),
                Apex("brisingamen", "Brísingamen", (Power.Brisingamen, 1), (Power.EitrRegenPenalty, 30)));
            ArmEnd war = Arm(map, "war", Left, 6,
                Minor("Battle Breath", (Power.StaminaRegen, 2)),
                Minor("Light Swing", (Power.AttackStamina, 1.5f)),
                Major("goddess_of_war", "Goddess of War", (Power.StaminaRegen, 15), (Power.HealthRegen, 10)),
                Minor("Battle Breath", (Power.StaminaRegen, 2)),
                Minor("Light Swing", (Power.AttackStamina, 1.5f)),
                Major("perfect_form", "Perfect Form", (Power.PerfectForm, 50)),
                Major("battle_fury", "Battle Fury", (Power.BattleFury, 1)),
                Minor("Precision", (Power.PerfectForm, 5)),
                Minor("Rising Fury", (Power.BattleFury, 0.1f)),
                Oath("chooser_of_the_slain", "Chooser of the Slain", (Power.HealOnKill, 8), (Power.DamageTaken, 15)),
                Apex("folkvangr", "Fólkvangr", (Power.Folkvangr, 50), (Power.MaxHealthPenalty, 20)));
            ArmEnd falcon = Arm(map, "falcon", Right, 3,
                Minor("Feather Step", (Power.DodgeStamina, 2)),
                Minor("Featherweight", (Power.JumpStamina, 2.5f)),
                Major("falcon_glide", "Falcon Glide", (Power.FalconGlide, 1)),
                Minor("Gilded", (Power.Armor, 1)),
                Minor("Feather Step", (Power.DodgeStamina, 2)),
                Major("beloved", "Beloved", (Power.Beloved, 1)),
                Major("battle_medic", "Battle Medic", (Power.BattleMedic, 2)),
                Minor("Gilded", (Power.Armor, 1)),
                Minor("Gentle Hands", (Power.BattleMedic, 0.25f)),
                Oath("charged_grace", "Charged Grace", (Power.ChargedGrace, 25), (Power.ArmorPenalty, 20)),
                Apex("falcon_form", "Falcon Form", (Power.FalconForm, 1), (Power.StaminaRegenPenalty, 30)));
            ArmEnd cauldron = Arm(map, "cauldron", UpperLeft, 2,
                Minor("Steeped", (Power.MeadDuration, 4)),
                Minor("Strong Brew", (Power.MeadStrength, 2.5f)),
                Major("volvas_brew", "Völva's Brew", (Power.MeadDuration, 25), (Power.MeadStrength, 15)),
                Minor("Steeped", (Power.MeadDuration, 4)),
                Minor("Strong Brew", (Power.MeadStrength, 2.5f)),
                Major("quick_draught", "Quick Draught", (Power.MeadCooldown, 25)),
                Major("last_drop", "Last Drop", (Power.MeadSave, 15)),
                Minor("Quick Hand", (Power.MeadCooldown, 2.5f)),
                Minor("Dregs", (Power.MeadSave, 2)),
                Oath("twice_brewed", "Twice-brewed", (Power.TwiceBrewed, 1), (Power.MaxEitrPenalty, 20)),
                Apex("elixir_of_seidr", "Elixir of Seiðr", (Power.Elixir, 20), (Power.FoodDurationPenalty, 15)));
            ArmEnd bond = Arm(map, "bond", Down, 1,
                Minor("Devotion", (Power.BondDamage, 1)),
                Minor("Tender", (Power.BondRegen, 2.5f)),
                Major("bonded", "Bonded", (Power.Bonded, 10)),
                Minor("Devotion", (Power.BondDamage, 1)),
                Minor("Tender", (Power.BondRegen, 2.5f)),
                Major("shared_burden", "Shared Burden", (Power.SharedBurden, 20)),
                Major("seidr_link", "Seiðr Link", (Power.SeidrLink, 50)),
                Minor("Shared", (Power.SharedBurden, 2)),
                Minor("Linked", (Power.SeidrLink, 5)),
                Oath("inseparable", "Inseparable", (Power.Inseparable, 15), (Power.AloneDamagePenalty, 15)),
                Apex("longing_for_odr", "Longing for Óðr", (Power.Longing, 40), (Power.DamageTaken, 15)));
            ArmEnd gold = Arm(map, "gold", UpperRight, 5,
                Minor("Haggler", (Power.TradePrice, 1.5f)),
                Minor("Gleam", (Power.CoinFind, 2.5f)),
                Major("dwarves_bargain", "Dwarves' Bargain", (Power.TradePrice, 15)),
                Minor("Haggler", (Power.TradePrice, 1.5f)),
                Minor("Gleam", (Power.CoinFind, 2.5f)),
                Major("gersemi", "Gersemi", (Power.Gersemi, 15)),
                Major("hoard", "Hoard", (Power.Hoard, 1)),
                Minor("Glint", (Power.Gersemi, 2)),
                Minor("Hoard-heap", (Power.Hoard, 0.2f)),
                Oath("gold_hunger", "Gold-hunger", (Power.GoldHunger, 1), (Power.LoseCoinsOnDeath, 1)),
                Apex("tears_of_red_gold", "Tears of Red Gold", (Power.RedGoldTears, 25), (Power.ArmorPenalty, 20)));
            Bridge(map, "herb_lore", "Herb-lore", seidr, cauldron, "Witch-brew", (Power.MeadStrength, 1.5f));
            Bridge(map, "war_song", "War-song", cauldron, war, "War-mead", (Power.MagicDamage, 1));
            Bridge(map, "shield_maiden", "Shield-maiden", war, bond, "Shield-sister", (Power.StaminaRegen, 1));
            Bridge(map, "light_feet", "Light Feet", bond, falcon, "Feathered Vow", (Power.DodgeStamina, 1.5f));
            Bridge(map, "golden_tears", "Golden Tears", falcon, gold, "Gilded Wing", (Power.EitrBase, 2));
            Bridge(map, "gold_thread", "Gold-thread", gold, seidr, "Seiðr-gold", (Power.TradePrice, 1));
            return map;
        }

        private static StarMapDefinition Hel()
        {
            StarMapDefinition map = NewMap(Deity.Hel, "hagalaz", "#E8E2D0", "#5A7FA8", "#9FD8FF");
            ArmEnd hellfire = Arm(map, "hellfire", Up, 1,
                Minor("Ash-skin", (Power.HeatResistance, 2.5f)),
                Minor("Ember-proof", (Power.FireResist, 2.5f)),
                Major("hellfire_adaptation", "Hellfire Adaptation", (Power.HeatResistance, 15)),
                Minor("Ash-skin", (Power.HeatResistance, 2.5f)),
                Minor("Ember-proof", (Power.FireResist, 2.5f)),
                Major("retort", "Retort", (Power.Retort, 6)),
                Major("wildfire", "Wildfire", (Power.Wildfire, 20)),
                Minor("Smoulder", (Power.Retort, 1)),
                Minor("Spreading Flame", (Power.Wildfire, 2.5f)),
                Oath("pyre_of_helheim", "Pyre of Helheim", (Power.PyreOfHelheim, 1)),
                Apex("garms_hunger", "Garm's Hunger", (Power.LifeLeech, 3), (Power.HealthRegenPenalty, 100)));
            ArmEnd balance = Arm(map, "balance", Left, 4,
                Minor("Jötunn's Breath", (Power.StaminaWhenHit, 1)),
                Minor("Grave Thirst", (Power.LifeLeech, 0.25f)),
                Major("balance_of_the_jotunn", "Balance of the Jötunn", (Power.StaminaWhenHit, 6)),
                Minor("Aesir's Breath", (Power.EitrWhenHit, 1)),
                Minor("Grave Thirst", (Power.LifeLeech, 0.25f)),
                Major("balance_of_the_aesir", "Balance of the Aesir", (Power.EitrWhenHit, 6)),
                Major("stored_for_later", "Stored for Later", (Power.StoredForLater, 1), (Power.Fortification, 3)),
                Minor("Aesir's Breath", (Power.EitrWhenHit, 1)),
                Minor("Grave Thirst", (Power.LifeLeech, 0.25f)),
                Oath("deaths_bargain", "Death's Bargain", (Power.DeathsBargain, 1)),
                Apex("naglfars_crew", "Náglfar's Crew", (Power.Naglfar, 30), (Power.MaxHealthPenalty, 25)));
            ArmEnd inevitable = Arm(map, "inevitable", Right, 6,
                Minor("Executioner", (Power.ExecuteDamage, 2)),
                Minor("Executioner", (Power.ExecuteDamage, 2)),
                Major("hasten_the_inevitable", "Hasten the Inevitable", (Power.SpeedOnKill, 15)),
                Minor("Reaper's Step", (Power.SpeedOnKill, 1.5f)),
                Minor("Executioner", (Power.ExecuteDamage, 2)),
                Major("silent_passing", "Silent Passing", (Power.SilentPassing, 1)),
                Major("open_wound", "Open Wound", (Power.OpenWound, 10)),
                Minor("Reaper's Step", (Power.SpeedOnKill, 1.5f)),
                Minor("Deep Cuts", (Power.OpenWound, 1.5f)),
                Oath("half_dead", "Half-Dead", (Power.AllDamage, 25), (Power.PoisonImmunity, 1), (Power.SpiritImmunity, 1), (Power.MaxHealthPenalty, 30)),
                Apex("queens_decree", "Queen's Decree", (Power.QueensDecree, 15), (Power.DamageTaken, 25)));
            ArmEnd rot = Arm(map, "rot", UpperLeft, 3,
                Minor("Bile", (Power.PoisonOnHit, 1)),
                Minor("Lingering", (Power.PoisonDamage, 2.5f)),
                Major("plague_touched", "Plague-touched", (Power.PoisonDamage, 15), (Power.PoisonOnHit, 5)),
                Minor("Bile", (Power.PoisonOnHit, 1)),
                Minor("Lingering", (Power.PoisonDamage, 2.5f)),
                Major("contagion", "Contagion", (Power.Contagion, 20)),
                Major("grasp_of_the_dead", "Grasp of the Dead", (Power.GraspOfTheDead, 10)),
                Minor("Spore", (Power.Contagion, 2.5f)),
                Minor("Grasping", (Power.GraspOfTheDead, 1)),
                Oath("sick_bed", "Sick-bed", (Power.PoisonDrain, 15), (Power.FoodValuePenalty, 15)),
                Apex("rotting_ground", "Rotting Ground", (Power.RottingGround, 10), (Power.DamageTaken, 15)));
            ArmEnd host = Arm(map, "host", Down, 5,
                Minor("Grave-chill", (Power.SummonDamage, 2)),
                Minor("Long Watch", (Power.SummonDuration, 5)),
                Major("lord_of_the_host", "Lord of the Host", (Power.SummonDamage, 15), (Power.SummonDuration, 25)),
                Minor("Grave-chill", (Power.SummonDamage, 2)),
                Minor("Long Watch", (Power.SummonDuration, 5)),
                Major("ascendant_thralls", "Ascendant Thralls", (Power.AscendantThralls, 1)),
                Major("thralls_burden", "Thrall's Burden", (Power.ThrallsBurden, 20)),
                Minor("Grave-chill", (Power.SummonDamage, 2)),
                Minor("Bound Soul", (Power.ThrallsBurden, 2)),
                Oath("legion", "Legion", (Power.Legion, 2), (Power.LessDamageDealt, 15)),
                Apex("soul_eater", "Soul Eater", (Power.SoulEater, 2), (Power.MaxStaminaPenalty, 20)));
            ArmEnd dread = Arm(map, "dread", UpperRight, 2,
                Minor("Night-eyed", (Power.NightDamage, 1.5f)),
                Minor("Mist-cloak", (Power.NightStealth, 2.5f)),
                Major("half_dark", "Half-Dark", (Power.NightDamage, 10), (Power.NightStealth, 15)),
                Minor("Night-eyed", (Power.NightDamage, 1.5f)),
                Minor("Mist-cloak", (Power.NightStealth, 2.5f)),
                Major("dread", "Dread", (Power.Dread, 8)),
                Major("hels_gaze", "Hel's Gaze", (Power.HelsGaze, 10)),
                Minor("Shudder", (Power.Dread, 1)),
                Minor("Long Shadow", (Power.HelsGaze, 1)),
                Oath("child_of_night", "Child of Night", (Power.ChildOfNight, 20), (Power.DayDamagePenalty, 20)),
                Apex("terror_of_niflhel", "Terror of Niflhel", (Power.Terror, 2)));
            Bridge(map, "cinder_blood", "Cinder Blood", hellfire, rot, "Fever", (Power.FireResist, 1.5f));
            Bridge(map, "sickly_thirst", "Sickly Thirst", rot, balance, "Wasting", (Power.LifeLeech, 0.25f));
            Bridge(map, "last_breath", "Last Breath", balance, host, "Grave-tithe", (Power.StaminaWhenHit, 0.5f));
            Bridge(map, "grave_guard", "Grave-guard", host, inevitable, "Death-march", (Power.SummonDamage, 1));
            Bridge(map, "grave_edge", "Grave Edge", inevitable, dread, "Night Reaper", (Power.ExecuteDamage, 1));
            Bridge(map, "ember_dark", "Ember-dark", dread, hellfire, "Corpse-fire", (Power.NightDamage, 1));
            return map;
        }

        // The Deep North's deity (docs/vidarr.md). All six arms are new, so they start at the halved
        // small-star scale.
        private static StarMapDefinition Vidarr()
        {
            StarMapDefinition map = NewMap(Deity.Vidarr, "eihwaz", "#7FB59A", "#D8E6EE", "#FFE7A8");
            ArmEnd shoe = Arm(map, "shoe", Up, 3,
                Minor("Hard Sole", (Power.UnarmedDamage, 1.5f)),
                Minor("Thick Heel", (Power.SnowStride, 5)),
                Major("the_thick_shoe", "The Thick Shoe", (Power.UnarmedDamage, 15), (Power.SnowStride, 50)),
                Minor("Hard Sole", (Power.UnarmedDamage, 1.5f)),
                Minor("Thick Heel", (Power.SnowStride, 5)),
                Major("heel_stamp", "Heel-stamp", (Power.HeelStamp, 30)),
                Major("gridrs_gloves", "Gríðr's Gloves", (Power.IronGloves, 25)),
                Minor("Heel-strike", (Power.HeelStamp, 3)),
                Minor("Knuckle-iron", (Power.IronGloves, 2.5f)),
                Oath("shoe_of_scraps", "Shoe of Scraps", (Power.ShoeOfScraps, 25), (Power.ArmedDamagePenalty, 15)),
                Apex("foot_on_the_jaw", "Foot on the Jaw", (Power.FootOnTheJaw, 1), (Power.MaxHealthPenalty, 15)));
            ArmEnd winter = Arm(map, "fimbulwinter", UpperLeft, 5,
                Minor("Fur-lined", (Power.ColdEndurance, 5)),
                Minor("Rime-skin", (Power.FrostResist, 2.5f)),
                Major("endure_the_winter", "Endure the Winter", (Power.ColdEndurance, 50), (Power.FrostResist, 15)),
                Minor("Fur-lined", (Power.ColdEndurance, 5)),
                Minor("Rime-skin", (Power.FrostResist, 2.5f)),
                Major("snow_on_the_roof", "Snow on the Roof", (Power.SnowShed, 20)),
                Major("three_winters", "Three Winters", (Power.ThreeWinters, 3)),
                Minor("Swept Eaves", (Power.SnowShed, 2)),
                Minor("Winter-worn", (Power.ThreeWinters, 0.5f)),
                Oath("winter_bound", "Winter-bound", (Power.WinterBound, 15), (Power.WarmDamagePenalty, 15)),
                Apex("fimbulvetr", "Fimbulvetr", (Power.Fimbulvetr, 1), (Power.FireDamageTaken, 25)));
            ArmEnd wolf = Arm(map, "wolf_slayer", Left, 6,
                Minor("Giant-feller", (Power.GreatFoeDamage, 1.5f)),
                Minor("Fetter-proof", (Power.Unbound, 3)),
                Major("fenrirs_bane", "Fenrir's Bane", (Power.GreatFoeDamage, 15)),
                Minor("Giant-feller", (Power.GreatFoeDamage, 1.5f)),
                Minor("Fetter-proof", (Power.Unbound, 3)),
                Major("slip_the_fetter", "Slip the Fetter", (Power.Unbound, 40)),
                Major("the_long_fight", "The Long Fight", (Power.LongFight, 20)),
                Minor("Slipknot", (Power.Unbound, 5)),
                Minor("Wolf-tooth", (Power.LongFight, 2)),
                Oath("sworn_against_the_wolf", "Sworn against the Wolf", (Power.WolfSworn, 30), (Power.LesserFoeDamagePenalty, 15)),
                Apex("tear_the_jaws", "Tear the Jaws", (Power.TearTheJaws, 10), (Power.DamageTaken, 25)));
            ArmEnd vengeance = Arm(map, "vengeance", Down, 4,
                Minor("Unforgiving", (Power.WrathDamage, 1.5f)),
                Minor("Hardened", (Power.Armor, 1)),
                Major("blood_debt", "Blood-debt", (Power.BloodDebt, 25)),
                Minor("Unforgiving", (Power.WrathDamage, 1.5f)),
                Minor("Hardened", (Power.Armor, 1)),
                Major("blood_for_blood", "Blood for Blood", (Power.DebtHeal, 25)),
                Major("kin_vengeance", "Kin-vengeance", (Power.KinVengeance, 30)),
                Minor("Wound-price", (Power.DebtHeal, 2.5f)),
                Minor("Kin-sworn", (Power.KinVengeance, 3)),
                Oath("unatoned", "Unatoned", (Power.Unatoned, 25), (Power.DamageTaken, 15)),
                Apex("avenger_of_the_allfather", "Avenger of the Allfather", (Power.Avenger, 1), (Power.HealthRegenPenalty, 30)));
            ArmEnd silence = Arm(map, "silence", Right, 2,
                Minor("Bated Breath", (Power.Patience, 2)),
                Minor("Hush", (Power.Stealth, 2.5f)),
                Major("the_silent_god", "The Silent God", (Power.Patience, 20), (Power.Stealth, 10)),
                Minor("Bated Breath", (Power.Patience, 2)),
                Minor("Hush", (Power.Stealth, 2.5f)),
                Major("stillness", "Stillness", (Power.Stillness, 50)),
                Major("one_word", "One Word", (Power.OneWord, 60)),
                Minor("Long Breath", (Power.Stillness, 5)),
                Minor("Measured", (Power.OneWord, 5)),
                Oath("vow_of_silence", "Vow of Silence", (Power.SilenceVow, 20), (Power.ImpatiencePenalty, 15)),
                Apex("the_single_blow", "The Single Blow", (Power.SingleBlow, 1), (Power.MaxStaminaPenalty, 20)));
            ArmEnd after = Arm(map, "world_after", UpperRight, 1,
                Minor("Second Spring", (Power.Relearn, 5)),
                Minor("Mending", (Power.HealthRegen, 2.5f)),
                Major("the_world_renewed", "The World Renewed", (Power.Relearn, 50), (Power.HealthRegen, 10)),
                Minor("Second Spring", (Power.Relearn, 5)),
                Minor("Mending", (Power.HealthRegen, 2.5f)),
                Major("survivor", "Survivor", (Power.LastStand, 20)),
                Major("hoddmimirs_wood", "Hoddmímir's Wood", (Power.KindDeath, 50)),
                Minor("Hold Fast", (Power.LastStand, 1)),
                Minor("Lesser Loss", (Power.KindDeath, 5)),
                Oath("outlast", "Outlast", (Power.Outlast, 2), (Power.MaxHealthPenalty, 15)),
                Apex("idavollr", "Iðavöllr", (Power.GoldenPieces, 1), (Power.MaxStaminaPenalty, 15)));
            Bridge(map, "snow_shod", "Snow-shod", shoe, winter, "Trackless Snow", (Power.SnowStride, 2.5f));
            Bridge(map, "wolf_winter", "Wolf-winter", winter, wolf, "Wolf-age", (Power.FrostResist, 1.5f));
            Bridge(map, "odins_son", "Odin's Son", wolf, vengeance, "Allfather's Due", (Power.GreatFoeDamage, 1));
            Bridge(map, "cold_wrath", "Cold Wrath", vengeance, silence, "Quiet Feud", (Power.WrathDamage, 1));
            Bridge(map, "long_wait", "Long Wait", silence, after, "Hoddmímir's Holt", (Power.Patience, 1));
            Bridge(map, "patched_sole", "Patched Sole", after, shoe, "New Leather", (Power.Relearn, 2.5f));
            return map;
        }

        // ---- Building blocks ----

        private sealed class Star
        {
            public string Id;
            public string Name;
            public NodeTier Tier;
            public (Power power, float value)[] Grants;
        }

        /// <summary>
        /// An arm as its bridges and lattices see it: its name, the slot it points along, and its stars in
        /// <see cref="ArmShape"/> order with their ids.
        /// </summary>
        private readonly struct ArmEnd
        {
            public readonly string Name;
            public readonly float Angle;
            public readonly string[] Ids;
            public readonly Star[] Stars;

            public ArmEnd(string name, float angle, string[] ids, Star[] stars)
            {
                Name = name;
                Angle = angle;
                Ids = ids;
                Stars = stars;
            }

            /// <summary>The second star, where the bridges attach.</summary>
            public string Second => Ids[1];
        }

        private static Star Minor(string name, params (Power, float)[] grants) => new Star() { Name = name, Tier = NodeTier.Minor, Grants = grants };
        private static Star Major(string id, string name, params (Power, float)[] grants) => new Star() { Id = id, Name = name, Tier = NodeTier.Major, Grants = grants };
        private static Star Oath(string id, string name, params (Power, float)[] grants) => new Star() { Id = id, Name = name, Tier = NodeTier.Oath, Grants = grants };
        private static Star Apex(string id, string name, params (Power, float)[] grants) => new Star() { Id = id, Name = name, Tier = NodeTier.Apex, Grants = grants };

        // Where each of an arm's eleven stars sits, as ring, angle off the arm's line, and the earlier
        // stars it links to (-1 for the root). Two minors lead out to the first major; the arm then
        // splits into two branches of minor, major, minor that meet again at the oath-bound star, and the
        // apex lies beyond.
        private static readonly (float ring, float offset, int[] links)[] ArmShape = {
            (1f, 0f, new[] { -1 }),
            (2f, 0f, new[] { 0 }),
            (3f, 0f, new[] { 1 }),
            (4f, -12f, new[] { 2 }),
            (4f, 12f, new[] { 2 }),
            (5f, -14f, new[] { 3 }),
            (5f, 14f, new[] { 4 }),
            (6f, -12f, new[] { 5 }),
            (6f, 12f, new[] { 6 }),
            (7f, 0f, new[] { 7, 8 }),
            (8.5f, 0f, new[] { 9 }),
        };

        // The looks each arm offers: its first major the aura, the two stars where it branches the arm's
        // own look, its oath-bound star the kill effect, and its apex the wings. The Left and Upper Right
        // arms branch to the weapon glow, the rest to footprints.
        private static string BranchLook(float angle) =>
            angle == Left || angle == UpperRight ? "WeaponGlow" : "Footsteps";

        /// <summary>
        /// Adds one arm along <paramref name="angle"/>, its stars given in <see cref="ArmShape"/> order.
        /// Minors are named after the arm and numbered outwards; the apex needs <paramref name="ascension"/>.
        /// </summary>
        private static ArmEnd Arm(StarMapDefinition map, string arm, float angle, int ascension, params Star[] stars)
        {
            if (stars.Length != ArmShape.Length) {
                throw new ArgumentException($"The {arm} arm has {stars.Length} stars; an arm has {ArmShape.Length}.");
            }
            string[] ids = new string[stars.Length];
            int minors = 0;
            for (int i = 0; i < stars.Length; i++) {
                ids[i] = stars[i].Id ?? $"{arm}_{++minors}";
            }
            for (int i = 0; i < stars.Length; i++) {
                (float ring, float offset, int[] links) = ArmShape[i];
                List<string> linkIds = new List<string>();
                foreach (int link in links) { linkIds.Add(link < 0 ? "root" : ids[link]); }
                StarNodeDefinition node = Node(map, ids[i], stars[i].Name, stars[i].Tier, ring, angle + offset, linkIds.ToArray());
                foreach ((Power power, float value) in stars[i].Grants) { node.With(power, value); }
                if (stars[i].Tier == NodeTier.Apex) { node.Ascension = ascension; }
                string look = i == 2 ? "Aura" : i == 5 || i == 6 ? BranchLook(angle)
                    : stars[i].Tier == NodeTier.Oath ? "KillEffect" : stars[i].Tier == NodeTier.Apex ? "Wings" : null;
                if (look != null) { node.Cosmetics = new List<string>() { look }; }
            }
            return new ArmEnd(arm, angle, ids, stars);
        }

        /// <summary>
        /// A minor star linking two neighbouring arms' second stars, so a build can cross over early, and
        /// the lattice that fills the gap beyond it (see <see cref="Lattice"/>), whose woven stars are called
        /// <paramref name="woven"/>. The bridge sits halfway round from <paramref name="from"/> to
        /// <paramref name="to"/>, going anticlockwise.
        /// </summary>
        private static void Bridge(StarMapDefinition map, string id, string name, ArmEnd from, ArmEnd to, string woven, params (Power, float)[] grants)
        {
            float angle = (from.Angle + (to.Angle - from.Angle + 360f) % 360f / 2f) % 360f;
            StarNodeDefinition node = Node(map, id, name, NodeTier.Minor, 2.6f, angle, from.Second, to.Second);
            foreach ((Power power, float value) in grants) { node.With(power, value); }
            Lattice(map, id, Minor(name, grants), angle, from, to, woven);
        }

        /// <summary>
        /// Eight minor stars across the gap between two neighbouring arms, joining them in more places than
        /// the bridge. The arm before the gap faces it with its anticlockwise branch, the arm after with its
        /// clockwise one. A star touching an arm repeats the arm star it touches, as a second level of it;
        /// the first repeats the bridge; the rest are woven, half of an opening star from each arm. The gaps
        /// take two shapes in turn round the map, a weave and a ladder. Positions are ring and angle off
        /// the gap's middle, negative towards the arm before it.
        /// </summary>
        private static void Lattice(StarMapDefinition map, string bridgeId, Star bridge, float middle, ArmEnd from, ArmEnd to, string woven)
        {
            string prefix = $"{from.Name}_{to.Name}";
            Star weave = Woven(woven, from.Stars[1], to.Stars[0]);
            int count = 0;
            string Add(Star star, float ring, float offset, params string[] links)
            {
                string id = $"{prefix}_{++count}";
                StarNodeDefinition node = Node(map, id, star.Name, NodeTier.Minor, ring, middle + offset, links);
                foreach ((Power power, float value) in star.Grants) { node.With(power, value); }
                return id;
            }

            string spur = Add(bridge, 3.6f, 0f, bridgeId);
            string fromInner = Add(Repeat(from, 4), 4.4f, -9f, spur, from.Ids[4]);
            string toInner = Add(Repeat(to, 3), 4.4f, 9f, spur, to.Ids[3]);
            if ((int)Math.Round(middle / 60f) % 2 == 0) {
                // The weave: a diamond across the gap that closes beyond the branch tips.
                string heart = Add(weave, 5.2f, 0f, fromInner, toInner);
                string fromOuter = Add(Repeat(from, 8), 6f, -8f, heart, from.Ids[8]);
                string toOuter = Add(Repeat(to, 7), 6f, 8f, heart, to.Ids[7]);
                string knot = Add(weave, 6.9f, 0f, fromOuter, toOuter);
                Add(weave, 7.8f, 0f, knot);
            } else {
                // The ladder: a rail beside each arm, joined by a rung and closed at the top.
                string fromRail = Add(Repeat(from, 0), 5.3f, -6f, fromInner);
                string toRail = Add(Repeat(to, 1), 5.3f, 6f, toInner, fromRail);
                string fromOuter = Add(Repeat(from, 8), 6.3f, -8f, fromRail, from.Ids[8]);
                string toOuter = Add(Repeat(to, 7), 6.3f, 8f, toRail, to.Ids[7]);
                Add(weave, 7.1f, 0f, fromOuter, toOuter);
            }
        }

        /// <summary>A second level of an arm's minor; where the slot holds a stronger star, the arm's first minor.</summary>
        private static Star Repeat(ArmEnd arm, int slot)
        {
            Star star = arm.Stars[slot].Tier == NodeTier.Minor ? arm.Stars[slot] : arm.Stars[0];
            return Minor(star.Name, star.Grants);
        }

        /// <summary>Half of one star from each arm, under a name of its own.</summary>
        private static Star Woven(string name, Star from, Star to)
        {
            Dictionary<Power, float> halves = new Dictionary<Power, float>();
            foreach ((Power power, float value) in from.Grants.Concat(to.Grants)) {
                halves[power] = (halves.TryGetValue(power, out float known) ? known : 0f) + Half(value);
            }
            return Minor(name, halves.Select(entry => (entry.Key, entry.Value)).ToArray());
        }

        // Halved and kept to steps of 0.05, so the tooltips read cleanly.
        private static float Half(float value) => Math.Max(0.05f, (float)Math.Round(value * 10f, MidpointRounding.AwayFromZero) / 20f);

        private static StarMapDefinition NewMap(Deity deity, string rune, string primary, string secondary, string glow)
        {
            StarMapDefinition map = new StarMapDefinition() {
                Rune = rune,
                Palette = new DeityPalette() { Primary = primary, Secondary = secondary, Glow = glow },
            };
            map.Nodes["root"] = new StarNodeDefinition() { Name = $"$al_{deity.ToString().ToLower()}_header", Tier = NodeTier.Root };
            return map;
        }

        private static StarNodeDefinition Node(StarMapDefinition map, string id, string name, NodeTier tier, float ring, float angle, params string[] links)
        {
            StarNodeDefinition node = new StarNodeDefinition() {
                Name = name,
                Tier = tier,
                Ring = ring,
                Angle = angle,
                Links = new List<string>(links),
            };
            map.Nodes.Add(id, node);
            return node;
        }

        private static StarNodeDefinition With(this StarNodeDefinition node, Power power, float value)
        {
            Dictionary<string, float> target = PowerInfo.Of(power).IsOath ? node.Oaths : node.Powers;
            target[power.ToString()] = value;
            return node;
        }
    }
}
