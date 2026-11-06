using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Víðarr's Fimbulwinter: enduring the cold rather than dealing frost. The cold's toll on regen,
    /// snow on the roof, strength gathered in the cold, and the blizzard that follows him.
    /// </summary>
    internal static class WinterPowers {
        private const float StackSeconds = 60f;
        private const int MaxStacks = 5;
        private const float GuardPerStack = 2f;
        private const float BlizzardRadius = 8f;
        private const float ChillEvery = 1f;
        private const float BlizzardShownEvery = 6f;
        // Snow on the Roof never reaches further than this, so a building only asks about players this close.
        private const float MaxShedRange = 40f;

        private static float coldFor;
        private static int stacks;
        private static float nextChill;
        private static float nextShown;

        internal static void Reset() {
            coldFor = 0f;
            stacks = 0;
            nextChill = 0f;
            nextShown = 0f;
        }

        // In freezing weather vanilla takes Cold away before adding Freezing, so a player another's blizzard
        // keeps from freezing has neither; outside shelter, that player is still out in the cold.
        private static bool Cold(Player player) {
            SEMan seman = player.GetSEMan();
            if (seman.HaveStatusEffect(SEMan.s_statusEffectCold) || seman.HaveStatusEffect(SEMan.s_statusEffectFreezing)) { return true; }
            return EnvMan.IsFreezing() && !seman.HaveStatusEffect(SEMan.s_statusEffectShelter) && Sheltered(player);
        }

        /// <summary>Where Winter-bound holds: the Mountains, the Deep North, and anywhere freezing.</summary>
        internal static bool InWinter(Player player) => MovementPowers.OnSnow(player) || EnvMan.IsFreezing();

        internal static void Tick(Player player) {
            CountWinters(player);
            Blizzard(player);
        }

        // Three Winters: a stack for each minute spent in the cold, kept until a fire warms the player.
        private static void CountWinters(Player player) {
            if (!PowerSet.Has(Power.ThreeWinters) || player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectCampFire)) {
                if (stacks > 0) { PowerFx.Hide(player, PowerFx.Winters); }
                stacks = 0;
                coldFor = 0f;
                return;
            }
            if (stacks > 0 && !PowerFx.Has(player, PowerFx.Winters)) { PowerFx.Show(player, PowerFx.Winters, 3600f); }
            if (stacks >= MaxStacks || !Cold(player)) { return; }
            coldFor += Time.deltaTime;
            if (coldFor < StackSeconds) { return; }
            coldFor -= StackSeconds;
            stacks++;
            PowerFx.Show(player, PowerFx.Winters, 3600f);
            PowerFx.Effect(Power.ThreeWinters, VfxRole.Proc, player.GetCenterPoint(), 0.6f);
        }

        // Fimbulvetr: enemies in the blizzard are chilled, a second at a time.
        private static void Blizzard(Player player) {
            if (!PowerSet.Has(Power.Fimbulvetr) || Time.time < nextChill) { return; }
            nextChill = Time.time + ChillEvery;
            Vector3 at = player.transform.position;
            int chilled = PowerFx.Hash(PowerFx.Chilled);
            Character[] enemies = PowerFx.EnemiesNear(at, BlizzardRadius).ToArray();
            foreach (Character enemy in enemies) {
                enemy.GetSEMan().AddStatusEffect(chilled, resetTime: true);
            }
            // Shown to everyone nearby, so only while it has someone to chill.
            if (enemies.Length > 0 && Time.time >= nextShown) {
                nextShown = Time.time + BlizzardShownEvery;
                PowerFx.Effect(Power.Fimbulvetr, VfxRole.Burst, at, BlizzardRadius / 5f, shared: true);
            }
        }

        internal static float OutgoingFactor(Player player) {
            float factor = 1f + stacks * PowerSet.Get(Power.ThreeWinters) / 100f;
            if (InWinter(player)) {
                factor *= 1f + PowerSet.Get(Power.WinterBound) / 100f;
            } else {
                factor *= Mathf.Max(0.2f, 1f - PowerSet.Get(Power.WarmDamagePenalty) / 100f);
            }
            return factor;
        }

        internal static float IncomingFactor() => 1f - stacks * GuardPerStack / 100f;

        /// <summary>Winter-bound's stamina regen, in percent.</summary>
        internal static float StaminaBonus(Player player) => InWinter(player) ? PowerSet.Get(Power.WinterBound) : 0f;

        /// <summary>Whether another player's blizzard covers the local player, so they can't freeze.</summary>
        internal static bool Sheltered(Player player) {
            foreach (Player ally in PowerFx.AlliesNear(player.transform.position, BlizzardRadius)) {
                if (ally.m_nview?.GetZDO()?.GetBool(AllyPowers.BlizzardKey, false) == true) { return true; }
            }
            return false;
        }

        /// <summary>Endure the Winter: a fresh Cold or Freezing takes less off the player's regen.</summary>
        internal static void Endure(StatusEffect effect) {
            if (!(effect is SE_Stats stats) || !PowerSet.Has(Power.ColdEndurance, out float endurance)) { return; }
            int hash = effect.NameHash();
            if (hash != SEMan.s_statusEffectCold && hash != SEMan.s_statusEffectFreezing) { return; }
            float kept = 1f - endurance / 100f;
            stats.m_healthRegenMultiplier = 1f - (1f - stats.m_healthRegenMultiplier) * kept;
            stats.m_staminaRegenMultiplier = 1f - (1f - stats.m_staminaRegenMultiplier) * kept;
        }

        // Snow on the Roof: a building with a player close enough sheds its snow, so none piles up to break
        // it. Runs where the building is owned, from what each player publishes.
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.CanHaveSnow), typeof(bool), typeof(bool))]
        private static class Shed {
            private static readonly List<Player> players = new List<Player>();

            private static void Postfix(WearNTear __instance, ref bool __result) {
                if (!__result) { return; }
                Vector3 at = __instance.transform.position;
                players.Clear();
                Player.GetPlayersInRange(at, MaxShedRange, players);
                foreach (Player player in players) {
                    float range = player.m_nview?.GetZDO()?.GetFloat(AllyPowers.SnowShedKey, 0f) ?? 0f;
                    if (range > 0f && Vector3.Distance(player.transform.position, at) <= range) {
                        __result = false;
                        return;
                    }
                }
            }
        }
    }
}
