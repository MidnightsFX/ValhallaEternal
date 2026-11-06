using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    /// <summary>Bows and spells: draw speed, extra arrows, spells that fire twice, and spells paid in blood.</summary>
    internal static class RangedPowers {
        private const float ExtraArrowSpread = 6f;
        private static bool echoing;

        // Worked out again from the draw time, as vanilla does, rather than scaling the result: vanilla's
        // result is already clamped at a full draw, so a slower draw could never show from it.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
        private static class DrawSpeed {
            private static void Postfix(Humanoid __instance, ref float __result) {
                if (__instance != Player.m_localPlayer || __result <= 0f) { return; }
                float speed = (1f + PowerSet.Get(Power.DrawSpeed) / 100f) * (1f - PowerSet.Get(Power.DrawSpeedPenalty) / 100f);
                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if (speed == 1f || weapon == null) { return; }
                float fastest = weapon.m_shared.m_attack.m_drawDurationMin;
                float duration = Mathf.Lerp(fastest, fastest * 0.2f, __instance.GetSkillFactor(weapon.m_shared.m_skillType));
                if (duration > 0f) { __result = Mathf.Clamp01(__instance.m_attackDrawTime * speed / duration); }
            }
        }

        private struct BurstState {
            public int Projectiles;
            public float Accuracy;
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        private static class Burst {
            // Rain of the North: extra arrows in this burst, a little wider.
            private static void Prefix(Attack __instance, out BurstState __state) {
                __state = new BurstState() { Projectiles = -1 };
                if (__instance.m_character != Player.m_localPlayer || __instance.m_weapon == null) { return; }
                if (__instance.m_weapon.m_shared.m_skillType != Skills.SkillType.Bows || !PowerSet.Has(Power.ExtraArrows, out float extra)) { return; }
                __state.Projectiles = __instance.m_projectiles;
                __state.Accuracy = __instance.m_projectileAccuracy;
                __instance.m_projectiles += Mathf.RoundToInt(extra);
                __instance.m_projectileAccuracy += ExtraArrowSpread;
            }

            // Echo: a spell sometimes fires again at once, without paying again.
            private static void Postfix(Attack __instance, BurstState __state) {
                if (__state.Projectiles >= 0) {
                    __instance.m_projectiles = __state.Projectiles;
                    __instance.m_projectileAccuracy = __state.Accuracy;
                }
                if (echoing || __instance.m_character != Player.m_localPlayer || __instance.m_weapon == null) { return; }
                Skills.SkillType skill = __instance.m_weapon.m_shared.m_skillType;
                if (skill != Skills.SkillType.ElementalMagic && skill != Skills.SkillType.BloodMagic) { return; }
                if (!PowerSet.Has(Power.SpellEcho, out float echo) || Random.value * 100f >= echo) { return; }
                bool paysPerBurst = __instance.m_perBurstResourceUsage;
                echoing = true;
                __instance.m_perBurstResourceUsage = false;
                try {
                    __instance.FireProjectileBurst();
                } finally {
                    __instance.m_perBurstResourceUsage = paysPerBurst;
                    echoing = false;
                }
            }
        }

        // Völva's Price: with too little eitr, a spell takes the rest from health.
        [HarmonyPatch(typeof(Player), nameof(Player.HaveEitr))]
        private static class EnoughEitr {
            private static void Postfix(Player __instance, float amount, ref bool __result) {
                if (__result || __instance != Player.m_localPlayer || !PowerSet.Has(Power.BloodSeidr)) { return; }
                __result = __instance.GetEitr() + __instance.GetHealth() - 1f > amount;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class PayInBlood {
            private static void Prefix(Player __instance, ref float v) {
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.BloodSeidr) || v <= 0f) { return; }
                // UseEitr scales by the world's eitr rate inside; compare like for like.
                float rate = Game.m_eitrRate > 0f ? Game.m_eitrRate : 1f;
                float needed = v * rate;
                float eitr = __instance.GetEitr();
                if (eitr >= needed) { return; }
                float shortfall = needed - eitr;
                __instance.SetHealth(Mathf.Max(1f, __instance.GetHealth() - shortfall));
                v = eitr / rate;
            }
        }
    }
}
