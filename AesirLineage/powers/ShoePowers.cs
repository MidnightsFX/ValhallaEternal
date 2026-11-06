using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Víðarr's Thick Shoe: fists, deep snow, the stamp of a hard landing, Gríðr's iron gloves, the shoe
    /// made of every leather scrap, and the foot that holds the wolf's jaw.
    /// </summary>
    internal static class ShoePowers {
        private const string ScrapsName = "$item_leatherscraps";
        private const int ScrapsPerStep = 10;
        private const float StampRadius = 4f;
        private const float StampPerMetre = 10f;
        // Vanilla's fall damage starts at 4 m and reaches 100 at 20 m; the height is read back from it.
        private const float FallStart = 4f;
        private const float FallSpan = 16f;
        private const float PinnedVulnerability = 0.25f;
        private const float StampCooldown = 5f;

        // The player's own deep snow slowdown, before Snow-shod stars shrink it.
        private static Player snowPlayer;
        private static float snowSlow;
        private static float nextStamp;
        private static int weighedVersion = -1;

        internal static void Reset() {
            snowPlayer = null;
            nextStamp = 0f;
            weighedVersion = -1;
        }

        internal static void Tick(Player player) {
            if (player != snowPlayer) {
                snowPlayer = player;
                snowSlow = player.m_deepSnowSlowMax;
            }
            player.m_deepSnowSlowMax = snowSlow * (1f - PowerSet.Get(Power.SnowStride) / 100f);
            // The inventory keeps its weight until something in it changes, so weightless scraps (and
            // Hoard's coins) are weighed again when the stars change.
            if (weighedVersion != PowerSet.Version) {
                weighedVersion = PowerSet.Version;
                player.GetInventory().UpdateTotalWeight();
            }
        }

        private static int Scraps(Player player) => player.GetInventory().CountItems(ScrapsName);

        /// <summary>Gríðr's Gloves: armor added to an unarmed hit as blunt damage, before the damage factors.</summary>
        internal static void PrepareHit(Player player, HitData hit) {
            if (hit.m_skill == Skills.SkillType.Unarmed && PowerSet.Has(Power.IronGloves, out float gloves)) {
                hit.m_damage.m_blunt += player.GetBodyArmor() * gloves / 100f;
            }
        }

        internal static float OutgoingFactor(Player player, HitData hit) {
            if (hit.m_skill != Skills.SkillType.Unarmed) {
                return Mathf.Max(0.2f, 1f - PowerSet.Get(Power.ArmedDamagePenalty) / 100f);
            }
            float factor = 1f + PowerSet.Get(Power.UnarmedDamage) / 100f;
            if (PowerSet.Has(Power.ShoeOfScraps, out float most)) {
                factor *= 1f + Mathf.Min(most, Scraps(player) / ScrapsPerStep) / 100f;
            }
            return factor;
        }

        /// <summary>Foot on the Jaw: an unarmed heavy attack on a staggered enemy pins it.</summary>
        internal static void AfterHit(Character target, HitData hit, bool heavy, bool staggered) {
            if (!heavy || !staggered || hit.m_skill != Skills.SkillType.Unarmed || !PowerSet.Has(Power.FootOnTheJaw)) { return; }
            target.GetSEMan().AddStatusEffect(PowerFx.Hash(target.IsBoss() ? PowerFx.PinnedBrief : PowerFx.Pinned), resetTime: true);
            PowerFx.Effect(Power.FootOnTheJaw, VfxRole.Proc, target.GetCenterPoint(), 0.8f, shared: true);
        }

        // A pinned creature takes more from everyone's hits. RPC_Damage runs where the creature is owned,
        // which is also where the pin was added.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class PinnedTakesMore {
            private static void Prefix(Character __instance, HitData hit) {
                if (hit == null || __instance is Player) { return; }
                if (PowerFx.Has(__instance, PowerFx.Pinned) || PowerFx.Has(__instance, PowerFx.PinnedBrief)) {
                    hit.m_damage.Modify(1f + PinnedVulnerability);
                }
            }
        }

        // Shoe of Scraps: the scraps weigh nothing.
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        private static class Weightless {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                if (__instance.m_shared.m_name == ScrapsName && PowerSet.Has(Power.ShoeOfScraps)) { __result = 0f; }
            }
        }

        // Heel-stamp: a landing that hurts sends the hurt into the ground around the player instead.
        // Vanilla only asks about fall damage for a fall of more than 4 m. A landing something else
        // already softens to nothing (a feather cape, Sure-footed, a glide) doesn't stamp; the other
        // postfix may run before or after this one, so its conditions are read here too.
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
        private static class Landing {
            private static void Postfix(SEMan __instance, float baseDamage, ref float damage) {
                if (!(__instance.m_character is Player player) || player != Player.m_localPlayer || !PowerSet.Has(Power.HeelStamp, out float stamp)) { return; }
                bool hurts = damage > 0f && !PowerSet.Has(Power.SureFooted) && !MovementPowers.SoftLanding;
                damage *= 1f - stamp / 100f;
                if (!hurts || Time.time < nextStamp) { return; }
                nextStamp = Time.time + StampCooldown;
                Stamp(player, FallStart + Mathf.Clamp01(baseDamage / 100f) * FallSpan);
            }
        }

        private static void Stamp(Player player, float fallen) {
            Vector3 at = player.transform.position;
            float blunt = fallen * StampPerMetre * (1f + PowerSet.Get(Power.UnarmedDamage) / 100f);
            PowerFx.Effect(Power.HeelStamp, VfxRole.Burst, at, StampRadius / 5f, shared: true);
            foreach (Character enemy in PowerFx.EnemiesNear(at, StampRadius).ToArray()) {
                PowerFx.Strike(enemy, new HitData.DamageTypes() { m_blunt = blunt }, at);
                if (!enemy.IsBoss()) { enemy.Stagger((enemy.transform.position - at).normalized); }
            }
        }
    }
}
