using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
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
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyBoar", 10 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreasePickableYields, 2 }
                    }
                } },
                { "T2",new() {
                    Name = "Quality Nourishment",
                    Description = "Gefjun enhances your food, making every bite more nurishing.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyBoar", 8 },
                        { "TrophyDeer", 5 },
                        { "TrophyNeck", 2 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreasePickableYields, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.QualityNourishment, 2 }
                    }
                } },
                { "T3",new() {
                    Name = "Fishing Prosperity",
                    Description = "May no fish escape your grasp.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "Fish1", 10 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreasePickableYields, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.FishingProsperity, 2 },
                    }
                } },
                { "T4",new() {
                    Name = "Life Devotion to Gefjun",
                    Description = "Grants access to increasing tiers of Gefjuns special feasts. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyFader", 1 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.GefjunFeasts, 1 },
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>()
                    {
                        { Oaths.DamageTakenIncrease, 5f }
                    },
                    ResetPlayer = new PlayerResetData {
                        ResetSkillPercentage = 0.5f,
                        TeleportToSpawn = true,
                        PrestigeLevelsGained = 1
                    }
                } },
                }
            },
            { Deity.Vor, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Wisdom of the Ages",
                    Description = "A tribute to Vör. Grants you a chance to recieve bonus skill experiance.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGreydwarf", 10 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.RandomXPBonus, 1 }
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
                        { Boons.HungerForKnowledge, 2 },
                    }
                }
                } }
            },
            { Deity.Skaldi, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Swift is the Shadow",
                    Description = "A tribute to Skaði. Under the cover of darkness or in mountains she hastens your sprint.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyUlv", 5 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.SwiftShadow, 1 }
                    }
                } },
                { "T2",new() {
                    Name = "Huntress Prowess",
                    Description = "Skaði gives you a chance to gain arrows from creature kills.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyWolf", 10 },
                        { "TrophyFenring", 2 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.SwiftShadow, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.HuntressArrowReturn, 2 }
                    }
                } },
                { "T3",new() {
                    Name = "Stormbound Rage",
                    Description = "Skaði teaches you to channel the rage of the storm, you deal bonus lightning damage during storms.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyCultist_Hildir", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.HuntressArrowReturn, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.StormboundRage, 2 },
                    }
                }
                } }
            },
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
                    }
                } },
                { "T2",new() {
                    Name = "Goddess of War",
                    Description = "Freya imbues some knowledge of battle to you, increasing your stamina regeneration.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGjall", 3 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseEitrRegen, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseStaminaRegen, 3 }
                    }
                } },
                { "T3",new() {
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
                    }
                }
                } }
            },
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
                    }
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
                        { Boons.ReturnStaminaOnDamage, 4 }
                    }
                } },
                { "T2.1",new() {
                    Name = "Balance of the Aesir",
                    Description = "Taking damage returns some stamina.",
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
                        { Boons.ReturnEitrOnDamage, 4 },
                    }
                } },
                { "T3",new() {
                    Name = "Haste the inevitable",
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
                    }
                }
                } }
            },
            { Deity.Baldur, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Shield Wall",
                    Description = "A tribute to Baldur. Shields become more powerful in your hands.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyAbomination", 2 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.PowerfulShield, 3 },
                    }
                } },
                { "T2",new() {
                    Name = "Light of Baldur",
                    Description = "You deal increased damage against undead.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyDraugr", 6 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.PowerfulShield, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.DamageAgainstUndead, 2 }
                    }
                } },
                { "T3",new() {
                    Name = "Purify",
                    Description = "Significantly reduces poison duration, reduces fire damage taken. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyWraith", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.DamageAgainstUndead, 1 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.ReduceFirePoison, 2 },
                    }
                }
                } }
            },
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
                        { Boons.BuiltDifferent, 3 },
                    }
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
                    }
                } },
                { "T3",new() {
                    Name = "Deadication to the Blade",
                    Description = "Increases melee damage done, reduces your ranged damage done.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGoblinBruteBrosShaman", 1 },
                        { "TrophyGoblinBruteBrosBrute", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.DedicationToTheBlade, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.DedicationToTheBlade, 1 },
                        { Boons.IncreaseMeleeDamage, 3 },
                    }
                }
                } }
            }
        };

        public static void SetupSacrificeData()
        {
            AllSacrifices = DefaultSacrifices;
        }
    }
}
