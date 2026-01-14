using HarmonyLib;
using UnityEngine;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class ModifyHealthEitirStam {
        [HarmonyPatch(typeof(Player))]
        public static class IncreaseFoodValue {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.GetTotalFoodValue))]
            public static void IncreaseTotalValue(Player __instance, ref float hp, ref float stamina, ref float eitr) {
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.QualityNourishment, out float foodBonus)) {
                    float bonusFactor = 1 + (foodBonus / 100f);
                    hp *= bonusFactor;
                    stamina *= bonusFactor;
                    eitr *= bonusFactor;
                }
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.IncreaseBaseEitr, out float addedEitr)) {
                    eitr += addedEitr;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.GetBaseFoodHP))]
            public static void IncreaseHPValue(Player __instance, ref float __result) {
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.QualityNourishment, out float foodBonus)) {
                    float bonusFactor = 1 + (foodBonus / 100f);
                    __result *= bonusFactor;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
            public static void NatureEffectOnFoodEat() {
                if (PlayerData.HasBoon(DataObjects.Boons.QualityNourishment)) {
                    if (Deities.DeityEffects.ContainsKey("leafburstverticle") && Player.m_localPlayer != null) {
                        GameObject vfx = UnityEngine.Object.Instantiate(Deities.DeityEffects["leafburstverticle"], Player.m_localPlayer.transform.position, Quaternion.identity);
                        vfx.transform.localScale *= 3f;
                    }
                }
            }
        }
    }
}