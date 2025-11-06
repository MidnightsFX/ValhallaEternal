using HarmonyLib;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.oaths {
    public static class DamageTaken {
        public static class MultiplayerDamageMod {
            [HarmonyPatch(typeof(Game), nameof(Game.GetDifficultyDamageScalePlayer))]
            public static class EnemyDamageScalingIncrease {
                public static void Postfix(ref float __result) {
                    if (Player.m_localPlayer != null &&  PlayerData.localPlayerConfig.TotalOaths.ContainsKey(DataObjects.Oaths.DamageTaken)) {
                        float extra_damagetaken_percent = PlayerData.localPlayerConfig.TotalOaths[DataObjects.Oaths.DamageTaken];
                        Logger.LogDebug($"Oath of Damage Taken: oath: {extra_damagetaken_percent}");
                        __result += extra_damagetaken_percent;
                    }
                }
            }
        }
    }
}
