using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.common
{
    public static class EternalConfigData
    {
        public static Dictionary<int, PlayerLevelConfiguration> activePlayerConfig = defaultPlayerConfig;

        internal static Dictionary<int, PlayerLevelConfiguration> defaultPlayerConfig = new Dictionary<int, PlayerLevelConfiguration>()
        {
            {1, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  5f}
                    }
                }
            },
            {2, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  10f}
                    }
                }
            },
            {3, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  15f}
                    }
                }
            },
            {4, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  20f}
                    }
                }
            },
            {5, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  25f}
                    }
                }
            },
            {6, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  30f}
                    }
                }
            },
            {7, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  35f}
                    }
                }
            },
            {8, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  40f}
                    }
                }
            },
            {9, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  45f}
                    }
                }
            },
            {10, new PlayerLevelConfiguration()
                {
                    TextColors = new LevelTextGradiant()
                    {
                        TopLeft = "#FFB431FF",
                        BottomLeft = "#FF6900FF",
                        TopRight = "#E7A500FF",
                        BottomRight = "#EE5B00FF",
                    },
                    DisplayStyle = DisplayStyle.Numeric,
                    Level = 1,
                    DifficultyOaths = new Dictionary<Oaths, float>()
                    {
                        {Oaths.DamageTaken,  50f}
                    }
                }
            },
        };

        // set from config file after load
    }
}
