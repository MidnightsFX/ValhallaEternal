using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class BonusSkillXP {

        static bool RaiseActive = false;

        [HarmonyPatch(typeof(Player))]
        public static class IncreaseFoodValue {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.RaiseSkill))]
            public static void ChanceBonus(Player __instance, Skills.SkillType skill) {
                if (RaiseActive == false && PlayerData.HasBoonWithValue(DataObjects.Boons.RandomXPBonus, out float bonuxXP)) {
                    int  roll = UnityEngine.Random.Range(0, 100);
                    if (bonuxXP >= roll) {
                        __instance.RaiseSkill(skill, bonuxXP);
                        RaiseActive = true;
                        Logger.LogDebug($"[RaiseSkillChanceXP] giving XP for {skill} {bonuxXP}");
                    }
                }
                RaiseActive = false;
            }
        }
    }
}
