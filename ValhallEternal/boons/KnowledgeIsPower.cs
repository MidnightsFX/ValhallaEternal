using HarmonyLib;
using System.Collections.Generic;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class KnowledgeIsPower {

        static readonly List<Skills.SkillType> CombatSkills = new List<Skills.SkillType>() {
            Skills.SkillType.Swords,
            Skills.SkillType.Knives,
            Skills.SkillType.Clubs,
            Skills.SkillType.Polearms,
            Skills.SkillType.Spears,
            Skills.SkillType.Axes,
            Skills.SkillType.Bows,
            Skills.SkillType.Crossbows,
            Skills.SkillType.Unarmed,
            //Skills.SkillType.ElementalMagic,
            //Skills.SkillType.BloodMagic
        };

        public static float CurrentBonus = 0;

        public static int SkillLevelReckFrequency = 10;

        [HarmonyPatch(typeof(Player))]
        public static class CheckForIncreaseToBonus {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.RaiseSkill))]
            public static void CheckSkillLevelBonus(Player __instance, Skills.SkillType skill) {
                // This is not a combat skill
                if (CombatSkills.Contains(skill) != true && PlayerData.HasBoonWithValue(DataObjects.Boons.KnowledgeIsPower, out float value)) {
                    float highest = 0;
                   foreach(Skills.SkillDef sk in __instance.GetSkills().m_skills) {
                        if (CombatSkills.Contains(sk.m_skill)) { continue; }
                        float sklevel = Player.m_localPlayer.GetSkillLevel(sk.m_skill);
                        if (sklevel > highest) { highest = sklevel; }
                    }
                   // value % of the highest non-combat skill
                   CurrentBonus = 1 + (highest * ((100 + value) / 100));
                }
            }
        }

        [HarmonyPatch]
        public static class ApplySkillBonusFactor {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SEMan),nameof(SEMan.ModifySkillLevel))]
            public static void ProvideSkillLevelBonus(Skills.SkillType skill, ref float level) {
                // If the player has KnowledgeIsPower and this is a combat skill, increase it
                if (PlayerData.HasBoon(DataObjects.Boons.KnowledgeIsPower) && CombatSkills.Contains(skill)) {
                    if (CurrentBonus > 0) {
                         level *= CurrentBonus;
                    }
                }
            }
        }
    }
}
