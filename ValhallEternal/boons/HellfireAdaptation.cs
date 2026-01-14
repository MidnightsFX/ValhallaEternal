using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class HellfireAdaptation {

        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentHeatResistanceModifier))]
        private static class HellfireHeatResist {
            private static void Postfix(Player __instance, ref float __result) {
                if (__instance == null) { return; }
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.IncreaseHeatResistance, out float value)) {
                    __result += ((100 + value) / 100);
                }
            }
        }
    }
}
