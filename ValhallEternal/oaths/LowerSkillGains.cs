using HarmonyLib;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.oaths
{
    internal static class LowerSkillGains
    {
        public static class PlayerOathOfReducedDamageDealt
        {
            [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
            public static class EnemyDamageScalingIncrease
            {
                public static void Prefix(Player __instance, Skills.SkillType skill, ref float value)
                {
                    if (__instance == Player.m_localPlayer && PlayerData.localPlayerConfig.ReduceSkillGainsActive == true) {
                        switch (skill) {
                            case Skills.SkillType.Clubs:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainClub, value);
                                break;
                            case Skills.SkillType.Swords:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainSword, value);
                                break;
                            case Skills.SkillType.Knives:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainKnives, value);
                                break;
                            case Skills.SkillType.Polearms:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainPolearms, value);
                                break;
                            case Skills.SkillType.Spears:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainSpears, value);
                                break;
                            case Skills.SkillType.Bows:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainBow, value);
                                break;
                            case Skills.SkillType.Crossbows:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainCrossbow, value);
                                break;
                            case Skills.SkillType.Sneak:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainSneak, value);
                                break;
                            case Skills.SkillType.Run:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainRun, value);
                                break;
                            case Skills.SkillType.BloodMagic:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainBloodMagic, value);
                                break;
                            case Skills.SkillType.ElementalMagic:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainElementalMagic, value);
                                break;
                            case Skills.SkillType.Axes:
                                value *= ModifySkillGain(DataObjects.Oaths.LowerSkillGainAxes, value);
                                break;
                        }

                    }
                }

                private static float ModifySkillGain(DataObjects.Oaths oathkey, float value) {
                    if (PlayerData.localPlayerConfig.TotalOaths.ContainsKey(oathkey)) {
                        float mod = (1f - PlayerData.localPlayerConfig.TotalOaths[oathkey]);
                        Logger.LogDebug($"Applied {oathkey} {value} * {mod} -> {value * mod}.");
                        value *= mod;
                    }
                    return value;
                }
            }
        }
    }
}
