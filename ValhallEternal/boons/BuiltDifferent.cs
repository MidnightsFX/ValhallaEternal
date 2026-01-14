using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class BuiltDifferent {
        private static class BuiltDifferentStagger {

            [HarmonyPatch(nameof(Character.GetStaggerTreshold))]
            private static void Postfix(Character __instance, ref float __result) {
                if (__instance as Player == Player.m_localPlayer && PlayerData.HasBoonWithValue(common.DataObjects.Boons.BuiltDifferent, out float value)) {
                    float modifier = 1 + ((100 + value * 2) / 100);
                    __result *= modifier;
                    Logger.LogDebug($"[BuiltDifferent] modifying stagger threshold by x{modifier}");
                }
            }
        }
    }
}
