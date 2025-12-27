using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons
{
    internal static class IncreasePickableYield
    {
        static readonly List<String> UnallowedPickables = new List<String>() {
            "SurtlingCore",
            "Flint",
            "Wood",
            "Branch",
            "Stone",
            "Amber",
            "AmberPearl",
            "Coins",
            "Ruby",
            "CryptRemains",
            "Obsidian",
            "Crystal",
            "Pot_Shard",
            "DragonEgg",
            "DvergrLantern",
            "DvergrMineTreasure",
            "SulfurRock",
            "VoltureEgg",
            "Swordpiece",
            "MoltenCore",
            "Hairstrands",
            "Tar",
            "BlackCore"
        };

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        public static class IncreaseHarvestYieldsFromDiety
        {
            static uint lastpicked;
            public static void Prefix(Pickable __instance, Humanoid character)
            {
                if (__instance.m_picked) {
                    return;
                }
                if (UnallowedPickables.Contains(__instance.m_itemPrefab.name))
                {
                    Logger.LogDebug($"Pickable is not a gathering item.");
                    return;
                }

                
                // Only run for players who have the effect
                Logger.LogDebug($"Checking if player has pickable {PlayerData.localPlayerConfig.HasBoon(DataObjects.Boons.IncreasePickableYields, out float value)} - {value}");
                if (PlayerData.localPlayerConfig.HasBoon(DataObjects.Boons.IncreasePickableYields, out float pickBonus) == false) {
                    return;
                }
                // Only activates if the random roll is within/equal to the activation chance
                uint id = 0; // 0 is the uninitialized id, so an actual running object should never have a zero zdo.id
                if (__instance.m_nview.GetZDO() != null) {
                    id = __instance.m_nview.GetZDO().m_uid.ID;
                }
                // Do not run multiple times for the same object
                if (id == lastpicked) { return; }
                if (UnityEngine.Random.value <= ValConfig.ChanceOfHarvestBonusBoon.Value) {
                    Logger.LogDebug("Failed chance roll for pickable bonus");
                    return;
                }

                lastpicked = id; // We have succeeded the picking roll, set this item as picked, so we can't spam re-picking it
                float random_amount = UnityEngine.Random.Range(1f, 10f);
                int extra_drops = 1;
                if (random_amount >= pickBonus) {
                    extra_drops = Mathf.RoundToInt(random_amount / pickBonus);
                }

                if (extra_drops > 0)
                {
                    Vector3 spawnp = __instance.transform.position + Vector3.up * __instance.m_spawnOffset;
                    Logger.LogDebug($"Spawning extra drops {extra_drops}");
                    if (Deities.DeityEffects.ContainsKey("leafburstverticle")) {
                        GameObject vfx = UnityEngine.Object.Instantiate(Deities.DeityEffects["leafburstverticle"], spawnp, Quaternion.identity);
                        vfx.transform.localScale *= 2f;
                    }
                    // Show bonus text amount
                    DamageText.instance.ShowText(DamageText.TextType.Heal, __instance.transform.position + Vector3.up * __instance.m_spawnOffset, $"{extra_drops}", player: true);
                    //__instance.m_bonusEffect.Create(spawnp, Quaternion.identity);
                    for (int i = 0; i < extra_drops; i++) {
                        UnityEngine.Object.Instantiate(__instance.m_itemPrefab, spawnp, __instance.transform.rotation);
                    }
                }
            }
        }
    }
}
