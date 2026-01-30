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

                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessAllDamage, out float alldmgreduced)) {
                            float reduction = (1 - alldmgreduced);
                            if (reduction < 0.2) { reduction = 0.2f; } //20% min dmg
                            hit.m_damage.Modify(reduction);
                            Logger.LogDebug($"Reducing all damage done by {1-reduction}");
                        }
                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessBluntDamage, out float bluntdmgmod)) {
                            float blunt_reduce = (1f - bluntdmgmod);
                            if (blunt_reduce < 0.2) { blunt_reduce = 0.2f; }
                            hit.m_damage.m_blunt *= blunt_reduce;
                            Logger.LogDebug($"Reduced Blunt damage done by {1f - blunt_reduce}");
                        }
                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessSlashDamage, out float slashdmgmod)) {
                            float slash_reduce = (1f - slashdmgmod);
                            hit.m_damage.m_slash *= slash_reduce;
                            Logger.LogDebug($"Reduced Slash damage done by {1f - slash_reduce}.");
                        }
                        if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.DealLessPierceDamage, out float piercedmgmod)) {
                            float pierce_reduce = (1f - piercedmgmod);
                            hit.m_damage.m_pierce *= pierce_reduce;
                            Logger.LogDebug($"Reduced Pierce damage done {1f - pierce_reduce}.");
                        }
                    }
                }
            }
        }
    }
}
