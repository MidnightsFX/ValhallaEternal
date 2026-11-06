using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;
using static AesirLineage.common.Deities;

namespace AesirLineage.souls {
    /// <summary>
    /// Which boss leaves which deity's soul, and raising it: whenever the boss dies with a player nearby
    /// who hasn't claimed its soul this run.
    /// </summary>
    internal static class SoulBosses {
        private static Dictionary<string, Deity> bosses = new Dictionary<string, Deity>();
        private static string parsedFrom;

        internal static bool TryGetDeity(string bossKey, out Deity deity) {
            return Bosses().TryGetValue(bossKey, out deity);
        }

        /// <summary>The first boss listed for a deity, which its shrine's quest asks for; null when none is.</summary>
        internal static string BossFor(Deity deity) {
            foreach (KeyValuePair<string, Deity> boss in Bosses()) {
                if (boss.Value == deity) { return boss.Key; }
            }
            return null;
        }

        private static Dictionary<string, Deity> Bosses() {
            string raw = ValConfig.SoulBosses.Value;
            if (raw != parsedFrom) {
                bosses = Deities.ParsePairs(raw, "SoulBosses");
                parsedFrom = raw;
            }
            return bosses;
        }

        private static readonly List<Player> nearby = new List<Player>();

        /// <summary>
        /// The start of the player keys that record a claimed soul, one per boss, such as
        /// al_claimed_defeated_eikthyr. The game saves them with the character, and Ascension removes them.
        /// </summary>
        internal const string ClaimPrefix = "al_claimed_";

        internal static string ClaimKey(string bossKey) => ClaimPrefix + bossKey;

        /// <summary>
        /// Whether a player near the boss hasn't claimed its soul this run. A player's keys stay on their
        /// own machine, so each player publishes their claims on their ZDO for the boss's owner to read.
        /// </summary>
        internal static bool PlayerOwed(string bossKey, Vector3 position) {
            nearby.Clear();
            Player.GetPlayersInRange(position, ValConfig.SoulReclaimRange.Value, nearby);
            foreach (Player player in nearby) {
                ZDO zdo = player?.m_nview?.GetZDO();
                if (zdo == null) { continue; }
                if (!zdo.GetString(DataObjects.ClaimsZKey, "").Split(',').Contains(bossKey, StringComparer.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// What a boss is known by: the global key it sets when it dies, or for one that sets none, its
        /// prefab name. The Deep North's Kall Fimbulbringer sets no key, so his last stage, FrozenKing_p3,
        /// is listed by name.
        /// </summary>
        internal static string BossKey(Character boss) =>
            string.IsNullOrEmpty(boss.m_defeatSetGlobalKey) ? Utils.GetPrefabName(boss.gameObject) : boss.m_defeatSetGlobalKey;

        /// <summary>
        /// Runs where the boss is owned. A mapped boss raises its soul above where it fell when a player
        /// within <c>SoulReclaimRange</c> hasn't claimed it this run. When every player there has, none rises.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class BossDeath {
            private static void Prefix(Character __instance) {
                if (__instance == null || __instance is Player) { return; }
                if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) { return; }
                string bossKey = BossKey(__instance);
                if (!TryGetDeity(bossKey, out Deity deity) || !PlayerOwed(bossKey, __instance.transform.position)) { return; }
                SoulOfPower.Spawn(deity, bossKey, __instance.transform.position + Vector3.up * SoulOfPower.Height);
            }
        }
    }
}
