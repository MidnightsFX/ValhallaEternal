using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Freyja's Bond: the nearest player, or failing that a tame, is the bonded one. Both are quicker
    /// near each other; the bond takes some of their hurt, passes on healing, and grieves when one falls.
    /// A bonded player learns of it from the bonder's ZDO; what moves damage between them is a routed RPC.
    /// </summary>
    internal static class BondPowers {
        private const float BondRange = 15f;
        private const float CloseRange = 10f;
        private const float InseparableGuard = 10f;
        private const float LongingRange = 15f;
        private const float LongingSeconds = 20f;
        private const float LongingCooldown = 30f;
        private const float LongingBelowHealth = 0.2f;
        private const string BurdenRpc = "AL_Burden";

        private static Player bondedPlayer;
        private static Character bondedTame;
        private static float nextChoice;
        private static float longingUntil;
        private static float longingReadyAt;
        private static readonly Dictionary<ZDOID, bool> wasLow = new Dictionary<ZDOID, bool>();

        internal static void Reset() {
            bondedPlayer = null;
            bondedTame = null;
            longingUntil = 0f;
            longingReadyAt = 0f;
            wasLow.Clear();
        }

        /// <summary>The bonded one, player or tame, if one is near.</summary>
        internal static Character Bonded => bondedPlayer != null ? (Character)bondedPlayer : bondedTame;

        internal static ZDOID BondedPlayerId() => bondedPlayer != null ? bondedPlayer.GetZDOID() : ZDOID.None;

        internal static bool IsBondedTame(Character tame) => tame != null && tame == bondedTame;

        /// <summary>The Bonded speed and stamina bonus the local player has: their own, or one bonded to them.</summary>
        internal static float BondBonus(Player player) {
            float bonus = Bonded != null ? PowerSet.Get(Power.Bonded) : 0f;
            foreach (Player ally in BondersOf(player)) {
                bonus = Mathf.Max(bonus, ally.m_nview.GetZDO().GetFloat(AllyPowers.BondSpeedKey, 0f));
            }
            return bonus;
        }

        // Players nearby who have bonded themselves to this one.
        private static List<Player> BondersOf(Player player) {
            List<Player> bonders = new List<Player>();
            ZDOID self = player.GetZDOID();
            foreach (Player ally in PowerFx.AlliesNear(player.transform.position, BondRange)) {
                if (ally.m_nview?.GetZDO()?.GetZDOID(AllyPowers.BondKey) == self) { bonders.Add(ally); }
            }
            return bonders;
        }

        /// <summary>Devotion, Inseparable and the oath against being alone: the damage factor from the bond.</summary>
        internal static float DamageFactor(Player player) {
            Character bonded = Bonded;
            bool close = bonded != null && Vector3.Distance(bonded.transform.position, player.transform.position) < CloseRange;
            float factor = 1f;
            if (bonded != null) { factor *= 1f + PowerSet.Get(Power.BondDamage) / 100f; }
            if (close) { factor *= 1f + PowerSet.Get(Power.Inseparable) / 100f; }
            if (bonded == null) { factor *= Mathf.Max(0.2f, 1f - PowerSet.Get(Power.AloneDamagePenalty) / 100f); }
            if (Time.time < longingUntil) { factor *= 1f + PowerSet.Get(Power.Longing) / 100f; }
            return factor;
        }

        /// <summary>Inseparable: less damage taken close to the bonded one.</summary>
        internal static float GuardFactor(Player player) {
            Character bonded = Bonded;
            if (bonded == null || !PowerSet.Has(Power.Inseparable)) { return 1f; }
            return Vector3.Distance(bonded.transform.position, player.transform.position) < CloseRange ? 1f - InseparableGuard / 100f : 1f;
        }

        /// <summary>Longing for Óðr: life leech while grieving, in percent.</summary>
        internal static float LongingLeech() => Time.time < longingUntil ? PowerSet.Get(Power.Longing) / 8f : 0f;

        internal static float BondRegen() => Bonded != null ? PowerSet.Get(Power.BondRegen) : 0f;

        internal static void Tick(Player player) {
            if (Time.time >= nextChoice) {
                nextChoice = Time.time + 1f;
                ChooseBonded(player);
            }
            if (PowerSet.Has(Power.Longing)) { WatchForFalls(player); }
        }

        private static void ChooseBonded(Player player) {
            bondedPlayer = null;
            bondedTame = null;
            // The oath against being alone is lifted by a bond, so it forms one even without Bonded; a star
            // map can reach the oath through a neighbouring path.
            if (!PowerSet.Has(Power.Bonded) && !PowerSet.Has(Power.AloneDamagePenalty)) { return; }
            float best = BondRange;
            foreach (Player ally in PowerFx.AlliesNear(player.transform.position, BondRange)) {
                float distance = Vector3.Distance(ally.transform.position, player.transform.position);
                if (distance < best) { best = distance; bondedPlayer = ally; }
            }
            if (bondedPlayer != null) { return; }
            best = BondRange;
            foreach (Character tame in AllyPowers.MyTamesNear(player.transform.position, BondRange)) {
                float distance = Vector3.Distance(tame.transform.position, player.transform.position);
                if (distance < best) { best = distance; bondedTame = tame; }
            }
        }

        // Longing for Óðr: the bonded one, or another player near, falling low or dying starts the grief.
        private static void WatchForFalls(Player player) {
            List<Character> watched = new List<Character>();
            watched.AddRange(PowerFx.AlliesNear(player.transform.position, LongingRange));
            if (bondedTame != null) { watched.Add(bondedTame); }
            foreach (Character other in watched) {
                ZDOID id = other.GetZDOID();
                bool low = other.IsDead() || other.GetHealthPercentage() < LongingBelowHealth;
                bool before = wasLow.TryGetValue(id, out bool known) && known;
                wasLow[id] = low;
                if (low && !before && Time.time >= longingReadyAt) {
                    longingUntil = Time.time + LongingSeconds;
                    longingReadyAt = Time.time + LongingCooldown;
                    PowerFx.Show(player, PowerFx.Longing, LongingSeconds);
                    PowerFx.Effect(Power.Longing, VfxRole.Burst, player.transform.position, 0.6f, shared: true);
                }
            }
            if (wasLow.Count > 64) { wasLow.Clear(); }
        }

        // ---- Shared Burden ----

        /// <summary>
        /// On the hurt player's machine: a player bonded to them takes a share of the hit. Returns the
        /// factor left for the hurt player.
        /// </summary>
        internal static float ShareBurden(Player player, float damage) {
            if (damage <= 0f) { return 1f; }
            foreach (Player ally in BondersOf(player)) {
                float share = ally.m_nview.GetZDO().GetFloat(AllyPowers.BurdenKey, 0f);
                if (share <= 0f) { continue; }
                ZRoutedRpc.instance?.InvokeRoutedRPC(ally.m_nview.GetZDO().GetOwner(), BurdenRpc, damage * share / 100f);
                return 1f - share / 100f;
            }
            return 1f;
        }

        /// <summary>On the tame's owner: the local player takes a share of what their bonded tame is hit for.</summary>
        internal static float ShareTameBurden(Character tame, float damage) {
            if (damage <= 0f || !IsBondedTame(tame) || !PowerSet.Has(Power.SharedBurden, out float share)) { return 1f; }
            TakeBurden(damage * share / 100f);
            return 1f - share / 100f;
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<float>(BurdenRpc, (sender, damage) => TakeBurden(damage));
            }
        }

        private static void TakeBurden(float damage) {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead() || damage <= 0f) { return; }
            HitData hit = new HitData() { m_hitType = HitData.HitType.Self };
            hit.m_damage.m_damage = damage;
            player.Damage(hit);
        }

        // Seiðr Link: healing past full flows to the bonded one.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Heal))]
        private static class Overflow {
            private static void Prefix(Character __instance, float hp) {
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.SeidrLink, out float share) || __instance.GetHealth() >= __instance.GetMaxHealth()) { return; }
                Character bonded = Bonded;
                float excess = __instance.GetHealth() + hp - __instance.GetMaxHealth();
                if (bonded == null || excess <= 1f) { return; }
                bonded.Heal(excess * share / 100f, true);
            }
        }
    }
}
