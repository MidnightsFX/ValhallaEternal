using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AesirLineage.modules;

namespace AesirLineage.common
{
    /// <summary>
    /// Which boss must be defeated before an item or build piece can be used, for the progression lock:
    /// built in, or <c>ProgressionLock.yaml</c> when <c>UseProgressionLockFile</c> is on. Clients always use
    /// the server's.
    /// </summary>
    /// <remarks>
    /// The YAML maps each boss key in <see cref="Deeds.BossOrder"/> to the prefabs that boss opens, and
    /// <see cref="Never"/> to the prefabs that are never sealed. A listed prefab needs its boss whatever it is
    /// made of; everything else is worked out from its recipes by <see cref="ProgressionLock"/>. Each boss
    /// opens the biome after its own, so its list holds that next biome's materials, plus its own drops.
    /// </remarks>
    internal static class LockData
    {
        /// <summary>The YAML key for prefabs that are never sealed.</summary>
        internal const string Never = "never";

        /// <summary>Prefab to the level it needs: a place in <see cref="Deeds.BossOrder"/> from 1, or 0 for never.</summary>
        internal static Dictionary<string, int> Levels { get; private set; } = new Dictionary<string, int>();

        /// <summary>Goes up each time <see cref="Levels"/> is replaced, so what was worked out from it starts over.</summary>
        internal static int Version { get; private set; }

        /// <summary>The YAML in use, which the server sends to clients.</summary>
        internal static string ActiveYaml { get; private set; } = "";

        // The sorting of materials up to the Ashlands follows VentureValheim's World Advancement Progression;
        // the Deep North's is from the game's own recipes. Each tier of battle and protection idols needs the
        // boss of its tier.
        private static readonly (string boss, string opens, string[] prefabs)[] BuiltIn = {
            ("defeated_eikthyr", "Eikthyr opens the Black Forest and the sea", new[] {
                "Upgrader1Armor", "Upgrader1Weapon", "HardAntler", "BjornHide", "BjornPaw", "Chitin", "Copper", "Bronze", "BronzeNails", "FineWood",
                "SerpentScale", "Tin", "TrollHide",
                "BjornMeat", "CookedBjornMeat", "Blueberries", "Carrot", "Entrails", "FishCooked", "MushroomYellow", "Thistle",
            }),
            ("defeated_gdking", "The Elder opens the Swamp", new[] {
                "Upgrader2Armor", "Upgrader2Weapon", "CryptKey", "Chain", "ElderBark", "Iron", "IronNails", "Ooze", "Root", "SharpeningStone",
                "Bloodbag", "SerpentMeat", "SerpentMeatCooked", "Turnip",
            }),
            ("defeated_bonemass", "Bonemass opens the Mountains", new[] {
                "Upgrader3Armor", "Upgrader3Weapon", "Wishbone", "FreezeGland", "JuteRed", "Obsidian", "Silver", "WolfHairBundle", "WolfPelt", "WolfClaw", "WolfFang",
                "Onion", "WolfMeat", "CookedWolfMeat",
            }),
            ("defeated_dragon", "Moder opens the Plains", new[] {
                "Upgrader4Armor", "Upgrader4Weapon", "DragonTear", "BlackMetal", "Tar", "Needle", "LinenThread", "LoxPelt", "TrophyBjornUndead", "UndeadBjornRibcage",
                "Barley", "BarleyFlour", "BreadDough", "Bread", "ChickenEgg", "CookedEgg", "ChickenMeat", "CookedChickenMeat",
                "Cloudberry", "FishAndBread", "LoxMeat", "LoxPie", "CookedLoxMeat", "MushroomBzerker", "FragrantBundle", "SpiceMountains",
            }),
            ("defeated_goblinking", "Yagluth opens the Mistlands", new[] {
                "Upgrader5Armor", "Upgrader5Weapon", "YagluthDrop", "DvergrKey", "Bilebag", "BlackMarble", "BlackCore", "Carapace", "DvergrKeyFragment", "Eitr",
                "JuteBlue", "Sap", "ScaleHide", "Wisp", "YggdrasilWood",
                "BugMeat", "CookedBugMeat", "GiantBloodSack", "HareMeat", "CookedHareMeat", "HoneyGlazedChicken",
                "MagicallyStuffedShroom", "MeatPlatter", "MisthareSupreme", "MushroomJotunPuffs", "RoyalJelly", "SpicePlains",
            }),
            ("defeated_queen", "The Queen opens the Ashlands", new[] {
                "Upgrader6Armor", "Upgrader6Weapon", "QueenDrop", "AskBladder", "AskHide", "BellFragment", "Blackwood", "BonemawSerpentTooth", "CelestialFeather",
                "CharcoalResin", "CharredBone", "CharredCogwheel", "FlametalNew", "GemstoneBlue", "GemstoneGreen", "GemstoneRed",
                "Grausten", "MoltenCore", "MorgenSinew", "MorgenHeart", "ProustitePowder", "SulfurStone",
                "AsksvinMeat", "CookedAsksvinMeat", "BoneMawSerpentMeat", "CookedBoneMawSerpentMeat", "Fiddleheadfern",
                "MushroomSmokePuff", "PiquantPie", "RoastedCrustPie", "Vineberry", "VoltureEgg", "VoltureMeat", "CookedVoltureMeat",
                "SpiceMistlands",
            }),
            ("defeated_fader", "Fader opens the Deep North", new[] {
                "Upgrader7Armor", "Upgrader7Weapon", "FaderDrop", "Gold", "Frostwood", "FirConeFrost", "BarkaBranch",
                "MooseHide", "MooseSinew", "SealHide", "ElakingHairBundle", "MoleClaws", "WrithanRoots", "NornThread",
                "OozeMork", "FrostCore", "FrozenFuel", "Ice",
                "MooseMeat", "SealBlubber", "Kale", "KaleSeeds", "Oat", "OatSeeds", "Poteitr", "PoteitrSeeds", "Lingonberry",
                "SpiceAshlands", "SpiceDeepNorth",
            }),
        };

