using System;
using System.Collections.Generic;
using System.IO;
using AesirLineage.starmap;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static AesirLineage.common.Deities;

namespace AesirLineage.common
{
    /// <summary>
    /// What each deity's shrine offers: the blessing it gives once a day, a full reset of the deity's
    /// star map and what that costs, and what Ascending with the deity asks for. Also the inspirations
    /// the deity's dreams leave.
    /// </summary>
    public class DeityShrine
    {
        public Dictionary<string, int> ResetCost { get; set; } = new Dictionary<string, int>();
        public AscensionTribute Ascension { get; set; }
        /// <summary>The shrine's daily blessing; none when left out.</summary>
        public Boon Blessing { get; set; }
        /// <summary>What a dream from the deity can leave; one is picked at random.</summary>
        public List<Boon> Inspirations { get; set; } = new List<Boon>();
    }

    /// <summary>Star map powers a deity lends for a while, as a blessing or a dream's inspiration.</summary>
    public class Boon
    {
        /// <summary>Saved on the character while the boon is active, so it must stay the same.</summary>
        public string Id { get; set; }
        /// <summary>Shown as written, or localized when it starts with '$'.</summary>
        public string Name { get; set; }
        /// <summary>What the player dreams before an inspiration, shown as Name is. Unused for blessings.</summary>
        public string Dream { get; set; }
        /// <summary>Item prefab name to amount, taken when the blessing is asked for. Unused for inspirations.</summary>
        public Dictionary<string, int> Offering { get; set; } = new Dictionary<string, int>();
        /// <summary>Power name to value, as in the star maps.</summary>
        public Dictionary<string, float> Powers { get; set; } = new Dictionary<string, float>();

        /// <summary><see cref="Powers"/> with the names resolved, skipping unknown ones.</summary>
        [YamlIgnore]
        public List<KeyValuePair<Power, float>> Grants { get; } = new List<KeyValuePair<Power, float>>();
    }

    /// <summary>
    /// The deity's part of the price of Ascending with it, and the points it gives. What the player loses
    /// (skills, recipes, boss keys) is server config, the same for every deity.
    /// </summary>
    public class AscensionTribute
    {
        /// <summary>Item prefab name to amount, taken each time.</summary>
        public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
        /// <summary>Global or player keys needed first: every boss defeated, by default.</summary>
        public List<string> RequiredKeys { get; set; } = new List<string>();
        /// <summary>Star points for the deity each time.</summary>
        public int Points { get; set; } = 3;
    }

    public static class ShrineData
    {
        public static Dictionary<Deity, DeityShrine> All = new Dictionary<Deity, DeityShrine>();

        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
            .Build();

        /// <summary>The YAML of the shrines in use, which the server sends to clients.</summary>
        public static string ActiveYaml { get; private set; } = "";

        // Every boss key, which Ascension needs (and, when the server says so, takes away).
        private static List<string> AllBossKeys() => new List<string> {
            "defeated_eikthyr",
            "defeated_gdking",
            "defeated_bonemass",
            "defeated_dragon",
            "defeated_goblinking",
            "defeated_queen",
            "defeated_fader"
        };

        private static AscensionTribute Ascend(params (string item, int amount)[] items) {
            AscensionTribute ascension = new AscensionTribute() { RequiredKeys = AllBossKeys() };
            foreach ((string item, int amount) in items) { ascension.Items[item] = amount; }
            return ascension;
        }

        private static Dictionary<string, int> Cost(string item, int amount) => new Dictionary<string, int>() { { item, amount } };

        private static Boon Lend(string id, params (Power power, float value)[] powers) {
            Boon boon = new Boon() { Id = id, Name = $"$al_boon_{id}" };
            foreach ((Power power, float value) in powers) { boon.Powers[power.ToString()] = value; }
            return boon;
        }

        private static Boon Inspire(string id, params (Power power, float value)[] powers) {
            Boon boon = Lend(id, powers);
            boon.Dream = $"$al_boon_{id}_dream";
            return boon;
        }

