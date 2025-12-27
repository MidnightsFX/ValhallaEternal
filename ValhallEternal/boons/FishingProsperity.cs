using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValhallEternal.common;
using ValhallEternal.modules;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.boons
{
    internal static class FishingProsperity
    {

        public class FishReward : IProbability
        {
            public float SelectionWeight { get; set; }
            public int MinLevelRequired { get; set; }
            public int MaxLevelAllowed { get; set; }
            public int RewardCountMax { get; set; }
            public int RewardsCountMin { get; set; }
            public string Name { get; set; }
        }

        static readonly List<FishReward> FishingProsperityOptions = new List<FishReward>()
        {
            { new FishReward() { Name = "Flint", SelectionWeight = 15, MinLevelRequired = 1, MaxLevelAllowed = 5, RewardCountMax = 3, RewardsCountMin = 1 } },
            { new FishReward() { Name = "SerpentScale", SelectionWeight = 1, MinLevelRequired = 5, MaxLevelAllowed = 100, RewardCountMax = 3, RewardsCountMin = 1 } },
            { new FishReward() { Name = "Coin", SelectionWeight = 5, MinLevelRequired = 1, MaxLevelAllowed = 100, RewardCountMax = 50, RewardsCountMin = 1 } },
            { new FishReward() { Name = "Ruby", SelectionWeight = 4, MinLevelRequired = 5, MaxLevelAllowed = 100, RewardCountMax = 1, RewardsCountMin = 1 } },
            { new FishReward() { Name = "Amber", SelectionWeight = 4, MinLevelRequired = 5, MaxLevelAllowed = 100, RewardCountMax = 1, RewardsCountMin = 1 } },
            { new FishReward() { Name = "AmberPearl", SelectionWeight = 3, MinLevelRequired = 10, MaxLevelAllowed = 100, RewardCountMax = 1, RewardsCountMin = 1 } },
            { new FishReward() { Name = "SilverNecklace", SelectionWeight = 2, MinLevelRequired = 15, MaxLevelAllowed = 100, RewardCountMax = 1, RewardsCountMin = 1 } },
        };

        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
        public static class IncreaseHarvestYieldsFromDiety
        {
            public static void Prefix(Fish fish, Character owner)
            {
                if (Player.m_localPlayer != null && owner == Player.m_localPlayer && PlayerData.HasBoonWithValue(DataObjects.Boons.FishingProsperity, out float value))
                {
                    // Activation chance
                    int chance = UnityEngine.Random.Range(0, 100);
                    Logger.LogDebug($"Rolling chance for fishing prosperity {chance} activate? {chance < value}");
                    if (chance < value) {
                        string selected = Extensions.RandomSelectFromWeightedListWithExclusions(FishingProsperityOptions.Cast<IProbability>().ToList());

                        FishReward selectedReward = FishingProsperityOptions.Where(x => x.Name == selected).First();

                        if (Deities.DeityEffects.ContainsKey("leafburstverticle")) {
                            UnityEngine.Object.Instantiate(Deities.DeityEffects["leafburstverticle"], fish.transform.position, Quaternion.identity);
                        }

                        int amount = UnityEngine.Random.Range(selectedReward.RewardsCountMin, selectedReward.RewardCountMax);
                        GameObject prefab = PrefabManager.Instance.GetPrefab(selectedReward.Name);
                        ItemDrop id = prefab.GetComponent<ItemDrop>();
                        DamageText.instance.ShowText(DamageText.TextType.Bonus, fish.transform.position + Vector3.up * 0.2f, $"+{amount} {Localization.instance.Localize(id.m_itemData.m_shared.m_name)}", player: true);
                        
                        Player.m_localPlayer.m_inventory.AddItem(prefab, amount);
                    }
                }
            }
        }
    }
}
