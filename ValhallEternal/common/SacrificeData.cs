using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using static UnityEngine.PostProcessing.BloomModel;
using static ValhallEternal.common.DataObjects;
using static ValhallEternal.common.Deities;

namespace ValhallEternal.common
{
    public static class SacrificeData
    {
        public static Dictionary<Deity, Dictionary<string, Sacrifice>> AllSacrifices = new Dictionary<Deity, Dictionary<string, Sacrifice>>();

        internal static Dictionary<Deity, Dictionary<string, Sacrifice>> DefaultSacrifices = new Dictionary<Deity, Dictionary<string, Sacrifice>>() {
            { Deity.Gefjun, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Bounty of the lands",
                    Description = "A tribute for the goddess of harvests, may your harvests be plenty.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyBoar", 10 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.IncreasePickableYields, 2 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.IncreasePickableYields, 100 }
                    }
                } },
                { "T2",new() {
                    Name = "Quality Nourishment",
                    Description = "Gefjun enhances your food, making every bite more nurishing.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyBoar", 8 },
                        { "TrophyDeer", 5 },
                        { "TrophyNeck", 2 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>() {
                        { Boons.IncreasePickableYields, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.QualityNourishment, 2 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.QualityNourishment, 100 }
                    }
                } },
                { "T3",new() {
                    Name = "Fishing Prosperity",
                    Description = "May no fish escape your grasp.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "Fish1", 10 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>() {
                        { Boons.IncreasePickableYields, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.FishingProsperity, 2 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.FishingProsperity, 100 }
                    }
                } },
                { "T4",new() {
                    Name = "Devotion for Gefjun",
                    Description = "Provides a small amount of permenant armor. This is a prestige level.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyEikthyr", 1 },

                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.BasicProtection, 1 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.BasicProtection, 20 }
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>() {
                        { Oaths.DamageTakenIncrease, 5f }
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>() {
                        { Boons.FishingProsperity, 1 }
                    },
                    ResetPlayer = new PlayerResetData {
                        ResetSkillPercentage = 0.5f,
                        TeleportToSpawn = true,
                        PrestigeLevelsGained = 1
                    },
                    PrestigeOptions = new List<DataObjects.PrestigeEffectDetails>() {
                        { new DataObjects.PrestigeEffectDetails() {
                            EffectType = PrestigeEffect.Aura,
                            EffectValue = "natureAura",
                            LevelRequirement = 5,
                        }}
                    }
                    
                } },
                }
            },
            { Deity.Vor, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Wisdom of the Ages",
                    Description = "A tribute to Vör. Grants you a chance to recieve bonus skill experiance.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyGreydwarf", 10 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.RandomXPBonus, 1 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.RandomXPBonus, 100 }
                    }
                } },
                { "T2",new() {
                    Name = "Bladebound Knowledge",
                    Description = "Combat skills sometimes provide gains to non-combat skills.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGreydwarfBrute", 2 },
                        { "TrophyGreydwarfShaman", 2 },
                        { "TrophyGhost", 2 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.RandomXPBonus, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.BladeboundKnowledge, 2 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.BladeboundKnowledge, 100 }
                    }
                } },
                { "T3",new() {
                    Name = "Hunger For Knowledge",
                    Description = "Discovering a recipe provides a small amount of experiance to a random skill.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyFrostTroll", 10 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.BladeboundKnowledge, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.HungerForKnowledge, 5 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.HungerForKnowledge, 100 }
                    }
                }
                } }
            },
            { Deity.Baldur, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Shield Wall",
                    Description = "A tribute to Baldur. Shields become more powerful in your hands.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyAbomination", 2 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.PowerfulShield, 3 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.PowerfulShield, 100 }
                    }
                } },
                { "T2",new() {
                    Name = "Light of Baldur",
                    Description = "You deal increased damage against undead.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyDraugr", 6 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>() {
                        { Boons.PowerfulShield, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.DamageAgainstUndead, 2 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.DamageAgainstUndead, 100 }
                    }
                } },
                { "T3",new() {
                    Name = "Purify",
                    Description = "Significantly reduces poison duration, reduces fire damage taken. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyEikthyr", 1 },
                        { "TrophyTheElder", 1 },
                        { "TrophyBonemass", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.DamageAgainstUndead, 1 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.ReduceFirePoison, 3 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.ReduceFirePoison, 80 }
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>()
                    {
                        { Oaths.DamageTakenIncrease, 5f }
                    },
                    ResetPlayer = new PlayerResetData {
                        ResetSkillPercentage = 0.5f,
                        TeleportToSpawn = true,
                        PrestigeLevelsGained = 1
                    },
                    PrestigeOptions = new List<DataObjects.PrestigeEffectDetails>() {
                        { new DataObjects.PrestigeEffectDetails() {
                            EffectType = PrestigeEffect.Aura,
                            EffectValue = "lightAura",
                            LevelRequirement = 10,
                        }}
                    }
                }
                } }
            },
            { Deity.Skaldi, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Swift is the Shadow",
                    Description = "A tribute to Skaði. Under the cover of darkness or in mountains she hastens your sprint.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyWolf", 10 },
                        { "TrophyUlv", 5 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.SwiftShadow, 1 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.SwiftShadow, 100 }
                    }
                } },
                { "T2",new() {
                    Name = "Stormbound Rage",
                    Description = "Skaði teaches you to channel the rage of the storm, you deal bonus lightning damage during storms.",
                    ItemRequirements = new Dictionary<string, int>() {
                        { "TrophyCultist_Hildir", 1 },
                        { "TrophyFenring", 2 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>() {
                        { Boons.SwiftShadow, 2 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>() {
                        { Boons.StormboundRage, 2 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.StormboundRage, 100 }
                    }
                }},
                { "T3",new() {
                    Name = "Huntress Prowess",
                    Description = "Skaði gives you a chance to gain arrows from creature kills.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyEikthyr", 1 },
                        { "TrophyTheElder", 1 },
                        { "TrophyBonemass", 1 },
                        { "TrophyDragonQueen", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.SwiftShadow, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.HuntressArrowReturn, 2 }
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>()
                    {
                        { Oaths.DamageTakenIncrease, 5f }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.HuntressArrowReturn, 100 }
                    },
                    ResetPlayer = new PlayerResetData {
                        ResetSkillPercentage = 0.5f,
                        TeleportToSpawn = true,
                        PrestigeLevelsGained = 1
                    },
                    PrestigeOptions = new List<DataObjects.PrestigeEffectDetails>() {
                        { new DataObjects.PrestigeEffectDetails() {
                            EffectType = PrestigeEffect.Aura,
                            EffectValue = "frostAura",
                            LevelRequirement = 10,
                        }}
                    }
                }},
            }},
            { Deity.Syn, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Built Different",
                    Description = "A tribute to Syn. Increases how much stagger damage you can take before becoming staggered.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGrowth", 2 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.BuiltDifferent, 2 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.BuiltDifferent, 100 }
                    },
                } },
                { "T2",new() {
                    Name = "Wealth of the Ages",
                    Description = "Greater Tribute to Syn. Gives a chance to loot valuable items when killing creatures.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyDeathsquito", 3 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.BuiltDifferent, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.WealthOfAges, 1 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.WealthOfAges, 100 }
                    },
                } },
                { "T3",new() {
                    Name = "Dedication to the Blade",
                    Description = "Increases melee damage done, reduces your ranged damage done.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGoblinBruteBrosShaman", 1 },
                        { "TrophyGoblinBruteBrosBrute", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.WealthOfAges, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.DedicationToTheBlade, 2.5f },
                        { Boons.IncreaseMeleeDamage, 5 },
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>()
                    {
                        { Oaths.DamageTakenIncrease, 5f }
                    },
                    ResetPlayer = new PlayerResetData {
                        ResetSkillPercentage = 0.5f,
                        TeleportToSpawn = true,
                        PrestigeLevelsGained = 1
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.DedicationToTheBlade, 100 },
                        { Boons.IncreaseMeleeDamage, 200 },
                    },
                }}
            }},
            { Deity.Freya, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Seiðr of Plenty",
                    Description = "Freya invokes magic to increase your Eitr reserves and your Eitr regeneration.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophySeeker", 5 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseEitrRegen, 1 },
                        { Boons.IncreaseBaseEitr, 3 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.IncreaseEitrRegen, 100 },
                        { Boons.IncreaseBaseEitr, 200 },
                    },
                } },
                { "T1.1",new() {
                    Name = "Goddess of War",
                    Description = "Freya imbues some knowledge of battle to you, increasing your stamina regeneration.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGjall", 3 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseStaminaRegen, 3 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.IncreaseStaminaRegen, 100 },
                    },
                } },
                { "T2",new() {
                    Name = "Perfect Form",
                    Description = "Freya teach you precision in the art of war. This gives you an innate chance to deal critical damage.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyCultist_Hildir", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseStaminaRegen, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.PerfectForm, 2 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.PerfectForm, 100 },
                    },
                }}
            }},
            { Deity.Hel, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Hellfire Adaptation",
                    Description = "A tribute to Hel. Your resistance to extreme heat increases.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyCharredMelee", 2 },
                        { "TrophyCharredArcher", 2 },
                        { "TrophyVolture", 2 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseHeatResistance, 3 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.IncreaseHeatResistance, 100 },
                    },
                } },
                { "T2",new() {
                    Name = "Balance of the Jotunn",
                    Description = "Taking damage returns some eitr.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyMorgen", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseHeatResistance, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.ReturnStaminaOnDamage, 3 }
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.ReturnStaminaOnDamage, 100 },
                    },
                } },
                { "T2.1",new() {
                    Name = "Balance of the Aesir",
                    Description = "Taking damage returns some stamina.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyFallenValkyrie", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseHeatResistance, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.ReturnEitrOnDamage, 3 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.ReturnEitrOnDamage, 100 },
                    },
                } },
                { "T3",new() {
                    Name = "Hasten the inevitable",
                    Description = "Hel hastens your movement after a kill",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyFallenValkyrie", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.ReturnStaminaOnDamage, 1 },
                        { Boons.ReturnEitrOnDamage, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.MovementSpeedOnKill, 2 },
                    },
                    PlayerBoonLimit = new Dictionary<Boons, float>() {
                        { Boons.MovementSpeedOnKill, 200 },
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>()
                    {
                        { Oaths.DamageTakenIncrease, 5f }
                    },
                    ResetPlayer = new PlayerResetData {
                        ResetSkillPercentage = 0.5f,
                        TeleportToSpawn = true,
                        PrestigeLevelsGained = 1
                    },
                    PrestigeOptions = new List<DataObjects.PrestigeEffectDetails>() {
                        { new DataObjects.PrestigeEffectDetails() {
                            EffectType = PrestigeEffect.Aura,
                            EffectValue = "fireAura",
                            LevelRequirement = 15,
                        }},
                        { new DataObjects.PrestigeEffectDetails() {
                            EffectType = PrestigeEffect.Aura,
                            EffectValue = "darkAura",
                            LevelRequirement = 20,
                        }}
                    }
                }}
            }}
        };

        public static void SetupSacrificeData()
        {
            // Load the default configuration
            AllSacrifices = DefaultSacrifices;
            try {
                UpdateYamlConfig(File.ReadAllText(ValConfig.sacrificeCfgPath));
            } catch (Exception e) {
                AllSacrifices = DefaultSacrifices;
                Logger.LogWarning($"There was an error updating the Sacrifice Data, defaults will be used. Exception: {e}");
            }
        }

        public static string YamlDefaultConfig() {
            var yaml = DataObjects.yamlserializer.Serialize(DefaultSacrifices);
            return yaml;
        }

        public static bool UpdateYamlConfig(string yaml) {
            try {
                AllSacrifices = DataObjects.yamldeserializer.Deserialize<Dictionary<Deity, Dictionary<string, Sacrifice>>>(yaml);
                Logger.LogDebug("Loaded new Sacrifice Data.");
            } catch (Exception ex) {
                Logger.LogError($"Failed to parse Sacrifices.yaml YAML: {ex.Message}");
                return false;
            }
            return true;
        }
    }
}
