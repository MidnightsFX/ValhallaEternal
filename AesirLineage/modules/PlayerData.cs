using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using AesirLineage.common;
using AesirLineage.powers;
using AesirLineage.souls;
using AesirLineage.starmap;
using static AesirLineage.common.DataObjects;
using static AesirLineage.common.Deities;
using AesirLineage.visuals;

namespace AesirLineage.modules {
    /// <summary>
    /// The local player's lineage: points and allocated nodes per deity, cosmetics, prestige level.
    /// Every change goes through here so the save, the <see cref="PowerSet"/> and the ZDO stay in step.
    /// </summary>
    public static class PlayerData {
        internal static LineageSave Save = new LineageSave();

        // Allocated nodes that resolve against the current maps and connect back to the root.
        // The save can list more (nodes from another server's maps); those are kept until the player
        // changes that deity's allocation, and cost nothing meanwhile.
        private static readonly Dictionary<Deity, HashSet<StarNode>> effective = new Dictionary<Deity, HashSet<StarNode>>();

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        public static class LoadLineage {
            // The character whose powers last ran. A respawn loads the same one again.
            private static long lastCharacter;

            // Load equips the character's gear before the postfix runs; clear the last character's
            // powers first so their oaths can't refuse this one's weapons.
            static void Prefix(Player __instance) {
                if (__instance != Player.m_localPlayer) { return; }
                PowerSet.Rebuild(Array.Empty<StarNode>());
                // The profile sets the id before loading, so it already names this character.
                long character = __instance.GetPlayerID();
                powers.PowerTicker.ResetAll(newCharacter: character != lastCharacter);
                lastCharacter = character;
            }

            static void Postfix(Player __instance) {
                // Player.Load also runs for the main menu's preview character; only the spawned one counts.
                if (__instance != Player.m_localPlayer) { return; }
                Load(__instance);
            }
        }

        /// <summary>Ascensions across every deity, shown next to the player's name.</summary>
        public static int PrestigeLevel => Save.PrestigeLevel;

        /// <summary>Goes up whenever points, allocation or cosmetics change, so open screens know to redraw.</summary>
        internal static int Revision { get; private set; }

        internal static void Load(Player player) {
            Save = new LineageSave();
            if (player.m_customData.TryGetValue(CustomDataKey, out string packed)) {
                try {
                    Save = saveDeserializer.Deserialize<LineageSave>(packed) ?? new LineageSave();
                } catch (Exception e) {
                    // Keep the unreadable text next to it rather than lose it on the next save.
                    Logger.LogError($"Could not read this character's Aesir Lineage data, starting fresh. The old data is kept as {CustomDataKey}_unreadable. {e.Message}");
                    player.m_customData[CustomDataKey + "_unreadable"] = packed;
                    Save = new LineageSave();
                }
            }
            Normalize();
            Recompute();

            SkillPowers.ForgetSkillLevels();

            PrestigeDisplays.UpdateLocalPlayerLevelDisplay(Save.PrestigeLevel);
            Publish(player);
        }

        /// <summary>
        /// Writes what other clients need to know to the player's ZDO, only where it changed: the
        /// Ascension total (shown by their name) and the souls claimed this run, which a boss's owner reads
        /// to decide whether anyone nearby is owed its soul.
        /// </summary>
        private static void Publish(Player player) {
            ZDO zdo = player?.m_nview?.GetZDO();
            if (zdo == null) { return; }
            if (zdo.GetInt(CustomLevelZKey, 0) != Save.PrestigeLevel) { zdo.Set(CustomLevelZKey, Save.PrestigeLevel); }
            string claims = string.Join(",", ClaimedSouls(player));
            if (zdo.GetString(ClaimsZKey, "") != claims) { zdo.Set(ClaimsZKey, claims); }
        }

        // The boss keys whose soul the player has claimed, from their al_claimed_* player keys.
        private static IEnumerable<string> ClaimedSouls(Player player) {
            return player.GetUniqueKeys().Where(key => key.StartsWith(SoulBosses.ClaimPrefix)).Select(key => key.Substring(SoulBosses.ClaimPrefix.Length));
        }

