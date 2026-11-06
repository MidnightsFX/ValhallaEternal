using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// What happens to damage that is about to land on the local player, after armour: reductions that
    /// depend on the player's state, and the powers that catch a killing blow.
    /// </summary>
    internal static class SurvivalPowers {
        private const int MaxFortification = 5;
        private const float FortificationLasts = 10f;
        private const float ShelterBelowHealth = 0.35f;
        private const float CheatDeathCooldown = 600f;
        private const float RootRadius = 6f;
        private const float BargainSeconds = 15f;
        private const float BargainCooldown = 3600f;
        private const float InvincibleBelowHealth = 0.2f;
        private const float InvincibleSeconds = 3f;
        private const float InvincibleCooldown = 300f;
        private const float LastDoorRange = 10f;

        private static int fortification;
        private static float fortificationGained;
        private static float cheatDeathReadyAt;
        private static float bargainUntil = -1f;
        private static float bargainReadyAt;
        private static float invincibleUntil;
        private static float invincibleReadyAt;
        // The bargain's own killing blow may arrive a frame later through the RPC, so it is waved through
        // for a moment rather than inside a try block.
        private static float executingBargainUntil;

        internal static void Reset(bool newCharacter) {
            fortification = 0;
            bargainUntil = -1f;
            invincibleUntil = 0f;
            // Dying doesn't skip a death save's wait; only a different character starts with them ready.
            if (newCharacter) {
                cheatDeathReadyAt = 0f;
                bargainReadyAt = 0f;
                invincibleReadyAt = 0f;
            }
        }

        internal static int FortificationStacks => fortification;

        /// <summary>Adds Fortification stacks (each one less damage taken), refreshing how long they last.</summary>
        internal static void Fortify(int stacks) {
            if (!PowerSet.Has(Power.Fortification)) { return; }
            fortification = Mathf.Min(MaxFortification, fortification + stacks);
            fortificationGained = Time.time;
            PowerFx.Show(Player.m_localPlayer, PowerFx.Fortified, FortificationLasts);
        }

        internal static void OnLocalKill() {
            if (bargainUntil > 0f) {
                bargainUntil = -1f;
                PowerFx.Hide(Player.m_localPlayer, PowerFx.Bargain);
            }
        }

        internal static void Tick(Player player) {
            if (fortification > 0 && Time.time - fortificationGained > FortificationLasts) { fortification = 0; }
            // Death's Bargain: no kill in time, and the debt is paid.
            if (bargainUntil > 0f && Time.time > bargainUntil) {
                bargainUntil = -1f;
                executingBargainUntil = Time.time + 1f;
                HitData hit = new HitData() { m_hitType = HitData.HitType.Self };
                hit.m_damage.m_damage = player.GetMaxHealth() * 10f;
                player.Damage(hit);
            }
        }

        internal static void OnDamageLanding(Player player, HitData hit) {
            if (Time.time < executingBargainUntil) { return; }
            if (Time.time < invincibleUntil) {
                hit.m_damage = new HitData.DamageTypes();
                return;
            }
            float total = hit.GetTotalDamage();
            if (total <= 0f) { return; }

            float factor = 1f;
            if (PowerSet.Has(Power.Shelter, out float shelter) && player.GetHealthPercentage() < ShelterBelowHealth) {
                factor *= 1f - shelter / 100f;
            }
            // Oath-ring: each oath carried is a stack that never fades.
            int stacks = PowerSet.Has(Power.OathRing) ? Mathf.Max(fortification, Mathf.Min(MaxFortification, StrikePowers.OathCount())) : fortification;
            if (stacks > 0 && PowerSet.Has(Power.Fortification, out float perStack)) {
                factor *= Mathf.Max(0.5f, 1f - stacks * perStack / 100f);
            }
            if (factor != 1f) {
                hit.m_damage.Modify(factor);
                total *= factor;
            }

            // Burdens moved onto a summon or a bonded player, then Tears of Red Gold paying in coins.
            float moved = StrikePowers.MoveBurdens(player, hit);
            float paid = TradePowers.PayInGold(player, total * moved);
            if (total > 0f && paid != total) {
                hit.m_damage.Modify(paid / total);
                total = paid;
            }

            // Brísingamen: while eitr is above half, eitr takes the damage first.
            if (PowerSet.Has(Power.Brisingamen)) {
                float spare = player.GetEitr() - player.GetMaxEitr() * 0.5f;
                if (spare > 0f) {
                    float absorbed = Mathf.Min(spare, total);
                    player.UseEitr(absorbed);
                    hit.m_damage.Modify((total - absorbed) / total);
                    total -= absorbed;
                }
            }

            float health = player.GetHealth();
            if (total >= health) {
                if (SaveFromDeath(player, hit, health)) { return; }
            } else if (PowerSet.Has(Power.Invincible) && Time.time >= invincibleReadyAt
                && (health - total) / player.GetMaxHealth() < InvincibleBelowHealth) {
                invincibleUntil = Time.time + InvincibleSeconds;
                invincibleReadyAt = Time.time + InvincibleCooldown;
                PowerFx.Show(player, PowerFx.Invincible, InvincibleSeconds);
                PowerFx.Effect(Power.Invincible, VfxRole.Burst, player.transform.position, 0.6f, shared: true);
            }
        }

        // The killing-blow powers, in order: Bloom of Zealand, Death's Bargain, then an ally's Last Door.
        private static bool SaveFromDeath(Player player, HitData hit, float health) {
            if (PowerSet.Has(Power.CheatDeath) && Time.time >= cheatDeathReadyAt) {
                cheatDeathReadyAt = Time.time + CheatDeathCooldown;
                LeaveAtOne(hit, health);
                PowerFx.Effect(Power.CheatDeath, VfxRole.Burst, player.transform.position, 0.8f, shared: true);
                int rooted = PowerFx.Hash(PowerFx.Rooted);
                foreach (Character enemy in PowerFx.EnemiesNear(player.transform.position, RootRadius).ToArray()) {
                    enemy.GetSEMan().AddStatusEffect(rooted, resetTime: true);
                }
                return true;
            }
            if (PowerSet.Has(Power.DeathsBargain) && Time.time >= bargainReadyAt) {
                bargainReadyAt = Time.time + BargainCooldown;
                bargainUntil = Time.time + BargainSeconds;
                PowerFx.Effect(Power.DeathsBargain, VfxRole.Burst, player.transform.position, 0.6f, shared: true);
                LeaveAtOne(hit, health);
                PowerFx.Show(player, PowerFx.Bargain, BargainSeconds);
                return true;
            }
            Player guardian = AllyPowers.ReadyGuardianNear(player, LastDoorRange);
            if (guardian != null) {
                float total = hit.GetTotalDamage();
                hit.m_damage = new HitData.DamageTypes();
                AllyPowers.SendBlowToGuardian(guardian, total);
                return true;
            }
            return false;
        }

        private static void LeaveAtOne(HitData hit, float health) {
            float total = hit.GetTotalDamage();
            if (total <= 0f) { return; }
            hit.m_damage.Modify(Mathf.Max(0f, health - 1f) / total);
        }

        // Overhealing becomes Fortification (Stored for Later).
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Heal))]
        private static class Overheal {
            private static void Prefix(Character __instance, float hp) {
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.StoredForLater) || __instance.GetHealth() >= __instance.GetMaxHealth()) { return; }
                float excess = __instance.GetHealth() + hp - __instance.GetMaxHealth();
                if (excess >= 5f) { Fortify(1); }
            }
        }
    }
}
