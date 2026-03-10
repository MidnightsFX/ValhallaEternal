using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ValhallEternal.modules;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.common
{
    public static class DataObjects {
        public static readonly string CustomLevelZKey = "VELevel";
        public static readonly string CustomDataKey = "VE_DATA";
        public static IDeserializer yamldeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        public static ISerializer yamlserializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build();
        public static ISerializer yamlserializerJsonCompat = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).JsonCompatible().Build();

        public static Sprite boonbackground;
        public static Sprite hastenDeath;

        public static void LoadAssets()
        {
            boonbackground = ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/bottom_border_divider.png");
            hastenDeath = ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/hastenDeath.png");

            boons.HastenTheInevitable.AddHastenDeathStatus();
        }

        const string color_increase = "#b9f2ff";
        const string color_decrease = "#ff4040";

        public static readonly string HarvestingBonusColor = "#ffc87c";
        public static readonly string FishBountyBonusColor = "#619CFF";

        public static readonly string None = "None";

        public enum DisplayStyle
        {
            Numeric,
            Roman,
            Nordic
        }

        public enum PrestigeEffect {
            Wings,
            Footprints,
            Aura,
            LevelColor,
            Title
        }

        public enum Oaths
        {
            DamageTakenIncrease,
            DealLessBluntDamage,
            DealLessPierceDamage,
            DealLessSlashDamage,
            DealLessAllDamage,
            LowerSkillGainBow,
            LowerSkillGainSword,
            LowerSkillGainClub,
            LowerSkillGainPolearms,
            LowerSkillGainKnives,
            LowerSkillGainRun,
            LowerSkillGainSneak,
            LowerSkillGainBloodMagic,
            LowerSkillGainElementalMagic,
            LowerSkillGainCrossbow,
            LowerSkillGainSpears,
            LowerSkillGainAxes,
            ReducePlayerHealthPercent,
            ReducePlayerStaminaPercent,
            ReducePlayerEitrPercent,
            ReducePlayerCarryWeight,
            NoBows,
            NoCrossbows,
        }

        public enum Boons
        {
            IncreasePickableYields,
            SeedsGrowEverywhere,
            QualityNourishment,
            FishingProsperity,
            GefjunFeasts,
            RandomXPBonus,
            KnowledgeIsPower,
            HuntressArrowReturn,
            SwiftShadow,
            PowerfulShield,
            ExtraLoot,
            IncreaseEitrRegen,
            IncreaseStaminaRegen,
            ReturnStaminaOnDamage,
            ReturnEitrOnDamage,
            ReduceDodgeCost,
            BladeboundKnowledge,
            StormboundRage,
            IncreaseBaseEitr,
            IncreaseBaseStamina,
            IncreaseHeatResistance,
            MovementSpeedOnKill,
            ReduceWet,
            IncreaseMeleeDamage,
            Everwatchful,
            BrutalDefiance,
            DamageAgainstUndead,
            StrongerTowerBlock,
            DedicationToTheBlade,
            WealthOfAges,
            BuiltDifferent,
            GoddessOfWar,
            SeidrOfPlenty,
            PerfectForm,
            BalanceOfTheJotunn,
            BalanceOfTheAesir,
            HastenTheInevitable,
            ReduceFirePoison,
            HungerForKnowledge,
            BasicProtection,
            ArrowCatcher
        }

        public static readonly List<Oaths> DamageReductionOaths = new List<Oaths>() {
            Oaths.DealLessBluntDamage,
            Oaths.DealLessPierceDamage,
            Oaths.DealLessSlashDamage
        };
        public static readonly List<Oaths> ReducedSkillGainOaths = new List<Oaths>() {
            Oaths.LowerSkillGainBow,
            Oaths.LowerSkillGainSword,
            Oaths.LowerSkillGainClub,
            Oaths.LowerSkillGainPolearms,
            Oaths.LowerSkillGainKnives,
            Oaths.LowerSkillGainRun,
            Oaths.LowerSkillGainSneak,
            Oaths.LowerSkillGainElementalMagic,
            Oaths.LowerSkillGainBloodMagic,
            Oaths.LowerSkillGainCrossbow,
            Oaths.LowerSkillGainSpears,
            Oaths.LowerSkillGainAxes,
        };

        public static string LocalizeOath(Oaths oath) {
            return $"$ve_{oath}";
        }

        public static string LocalizeOathDesc(Oaths oath, float value = 0) {
            if (value > 0) { return string.Format($"$ve_{oath}_desc", value); }
            return $"$ve_{oath}_desc_noval";
        }

        public static string LocalizeBoon(Boons boon) {
            return $"$ve_{boon}";
        }

        public static string LocalizeBoonDesc(Boons boon, float value = 0) {
            if (value > 0) { return string.Format($"$ve_{boon}_desc", value); }
            return $"$ve_{boon}_desc_noval";
        }

        public interface IProbability
        {
            public string Name { get; set; }
            public float SelectionWeight { get; set; }
        }

        public class LevelTextGradiant
        {
            public string TopLeft { get; set; }
            public string BottomLeft { get; set; }
            public string TopRight { get; set; }
            public string BottomRight { get; set; }
        }

        public class PlayerLevelConfiguration {
            public LevelTextGradiant TextColors { get; set; }
            public DisplayStyle DisplayStyle { get; set; }
            public int Level { get; set; }
            public Dictionary<Oaths, float> DifficultyOaths { get; set; }
            public Dictionary<Boons, float> DifficultyBoons { get; set; }
        }

        public class CompositePlayerConfig
        {
            public int PlayerLevel { get; set; } = 0;
            public bool ReduceSkillGainsActive { get; set; } = false;
            public bool DealReductedDamageActive { get; set; } = false;
            public Dictionary<Oaths, float> TotalOaths { get; set; } = new Dictionary<Oaths, float>();
            public Dictionary<Boons, float> TotalBoons { get; set; } = new Dictionary<Boons, float>();
            public Dictionary<PrestigeEffect, string> ActiveEffectsForPlayer { get; set; } = new Dictionary<PrestigeEffect, string>();
            public Dictionary<PrestigeEffect, List<string>> AvailableEffectsForPlayer { get; set; } = new Dictionary<PrestigeEffect, List<string>>() {
                {PrestigeEffect.Wings, new List<string>() { DataObjects.None } },
                {PrestigeEffect.Aura, new List<string>() { DataObjects.None } },
            };

            public bool HasOath(Oaths oath, out float OathValue)
            {
                OathValue = 0;
                if (TotalOaths.ContainsKey(oath)) {
                    OathValue = TotalOaths[oath];
                    return true;
                }
                return false;
            }

            public bool HasOath(Oaths oath)
            {
                if (TotalOaths.ContainsKey(oath)) {
                    return true;
                }
                return false;
            }

            public bool HasBoon(Boons boon, out float BoonValue)
            {
                BoonValue = 0;
                if (TotalBoons.ContainsKey(boon))
                {
                    BoonValue = TotalBoons[boon];
                    return true;
                }
                return false;
            }

            public bool HasBoon(Boons boon) {
                if (TotalBoons.ContainsKey(boon)) {
                    return true;
                }
                return false;
            }
        }

        [Serializable]
        public class PlayerLevelData
        {
            [DataMember]
            public int PlayerLevel { get; set; }
            [DataMember]
            public Dictionary<Oaths, float> PlayerOaths { get; set; }
            [DataMember]
            public Dictionary<Boons, float> PlayerBoons { get; set; }
            [DataMember]
            public Dictionary<PrestigeEffect, string> ActiveEffectsForPlayer { get; set; }
            [DataMember]
            public Dictionary<PrestigeEffect, List<string>> AvailableEffectsForPlayer { get; set; }
        }

        public class PlayerResetData
        {
            public float ResetSkillPercentage { get; set; } = .5f;
            public bool ResetKnownRecipes { get; set; } = true;
            public bool TeleportToSpawn { get; set; } = false;
            public int PrestigeLevelsGained { get; set; } = 1;
        }

        public class PrestigeEffectDetails {
            public int LevelRequirement { get; set; }
            public Dictionary<Oaths, float> PlayerOathRequirements { get; set; }
            public Dictionary<Boons, float> PlayerBoonRequirements { get; set; }
            public PrestigeEffect EffectType { get; set; }
            public string EffectValue { get; set; }

            public bool PlayerMeetsPrestigeRequirements() {
                bool includePrestigeReward = false;
                bool boonRequirementsMet = false;
                bool oathRequirementsMet = false;
                // Check boon requirements
                if (PlayerBoonRequirements != null && PlayerBoonRequirements.Count > 0) {
                    foreach (KeyValuePair<Boons, float> kvp in PlayerBoonRequirements) {
                        if (PlayerData.HasBoonWithValue(kvp.Key, out float value) && value > kvp.Value) {
                            boonRequirementsMet = true;
                        }
                    }
                } else {
                    boonRequirementsMet = true;
                }
                // check oath requirements
                if (PlayerOathRequirements != null && PlayerOathRequirements.Count > 0) {
                    foreach (KeyValuePair<Oaths, float> kvp in PlayerOathRequirements) {
                        if (PlayerData.HasOathWithValue(kvp.Key, out float value) && value > kvp.Value) {
                            oathRequirementsMet = true;
                        }
                    }
                } else {
                    oathRequirementsMet = true;
                }
                // Check player level
                if (LevelRequirement <= PlayerData.localPlayerConfig.PlayerLevel && boonRequirementsMet && oathRequirementsMet) {
                    includePrestigeReward = true;
                }
                return includePrestigeReward;
            }
        }

        [Serializable]
        public class Sacrifice
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public PlayerResetData ResetPlayer { get; set; }
            public Dictionary<string, int> ItemRequirements { get; set; }
            public List<string> PlayerKeyRequirements { get; set; }
            public Dictionary<Oaths, float> PlayerOathRequirements { get; set; }
            public Dictionary<Boons, float> PlayerBoonRequirements { get; set; }
            [DataMember]
            public Dictionary<Oaths, float> PlayerOathChanges { get; set; }
            [DataMember]
            public Dictionary<Boons, float> PlayerBoonsChanges { get; set; }
            public Dictionary<Boons, float> PlayerBoonLimit { get; set; }
            public List<PrestigeEffectDetails> PrestigeOptions { get; set; }

            public string GetPlayerRequirementsDescription(bool includeOathsInDescription = true, bool includeBoonsInDescription = true, bool includeKeysInDescription = false, bool includeItemReference = true)
            {
                StringBuilder sb = new StringBuilder();
                if (PlayerOathRequirements != null && PlayerOathRequirements.Count > 0 || PlayerBoonRequirements != null && PlayerBoonRequirements.Count > 0 || PlayerKeyRequirements != null && PlayerKeyRequirements.Count > 0)
                {
                    sb.AppendLine(Localization.instance.Localize("$ve_requires"));
                }

                if (includeOathsInDescription && PlayerOathRequirements != null && PlayerOathRequirements.Count > 0) {
                    foreach (KeyValuePair<Oaths, float> oath in PlayerOathRequirements)
                    {
                        sb.AppendLine(Localization.instance.Localize($"{LocalizeOath(oath.Key)} <color={color_decrease}>-($ve_oath)-</color> $ve_level_required {oath.Value}"));
                    }
                }
                if (sb.Length > 0) { sb.AppendLine(""); }
                if (includeBoonsInDescription && PlayerBoonRequirements != null && PlayerBoonRequirements.Count > 0) {
                    foreach (KeyValuePair<Boons, float> boon in PlayerBoonRequirements)
                    {
                        sb.AppendLine(Localization.instance.Localize($"{LocalizeBoon(boon.Key)} <color={color_increase}>-($ve_boon)-</color> $ve_level_required {boon.Value}"));
                    }
                    sb.AppendLine("");
                }
                if (includeKeysInDescription && PlayerKeyRequirements != null && PlayerKeyRequirements.Count > 0) {
                    foreach (string key in PlayerKeyRequirements)
                    {
                        sb.AppendLine(Localization.instance.Localize($"$ve_playerkey {key}"));
                    }
                    sb.AppendLine("");
                }
                if (includeItemReference && ItemRequirements != null && ItemRequirements.Count > 0) {
                    sb.AppendLine(Localization.instance.Localize("$ve_item_req"));
                }

                return sb.ToString();
            }

            public string GetChangesGrantedDescription()
            {
                StringBuilder sb = new StringBuilder();

                if (PrestigeOptions != null && PrestigeOptions.Count > 0) {
                    foreach(PrestigeEffectDetails ped in PrestigeOptions) {
                        // Skip already registered effects
                        if (PlayerData.PlayerHasPrestigeEffect(ped.EffectType, ped.EffectValue) == true) { continue; }
                        if (ped.PlayerMeetsPrestigeRequirements()) {
                            switch (ped.EffectType) {
                                case PrestigeEffect.Wings:
                                    sb.AppendLine($"{Localization.instance.Localize($"$ve_prestige_wings_granted $ve_{ped.EffectValue}_local")}");
                                    break;
                                case PrestigeEffect.Footprints:
                                    sb.AppendLine($"{Localization.instance.Localize($"$ve_prestige_footprints_granted $ve_{ped.EffectValue}_local")}");
                                    break;
                                case PrestigeEffect.Aura:
                                    sb.AppendLine($"{Localization.instance.Localize($"$ve_prestige_aura_granted $ve_{ped.EffectValue}_local")}");
                                    break;
                                case PrestigeEffect.LevelColor:
                                    sb.AppendLine($"{Localization.instance.Localize($"$ve_prestige_levelcolor_granted $ve_{ped.EffectValue}_local")}");
                                    break;
                                case PrestigeEffect.Title:
                                    sb.AppendLine($"{Localization.instance.Localize($"$ve_prestige_title_granted $ve_{ped.EffectValue}_local")}");
                                    break;
                            }
                        }
                    }
                }

                if (PlayerOathChanges != null && PlayerOathChanges.Count > 0) {
                    if (sb.Length > 0) { sb.AppendLine(""); }
                    sb.AppendLine($"{Localization.instance.Localize("$ve_oath_changes")}");
                    foreach (KeyValuePair<Oaths, float> kvp in PlayerOathChanges) {
                        if (kvp.Value > 0) {
                            sb.AppendLine(Localization.instance.Localize($"  « <size=18>{LocalizeOath(kvp.Key)}</size> <color={color_increase}>+{kvp.Value}</color> | {LocalizeOathDesc(kvp.Key, kvp.Value)}\n"));
                        } else {
                            sb.AppendLine(Localization.instance.Localize($"  « <size=18>{LocalizeOath(kvp.Key)}</size> <color={color_decrease}>-{kvp.Value}</color> | {LocalizeOathDesc(kvp.Key, kvp.Value)}\n"));
                        }
                    }
                }

                if (PlayerBoonsChanges != null && PlayerBoonsChanges.Count > 0) {
                    // spacing between boon and oath changes if both are defined
                    if (PlayerOathChanges != null && PlayerOathChanges.Count > 0) {
                        sb.AppendLine("");
                    }

                    sb.AppendLine($"{Localization.instance.Localize("$ve_boon_changes")}");
                    foreach (KeyValuePair<Boons, float> kvp in PlayerBoonsChanges) {
                        if (kvp.Value > 0)
                        {
                            sb.AppendLine(Localization.instance.Localize($"  » <size=18>{LocalizeBoon(kvp.Key)}</size> <color={color_increase}>+{kvp.Value}</color> | {LocalizeBoonDesc(kvp.Key)}\n"));
                        } else {
                            sb.AppendLine(Localization.instance.Localize($"  » <size=18>{LocalizeBoon(kvp.Key)}</size> <color={color_decrease}>-{kvp.Value}</color> | {LocalizeBoonDesc(kvp.Key)}\n"));
                        }
                    }
                }
                return sb.ToString();
            }

            public string GetResetDetails()
            {
                StringBuilder sb = new StringBuilder();
                if (ResetPlayer != null)
                {
                    sb.AppendLine(Localization.instance.Localize("$ve_this_prestige"));
                    if (ResetPlayer.ResetSkillPercentage > 0) {
                        sb.AppendLine(Localization.instance.Localize($"$ve_skill_decrease <color={color_decrease}>{ResetPlayer.ResetSkillPercentage*100}%</color>"));
                    }
                    if (ResetPlayer.ResetKnownRecipes) {
                        sb.AppendLine(Localization.instance.Localize("$ve_forget_all_recipes"));
                    }
                    if (ResetPlayer.TeleportToSpawn) {
                        sb.AppendLine(Localization.instance.Localize("$ve_teleport_to_spawn"));
                    }
                    if (ResetPlayer.PrestigeLevelsGained > 0) {
                        sb.AppendLine(Localization.instance.Localize($"$ve_you_gain <color={color_increase}>{ResetPlayer.PrestigeLevelsGained}</color> $ve_prestige_levels"));
                    }
                    if (ResetPlayer.PrestigeLevelsGained < 0) {
                        sb.AppendLine(Localization.instance.Localize($"$ve_you_loose <color={color_decrease}>{ResetPlayer.PrestigeLevelsGained}</color> $ve_prestige_levels"));
                    }
                }


                return sb.ToString();
            }

            public string GetTotalDescription(bool showreqboons = true, bool showreqoaths = true) {
                string reqdesc = GetPlayerRequirementsDescription(includeOathsInDescription: showreqoaths, includeBoonsInDescription: showreqboons);
                string prestige = GetResetDetails();
                string totaldesc = "";

                totaldesc += GetChangesGrantedDescription();
                if (prestige.Length > 0) { totaldesc += "\n"; }
                totaldesc += prestige;
                if (reqdesc.Length > 0) { totaldesc += "\n"; }
                totaldesc += reqdesc;
                return totaldesc;
            }
        }
    }
}