        // A full reset costs the biome's boss trophy. A blessing opens once the biome's boss is down, so
        // each helps with going back there, at about a major star's worth. An inspiration is about an
        // oath-bound star's worth of one of the deity's aspects, without the oath.
        internal static Dictionary<Deity, DeityShrine> Defaults() => new Dictionary<Deity, DeityShrine>() {
            { Deity.Gefjun, new DeityShrine() {
                ResetCost = Cost("TrophyEikthyr", 1),
                Ascension = Ascend(("TrophyEikthyr", 1)),
                Blessing = Lend("gefjun_plough", (Power.CarryWeight, 100), (Power.GatherYield, 10), (Power.HarvestBounty, 10)),
                Inspirations = new List<Boon>() { Inspire("gefjun_long_table", (Power.FoodBonus, 15), (Power.HarvestBounty, 20)) },
            } },
            { Deity.Vor, new DeityShrine() {
                ResetCost = Cost("TrophyTheElder", 1),
                Ascension = Ascend(("TrophyTheElder", 1)),
                Blessing = Lend("vor_sight", (Power.OreSense, 50), (Power.ExploreRadius, 30), (Power.Stealth, 10)),
                Inspirations = new List<Boon>() { Inspire("vor_knowing", (Power.SkillXp, 30), (Power.CritChance, 5)) },
            } },
            { Deity.Baldur, new DeityShrine() {
                ResetCost = Cost("TrophyBonemass", 1),
                Ascension = Ascend(("TrophyEikthyr", 1), ("TrophyTheElder", 1), ("TrophyBonemass", 1)),
                Blessing = Lend("baldur_purity", (Power.FireAndPoisonResist, 20), (Power.WetResist, 50), (Power.UndeadDamage, 15)),
                Inspirations = new List<Boon>() { Inspire("baldur_shining", (Power.BlockPower, 20), (Power.HealthRegen, 20), (Power.UndeadDamage, 20)) },
            } },
            { Deity.Skaldi, new DeityShrine() {
                ResetCost = Cost("TrophyDragonQueen", 1),
                Ascension = Ascend(("TrophyEikthyr", 1), ("TrophyTheElder", 1), ("TrophyBonemass", 1), ("TrophyDragonQueen", 1)),
                Blessing = Lend("skadi_mantle", (Power.ColdImmunity, 1), (Power.FrostResist, 15), (Power.RunStamina, 10)),
                Inspirations = new List<Boon>() { Inspire("skadi_huntress", (Power.BowDamage, 15), (Power.DrawSpeed, 15), (Power.RunStamina, 15)) },
            } },
            { Deity.Syn, new DeityShrine() {
                ResetCost = Cost("TrophyGoblinKing", 1),
                Ascension = Ascend(("TrophyGoblinBruteBrosShaman", 1), ("TrophyGoblinBruteBrosBrute", 1)),
                Blessing = Lend("syn_ward", (Power.BlockPower, 15), (Power.StaggerThreshold, 20), (Power.Armor, 6)),
                Inspirations = new List<Boon>() { Inspire("syn_threshold", (Power.StaggerThreshold, 40), (Power.MeleeDamage, 12)) },
            } },
            { Deity.Freya, new DeityShrine() {
                ResetCost = Cost("TrophySeekerQueen", 1),
                Ascension = Ascend(("TrophySeekerQueen", 1)),
                Blessing = Lend("freya_falcon_cloak", (Power.FalconGlide, 1), (Power.JumpStamina, 25), (Power.EitrRegen, 15)),
                Inspirations = new List<Boon>() { Inspire("freya_sessrumnir", (Power.StaminaRegen, 20), (Power.EitrRegen, 20), (Power.MagicDamage, 12)) },
            } },
            { Deity.Hel, new DeityShrine() {
                ResetCost = Cost("TrophyFader", 1),
                Ascension = Ascend(("TrophyFallenValkyrie", 1)),
                Blessing = Lend("hel_embrace", (Power.HeatResistance, 25), (Power.FireResist, 20)),
                Inspirations = new List<Boon>() { Inspire("hel_harvest", (Power.LifeLeech, 2), (Power.ExecuteDamage, 20)) },
            } },
            // Kall Fimbulbringer leaves no trophy, so the reset and Ascension ask for the Krigen and Hexen,
            // the Jötnar of the Deep North.
            { Deity.Vidarr, new DeityShrine() {
                ResetCost = Cost("TrophyJotunWarrior", 1),
                Ascension = Ascend(("TrophyJotunWarrior", 1), ("TrophyJotunWitch", 1)),
                Blessing = Lend("vidarr_stride", (Power.SnowStride, 50), (Power.ColdEndurance, 50), (Power.FrostResist, 15)),
                Inspirations = new List<Boon>() { Inspire("vidarr_silent_vow", (Power.UnarmedDamage, 20), (Power.ColdEndurance, 30)) },
            } },
        };

