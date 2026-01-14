using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal class NightSwiftness {



        [HarmonyPatch(typeof(Character))]
        public static class RunningSpeedPatch {

            [HarmonyPatch(nameof(Character.GetRunSpeedFactor))]
            static void Postfix(Character __instance, ref float __result) {
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.SwiftShadow, out float swiftShadowValue) == true && Player.m_localPlayer != null && __instance == Player.m_localPlayer) {
                    if (Player.m_localPlayer.m_currentBiome == Heightmap.Biome.DeepNorth || Player.m_localPlayer.m_currentBiome == Heightmap.Biome.Mountain || EnvMan.IsNight()) {
                        float speedbonus = 0.1f + (swiftShadowValue * 0.01f);
                        float modified_run_speed = __instance.m_runSpeed + speedbonus;
                        __result = modified_run_speed;
                    }
                }
            }
        }
    }
}
