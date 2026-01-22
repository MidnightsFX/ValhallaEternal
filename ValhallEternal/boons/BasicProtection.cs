using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class BasicProtection {

        [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
        private static class BodyArmor {
            private static void Postfix(ref float __result) {
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.BasicProtection, out float value)) {
                    __result += value;
                }
            }
        }
    }
}
