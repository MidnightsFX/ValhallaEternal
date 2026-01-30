using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class RecipeGainBonus {
        [HarmonyPatch(typeof(Player))]
        public static class IncreaseFoodValue {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.AddKnownRecipe))]
            public static void ThirstForKnowledgeXPGain(Player __instance) {
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.HungerForKnowledge, out float KnowledgeThirst)) {
                    int roll = UnityEngine.Random.Range(0, 100);
                    if (roll <= KnowledgeThirst) {
                        Skills.SkillType[] skills = (Skills.SkillType[])Enum.GetValues(typeof(Skills.SkillType));
                        
                        Skills.SkillType selectedSkill = skills[UnityEngine.Random.Range(0, skills.Length - 1)];
                        __instance.RaiseSkill(selectedSkill, KnowledgeThirst);
                        Logger.LogDebug($"[HungerForKnowledgeXPGain] giving XP for {selectedSkill} {KnowledgeThirst}");
                    }
                }
            }
        }
    }
}
