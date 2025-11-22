using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ValhallEternal.common
{
    public static class DataObjects {
        public static readonly string CustomLevelZKey = "VELevel";
        public static readonly string CustomDataKey = "VEOath";
        internal static JsonSerializer serializer = new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore};
        internal static JsonSerializer compactSerializer = new JsonSerializer() {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
            };
        internal static JsonSerializerSettings compactSerializationSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
        };
        public static Dictionary<Diety, Sprite> DietyImages = new Dictionary<Diety, Sprite>();

        public enum Diety
        {
            Baldur,
            Hel,
            Gefjun,
            Skaldi,
            Freya,
            Vor
        }

        public enum DisplayStyle
        {
            Numeric,
            Roman,
            Nordic
        }

        public enum Oaths
        {
            DamageTaken = 0,
            DealLessBluntDamage = 1,
            DealLessPierceDamage = 2,
            DealLessSlashDamage = 3,
            LowerSkillGainBow = 4,
            LowerSkillGainSword = 5,
            LowerSkillGainClub = 6,
            LowerSkillGainPolearms = 7,
            LowerSkillGainKnives = 8,
            LowerSkillGainRun = 9,
            LowerSkillGainSneak = 10,
            LowerSkillGainBloodMagic = 11,
            LowerSkillGainElementalMagic = 12,
            ReducePlayerHealthPercent = 13,
            ReducePlayerStaminaPercent = 14,
            ReducePlayerCarryWeight = 15,
        }

        public enum Boons
        {
            IncreasePickableYields = 0,
            SeedsGrowEverywhere = 1,
            FoodForAll = 2,
            FishingProsperity = 3,
            RandomXPBonus = 4,
            KnowledgeIsPower = 5,
            HuntressArrowReturn = 6,
            SwiftShadow = 7,
            TowershieldPowerful = 8,
            ExtraLoot = 9,
            IncreaseEitrRegen = 10,
            IncreaseStaminaRegen = 11,
            ReturnStaminaOnDamage = 12,
            ReturnEitrOnDamage = 13
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
            public Dictionary<Oaths, float> TotalOaths { get; set; } = new Dictionary<Oaths, float>();
            public Dictionary<Boons, float> TotalBoons { get; set; } = new Dictionary<Boons, float>();
        }

        [Serializable]
        public class PlayerLevelData
        {
            [DataMember]
            public Dictionary<Oaths, float> PlayerOaths { get; set; }
            [DataMember]
            public Dictionary<Boons, float> PlayerBoons { get; set; }
        }

        public class PlayerResetData
        {
            public float ResetSkillPercentage { get; set; } = .5f;
            public bool ResetKnownRecipes { get; set; } = true;
            public bool TeleportToSpawn { get; set; } = false;
            public int PrestigeLevelsGained { get; set; } = 1;
        }

        [Serializable]
        public class Sacrifice
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool TriggerPrestige { get; set; }
            public PlayerResetData ResetPlayer { get; set; }
            public Dictionary<string, int> ItemRequirements { get; set; }
            public List<string> PlayerKeyRequirements { get; set; }
            public List<Oaths> PlayerOathRequirements { get; set; }
            public List<Boons> PlayerBoonRequirements { get; set; }
            [DataMember]
            public Dictionary<Oaths, float> PlayerOathChanges { get; set; }
            [DataMember]
            public Dictionary<Boons, float> PlayerBoonsChanges { get; set; }

            public string GetPlayerRequirements(bool includeOathsInDescription = true, bool includeBoonsInDescription = true, bool includeKeysInDescription = false)
            {
                StringBuilder sb = new StringBuilder();
                if (PlayerOathRequirements != null && PlayerOathRequirements.Count > 0 || PlayerBoonRequirements != null && PlayerBoonRequirements.Count > 0 || PlayerKeyRequirements != null && PlayerKeyRequirements.Count > 0)
                {
                    sb.AppendLine($"Requires the following.");
                }

                if (includeOathsInDescription && PlayerOathRequirements != null && PlayerOathRequirements.Count > 0) {
                    foreach (Oaths oath in PlayerOathRequirements)
                    {
                        sb.AppendLine($"Oath: {oath}");
                    }
                }
                if (includeBoonsInDescription && PlayerBoonRequirements != null && PlayerBoonRequirements.Count > 0) {
                    foreach (Boons boon in PlayerBoonRequirements)
                    {
                        sb.AppendLine($"Boon: {boon}");
                    }
                }
                if (includeKeysInDescription && PlayerKeyRequirements != null && PlayerKeyRequirements.Count > 0) {
                    foreach (string key in PlayerKeyRequirements)
                    {
                        sb.AppendLine($"PlayerKey {key}");
                    }
                }

                return sb.ToString();
            }

            public string GetOathChanges() {
                StringBuilder sb = new StringBuilder();
                if (PlayerOathChanges != null && PlayerOathChanges.Count > 0) {
                    sb.AppendLine($"The following Oath changes will happen.");
                    foreach (KeyValuePair<Oaths, float> kvp in PlayerOathChanges) {
                        if (kvp.Value > 0) {
                            sb.AppendLine($"{kvp.Key} will increase by +{kvp.Value}");
                        } else {
                            sb.AppendLine($"{kvp.Key} will decrease by -{kvp.Value}");
                        }
                    }
                }

                return sb.ToString();
            }

            public string GetBoonChanges()
            {
                StringBuilder sb = new StringBuilder();
                if (PlayerOathChanges != null && PlayerOathChanges.Count > 0)
                {
                    sb.AppendLine($"The following Boon changes will happen.");
                    foreach (KeyValuePair<Boons, float> kvp in PlayerBoonsChanges)
                    {
                        if (kvp.Value > 0)
                        {
                            sb.AppendLine($"{kvp.Key} will increase by +{kvp.Value}");
                        }
                        else
                        {
                            sb.AppendLine($"{kvp.Key} will decrease by -{kvp.Value}");
                        }
                    }
                }
                return sb.ToString();
            }

            public string GetResetDetails()
            {
                StringBuilder sb = new StringBuilder();
                if (TriggerPrestige == true && ResetPlayer != null)
                {
                    sb.AppendLine("This is a prestige increase.");
                    if (ResetPlayer.ResetSkillPercentage > 0) {
                        sb.AppendLine($"All skills will be reduced by: {ResetPlayer.ResetSkillPercentage}");
                    }
                    if (ResetPlayer.ResetKnownRecipes) {
                        sb.AppendLine("All known recipes will be forgotten.");
                    }
                    if (ResetPlayer.TeleportToSpawn) {
                        sb.AppendLine("You will be teleported to spawn.");
                    }
                    if (ResetPlayer.PrestigeLevelsGained > 0) {
                        sb.AppendLine($"You will gain {ResetPlayer.PrestigeLevelsGained} Prestige levels.");
                    }
                    if (ResetPlayer.PrestigeLevelsGained < 0) {
                        sb.AppendLine($"You will loose {ResetPlayer.PrestigeLevelsGained} Prestige levels.");
                    }
                }


                return sb.ToString();
            }
        }
    }
}
