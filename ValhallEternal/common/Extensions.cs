using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HitData;

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

        public static float GetTotalDamageNoHarvestValues(this DamageTypes dmgs, float poisonMod = 0.5f) {
            float total = dmgs.m_frost + dmgs.m_fire + dmgs.m_spirit + dmgs.m_lightning + dmgs.m_blunt + dmgs.m_slash + dmgs.m_pierce;
            total += (dmgs.m_poison * poisonMod);
            return total;
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

        internal static void DropItemsImmediate(Dictionary<GameObject, int> drops, Vector3 centerPos, float dropArea) {
            foreach (var drop in drops) {
                bool set_stack_size = false;
                int max_stack_size = 0;
                var item = drop.Key;
                int amount = drop.Value;
                Logger.LogDebug($"Dropping {item.name} {amount}");
                for (int i = 0; i < amount;) {
                    // Drop the item at the specified position
                    GameObject droppedItem = UnityEngine.Object.Instantiate(item, centerPos, Quaternion.identity);

                    ItemDrop component = droppedItem.GetComponent<ItemDrop>();
                    if (set_stack_size == false) {
                        set_stack_size = true;
                        if (component) { max_stack_size = component.m_itemData.m_shared.m_maxStackSize; }
                    }

                    // Drop in stacks if this is an item
                    if (component is not null) {
                        int remaining = (amount - i);
                        if (remaining > 0) {
                            if (amount > max_stack_size) {
                                component.m_itemData.m_stack = max_stack_size;
                                i += max_stack_size;
                            } else {
                                component.m_itemData.m_stack = remaining;
                                i += remaining;
                            }
                        }
                        component.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
                    }

                    Rigidbody component2 = droppedItem.GetComponent<Rigidbody>();
                    if ((bool)component2) {
                        Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere * dropArea;
                        if (insideUnitSphere.y < 0f) {
                            insideUnitSphere.y = 0f - insideUnitSphere.y;
                        }
                        component2.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
                    }
                    i++;
                }
            }
        }
    }
}
