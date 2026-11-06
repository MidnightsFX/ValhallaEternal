using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using static Skills;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    internal static class SkillPowers {
        internal static readonly HashSet<SkillType> CombatSkills = new HashSet<SkillType>() {
            SkillType.Swords,
            SkillType.Knives,
            SkillType.Clubs,
            SkillType.Polearms,
            SkillType.Spears,
            SkillType.Blocking,
            SkillType.Axes,
            SkillType.Bows,
            SkillType.ElementalMagic,
            SkillType.BloodMagic,
            SkillType.Unarmed,
            SkillType.Crossbows,
            SkillType.Dodge,
        };

        private static readonly SkillType[] NonCombatSkills = new SkillType[] {
            SkillType.Pickaxes,
            SkillType.WoodCutting,
            SkillType.Jump,
            SkillType.Sneak,
            SkillType.Run,
            SkillType.Swim,
            SkillType.Fishing,
            SkillType.Cooking,
            SkillType.Farming,
            SkillType.Crafting,
            SkillType.Ride,
        };

        // Hunger for Knowledge raises a skill as much as this many ordinary uses.
        private const float RecipeInsightFactor = 5f;
        // Knowledge Is Power and Strength of the Valkyries never add more than this many levels.
        private const float KnowledgeIsPowerMaxLevels = 10f;
        private const float ValkyrieMaxLevels = 10f;
        private const float VoluspaSeconds = 10f;

        private static readonly HashSet<SkillType> WeaponSkills = new HashSet<SkillType>() {
            SkillType.Swords,
            SkillType.Knives,
            SkillType.Clubs,
            SkillType.Polearms,
            SkillType.Spears,
            SkillType.Axes,
            SkillType.Bows,
            SkillType.Crossbows,
            SkillType.Unarmed,
        };

        // Best non-combat skill level, or -1 to count it again.
        private static float bestNonCombatLevel = -1f;
        // The two best weapon skills, so each weapon can be measured against the best other one.
        private static bool weaponLevelsKnown;
        private static SkillType bestWeapon;
        private static float bestWeaponLevel;
        private static float secondWeaponLevel;
        private static float voluspaUntil;

        internal static void ForgetSkillLevels() {
            bestNonCombatLevel = -1f;
            weaponLevelsKnown = false;
        }

        internal static void Reset() {
            ForgetSkillLevels();
            voluspaUntil = 0f;
        }

        /// <summary>Völuspá: the bonus to damage and skill gain for a while after a skill goes up.</summary>
        internal static float VoluspaBonus() => Time.time < voluspaUntil ? PowerSet.Get(Power.Voluspa) : 0f;

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class SkillGain {
            // __state carries the factor as the game asked for it, so Bladebound Knowledge doesn't
            // apply the skill XP bonus twice.
            private static void Prefix(Skills __instance, SkillType skillType, ref float factor, out float __state) {
                __state = factor;
                if (__instance.m_player != Player.m_localPlayer || skillType == SkillType.None) { return; }

                float multiplier = 1f + PowerSet.Get(Power.SkillXp) / 100f;
                if (!CombatSkills.Contains(skillType)) { multiplier *= 1f + PowerSet.Get(Power.NonCombatSkillXp) / 100f; }
                if (skillType == SkillType.Farming) { multiplier *= 1f + PowerSet.Get(Power.FarmingXp) / 100f; }
                multiplier *= 1f + VoluspaBonus() / 100f;
                multiplier *= 1f + (StrikePowers.VowBonus() + StrikePowers.WitnessBonus(__instance.m_player)) / 100f;
                multiplier *= 1f + AfterPowers.SkillBonus(__instance.m_player, skillType) / 100f;
                if (PowerSet.Has(Power.SkillEcho, out float echo) && Random.value * 100f < echo) { multiplier *= 2f; }
                multiplier *= 1f - PowerSet.Get(Power.SkillGainPenalty) / 100f;
                if (CombatSkills.Contains(skillType)) {
                    multiplier *= 1f - PowerSet.Get(Power.CombatSkillGainPenalty) / 100f;
                }
                factor *= multiplier;
            }

            private static void Postfix(Skills __instance, SkillType skillType, float __state) {
                if (__instance.m_player != Player.m_localPlayer) { return; }
                ForgetSkillLevels();
                if (!CombatSkills.Contains(skillType)) { return; }
                if (PowerSet.Has(Power.BladeboundKnowledge, out float chance) && Random.value * 100f < chance) {
                    __instance.RaiseSkill(NonCombatSkills[Random.Range(0, NonCombatSkills.Length)], __state);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownRecipe))]
        private static class HungerForKnowledge {
            // Vanilla calls this for recipes already known too; only a new one counts.
            private static void Prefix(Player __instance, Recipe recipe, out bool __state) {
                __state = __instance == Player.m_localPlayer && recipe?.m_item != null
                    && !__instance.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name);
            }

            private static void Postfix(Player __instance, bool __state) {
                if (!__state || !PowerSet.Has(Power.RecipeInsight, out float chance) || Random.value * 100f >= chance) { return; }
                List<SkillDef> skills = __instance.GetSkills().m_skills;
                if (skills == null || skills.Count == 0) { return; }
                SkillType skill = skills[Random.Range(0, skills.Count)].m_skill;
                if (skill == SkillType.None || skill == SkillType.All) { return; }
                __instance.RaiseSkill(skill, RecipeInsightFactor);
            }
        }

        // Combat skills count as higher by a share of the best non-combat skill.
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifySkillLevel))]
        private static class KnowledgeIsPower {
            private static void Postfix(SEMan __instance, SkillType skill, ref float level) {
                Player local = Player.m_localPlayer;
                if (local == null || __instance.m_character != local || !CombatSkills.Contains(skill)) { return; }
                if (!PowerSet.Has(Power.KnowledgeIsPower, out float share)) { return; }
                if (bestNonCombatLevel < 0f) { bestNonCombatLevel = BestNonCombatLevel(local); }
                level += Mathf.Min(KnowledgeIsPowerMaxLevels, bestNonCombatLevel * share / 100f);
            }

            // Reads the raw levels: GetSkillLevel would come back through this patch.
            private static float BestNonCombatLevel(Player player) {
                float best = 0f;
                foreach (KeyValuePair<SkillType, Skill> skill in player.GetSkills().m_skillData) {
                    if (!CombatSkills.Contains(skill.Key) && skill.Value.m_level > best) { best = skill.Value.m_level; }
                }
                return best;
            }
        }

        // Weapon skills count as higher by a share of the best other weapon skill.
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifySkillLevel))]
        private static class StrengthOfTheValkyries {
            private static void Postfix(SEMan __instance, SkillType skill, ref float level) {
                Player local = Player.m_localPlayer;
                if (local == null || __instance.m_character != local || !WeaponSkills.Contains(skill)) { return; }
                if (!PowerSet.Has(Power.StrengthOfValkyries, out float share)) { return; }
                if (!weaponLevelsKnown) { CountWeaponLevels(local); }
                float best = skill == bestWeapon ? secondWeaponLevel : bestWeaponLevel;
                level += Mathf.Min(ValkyrieMaxLevels, best * share / 100f);
            }

            private static void CountWeaponLevels(Player player) {
                bestWeapon = SkillType.None;
                bestWeaponLevel = 0f;
                secondWeaponLevel = 0f;
                foreach (KeyValuePair<SkillType, Skill> skill in player.GetSkills().m_skillData) {
                    if (!WeaponSkills.Contains(skill.Key)) { continue; }
                    float level = skill.Value.m_level;
                    if (level > bestWeaponLevel) {
                        secondWeaponLevel = bestWeaponLevel;
                        bestWeaponLevel = level;
                        bestWeapon = skill.Key;
                    } else if (level > secondWeaponLevel) {
                        secondWeaponLevel = level;
                    }
                }
                weaponLevelsKnown = true;
            }
        }

        // Eureka: a level-up sometimes brings a second level with it. Völuspá: a level-up opens a short
        // window of more damage and faster learning.
        [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
        private static class LevelUp {
            private static void Postfix(Player __instance, SkillType skill, float level) {
                if (__instance != Player.m_localPlayer) { return; }
                if (PowerSet.Has(Power.Voluspa)) {
                    voluspaUntil = Time.time + VoluspaSeconds;
                    PowerFx.Effect(Power.Voluspa, VfxRole.LevelUp, __instance.transform.position, 0.8f);
                    PowerFx.Show(__instance, PowerFx.Voluspa, VoluspaSeconds);
                }
                if (!PowerSet.Has(Power.Eureka, out float chance) || Random.value * 100f >= chance) { return; }
                Skill raised = __instance.GetSkills().GetSkill(skill);
                if (raised == null || raised.m_level >= 100f) { return; }
                // Setting the level directly doesn't call OnSkillLevelup again.
                raised.m_level = Mathf.Min(100f, Mathf.Floor(raised.m_level) + 1f);
                raised.m_accumulator = 0f;
                ForgetSkillLevels();
                PowerFx.Effect(Power.Eureka, VfxRole.LevelUp, __instance.transform.position, 1f, shared: true);
                // Localize doesn't translate the words it inserts, so the skill name goes in already translated.
                string skillName = Localization.instance.Localize("$skill_" + skill.ToString().ToLower());
                __instance.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$al_eureka", skillName, ((int)raised.m_level).ToString()));
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.LowerAllSkills))]
        private static class SkillsLowered {
            private static void Postfix() { ForgetSkillLevels(); }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
        private static class DeathSkillLoss {
            private static void Prefix(Skills __instance, out float __state) {
                __state = __instance.m_DeathLowerFactor;
                if (__instance.m_player != Player.m_localPlayer) { return; }
                if (PowerSet.Has(Power.DeathSkillLoss, out float extra)) {
                    __instance.m_DeathLowerFactor *= 1f + extra / 100f;
                }
                __instance.m_DeathLowerFactor *= AfterPowers.DeathLossKept();
            }

            private static void Postfix(Skills __instance, float __state) {
                __instance.m_DeathLowerFactor = __state;
            }
        }
    }
}
