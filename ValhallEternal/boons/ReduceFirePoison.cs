using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class ReduceFirePoison {

        [HarmonyPatch(typeof(Character), nameof(Character.AddFireDamage))]
        private static class ReduceFireDamageApplied {
            private static void Prefix(Character __instance, ref float damage) {
                if (__instance != null && __instance as Player == Player.m_localPlayer && PlayerData.HasBoonWithValue(common.DataObjects.Boons.ReduceFirePoison, out float value)) {
                    float reduction = (100 - value) * 0.001f;
                    // Cap damage to a minimum of 20%
                    if (reduction < 0.2) { reduction = 0.2f; }
                    Logger.LogDebug($"[ReduceFirePoison] is reducing Fire {damage} by {reduction} = ({damage * reduction})");
                    damage *= reduction;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.AddPoisonDamage))]
        private static class ReducePoisonDamageApplied {
            private static void Prefix(Character __instance, ref float damage) {
                if (__instance != null && __instance as Player == Player.m_localPlayer && PlayerData.HasBoonWithValue(common.DataObjects.Boons.ReduceFirePoison, out float value)) {
                    float reduction = (100 - value) * 0.001f;
                    // Cap damage to a minimum of 20%
                    if (reduction < 0.2) { reduction = 0.2f; }
                    Logger.LogDebug($"[ReduceFirePoison] is reducing Poison {damage} by {reduction} = ({damage * reduction})");
                    damage *= reduction;
                }
            }
        }


    }
}
