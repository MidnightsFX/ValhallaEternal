using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.modules;
using static Skills;

namespace ValhallEternal.boons
{
    internal static class WeaponLearning
    {
        public static readonly List<Skills.SkillType> WeaponsSkills = new List<Skills.SkillType>() {
            SkillType.Swords,
            SkillType.Clubs,
            SkillType.Knives,
            SkillType.Crossbows,
            SkillType.Axes,
            SkillType.Bows,
            SkillType.BloodMagic,
            SkillType.Dodge,
            SkillType.ElementalMagic,
            SkillType.Polearms,
            SkillType.Spears
        };

        public static readonly List<Skills.SkillType> NonCombatSkills = new List<Skills.SkillType>() {
            SkillType.Run,
            SkillType.Cooking,
            SkillType.Fishing,
            SkillType.Farming,
            SkillType.Crafting,
            SkillType.Ride,
            SkillType.Sneak,
            SkillType.Pickaxes,
            SkillType.WoodCutting,
            SkillType.Jump
        };

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        internal static class WeaponSharedKnowledgePatch
        {
            public static void Prefix(Skills __instance, SkillType skillType, float factor)
            {
                if (!PlayerData.HasBoonWithValue(common.DataObjects.Boons.RandomXPBonus, out float value)) { return; }

                // Random skill bonus raise
                float roll = UnityEngine.Random.Range(0, 100);
                if (roll < value)
                {
                    float bonus = UnityEngine.Random.Range(1, value);
                    Logger.LogDebug($"[WeaponSharedKnowledgePatch] Raising skill {skillType} by {bonus} due to RandomXPBonus boon.");
                    factor += bonus;
                }
            }

            public static void Postfix(Skills __instance, SkillType skillType, float factor)
            {
                if (WeaponsSkills.Contains(skillType)) { return; }
                if (!PlayerData.HasBoonWithValue(common.DataObjects.Boons.BladeboundKnowledge, out float value)) { return; }
                float roll = UnityEngine.Random.Range(0, 100);
                if (roll < value)
                {
                    float bonusAmount = UnityEngine.Random.Range(1, value);
                    SkillType selectedSkill = NonCombatSkills[UnityEngine.Random.Range(0, NonCombatSkills.Count)];
                    Logger.LogDebug($"[WeaponSharedKnowledgePatch] Raising non-weapon skill {selectedSkill} by {bonusAmount} due to Bladebound Knowledge boon.");
                    __instance.RaiseSkill(selectedSkill, bonusAmount);
                }
            }
        }
    }
}