        // Admins can change claims with the game's own player key commands (setkeyplayer, removekeyplayer,
        // resetkeys), so the published list follows the keys whenever they change.
        [HarmonyPatch(typeof(Player), nameof(Player.AddUniqueKey))]
        private static class ClaimAdded {
            private static void Postfix(Player __instance, string name) {
                if (__instance == Player.m_localPlayer && name != null && name.StartsWith(SoulBosses.ClaimPrefix)) { Publish(__instance); }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.RemoveUniqueKey))]
        private static class ClaimRemoved {
            private static void Postfix(Player __instance, string name) {
                if (__instance == Player.m_localPlayer && name != null && name.StartsWith(SoulBosses.ClaimPrefix)) { Publish(__instance); }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ResetUniqueKeys))]
        private static class ClaimsReset {
            private static void Postfix(Player __instance) {
                if (__instance == Player.m_localPlayer) { Publish(__instance); }
            }
        }

        private static void Normalize() {
            Save.Deities ??= new Dictionary<Deity, DeityProgress>();
            foreach (DeityProgress progress in Save.Deities.Values) {
                if (progress == null) { continue; }
                progress.Allocated ??= new List<string>();
            }
            Save.Boons ??= new List<ActiveBoon>();
            Save.Boons.RemoveAll(boon => boon == null || string.IsNullOrEmpty(boon.Id) || boon.SecondsLeft <= 0f);
            Save.Run ??= new RunState();
            Save.Run.QuestsDone ??= new Dictionary<Deity, int>();
            Save.Run.QuestsUnderWay ??= new Dictionary<Deity, string>();
            Save.Run.QuestTaken ??= new Dictionary<Deity, DayStamp>();
            Save.Run.LastBlessing ??= new Dictionary<Deity, DayStamp>();
            Save.Run.LuckyPoints ??= new Dictionary<Deity, int>();
            Save.Run.MinibossesKilled ??= new List<string>();
            Save.Cosmetics ??= new Dictionary<CosmeticSlot, string>();
            Save.UnlockedCosmetics ??= new List<string>();
            // The prestige number is the Ascensions; Valhalla Eternal's devotions counted on their own.
            Save.PrestigeLevel = TotalAscension();
        }

        internal static DeityProgress Progress(Deity deity) {
            if (!Save.Deities.TryGetValue(deity, out DeityProgress progress) || progress == null) {
                progress = new DeityProgress();
                Save.Deities[deity] = progress;
            }
            return progress;
        }

        // ---- Points and allocation ----

        public static int PointsEarned(Deity deity) => Save.Deities.TryGetValue(deity, out DeityProgress p) && p != null ? p.Points : 0;

        public static int PointsSpent(Deity deity) => effective.TryGetValue(deity, out HashSet<StarNode> taken) ? taken.Sum(n => n.Cost) : 0;

        public static int PointsAvailable(Deity deity) => PointsEarned(deity) - PointsSpent(deity);

        internal static bool IsAllocated(StarNode node) => effective.TryGetValue(node.Deity, out HashSet<StarNode> taken) && taken.Contains(node);

        internal static IReadOnlyCollection<StarNode> Allocated(Deity deity) => effective.TryGetValue(deity, out HashSet<StarNode> taken) ? taken : (IReadOnlyCollection<StarNode>)Array.Empty<StarNode>();

        /// <summary>Every node that counts, across all deities.</summary>
        internal static IEnumerable<StarNode> AllAllocated() => effective.Values.SelectMany(taken => taken);

        public static void AddPoints(Deity deity, int amount) {
            DeityProgress progress = Progress(deity);
            progress.Points = Math.Max(0, progress.Points + amount);
            SavePlayerConfiguration();
            Compendium.UpdateDietyPrestigeExplanations();
        }

        /// <summary>
        /// Takes points away, down to none. When what is left no longer pays for the deity's stars, its star
        /// map is reset so nothing is kept unpaid for. Returns whether it was.
        /// </summary>
        internal static bool TakePoints(Deity deity, int amount) {
            AddPoints(deity, -amount);
            if (PointsAvailable(deity) >= 0) { return false; }
            ResetDeity(deity);
            return true;
        }

        internal static bool CanAllocate(StarNode node, out string reason) {
            reason = null;
            if (node.Tier == NodeTier.Root) { reason = "$al_reason_root"; return false; }
            if (IsAllocated(node)) { reason = "$al_reason_taken"; return false; }
            if (node.Definition.Ascension > Progress(node.Deity).Ascension) {
                reason = Localization.instance.Localize("$al_reason_ascension", node.Definition.Ascension.ToString());
                return false;
            }
            if (!StarMaps.TryGet(node.Deity, out StarMap map) || !node.Neighbours.Any(n => n == map.Root || IsAllocated(n))) {
                reason = "$al_reason_unlinked";
                return false;
            }
            int available = PointsAvailable(node.Deity);
            if (available < node.Cost) {
                reason = Localization.instance.Localize("$al_reason_points", node.Cost.ToString(), available.ToString());
                return false;
            }
            return true;
        }

        internal static bool Allocate(StarNode node, out string reason) {
            if (!CanAllocate(node, out reason)) { return false; }
            WriteAllocation(node.Deity, Allocated(node.Deity).Append(node));
            return true;
        }

        /// <summary>A node can be refunded while everything else taken still links back to the root without it.</summary>
        internal static bool CanRefund(StarNode node, out string reason) {
            reason = null;
            if (!IsAllocated(node)) { reason = "$al_reason_not_taken"; return false; }
            if (!StarMaps.TryGet(node.Deity, out StarMap map)) { reason = "$al_reason_unlinked"; return false; }
            HashSet<StarNode> rest = new HashSet<StarNode>(Allocated(node.Deity));
            rest.Remove(node);
            if (Reachable(map, rest).Count != rest.Count) { reason = "$al_reason_strands"; return false; }
            return true;
        }

        internal static bool Refund(StarNode node, out string reason) {
            if (!CanRefund(node, out reason)) { return false; }
            WriteAllocation(node.Deity, Allocated(node.Deity).Where(n => n != node));
            return true;
        }

        internal static void ResetDeity(Deity deity) {
            WriteAllocation(deity, Enumerable.Empty<StarNode>());
        }

        internal static bool HasClaimedSoul(string bossKey) => Player.m_localPlayer != null && Player.m_localPlayer.HaveUniqueKey(SoulBosses.ClaimKey(bossKey));

        // ---- This run ----

        internal static int QuestsDone(Deity deity) => Save.Run.QuestsDone.TryGetValue(deity, out int done) ? done : 0;

        /// <summary>Records one of the deity's shrine quests as done this run. The first opens its blessing.</summary>
        internal static void CompleteQuest(Deity deity) {
            Save.Run.QuestsDone[deity] = QuestsDone(deity) + 1;
            SavePlayerConfiguration();
        }

        /// <summary>Today in the world the character is in, or null outside one.</summary>
        internal static DayStamp Today() {
            if (ZNet.instance == null || EnvMan.instance == null) { return null; }
            return new DayStamp() { World = ZNet.instance.GetWorldUID(), Day = EnvMan.instance.GetDay() };
        }

        /// <summary>Whether a day is today in this world. A day from another world never is.</summary>
        internal static bool IsToday(DayStamp stamp) {
            DayStamp today = Today();
            return stamp != null && today != null && stamp.World == today.World && stamp.Day == today.Day;
        }

        internal static bool BlessedToday(Deity deity) => Save.Run.LastBlessing.TryGetValue(deity, out DayStamp stamp) && IsToday(stamp);

        internal static void RecordBlessing(Deity deity) {
            Save.Run.LastBlessing[deity] = Today();
            SavePlayerConfiguration();
        }

        // ---- Ascension ----

        internal static int AscensionLevel(Deity deity) => Save.Deities.TryGetValue(deity, out DeityProgress p) && p != null ? p.Ascension : 0;

        internal static int TotalAscension() => Save.Deities.Values.Where(p => p != null).Sum(p => p.Ascension);

        /// <summary>
        /// Sets a deity's Ascension level. Apex stars above it stop counting, kept in the save until the
        /// level is reached again.
        /// </summary>
        internal static void SetAscension(Deity deity, int level) {
            Progress(deity).Ascension = Math.Max(0, level);
            Save.PrestigeLevel = TotalAscension();
            SavePlayerConfiguration();
            Recompute();
        }

        /// <summary>
        /// Ascension starts a new run: the soul claim keys go, so every boss's soul can be claimed again,
        /// and the run's quests, blessings, lucky points and minibosses start over.
        /// </summary>
        internal static void StartNewRun() {
            Player player = Player.m_localPlayer;
            if (player != null) {
                foreach (string claim in player.GetUniqueKeys().Where(key => key.StartsWith(SoulBosses.ClaimPrefix)).ToList()) {
                    player.RemoveUniqueKey(claim);
                }
            }
            Save.Run = new RunState();
            SavePlayerConfiguration();
        }

        /// <summary>
        /// Takes a Soul of Power's favour: the player gets the boss's claim key and its deity grants points.
        /// </summary>
        internal static void ClaimSoul(string bossKey, Deity deity, int points) {
            Player player = Player.m_localPlayer;
            if (player == null || HasClaimedSoul(bossKey)) { return; }
            player.AddUniqueKey(SoulBosses.ClaimKey(bossKey));
            AddPoints(deity, points);
        }

        private static void WriteAllocation(Deity deity, IEnumerable<StarNode> nodes) {
            Progress(deity).Allocated = nodes.Select(n => n.Id).ToList();
            SavePlayerConfiguration();
            Recompute();
        }

        /// <summary>Called when the maps or the shrines (with the boons they lend) are replaced, by config sync or a file edit.</summary>
        internal static void OnDataChanged() {
            if (Player.m_localPlayer == null) { return; }
            Recompute();
        }

        /// <summary>Works out which saved nodes count, rebuilds the powers with the active boons, and applies what follows from them.</summary>
        internal static void Recompute() {
            effective.Clear();
            List<StarNode> all = new List<StarNode>();
            foreach (StarMap map in StarMaps.All) {
                HashSet<StarNode> taken = EffectiveAllocation(map);
                effective[map.Deity] = taken;
                all.AddRange(taken);
            }
            PowerSet.Rebuild(all, Boons.Grants());
            Revision++;
            WeaponOaths.EnforceOnLocalPlayer();
            Compendium.UpdateDietyPrestigeExplanations();
        }

        private static HashSet<StarNode> EffectiveAllocation(StarMap map) {
            HashSet<StarNode> listed = new HashSet<StarNode>();
            if (Save.Deities.TryGetValue(map.Deity, out DeityProgress progress) && progress != null) {
                foreach (string id in progress.Allocated) {
                    if (map.Nodes.TryGetValue(id, out StarNode node) && node != map.Root && node.Definition.Ascension <= progress.Ascension) {
                        listed.Add(node);
                    }
                }
            }
            return Reachable(map, listed);
        }

        /// <summary>The nodes of <paramref name="taken"/> that link back to the root through taken nodes.</summary>
        private static HashSet<StarNode> Reachable(StarMap map, HashSet<StarNode> taken) {
            HashSet<StarNode> reached = new HashSet<StarNode>();
            Queue<StarNode> open = new Queue<StarNode>();
            open.Enqueue(map.Root);
            while (open.Count > 0) {
                foreach (StarNode next in open.Dequeue().Neighbours) {
                    if (taken.Contains(next) && reached.Add(next)) { open.Enqueue(next); }
                }
            }
            return reached;
        }

        // ---- Cosmetics ----

        /// <summary>The look chosen for a slot, "None", or null when the player has not chosen.</summary>
        internal static string CosmeticChoice(CosmeticSlot slot) => Save.Cosmetics.TryGetValue(slot, out string choice) ? choice : null;

        internal static void ChooseCosmetic(CosmeticSlot slot, string choice) {
            Save.Cosmetics[slot] = choice;
            SavePlayerConfiguration();
        }

        /// <summary>Unlocks a deity's look for good. Returns false when it already was.</summary>
        internal static bool UnlockCosmetic(CosmeticSlot slot, Deity deity) {
            string key = Cosmetics.UnlockKey(slot, deity);
            if (Save.UnlockedCosmetics.Contains(key)) { return false; }
            Save.UnlockedCosmetics.Add(key);
            return true;
        }

        // ---- Persistence ----

        public static void SavePlayerConfiguration() {
            if (Player.m_localPlayer == null) {
                Logger.LogWarning("Cannot save player configuration, local player is null.");
                return;
            }
            Revision++;
            Player.m_localPlayer.m_customData[CustomDataKey] = yamlserializerJsonCompat.Serialize(Save);
            Publish(Player.m_localPlayer);
        }
    }
}
