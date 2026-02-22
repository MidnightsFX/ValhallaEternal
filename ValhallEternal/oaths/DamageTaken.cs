using HarmonyLib;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.oaths {
    internal static class DamageTaken {
        public static class PlayerOathOfDamageTaken {
            [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
            public static class EnemyDamageScalingIncrease {
                public static void Prefix(Character __instance, ref HitData hit) {
                    if (__instance.IsPlayer() && PlayerData.localPlayerConfig.TotalOaths.ContainsKey(DataObjects.Oaths.DamageTakenIncrease) && __instance as Player == Player.m_localPlayer) {
                        float extra_damagetaken_percent = (PlayerData.localPlayerConfig.TotalOaths[DataObjects.Oaths.DamageTakenIncrease]/100f) + 1;
                        Logger.LogDebug($"Oath of Damage Taken mult: {extra_damagetaken_percent} Hit total dmg: {hit.GetTotalDamage()}");
                        hit.m_damage.Modify(extra_damagetaken_percent);
                        Logger.LogDebug($"New Oath increased Damage: {hit.GetTotalDamage()}");
                    }
                }
            }
        }
    }
}
