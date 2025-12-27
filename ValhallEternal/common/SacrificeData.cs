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
                    Name = "Tribute to Gefjun",
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
                    Name = "Greater Tribute to Gefjun",
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
                    Name = "Offering of Fish for Gefjun",
                    Description = "May no fish escape your grasp.",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "fish1", 10 },
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
                    Name = "Tribute to Vör",
                    Description = "A tribute to Vör. May she grant you knowledge in return.",
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
                    Name = "Greater Tribute to Vör",
                    Description = "A large tribute for knowledge, makes weapon usage give other skill gains.",
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
                    Name = "Devotion to Vör",
                    Description = "Knowledge is interconnected, gain increased skill level with weapon skills. TODO",
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
                        { Boons.KnowledgeIsPower, 2 },
                    }
                }
                } }
            },
            { Deity.Skaldi, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Tribute to Skaði",
                    Description = "A tribute to Skaði. Under the cover of darkness she will hasten your travels. TODO",
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
                    Name = "Greater Tribute to Skaði",
                    Description = "The great huntress returns arrows to her dedicated followers. TODO",
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
                    Name = "Devotion to Skaði",
                    Description = "Skaði teaches you to channel the rage of the storm, you deal bonus damage during storms. TODO",
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
                    Name = "Tribute to Freya",
                    Description = "A tribute to Freya. Seiðr of Plenty provides increased eitr regen. TODO",
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
                    Name = "Greater Tribute to Freya",
                    Description = "Taking damage gives you some stamina. TODO",
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
                        { Boons.ReturnStaminaOnDamage, 4 }
                    }
                } },
                { "T3",new() {
                    Name = "Devotion to Freya",
                    Description = "Skaði teaches you to channel the rage of the storm, you deal bonus damage during storms. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyCultist_Hildir", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.ReturnStaminaOnDamage, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.StormboundRage, 2 },
                    }
                }
                } }
            },
            { Deity.Hel, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Tribute to Hel",
                    Description = "A tribute to Hel. You become more accustomed to extreme heat. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyCharredMelee", 2 },
                        { "TrophyCharredArcher", 2 },
                        { "TrophyVolture", 2 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseHeatResistance, 2 },
                    }
                } },
                { "T2",new() {
                    Name = "Greater Tribute to Hel",
                    Description = "Taking damage gives you some stamina and eitr. TODO",
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
                        { Boons.ReturnStaminaOnDamage, 4 },
                        { Boons.ReturnEitrOnDamage, 4 }
                    }
                } },
                { "T3",new() {
                    Name = "Devotion to Hel",
                    Description = "Hel hastens your movement with every kill. TODO",
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
                    Name = "Tribute to Baldur",
                    Description = "A tribute to Baldur. Shields become more powerful in your hands.  TODO",
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
                    Name = "Greater Tribute to Baldur",
                    Description = "Taking damage gives you some stamina and eitr. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyMorgen", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.PowerfulShield, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.ReturnStaminaOnDamage, 4 },
                        { Boons.ReturnEitrOnDamage, 4 }
                    }
                } },
                { "T3",new() {
                    Name = "Devotion to Baldur",
                    Description = "Hel hastens your movement with every kill. TODO",
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
            { Deity.Syn, new Dictionary<string,Sacrifice>() {
                { "T1",new() {
                    Name = "Tribute to Syn",
                    Description = "A tribute to Syn. Allows you to shield bash, blocking repeatedly in a short time causes a retribution strike. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGrowth", 2 },
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.BrutalDefiance, 3 },
                    }
                } },
                { "T2",new() {
                    Name = "Greater Tribute to Syn",
                    Description = "Greater Tribute to Syn. Reduces damage you take from backstab and when staggered. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyDeathsquito", 3 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.Everwatchful, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.Everwatchful, 5 },
                    }
                } },
                { "T3",new() {
                    Name = "Devotion to Syn",
                    Description = "Devotion to Syn, grants immense power in melee. You can't use bows or crossbows. TODO",
                    ItemRequirements = new Dictionary<string, int>()
                    {
                        { "TrophyGoblinBruteBrosShaman", 1 },
                        { "TrophyGoblinBruteBrosBrute", 1 },
                    },
                    PlayerBoonRequirements = new Dictionary<Boons, float>()
                    {
                        { Boons.Everwatchful, 1 }
                    },
                    PlayerBoonsChanges = new Dictionary<Boons, float>()
                    {
                        { Boons.IncreaseMeleeDamage, 20 },
                    },
                    PlayerOathChanges = new Dictionary<Oaths, float>()
                    {
                        { Oaths.NoBows, 1 },
                        { Oaths.NoCrossbows, 1 },
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
