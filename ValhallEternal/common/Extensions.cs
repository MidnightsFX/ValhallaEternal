using System;
using System.Collections.Generic;
using System.Linq;
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

        public static Dictionary<string, int> GetItemTotalsByName(this Inventory inv) {
            List<ItemDrop.ItemData> user_inventory = inv.GetAllItems();
            Dictionary<string, int> itemCountByName = new Dictionary<string, int>();
            foreach (ItemDrop.ItemData user_item in user_inventory) {
                if (itemCountByName.ContainsKey(user_item.m_dropPrefab.name)) {
                    itemCountByName[user_item.m_dropPrefab.name] += user_item.m_stack;
                } else {
                    itemCountByName.Add(user_item.m_dropPrefab.name, user_item.m_stack);
                }
            }
            return itemCountByName;
        }

        public static bool RemoveItemByPrefab(this Inventory inv, string prefab,  int countToRemove) {
            List<ItemDrop.ItemData> user_inventory = inv.GetAllItems();
            int remaining = countToRemove;
            List<ItemDrop.ItemData> itemsToRemove = new List<ItemDrop.ItemData>();
            foreach (ItemDrop.ItemData user_item in user_inventory) {
                //Logger.LogDebug($"Comparing {user_item.m_dropPrefab.name} to {prefab} match? {user_item.m_dropPrefab.name == prefab}");
                if (user_item.m_dropPrefab.name == prefab) {
                    //Logger.LogDebug($"stack {user_item.m_stack} > 0 = {user_item.m_stack > 0}");
                    if (user_item.m_stack > 0) {
                        if (remaining >= user_item.m_stack) {
                            if (user_item.m_stack <= remaining) {
                                itemsToRemove.Add(user_item);
                                remaining -= user_item.m_stack;
                            } else {
                                user_item.m_stack -= remaining;
                                remaining = 0;
                            }
                        } else {
                            user_item.m_stack -= remaining;
                            break;
                        }
                    } else {
                        // zero sized or less than zero size stacks are invalid and should be removed regardless
                        // but it doesn't count towards the tribute contribution you monster
                        itemsToRemove.Add(user_item);
                    }
                }
            }

            foreach(ItemDrop.ItemData item in itemsToRemove) {
                inv.RemoveItem(item);
            }
            Logger.LogDebug($"Remove summary: {prefab}x{countToRemove} successfully removed: {countToRemove - remaining}");
            return remaining == 0;
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
