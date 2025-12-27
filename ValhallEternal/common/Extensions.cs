using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValhallEternal.common
{
    internal static class Extensions
    {
        public static bool PlayerHasUniqueKey(this Player player, string key) {
            foreach (string pkey in player.GetUniqueKeys()) {
                if (pkey.StartsWith(key)) { return true; }
            }
            return false;
        }

        public static bool PlayerRemoveUniqueKey(this Player player, string key) {
            List<string> keys = player.GetUniqueKeys();
            foreach (string pkey in keys) {
                if (pkey.StartsWith(key)) {
                    player.RemoveUniqueKey(pkey);
                    return true;
                }
            }
            return false;
        }

        public static string RandomSelectFromWeightedListWithExclusions(List<DataObjects.IProbability> listOfWeights, List<string> exclude = null)
        {
            if (exclude == null) { exclude = new List<string>() { }; }
            List<DataObjects.IProbability> possibleOptions = listOfWeights.Where(x => exclude.Contains(x.Name) == false).ToList();
            float totalweight = possibleOptions.Select(x => x.SelectionWeight).Sum();
            if (totalweight == 0) { return null; } // Nothing selectable
            float selection = UnityEngine.Random.Range(0, totalweight);
            float current_weight = 0f;
            //Logger.LogDebug($"Total weight is {totalweight}, random selection is {selection}");
            foreach (var entry in listOfWeights)
            {
                current_weight += entry.SelectionWeight;
                //Logger.LogDebug($"Current weight is {current_weight} >= {selection} for entry {entry.Name} - {entry.SelectionWeight}");
                if (current_weight >= selection)
                {
                    //Logger.LogDebug($"Randomly selected {entry.Name}");
                    return entry.Name;
                }
            }
            // Fallback, realistically this is never used.
            // Logger.LogWarning($"Failed to select a random entry from the list, returning a random entry instead.");
            return possibleOptions.ToArray()[UnityEngine.Random.Range(0, listOfWeights.Count - 1)].Name;
        }
    }
}
