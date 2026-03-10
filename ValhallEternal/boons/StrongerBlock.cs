using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class StrongerBlock {

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBaseBlockPower), typeof(int))]
        private static class StrongerShieldBlock {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.PowerfulShield, out float block)) {
                    float modifier = 1f + ((100f + block) / 100f);
                    __result *= modifier;
                }
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.StrongerTowerBlock, out float towerblock) && __instance.m_shared.m_timedBlockBonus == 0) {
                    float towerblockbonus = 1f + ((100f + towerblock) / 100f);
                    __result *= towerblockbonus;
                }
            }
        }
    }
}
