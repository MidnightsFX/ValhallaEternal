using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.modules {
    /// <summary>
    /// The progression lock: gear, food and build pieces made from a biome's materials stay sealed until
    /// the boss before that biome is defeated. Off unless <c>EnableProgressionLock</c> is on.
    /// </summary>
    /// <remarks>
    /// Each material below needs a boss, given as its place in <see cref="Deeds.BossOrder"/> (1 is Eikthyr,
    /// 7 is Fader); a boss's own drops need that boss, and each tier of the game's battle and protection
    /// idols (Upgrader1Weapon to Upgrader7Armor) the boss of its tier. Everything else needs the highest
    /// boss among the ingredients of its recipe, followed down through every recipe, so items from other
    /// mods are sealed by the materials they are made of. What a cooking station, oven, fermenter or
    /// smelter turns an item into needs what that item needs. An upgrade counts the ingredients of every
    /// level up to it. The sorting of materials up to the Ashlands follows VentureValheim's World
    /// Advancement Progression; the Deep North's is from the game's own recipes.
    /// A player's level is <see cref="Deeds.BiomeLevel(Player, bool)"/>: the last boss in the order they
    /// have beaten, counting their own player keys, and the world's keys too when
    /// <c>ProgressionLockSource</c> is World. The game gives the player key only to the player whose
    /// machine runs the boss, so <see cref="OnBossKilled"/> gives it to everyone who took part.
    /// Every check runs on the player's own machine.
    /// </remarks>
    internal static class ProgressionLock {
        private static readonly Dictionary<string, int> BuiltIn = Sorted(
            // Eikthyr: the Black Forest and the sea.
            (1, new[] {
                "Upgrader1Armor", "Upgrader1Weapon", "HardAntler", "BjornHide", "BjornPaw", "Chitin", "Copper", "Bronze", "BronzeNails", "FineWood",
                "SerpentScale", "Tin", "TrollHide",
                "BjornMeat", "CookedBjornMeat", "Blueberries", "Carrot", "Entrails", "FishCooked", "MushroomYellow", "Thistle",
            }),
            // The Elder: the Swamp.
            (2, new[] {
                "Upgrader2Armor", "Upgrader2Weapon", "CryptKey", "Chain", "ElderBark", "Iron", "IronNails", "Ooze", "Root", "SharpeningStone",
                "Bloodbag", "SerpentMeat", "SerpentMeatCooked", "Turnip",
            }),
            // Bonemass: the Mountains.
            (3, new[] {
                "Upgrader3Armor", "Upgrader3Weapon", "Wishbone", "FreezeGland", "JuteRed", "Obsidian", "Silver", "WolfHairBundle", "WolfPelt", "WolfClaw", "WolfFang",
                "Onion", "WolfMeat", "CookedWolfMeat",
            }),
            // Moder: the Plains.
            (4, new[] {
                "Upgrader4Armor", "Upgrader4Weapon", "DragonTear", "BlackMetal", "Tar", "Needle", "LinenThread", "LoxPelt", "TrophyBjornUndead", "UndeadBjornRibcage",
                "Barley", "BarleyFlour", "BreadDough", "Bread", "ChickenEgg", "CookedEgg", "ChickenMeat", "CookedChickenMeat",
                "Cloudberry", "FishAndBread", "LoxMeat", "LoxPie", "CookedLoxMeat", "MushroomBzerker", "FragrantBundle", "SpiceMountains",
            }),
            // Yagluth: the Mistlands.
            (5, new[] {
                "Upgrader5Armor", "Upgrader5Weapon", "YagluthDrop", "DvergrKey", "Bilebag", "BlackMarble", "BlackCore", "Carapace", "DvergrKeyFragment", "Eitr",
                "JuteBlue", "Sap", "ScaleHide", "Wisp", "YggdrasilWood",
                "BugMeat", "CookedBugMeat", "GiantBloodSack", "HareMeat", "CookedHareMeat", "HoneyGlazedChicken",
                "MagicallyStuffedShroom", "MeatPlatter", "MisthareSupreme", "MushroomJotunPuffs", "RoyalJelly", "SpicePlains",
            }),
            // The Queen: the Ashlands.
            (6, new[] {
                "Upgrader6Armor", "Upgrader6Weapon", "QueenDrop", "AskBladder", "AskHide", "BellFragment", "Blackwood", "BonemawSerpentTooth", "CelestialFeather",
                "CharcoalResin", "CharredBone", "CharredCogwheel", "FlametalNew", "GemstoneBlue", "GemstoneGreen", "GemstoneRed",
                "Grausten", "MoltenCore", "MorgenSinew", "MorgenHeart", "ProustitePowder", "SulfurStone",
                "AsksvinMeat", "CookedAsksvinMeat", "BoneMawSerpentMeat", "CookedBoneMawSerpentMeat", "Fiddleheadfern",
                "MushroomSmokePuff", "PiquantPie", "RoastedCrustPie", "Vineberry", "VoltureEgg", "VoltureMeat", "CookedVoltureMeat",
                "SpiceMistlands",
            }),
            // Fader: the Deep North.
            (7, new[] {
                "Upgrader7Armor", "Upgrader7Weapon", "FaderDrop", "Gold", "Frostwood", "FirConeFrost", "BarkaBranch",
                "MooseHide", "MooseSinew", "SealHide", "ElakingHairBundle", "MoleClaws", "WrithanRoots", "NornThread",
                "OozeMork", "FrostCore", "FrozenFuel", "Ice",
                "MooseMeat", "SealBlubber", "Kale", "KaleSeeds", "Oat", "OatSeeds", "Poteitr", "PoteitrSeeds", "Lingonberry",
                "SpiceAshlands", "SpiceDeepNorth",
            })
        );

        private static Dictionary<string, int> Sorted(params (int boss, string[] prefabs)[] bosses) {
            var sorted = new Dictionary<string, int>();
            foreach ((int boss, string[] prefabs) in bosses) {
                foreach (string prefab in prefabs) { sorted[prefab] = boss; }
            }
            return sorted;
        }

        internal static bool Active => ValConfig.EnableProgressionLock.Value;

        private static bool WorldKeys => string.Equals(ValConfig.ProgressionLockSource.Value, "World", StringComparison.OrdinalIgnoreCase);

        /// <summary>The player's level: how many bosses in the order they have got past.</summary>
        internal static int Level(Player player) => Deeds.BiomeLevel(player, WorldKeys);

        /// <summary>The boss a level needs, by name.</summary>
        internal static string BossName(int needed) => Quests.BossName(Deeds.BossOrder[Mathf.Clamp(needed, 1, Deeds.BossOrder.Length) - 1]);

        /// <summary>
        /// On the machine of each player who took part in a boss's death: gives them the boss's player key,
        /// which the game itself gives only to the player whose machine runs the boss.
        /// </summary>
        internal static void OnBossKilled(Player player, string bossKey) {
            if (!Active || player.HaveUniqueKey(bossKey)) { return; }
            player.AddUniqueKey(bossKey);
            Logger.LogDebug($"Progression lock: {bossKey} recorded for this character.");
        }

        // ---- What an item needs ----

        // An item's level at each quality, worked out once per world from its recipes.
        private static readonly Dictionary<string, int[]> items = new Dictionary<string, int[]>();
        private static readonly Dictionary<string, int> pieces = new Dictionary<string, int>();
        private static readonly Dictionary<string, List<Recipe>> recipesFor = new Dictionary<string, List<Recipe>>();
        private static readonly Dictionary<string, List<string>> convertedFrom = new Dictionary<string, List<string>>();
        private static readonly HashSet<string> resolving = new HashSet<string>();
        private static Dictionary<string, int> overrides = new Dictionary<string, int>();
        private static ObjectDB cachedFor;
        private static ZNetScene cachedScene;
        private static int cachedRecipes = -1;
        private static string overridesFrom;

        // Starts over when the world's items or recipes change, or the server's overrides do.
        private static void Refresh() {
            ObjectDB db = ObjectDB.instance;
            int recipeCount = db != null ? db.m_recipes.Count : -1;
            string raw = ValConfig.ProgressionLockItems.Value;
            if (db == cachedFor && ZNetScene.instance == cachedScene && recipeCount == cachedRecipes && raw == overridesFrom) { return; }
            cachedFor = db;
            cachedScene = ZNetScene.instance;
            cachedRecipes = recipeCount;
            overridesFrom = raw;
            items.Clear();
            pieces.Clear();
            recipesFor.Clear();
            convertedFrom.Clear();
            overrides = ParseOverrides(raw);
            if (db == null) { return; }
            foreach (Recipe recipe in db.m_recipes) {
                if (recipe == null || recipe.m_item == null || !recipe.m_enabled) { continue; }
                string made = Utils.GetPrefabName(recipe.m_item.gameObject);
                if (!recipesFor.TryGetValue(made, out List<Recipe> list)) { recipesFor[made] = list = new List<Recipe>(); }
                list.Add(recipe);
            }
            if (cachedScene == null) { return; }
            foreach (GameObject prefab in cachedScene.m_prefabs) {
                if (prefab == null) { continue; }
                CookingStation cooker = prefab.GetComponent<CookingStation>();
                if (cooker != null) { foreach (var conversion in cooker.m_conversion) { AddConversion(conversion.m_from, conversion.m_to); } }
                Fermenter fermenter = prefab.GetComponent<Fermenter>();
                if (fermenter != null) { foreach (var conversion in fermenter.m_conversion) { AddConversion(conversion.m_from, conversion.m_to); } }
                Smelter smelter = prefab.GetComponent<Smelter>();
                if (smelter != null) { foreach (var conversion in smelter.m_conversion) { AddConversion(conversion.m_from, conversion.m_to); } }
            }
        }

        private static void AddConversion(ItemDrop from, ItemDrop to) {
            if (from == null || to == null) { return; }
            string made = Utils.GetPrefabName(to.gameObject);
            string source = Utils.GetPrefabName(from.gameObject);
            if (made == source) { return; }
            if (!convertedFrom.TryGetValue(made, out List<string> list)) { convertedFrom[made] = list = new List<string>(); }
            if (!list.Contains(source)) { list.Add(source); }
        }

        // "Prefab=bosskey, ..." into prefab to level; an empty boss key is level 0, never sealed.
        private static Dictionary<string, int> ParseOverrides(string raw) {
            var parsed = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(raw)) { return parsed; }
            foreach (string pair in raw.Split(',')) {
                if (string.IsNullOrWhiteSpace(pair)) { continue; }
                string[] parts = pair.Split('=');
                string prefab = parts[0].Trim();
                string key = parts.Length > 1 ? parts[1].Trim() : "";
                int level = key.Length == 0 ? 0 : Array.FindIndex(Deeds.BossOrder, boss => string.Equals(boss, key, StringComparison.OrdinalIgnoreCase)) + 1;
                if (prefab.Length == 0 || parts.Length != 2 || (key.Length > 0 && level == 0)) {
                    Logger.LogWarning($"ProgressionLockItems: ignoring '{pair.Trim()}'. Write prefab=bosskey, with a boss key from {string.Join(", ", Deeds.BossOrder)}, or nothing after = to never seal it.");
                    continue;
                }
                parsed[prefab] = level;
            }
            return parsed;
        }

        /// <summary>The level an item needs at a quality: 0 when it is never sealed.</summary>
        internal static int Needed(ItemDrop.ItemData item, int quality) {
            if (item?.m_dropPrefab == null) { return 0; }
            return Needed(Utils.GetPrefabName(item.m_dropPrefab), quality);
        }

        internal static int Needed(string prefab, int quality) {
            Refresh();
            if (overrides.TryGetValue(prefab, out int fixedLevel)) { return fixedLevel; }
            if (!items.TryGetValue(prefab, out int[] byQuality)) { byQuality = Resolve(prefab); }
            return byQuality[Mathf.Clamp(quality, 1, byQuality.Length - 1)];
        }

        /// <summary>
        /// The level crafting or upgrading with this recipe needs: the item's own, and that of every
        /// ingredient up to the quality made.
        /// </summary>
        internal static int Needed(Recipe recipe, int quality) {
            if (recipe == null || recipe.m_item == null) { return 0; }
            string made = Utils.GetPrefabName(recipe.m_item.gameObject);
            Refresh();
            if (overrides.TryGetValue(made, out int fixedLevel)) { return fixedLevel; }
            return Math.Max(BuiltIn.TryGetValue(made, out int own) ? own : 0, RecipeLevel(recipe, quality));
        }

        /// <summary>The level a build piece needs: the highest among what it is built from.</summary>
        internal static int Needed(Piece piece) {
            if (piece == null) { return 0; }
            string name = Utils.GetPrefabName(piece.gameObject);
            Refresh();
            if (overrides.TryGetValue(name, out int fixedLevel)) { return fixedLevel; }
            if (pieces.TryGetValue(name, out int level)) { return level; }
            level = 0;
            if (piece.m_resources != null) {
                foreach (Piece.Requirement req in piece.m_resources) {
                    if (req?.m_resItem != null) { level = Math.Max(level, Needed(Utils.GetPrefabName(req.m_resItem.gameObject), 1)); }
                }
            }
            pieces[name] = level;
            return level;
        }

        // An item's level at each quality from 1 to its highest. Of several recipes and conversions that
        // make it, the easiest counts.
        private static int[] Resolve(string prefab) {
            int own = BuiltIn.TryGetValue(prefab, out int listed) ? listed : 0;
            // A recipe that leads back to an item already being worked out adds nothing.
            if (!resolving.Add(prefab)) { return new[] { own, own }; }
            ItemDrop drop = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>() : null;
            int maxQuality = Math.Max(1, drop != null ? drop.m_itemData.m_shared.m_maxQuality : 1);
            int[] byQuality = new int[maxQuality + 1];
            recipesFor.TryGetValue(prefab, out List<Recipe> recipes);
            int converted = convertedFrom.TryGetValue(prefab, out List<string> sources) ? sources.Min(source => Needed(source, 1)) : int.MaxValue;
            // A feast is food that is also a build piece, made from what the piece is built from. Food that is
            // set down as itself says nothing more.
            Piece piece = drop != null ? drop.GetComponent<Piece>() : null;
            if (piece != null && piece.m_resources != null && piece.m_resources.Length > 0
                && !piece.m_resources.Any(req => req?.m_resItem != null && Utils.GetPrefabName(req.m_resItem.gameObject) == prefab)) {
                converted = Math.Min(converted, Needed(piece));
            }
            for (int quality = 1; quality <= maxQuality; quality++) {
                int easiest = recipes != null ? Math.Min(converted, recipes.Min(recipe => RecipeLevel(recipe, quality))) : converted;
                byQuality[quality] = Math.Max(own, easiest == int.MaxValue ? 0 : easiest);
            }
            byQuality[0] = byQuality[1];
            resolving.Remove(prefab);
            items[prefab] = byQuality;
            return byQuality;
        }

        // The highest level among a recipe's ingredients at every level up to the quality. A recipe that
        // takes any one of its ingredients needs only the easiest of them at each level.
        private static int RecipeLevel(Recipe recipe, int quality) {
            int level = 0;
            if (recipe.m_resources == null) { return level; }
            for (int at = 1; at <= quality; at++) {
                int easiest = int.MaxValue;
                foreach (Piece.Requirement req in recipe.m_resources) {
                    if (req?.m_resItem == null || req.GetAmount(at) <= 0) { continue; }
                    int needed = Needed(Utils.GetPrefabName(req.m_resItem.gameObject), 1);
                    if (recipe.m_requireOnlyOneIngredient) {
                        easiest = Math.Min(easiest, needed);
                    } else {
                        level = Math.Max(level, needed);
                    }
                }
                if (recipe.m_requireOnlyOneIngredient && easiest != int.MaxValue) { level = Math.Max(level, easiest); }
            }
            return level;
        }

        // ---- Sealing ----

        private static float nextSweep;
        private static float lastMessageTime;
        private static string lastMessage;

        /// <summary>
        /// Whether the local player is turned away from something that needs this level, telling them which
        /// boss opens it.
        /// </summary>
        private static bool Sealed(Player player, int needed, bool setting) {
            if (!Active || !setting || player == null || player != Player.m_localPlayer || needed <= 0) { return false; }
            if (Level(player) >= needed) { return false; }
            Tell(player, needed);
            return true;
        }

        // One message per boss every few seconds: some checks run on every attempt of a held button.
        private static void Tell(Player player, int needed) {
            string message = Localization.instance.Localize("$al_lock_sealed", BossName(needed));
            if (message == lastMessage && Time.time - lastMessageTime < 3f) { return; }
            lastMessage = message;
            lastMessageTime = Time.time;
            player.Message(MessageHud.MessageType.Center, message);
        }

        /// <summary>
        /// Takes off sealed gear the player is wearing, as when Ascending takes their boss keys away or the
        /// server turns the lock on, since the seals are otherwise only checked when gear is put on.
        /// </summary>
        internal static void Tick(Player player) {
            if (Time.time < nextSweep) { return; }
            nextSweep = Time.time + 1f;
            if (!Active || !ValConfig.LockEquipment.Value) { return; }
            int level = -1;
            foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems()) {
                int needed = Needed(item, item.m_quality);
                if (needed <= 0) { continue; }
                if (level < 0) { level = Level(player); }
                if (needed <= level) { continue; }
                player.UnequipItem(item);
                Tell(player, needed);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class Equip {
            private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result) {
                if (item == null || !Sealed(__instance as Player, Needed(item, item.m_quality), ValConfig.LockEquipment.Value)) { return true; }
                __result = false;
                return false;
            }
        }

        // An attack takes its ammo from the inventory when none is equipped, so this checks what it would fire.
        [HarmonyPatch(typeof(Attack), nameof(Attack.HaveAmmo))]
        private static class Ammo {
            private static void Postfix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result) {
                if (!__result || weapon == null) { return; }
                ItemDrop.ItemData ammo = Attack.FindAmmo(character, weapon);
                if (ammo != null && Sealed(character as Player, Needed(ammo, ammo.m_quality), ValConfig.LockEquipment.Value)) { __result = false; }
            }
        }

        // Only when the player holds the key, so the game's own message still says when they don't.
        [HarmonyPatch(typeof(Door), nameof(Door.Interact))]
        private static class DoorKey {
            private static bool Prefix(Door __instance, Humanoid character, bool hold, ref bool __result) {
                if (hold || __instance.m_keyItem == null || !__instance.HaveKey(character)) { return true; }
                // The key is the item's prefab, whose own item data names no prefab.
                if (!Sealed(character as Player, Needed(Utils.GetPrefabName(__instance.m_keyItem.gameObject), 1), ValConfig.LockEquipment.Value)) { return true; }
                __instance.m_lockedEffects.Create(__instance.transform.position, __instance.transform.rotation);
                __result = true;
                return false;
            }
        }

        // Eating, drinking and consumable ammo such as bait all ask this first.
        [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem))]
        private static class Consume {
            private static bool Prefix(Player __instance, ItemDrop.ItemData item, ref bool __result) {
                if (item == null || !Sealed(__instance, Needed(item, item.m_quality), ValConfig.LockConsumables.Value)) { return true; }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class Craft {
            private static bool Prefix(InventoryGui __instance, Player player) {
                int quality = __instance.m_craftUpgradeItem == null ? 1 : __instance.m_craftUpgradeItem.m_quality + 1;
                return !Sealed(player, Needed(__instance.m_craftRecipe, quality), ValConfig.LockCrafting.Value);
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnUseItem))]
        private static class Cook {
            private static bool Prefix(Humanoid user, ItemDrop.ItemData item, ref bool __result) {
                if (item == null || !Sealed(user as Player, Needed(item, item.m_quality), ValConfig.LockCrafting.Value)) { return true; }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.AddItem))]
        private static class Ferment {
            private static bool Prefix(Humanoid user, ItemDrop.ItemData item, ref bool __result) {
                if (item == null || !Sealed(user as Player, Needed(item, item.m_quality), ValConfig.LockCrafting.Value)) { return true; }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static class Build {
            private static bool Prefix(Player __instance, Piece piece, ref bool __result) {
                if (!Sealed(__instance, Needed(piece), ValConfig.LockBuilding.Value)) { return true; }
                __result = false;
                return false;
            }
        }

        // Whether a setting that seals this kind of item is on: gear is worn, food is eaten, and anything
        // else is only crafted or built with.
        private static bool Applies(ItemDrop.ItemData item, bool crafting) {
            if (crafting) { return ValConfig.LockCrafting.Value; }
            if (item.IsEquipable()) { return ValConfig.LockEquipment.Value; }
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) { return ValConfig.LockConsumables.Value; }
            return ValConfig.LockCrafting.Value || ValConfig.LockBuilding.Value;
        }

        // Says on the item which boss opens it, in the inventory, chests and the crafting list. A tooltip
        // the game appends to another item's is left alone, so the line shows once.
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
            typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
        private static class Tooltip {
            private static void Postfix(ItemDrop.ItemData item, int qualityLevel, bool crafting, bool appending, ref string __result) {
                Player player = Player.m_localPlayer;
                if (!Active || appending || player == null || item == null || !Applies(item, crafting)) { return; }
                int needed = Needed(item, qualityLevel);
                if (needed <= 0 || Level(player) >= needed) { return; }
                __result += "\n\n<color=#FF6A4D>" + Localization.instance.Localize("$al_lock_tooltip", BossName(needed)) + "</color>";
            }
        }

        // ---- For admins ----

        /// <summary>Every item and build piece that is sealed, by the boss it needs, and those never sealed.</summary>
        internal static (SortedDictionary<int, List<string>> byBoss, List<string> open) Survey() {
            var byBoss = new SortedDictionary<int, List<string>>();
            var open = new List<string>();
            void Add(string name, int needed) {
                if (needed <= 0) { open.Add(name); return; }
                if (!byBoss.TryGetValue(needed, out List<string> list)) { byBoss[needed] = list = new List<string>(); }
                list.Add(name);
            }
            if (ObjectDB.instance != null) {
                foreach (GameObject prefab in ObjectDB.instance.m_items) {
                    if (prefab != null) { Add(prefab.name, Needed(prefab.name, 1)); }
                }
            }
            if (ZNetScene.instance != null) {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs) {
                    // Food that can be set down is a piece too, and already listed as an item.
                    Piece piece = prefab != null && prefab.GetComponent<ItemDrop>() == null ? prefab.GetComponent<Piece>() : null;
                    if (piece != null && piece.m_resources != null && piece.m_resources.Length > 0) { Add(prefab.name, Needed(piece)); }
                }
            }
            foreach (List<string> list in byBoss.Values) { list.Sort(StringComparer.OrdinalIgnoreCase); }
            open.Sort(StringComparer.OrdinalIgnoreCase);
            return (byBoss, open);
        }
    }
}
