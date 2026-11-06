using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    /// <summary>
    /// Stamina costs, movement speed, falls and stealth, through SEMan's modifier hooks. SEMan exists on
    /// every character, so each hook checks it is the local player's.
    /// </summary>
    [HarmonyPatch(typeof(SEMan))]
    internal static class StaminaPowers {
        private static bool Local(SEMan seman) => seman.m_character != null && seman.m_character == Player.m_localPlayer;

        private static float Cheaper(Power power) => 1f - PowerSet.Get(power) / 100f;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyRunStaminaDrain))]
        private static void RunStamina(SEMan __instance, ref float drain) {
            if (!Local(__instance)) { return; }
            drain *= Cheaper(Power.RunStamina);
            if (PowerSet.Has(Power.Skis, out float skis) && MovementPowers.OnSnow((Player)__instance.m_character)) {
                drain *= 1f - skis / 100f;
            }
            // Furrow Stride: running on paths and tilled ground costs twice its speed bonus less.
            if (PowerSet.Has(Power.PathSpeed, out float path) && MovementPowers.OnPath((Player)__instance.m_character)) {
                drain *= Mathf.Max(0f, 1f - 2f * path / 100f);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyJumpStaminaUsage))]
        private static void JumpStamina(SEMan __instance, ref float staminaUse) {
            if (Local(__instance)) { staminaUse *= Cheaper(Power.JumpStamina); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyAttackStaminaUsage))]
        private static void AttackStamina(SEMan __instance, ref float staminaUse) {
            if (!Local(__instance)) { return; }
            staminaUse *= Cheaper(Power.AttackStamina);
            Skills.SkillType skill = ((Player)__instance.m_character).GetCurrentWeapon()?.m_shared.m_skillType ?? Skills.SkillType.None;
            if (skill == Skills.SkillType.Axes || skill == Skills.SkillType.Pickaxes) { staminaUse *= Cheaper(Power.ToolStamina); }
            if (skill == Skills.SkillType.Polearms) { staminaUse *= Cheaper(Power.PolearmStamina); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyBlockStaminaUsage))]
        private static void BlockStamina(SEMan __instance, ref float staminaUse) {
            // Negative usage is a refund (perfect blocks can give stamina back); leave those alone.
            if (Local(__instance) && staminaUse > 0f) { staminaUse *= Cheaper(Power.BlockStamina); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyDodgeStaminaUsage))]
        private static void DodgeStamina(SEMan __instance, ref float staminaUse) {
            if (Local(__instance)) { staminaUse *= Cheaper(Power.DodgeStamina) * (1f + PowerSet.Get(Power.DodgeStaminaPenalty) / 100f); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifySwimStaminaUsage))]
        private static void SwimStamina(SEMan __instance, ref float staminaUse) {
            if (Local(__instance)) { staminaUse *= Cheaper(Power.SwimStamina); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyFallDamage))]
        private static void FallDamage(SEMan __instance, ref float damage) {
            if (Local(__instance) && (PowerSet.Has(Power.SureFooted) || MovementPowers.SoftLanding)) { damage = 0f; }
        }

        // Lower is harder to notice; vanilla only asks while crouching.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyStealth))]
        private static void Stealth(SEMan __instance, ref float stealth) {
            if (!Local(__instance)) { return; }
            stealth *= 1f - PowerSet.Get(Power.Stealth) / 100f;
            if (StrikePowers.InDark((Player)__instance.m_character)) { stealth *= 1f - PowerSet.Get(Power.NightStealth) / 100f; }
            if (MovementPowers.Silent) { stealth *= 0.5f; }
            stealth *= SilencePowers.StealthFactor();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyNoise))]
        private static void Noise(SEMan __instance, ref float noise) {
            if (Local(__instance) && MovementPowers.Silent) { noise = 0f; }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static void Speed(SEMan __instance, ref float speed) {
            if (!Local(__instance)) { return; }
            speed *= 1f - PowerSet.Get(Power.MoveSpeedPenalty) / 100f;
            Player player = (Player)__instance.m_character;
            float faster = BondPowers.BondBonus(player) + StrikePowers.TerrorStacks * PowerSet.Get(Power.Terror) / 4f;
            if (EnvMan.IsNight()) { faster += PowerSet.Get(Power.ChildOfNight) / 2f; }
            if (MovementPowers.OnPath(player)) { faster += PowerSet.Get(Power.PathSpeed); }
            faster += AfterPowers.SpeedBonus(player);
            speed *= 1f + faster / 100f;
            // Skis of Skaði: on snow, a fifth of their stamina saving again as speed.
            if (PowerSet.Has(Power.Skis, out float skis) && MovementPowers.OnSnow((Player)__instance.m_character)) {
                speed *= 1f + skis / 500f;
            }
        }
    }
}
