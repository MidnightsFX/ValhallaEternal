using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class IncreaseRegens {
        [HarmonyPatch(typeof(SEMan))]
        private static class IncreaseAllRegenTypes {

            [HarmonyPostfix]
            [HarmonyPatch(nameof(SEMan.ModifyHealthRegen))]
            private static void ModifyHealthRegen(ref float regenMultiplier) {
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.GoddessOfWar, out float value)) {
                    float modifier = 1f + ((100 + value) / 100);
                    regenMultiplier *= modifier;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(SEMan.ModifyStaminaRegen))]
            private static void ModifyStaminaRegen(ref float staminaMultiplier) {
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.GoddessOfWar, out float value)) {
                    float modifier = 1f + ((100 + value) / 100);
                    staminaMultiplier *= modifier;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(SEMan.ModifyEitrRegen))]
            private static void ModifyEitrRegen(ref float eitrMultiplier) {
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.SeidrOfPlenty, out float value)) {
                    float modifier = 1f + ((100 + value) / 100);
                    eitrMultiplier *= modifier;
                }
            }
        }
    }
}
