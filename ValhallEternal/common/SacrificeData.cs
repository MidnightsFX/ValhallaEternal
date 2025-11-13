using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.common
{
    internal static class SacrificeData
    {
        internal static List<Sacrifice> AllSacrifices = DefaultSacrifices;

        static List<Sacrifice> DefaultSacrifices = new List<Sacrifice>()
        {
            new() {
                Name = "Tribute to Gefjun",
                Description = "A tribute for the god of harvests, may your harvests be plenty.",
                ItemRequirements = new Dictionary<string, int>()
                {
                    { "TrophyBoar", 10 },
                },
                PlayerBoonsChanges = new Dictionary<Boons, float>()
                {
                    { Boons.IncreasePickableYields, 2 }
                }
            },
            new() {
                Name = "Greater Tribute to Gefjun",
                Description = "A tribute for the god of harvests, may your harvests be plenty.",
                ItemRequirements = new Dictionary<string, int>()
                {
                    { "TrophyBoar", 8 },
                    { "TrophyDeer", 5 },
                    { "TrophyNeck", 2 },
                },
                PlayerBoonRequirements = new List<Boons>()
                {
                    { Boons.IncreasePickableYields }
                },
                PlayerBoonsChanges = new Dictionary<Boons, float>()
                {
                    { Boons.IncreasePickableYields, 1 },
                    { Boons.FoodForAll, 2 }
                }
            },
            new() {
                Name = "Offering of Fish for Gefjun",
                Description = "May no fish escape your grasp.",
                ItemRequirements = new Dictionary<string, int>()
                {
                    { "fish1", 10 },
                },
                PlayerBoonRequirements = new List<Boons>()
                {
                    { Boons.IncreasePickableYields }
                },
                PlayerBoonsChanges = new Dictionary<Boons, float>()
                {
                    { Boons.FishingProsperity, 2 },
                }
            }
        };
    }
}
