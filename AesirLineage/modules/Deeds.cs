using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.powers;
using AesirLineage.souls;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// What players do in the world that the deities notice: a boss's death (for shrine quests), the first
    /// kill of each miniboss, every kill of an Eternal Hall herald of Ragnarok, and by luck, starred kills,
    /// crafts and harvests. Lucky points are capped per deity per run by <c>LuckyPointsPerDeity</c>.
    /// </summary>
    /// <remarks>
    /// A death is seen only on the creature's owner, and a tree or rock breaks only on its owner. The owner
    /// tells each player who took part by a routed RPC, and each player then checks it against their own
    /// save. For a kill, taking part means having hit it (the game marks every player who does on the
    /// creature's ZDO, which is how it gives each of them the kill) or, for a boss or miniboss, being within
    /// <c>SoulReclaimRange</c> of where it fell. For a tree or rock, it means landing the breaking hit.
    /// Picking and crafting happen on the player's own machine.
    /// </remarks>
    internal static class Deeds {
        private const string DeedRpc = "AL_Deed";
        private const string HarvestRpc = "AL_Harvest";
        // How long a kill told while the player is between bodies is kept for them.
        private const float AwaySeconds = 300f;

        // The bosses in the order the game opens its biomes, each opening the next one in BiomeOrder.
        internal static readonly string[] BossOrder = {
            "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon",
            "defeated_goblinking", "defeated_queen", "defeated_fader",
        };
        private static readonly Heightmap.Biome[] BiomeOrder = {
            Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
            Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth,
        };

        /// <summary>Set by <c>al-deeds chance</c> to try every lucky chance at this percent; below 0, the settings decide.</summary>
        internal static float ForcedChance = -1f;

        private static readonly List<Player> nearby = new List<Player>();
        private static readonly HashSet<long> tookPart = new HashSet<long>();
        private static readonly List<(string prefab, int level, Vector3 at, string bossKey, float time)> whileAway = new List<(string, int, Vector3, string, float)>();
        private static uint lastPickRolled;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<string, int, Vector3, string>(DeedRpc, OnDeed);
                ZRoutedRpc.instance?.Register<Vector3>(HarvestRpc, OnHarvest);
            }
        }

        // ---- Kills ----

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Death {
            private static void Prefix(Character __instance) {
                if (__instance == null || __instance is Player || ZRoutedRpc.instance == null || ZNet.instance == null) { return; }
                if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) { return; }
                string prefab = Utils.GetPrefabName(__instance.gameObject);
                string bossKey = SoulBosses.BossKey(__instance);
                bool boss = SoulBosses.TryGetDeity(bossKey, out _);
                bool great = boss || TryGetMiniboss(prefab, out _, out _) || TryGetHerald(prefab, out _);
                if (!great && !Starred(__instance)) { return; }
                Vector3 at = __instance.transform.position;
                float range = great ? ValConfig.SoulReclaimRange.Value : 0f;
                // A routed RPC to this machine's own id runs here, so the owner is told the same way.
                foreach (long peer in TookPart(__instance.m_nview.GetZDO(), at, range)) {
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer, DeedRpc, prefab, __instance.GetLevel(), at, boss ? bossKey : "");
                }
            }
        }

        // A kill that can bring luck: hostile, never tamable, not a boss, with enough stars.
        private static bool Starred(Character creature) {
            return creature.GetLevel() - 1 >= ValConfig.StarredKillMinStars.Value && !creature.IsBoss()
                && creature.IsMonsterFaction(Time.time) && creature.GetComponent<Tameable>() == null;
        }

        // The machines of the players who hit the creature, and of those within range of where it fell.
        private static HashSet<long> TookPart(ZDO creature, Vector3 at, float range) {
            tookPart.Clear();
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList()) {
                // The key Character.RPC_Damage writes for each player who hits the creature.
                if (creature.GetBool(ZDOVars.s_attackers + info.m_name)) { tookPart.Add(info.m_characterID.UserID); }
            }
            if (range > 0f) {
                nearby.Clear();
                Player.GetPlayersInRange(at, range, nearby);
                foreach (Player player in nearby) {
                    ZDO zdo = player?.m_nview?.GetZDO();
                    if (zdo != null) { tookPart.Add(zdo.GetOwner()); }
                }
            }
            // A routed RPC to 0 goes to everyone.
            tookPart.Remove(0L);
            return tookPart;
        }

        // On the machine of a player who took part in a kill. A player killed in the fight still counts:
        // while their body lies there the game saves it with what this writes. After that, until they
        // respawn, anything written would be lost when the new body loads, so the kill waits for them.
        private static void OnDeed(long sender, string prefab, int level, Vector3 at, string bossKey) {
            Player player = Player.m_localPlayer;
            if (player == null) {
                whileAway.Add((prefab, level, at, bossKey, Time.time));
                return;
            }
            Counted(player, prefab, level, at, bossKey);
        }

        /// <summary>Counts the kills told while the player was away, once they are back.</summary>
        internal static void Tick(Player player) {
            if (whileAway.Count == 0) { return; }
            var kills = whileAway.ToArray();
            whileAway.Clear();
            foreach ((string prefab, int level, Vector3 at, string bossKey, float time) in kills) {
                if (Time.time - time < AwaySeconds) { Counted(player, prefab, level, at, bossKey); }
            }
        }

        private static void Counted(Player player, string prefab, int level, Vector3 at, string bossKey) {
            if (!string.IsNullOrEmpty(bossKey)) {
                ProgressionLock.OnBossKilled(player, bossKey);
                Quests.OnBossKilled(player, bossKey);
            } else if (TryGetHerald(prefab, out Deity heraldDeity)) {
                HeraldKilled(player, prefab, heraldDeity);
            } else if (TryGetMiniboss(prefab, out string entry, out Deity deity)) {
                MinibossKilled(player, prefab, entry, deity);
            } else {
                StarredKill(player, level, at);
            }
        }

        // A herald of Ragnarok (Eternal Hall). There is a new one each time, so every kill counts.
        private static void HeraldKilled(Player player, string prefab, Deity deity) {
            int points = ValConfig.RagnarokHeraldPoints.Value;
            if (points <= 0) { return; }
            PlayerData.AddPoints(deity, points);
            DeityVfx.Share(deity, VfxRole.Burst, player.transform.position, 1f);
            Character creature = ZNetScene.instance?.GetPrefab(prefab)?.GetComponent<Character>();
            string name = creature != null ? Localization.instance.Localize(creature.m_name) : prefab;
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_deed_herald", Compendium.DeityName(deity), name, points.ToString()));
        }

        private static void MinibossKilled(Player player, string prefab, string entry, Deity deity) {
            List<string> killed = PlayerData.Save.Run.MinibossesKilled;
            if (killed.Contains(entry)) { return; }
            killed.Add(entry);
            int points = ValConfig.MinibossPoints.Value;
            PlayerData.AddPoints(deity, points);
            DeityVfx.Share(deity, VfxRole.Burst, player.transform.position, 1f);
            Character creature = ZNetScene.instance?.GetPrefab(prefab)?.GetComponent<Character>();
            string name = creature != null ? Localization.instance.Localize(creature.m_name) : prefab;
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_deed_miniboss", Compendium.DeityName(deity), name, points.ToString()));
        }

        private static void StarredKill(Player player, int level, Vector3 at) {
            if (level - 1 < ValConfig.StarredKillMinStars.Value) { return; }
            Heightmap.Biome biome = BiomeAt(at);
            if (!TryGetBiomeDeity(biome, out Deity deity)) { return; }
            // A biome off the path, such as the ocean, counts as level with the player.
            int tier = Array.IndexOf(BiomeOrder, biome);
            if (tier >= 0 && tier < BiomeLevel(player)) { return; }
            TryLuck(player, deity, ValConfig.StarredKillChance.Value, "$al_deed_hunt");
        }

        // ---- Crafting ----

        // Only the end of InventoryGui.DoCrafting counts a craft or upgrade, so this is one per craft. A
        // craft the game marks as cheated brings no luck.
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.IncrementStat))]
        private static class Crafted {
            private static void Postfix(PlayerStatType stat, bool cheated) {
                Player player = Player.m_localPlayer;
                if (stat != PlayerStatType.CraftsOrUpgrades || cheated || player == null) { return; }
                List<Deity> deities = DiscoveredDeities(player);
                if (deities.Count > 0) { TryLuck(player, deities[UnityEngine.Random.Range(0, deities.Count)], ValConfig.CraftChance.Value, "$al_deed_craft"); }
            }
        }

        // The deities whose biome the player has discovered and that can still give lucky points this run.
        private static List<Deity> DiscoveredDeities(Player player) {
            List<Deity> deities = new List<Deity>();
            foreach (KeyValuePair<Heightmap.Biome, Deity> entry in BiomeDeities()) {
                if (deities.Contains(entry.Value) || LuckyPoints(entry.Value) >= ValConfig.LuckyPointsPerDeity.Value) { continue; }
                if (player.m_knownBiome.Contains(BiomeSector.GetBiomeName(entry.Key))) { deities.Add(entry.Value); }
            }
            return deities;
        }

        // ---- Harvests ----

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class Picked {
            private static void Prefix(Pickable __instance, Humanoid character) {
                Player player = Player.m_localPlayer;
                if (character != player || player == null || __instance.m_picked || __instance.m_itemPrefab == null) { return; }
                if (GatheringPowers.NotHarvests.Contains(__instance.m_itemPrefab.name)) { return; }
                ZDO zdo = __instance.m_nview?.GetZDO();
                // One roll per plant, however many times Interact fires on it.
                if (zdo == null || zdo.m_uid.ID == lastPickRolled) { return; }
                lastPickRolled = zdo.m_uid.ID;
                Harvested(player, __instance.transform.position);
            }
        }

        // A whole tree, rock or deposit counts once, when it is felled or its last piece breaks, not for
        // each log or piece. Stumps and other breakables don't count.
        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
        private static class TreeFelled {
            private static void Prefix(TreeBase __instance, out bool __state) {
                ZNetView nview = __instance.m_nview;
                __state = nview != null && nview.IsValid() && nview.IsOwner() && nview.GetZDO().GetFloat(ZDOVars.s_health, __instance.m_health) > 0f;
            }

            private static void Postfix(TreeBase __instance, HitData hit, bool __state) {
                if (__state && !__instance.m_nview.IsValid()) { Broken(hit, __instance.transform.position); }
            }
        }

        [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
        private static class RockBroken {
            private static void Prefix(MineRock __instance, out bool __state) {
                ZNetView nview = __instance.m_nview;
                __state = nview != null && nview.IsValid() && nview.IsOwner() && !__instance.AllDestroyed();
            }

            // A rock that is removed when its last piece breaks has no ZDO left to ask.
            private static void Postfix(MineRock __instance, HitData hit, bool __state) {
                if (__state && (!__instance.m_nview.IsValid() || __instance.AllDestroyed())) { Broken(hit, __instance.transform.position); }
            }
        }

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
        private static class DepositBroken {
            private static void Prefix(MineRock5 __instance, out bool __state) {
                __state = !__instance.m_allDestroyed;
            }

            private static void Postfix(MineRock5 __instance, HitData hit, bool __state) {
                if (__state && __instance.m_allDestroyed) { Broken(hit, __instance.transform.position); }
            }
        }

        // On the owner of what broke: tells whoever landed the breaking hit, if it was a player.
        private static void Broken(HitData hit, Vector3 at) {
            if (ZRoutedRpc.instance == null || !(hit?.GetAttacker() is Player player)) { return; }
            long peer = player.m_nview?.GetZDO()?.GetOwner() ?? 0L;
            if (peer != 0L) { ZRoutedRpc.instance.InvokeRoutedRPC(peer, HarvestRpc, at); }
        }

        private static void OnHarvest(long sender, Vector3 at) {
            Player player = Player.m_localPlayer;
            if (player != null) { Harvested(player, at); }
        }

        private static void Harvested(Player player, Vector3 at) {
            if (TryGetBiomeDeity(BiomeAt(at), out Deity deity)) { TryLuck(player, deity, ValConfig.HarvestChance.Value, "$al_deed_harvest"); }
        }

        // ---- Luck ----

        internal static int LuckyPoints(Deity deity) => PlayerData.Save.Run.LuckyPoints.TryGetValue(deity, out int points) ? points : 0;

        // A chance at one of the deity's lucky points, until the run's limit for it is reached.
        private static void TryLuck(Player player, Deity deity, float chance, string message) {
            int limit = ValConfig.LuckyPointsPerDeity.Value;
            if (limit <= 0 || LuckyPoints(deity) >= limit) { return; }
            if (ForcedChance >= 0f) { chance = ForcedChance; }
            if (UnityEngine.Random.value * 100f >= chance) { return; }
            PlayerData.Save.Run.LuckyPoints[deity] = LuckyPoints(deity) + 1;
            PlayerData.AddPoints(deity, 1);
            DeityVfx.Share(deity, VfxRole.Burst, player.transform.position, 0.7f);
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(message, Compendium.DeityName(deity)));
        }

        // ---- Biomes ----

        /// <summary>
        /// The player's biome level, as an index into the biomes in the order the game opens them: the
        /// biome after the last boss they have beaten. A boss's key counts as the world's or the player's
        /// own, as the shrine reads keys. Ascension takes the keys, so a new run starts in the Meadows.
        /// </summary>
        internal static int BiomeLevel(Player player) => BiomeLevel(player, worldKeys: true);

        /// <summary>The same, counting the world's keys only when asked; otherwise only the player's own.</summary>
        internal static int BiomeLevel(Player player, bool worldKeys) {
            for (int i = BossOrder.Length - 1; i >= 0; i--) {
                string key = BossOrder[i];
                if ((worldKeys && ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key)) || player.PlayerHasUniqueKey(key)) { return i + 1; }
            }
            return 0;
        }

        internal static Heightmap.Biome BiomeAtLevel(int level) => BiomeOrder[Mathf.Clamp(level, 0, BiomeOrder.Length - 1)];

        // A dungeon sits high above the world, so this is the biome on the ground below it.
        private static Heightmap.Biome BiomeAt(Vector3 at) => WorldGenerator.instance != null ? WorldGenerator.instance.GetBiome(at) : Heightmap.Biome.None;

        // ---- Settings ----

        private static readonly Dictionary<Heightmap.Biome, Deity> biomeDeities = new Dictionary<Heightmap.Biome, Deity>();
        private static string biomesFrom;
        // Prefab name to the entry it belongs to, so the prefabs of one "a+b=Deity" entry count as one miniboss.
        private static readonly Dictionary<string, (string entry, Deity deity)> minibosses = new Dictionary<string, (string, Deity)>(StringComparer.OrdinalIgnoreCase);
        private static string minibossesFrom;
        private static Dictionary<string, Deity> heralds = new Dictionary<string, Deity>();
        private static string heraldsFrom;

        private static Dictionary<Heightmap.Biome, Deity> BiomeDeities() {
            string raw = ValConfig.BiomeDeities.Value;
            if (raw == biomesFrom) { return biomeDeities; }
            biomeDeities.Clear();
            foreach (KeyValuePair<string, Deity> pair in ParsePairs(raw, "BiomeDeities")) {
                if (Enum.TryParse(pair.Key, ignoreCase: true, out Heightmap.Biome biome) && biome != Heightmap.Biome.None) {
                    biomeDeities[biome] = pair.Value;
                } else {
                    Logger.LogWarning($"BiomeDeities: '{pair.Key}' is not a biome.");
                }
            }
            biomesFrom = raw;
            return biomeDeities;
        }

        internal static bool TryGetBiomeDeity(Heightmap.Biome biome, out Deity deity) => BiomeDeities().TryGetValue(biome, out deity);

        internal static bool TryGetHerald(string prefab, out Deity deity) {
            string raw = ValConfig.RagnarokHeralds.Value;
            if (raw != heraldsFrom) {
                heralds = ParsePairs(raw, "RagnarokHeralds");
                heraldsFrom = raw;
            }
            return heralds.TryGetValue(prefab, out deity);
        }

        internal static bool TryGetMiniboss(string prefab, out string entry, out Deity deity) {
            string raw = ValConfig.Minibosses.Value;
            if (raw != minibossesFrom) {
                minibosses.Clear();
                foreach (KeyValuePair<string, Deity> pair in ParsePairs(raw, "Minibosses")) {
                    foreach (string name in pair.Key.Split('+')) {
                        if (!string.IsNullOrWhiteSpace(name)) { minibosses[name.Trim()] = (pair.Key, pair.Value); }
                    }
                }
                minibossesFrom = raw;
            }
            bool found = minibosses.TryGetValue(prefab, out (string entry, Deity deity) miniboss);
            entry = miniboss.entry;
            deity = miniboss.deity;
            return found;
        }
    }
}
