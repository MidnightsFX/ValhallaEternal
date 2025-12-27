using HarmonyLib;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.oaths
{
    internal static class DealLessDamage
    {
        public static class PlayerOathOfReducedDamageDealt
        {
            [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
            public static class EnemyDamageScalingIncrease
            {
                public static void Prefix(ref HitData hit)
                {
                    Character attacker = hit.GetAttacker();

                    if (attacker != null && attacker.IsPlayer() && attacker as Player == Player.m_localPlayer && PlayerData.localPlayerConfig.DealReductedDamageActive == true) {
                        
                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessBluntDamage, out float bluntdmgmod))
                        {
                            hit.m_damage.m_blunt *= (1f - bluntdmgmod);
                            Logger.LogDebug("Reduced Blunt damage done.");
                        }
                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessSlashDamage, out float slashdmgmod)) {
                            hit.m_damage.m_slash *= (1f - slashdmgmod);
                            Logger.LogDebug("Reduced Slash damage done.");
                        }
                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessPierceDamage, out float piercedmgmod)) {
                            hit.m_damage.m_pierce *= (1f - piercedmgmod);
                            Logger.LogDebug("Reduced Pierce damage done.");
                        }
                    }
                }
            }
        }
    }
}