        internal static string DefaultYaml() => Serializer.Serialize(Defaults());

        /// <summary>Loads what this machine uses when nobody else decides: built in, or the file when enabled.</summary>
        internal static void LoadLocal()
        {
            if (ValConfig.UseShrinesFile.Value) {
                try {
                    if (ApplyYaml(File.ReadAllText(ValConfig.shrinesCfgPath))) { return; }
                } catch (Exception e) {
                    Logger.LogWarning($"Could not read {ValConfig.shrinesCfgPath}, using the built-in shrines. {e.Message}");
                }
            }
            All = Defaults();
            foreach (KeyValuePair<Deity, DeityShrine> entry in All) { ResolveBoons(entry.Key, entry.Value); }
            ActiveYaml = DefaultYaml();
            modules.PlayerData.OnDataChanged();
        }

        /// <summary>Replaces the shrines with parsed YAML. Leaves the current ones alone if it does not parse.</summary>
        internal static bool ApplyYaml(string yaml)
        {
            Dictionary<Deity, DeityShrine> parsed;
            try {
                parsed = DataObjects.yamldeserializer.Deserialize<Dictionary<Deity, DeityShrine>>(yaml);
            } catch (Exception ex) {
                Logger.LogError($"Failed to parse the shrines: {ex.Message}");
                return false;
            }
            if (parsed == null) {
                Logger.LogError("The shrines are empty.");
                return false;
            }
            foreach (KeyValuePair<Deity, DeityShrine> entry in parsed) {
                DeityShrine shrine = entry.Value;
                if (shrine == null) { continue; }
                ResolveBoons(entry.Key, shrine);
                shrine.ResetCost ??= new Dictionary<string, int>();
                if (shrine.Ascension != null) {
                    shrine.Ascension.Items ??= new Dictionary<string, int>();
                    shrine.Ascension.RequiredKeys ??= new List<string>();
                }
            }
            All = parsed;
            ActiveYaml = yaml;
            Logger.LogDebug($"Loaded shrines for {parsed.Count} deities.");
            // Active boons take their powers from here.
            modules.PlayerData.OnDataChanged();
            return true;
        }

        private static void ResolveBoons(Deity deity, DeityShrine shrine)
        {
            if (!ResolveBoon(deity, shrine.Blessing)) { shrine.Blessing = null; }
            shrine.Inspirations ??= new List<Boon>();
            shrine.Inspirations.RemoveAll(boon => !ResolveBoon(deity, boon));
        }

        // Turns a boon's power names into powers, warning about any it doesn't know. A boon without an id
        // can't be saved on a character, so it is dropped.
        private static bool ResolveBoon(Deity deity, Boon boon)
        {
            if (boon == null) { return false; }
            if (string.IsNullOrWhiteSpace(boon.Id)) {
                Logger.LogWarning($"A boon of {deity}'s has no id and is skipped.");
                return false;
            }
            boon.Offering ??= new Dictionary<string, int>();
            boon.Powers ??= new Dictionary<string, float>();
            boon.Grants.Clear();
            foreach (KeyValuePair<string, float> grant in boon.Powers) {
                if (Enum.TryParse(grant.Key, ignoreCase: true, out Power power) && Enum.IsDefined(typeof(Power), power)) {
                    boon.Grants.Add(new KeyValuePair<Power, float>(power, grant.Value));
                } else {
                    Logger.LogWarning($"{deity}'s boon '{boon.Id}' grants unknown power '{grant.Key}'.");
                }
            }
            return true;
        }
    }
}
