using HarmonyLib;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    // SEMan exists on every character, so each hook checks it is the local player's. Valhalla Eternal's
    // regen boons skipped that check and sped up every creature the player owned.
    [HarmonyPatch(typeof(SEMan))]
    internal static class RegenPowers {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyHealthRegen))]
        private static void HealthRegen(SEMan __instance, ref float regenMultiplier) {
            if (__instance.m_character is Player player && player == Player.m_localPlayer) {
                regenMultiplier *= (1f + PowerSet.Get(Power.HealthRegen) / 100f) * (1f - PowerSet.Get(Power.HealthRegenPenalty) / 100f);
                float bonus = LightPowers.SanctuaryBonus(player) + BondPowers.BondRegen();
                if (LightPowers.InSunlight(player)) { bonus += PowerSet.Get(Power.DaylightRegen); }
                regenMultiplier *= 1f + bonus / 100f;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyStaminaRegen))]
        private static void StaminaRegen(SEMan __instance, ref float staminaMultiplier) {
            if (__instance.m_character is Player player && player == Player.m_localPlayer) {
                staminaMultiplier *= (1f + PowerSet.Get(Power.StaminaRegen) / 100f) * (1f - PowerSet.Get(Power.StaminaRegenPenalty) / 100f);
                float bonus = HomePowers.HearthBonus(player) + BondPowers.BondBonus(player) + WinterPowers.StaminaBonus(player) + SilencePowers.StaminaBonus();
                if (LightPowers.InSunlight(player)) { bonus += PowerSet.Get(Power.DaylightRegen); }
                staminaMultiplier *= 1f + bonus / 100f;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyEitrRegen))]
        private static void EitrRegen(SEMan __instance, ref float eitrMultiplier) {
            if (__instance.m_character == Player.m_localPlayer) {
                eitrMultiplier *= (1f + PowerSet.Get(Power.EitrRegen) / 100f) * (1f - PowerSet.Get(Power.EitrRegenPenalty) / 100f);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SEMan.ModifyMaxCarryWeight))]
        private static void CarryWeight(SEMan __instance, ref float limit) {
            if (__instance.m_character == Player.m_localPlayer) {
                limit += PowerSet.Get(Power.CarryWeight) - PowerSet.Get(Power.CarryWeightPenalty) + TamePowers.FourOxenCarry();
            }
        }
    }
}