        private static Dictionary<string, int> DefaultLevels()
        {
            var levels = new Dictionary<string, int>();
            foreach ((string boss, string _, string[] prefabs) in BuiltIn) {
                int level = LevelOf(boss);
                foreach (string prefab in prefabs) { levels[prefab] = level; }
            }
            return levels;
        }

        // A boss key's place in the order from 1, 0 for never, or -1 for a key that is neither.
        private static int LevelOf(string key)
        {
            if (string.Equals(key, Never, StringComparison.OrdinalIgnoreCase)) { return 0; }
            int index = Array.FindIndex(Deeds.BossOrder, boss => string.Equals(boss, key, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? index + 1 : -1;
        }

        /// <summary>The built-in list as YAML, with a comment over each boss saying what it opens.</summary>
        internal static string DefaultYaml()
        {
            StringBuilder sb = new StringBuilder();
            foreach ((string boss, string opens, string[] prefabs) in BuiltIn) {
                sb.AppendLine($"# {opens}.");
                sb.AppendLine($"{boss}:");
                foreach (string prefab in prefabs) { sb.AppendLine($"  - {prefab}"); }
                sb.AppendLine();
            }
            sb.AppendLine("# Never sealed, whatever they are made of.");
            sb.AppendLine($"{Never}: []");
            return sb.ToString();
        }

        /// <summary>Loads what this machine uses when nobody else decides: built in, or the file when enabled.</summary>
        internal static void LoadLocal()
        {
            if (ValConfig.UseProgressionLockFile.Value) {
                try {
                    if (ApplyYaml(File.ReadAllText(ValConfig.progressionLockCfgPath))) { return; }
                } catch (Exception e) {
                    Logger.LogWarning($"Could not read {ValConfig.progressionLockCfgPath}, using the built-in progression lock. {e.Message}");
                }
            }
            Levels = DefaultLevels();
            ActiveYaml = DefaultYaml();
            Version++;
        }

        /// <summary>Replaces the list with parsed YAML. Leaves the current one alone if it does not parse.</summary>
        internal static bool ApplyYaml(string yaml)
        {
            Dictionary<string, List<string>> parsed;
            try {
                parsed = DataObjects.yamldeserializer.Deserialize<Dictionary<string, List<string>>>(yaml);
            } catch (Exception ex) {
                Logger.LogError($"Failed to parse the progression lock: {ex.Message}");
                return false;
            }
            if (parsed == null) {
                Logger.LogError("The progression lock is empty.");
                return false;
            }
            var levels = new Dictionary<string, int>();
            foreach (KeyValuePair<string, List<string>> entry in parsed) {
                int level = LevelOf(entry.Key);
                if (level < 0) {
                    Logger.LogWarning($"Progression lock: '{entry.Key}' is not a boss key, so its prefabs are skipped. Use {string.Join(", ", Deeds.BossOrder)} or {Never}.");
                    continue;
                }
                if (entry.Value == null) { continue; }
                foreach (string raw in entry.Value) {
                    string prefab = raw?.Trim();
                    if (string.IsNullOrEmpty(prefab)) { continue; }
                    // Listed twice, the later boss wins (never is the earliest), so a mistake seals too much
                    // rather than too little.
                    if (levels.TryGetValue(prefab, out int before)) {
                        if (before != level) { Logger.LogWarning($"Progression lock: {prefab} is listed more than once; it needs the later boss."); }
                        levels[prefab] = Math.Max(before, level);
                    } else {
                        levels[prefab] = level;
                    }
                }
            }
            Levels = levels;
            ActiveYaml = yaml;
            Version++;
            Logger.LogDebug($"Loaded the progression lock: {levels.Count(l => l.Value > 0)} sealed prefabs, {levels.Count(l => l.Value == 0)} never sealed.");
            return true;
        }
    }
}
