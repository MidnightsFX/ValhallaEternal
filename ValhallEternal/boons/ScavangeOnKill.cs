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
using static ValhallEternal.common.Extensions;

namespace ValhallEternal.boons {
    internal static class ScavangeOnKill {

        public class LootReward : IProbability {
            public float SelectionWeight { get; set; }
            public int MinLevelRequired { get; set; }
            public int MaxLevelAllowed { get; set; }
            public int MinDropAmount { get; set; }
            public int MaxDropAmount { get; set; }
            public float ChanceForDropIncrease { get; set; }
            public string Name { get; set; }
        }

        static readonly List<LootReward> ArrowReturnProbabilityRewards = new List<LootReward>()
        {
            { new LootReward() { Name = "ArrowWood", SelectionWeight = 5, MinLevelRequired = 1, MaxLevelAllowed = 10 } },
            { new LootReward() { Name = "ArrowFlint", SelectionWeight = 5, MinLevelRequired = 5, MaxLevelAllowed = 20 } },
            { new LootReward() { Name = "ArrowBronze", SelectionWeight = 5, MinLevelRequired = 10, MaxLevelAllowed = 35} },
            { new LootReward() { Name = "ArrowFire", SelectionWeight = 5, MinLevelRequired = 15, MaxLevelAllowed = 40} },
            { new LootReward() { Name = "ArrowIron", SelectionWeight = 4, MinLevelRequired = 20, MaxLevelAllowed = 50} },
            { new LootReward() { Name = "ArrowPoison", SelectionWeight = 4, MinLevelRequired = 25, MaxLevelAllowed = 55} },
            { new LootReward() { Name = "ArrowObsidian", SelectionWeight = 4, MinLevelRequired = 30, MaxLevelAllowed = 60 } },
            { new LootReward() { Name = "ArrowFrost", SelectionWeight = 3, MinLevelRequired = 35, MaxLevelAllowed = 65} },
            { new LootReward() { Name = "ArrowSilver", SelectionWeight = 3, MinLevelRequired = 40, MaxLevelAllowed = 70} },
            { new LootReward() { Name = "ArrowNeedle", SelectionWeight = 2, MinLevelRequired = 45, MaxLevelAllowed = 75} },
            { new LootReward() { Name = "ArrowCarapace", SelectionWeight = 2, MinLevelRequired = 50, MaxLevelAllowed = 80} },
            { new LootReward() { Name = "ArrowCharred", SelectionWeight = 1, MinLevelRequired = 55, MaxLevelAllowed = 100} },
        };

        static readonly List<LootReward> WealthProbabilityRewards = new List<LootReward>()
        {
            { new LootReward() { Name = "Coin", SelectionWeight = 5, MinLevelRequired = 1, MaxLevelAllowed = 10, MinDropAmount = 1, MaxDropAmount = 25, ChanceForDropIncrease = 0.1f } },
            { new LootReward() { Name = "Coin", SelectionWeight = 5, MinLevelRequired = 10, MaxLevelAllowed = 100, MinDropAmount = 3, MaxDropAmount = 25, ChanceForDropIncrease = 0.3f } },
            { new LootReward() { Name = "Ruby", SelectionWeight = 4, MinLevelRequired = 15, MaxLevelAllowed = 100, MinDropAmount = 1, MaxDropAmount = 7, ChanceForDropIncrease = 0.05f } },
            { new LootReward() { Name = "Amber", SelectionWeight = 4, MinLevelRequired = 20, MaxLevelAllowed = 100, MinDropAmount = 1, MaxDropAmount = 8, ChanceForDropIncrease = 0.05f } },
            { new LootReward() { Name = "AmberPearl", SelectionWeight = 3, MinLevelRequired = 25, MaxLevelAllowed = 100, MinDropAmount = 1, MaxDropAmount = 5, ChanceForDropIncrease = 0.05f } },
            { new LootReward() { Name = "SilverNecklace", SelectionWeight = 2, MinLevelRequired = 30, MaxLevelAllowed = 100, MinDropAmount = 1, MaxDropAmount = 3, ChanceForDropIncrease = 0.05f } },
        };

        [HarmonyPatch]
        private static class OnKillLootBonuses {

            [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
            private static void Postfix(CharacterDrop __instance) {
                // Does not apply to tamed
                if (__instance.m_character.IsTamed()) { return; }
                // Determine if the player was the last hitter?

                // Huntress arrow scavange effect
                if (PlayerData.HasBoonWithValue(common.DataObjects.Boons.HuntressArrowReturn, out float arrowReturnVal)) {
                    float roll = UnityEngine.Random.Range(0, 100);
                    float chance = arrowReturnVal;
                    if (chance > 10) { chance = 10f; }
                    Logger.LogInfo($"[HuntressArrowReturn] Roll: {roll} <= {chance} | {roll <= chance}");
                    if (roll <= chance) {
                        List<LootReward> levelselectedOptions = ArrowReturnProbabilityRewards.Where(x => x.MinLevelRequired < arrowReturnVal && x.MaxLevelAllowed >= arrowReturnVal).ToList();
                        string selected = RandomSelectFromWeightedListWithExclusions(levelselectedOptions.Cast<IProbability>().ToList());
                        Logger.LogDebug($"Selected Reward {selected}");
                        List<LootReward> determined = ArrowReturnProbabilityRewards.Where(x => x.Name == selected).ToList();
                        if (determined.Count > 0) {
                            if (Deities.DeityEffects.ContainsKey("snowswirl")) {
                                UnityEngine.Object.Instantiate(Deities.DeityEffects["snowswirl"], Player.m_localPlayer.transform.position, Quaternion.identity);
                            }

                            GameObject prefab = PrefabManager.Instance.GetPrefab(determined.First().Name);
                            ItemDrop id = prefab.GetComponent<ItemDrop>();
                            DamageText.instance.ShowText(DamageText.TextType.Bonus, Player.m_localPlayer.transform.position + Vector3.up * 0.2f, $"+1 {Localization.instance.Localize(id.m_itemData.m_shared.m_name)}", player: true);
                            Logger.LogInfo($"[HuntressArrowReturn] providing +1 {id.m_itemData.m_shared.m_name}");
                            Player.m_localPlayer.m_inventory.AddItem(prefab, 1);
                        }
                    }
                }

                if (PlayerData.HasBoonWithValue(Boons.WealthOfAges, out float wealthValue)) {
                    float roll = UnityEngine.Random.Range(0, 100);
                    float chance = arrowReturnVal;
                    if (chance > 10) { chance = 10f; }
                    Logger.LogInfo($"[WealthOfAges] Roll: {roll} <= {chance} | {(roll <= chance)}");
                    if (roll <= chance) {
                        List<LootReward> levelselectedOptions = WealthProbabilityRewards.Where(x => x.MinLevelRequired < wealthValue && x.MaxLevelAllowed >= wealthValue).ToList();
                        string selected = RandomSelectFromWeightedListWithExclusions(levelselectedOptions.Cast<IProbability>().ToList());
                        Logger.LogDebug($"Selected Reward {selected}");
                        List<LootReward> determined = ArrowReturnProbabilityRewards.Where(x => x.Name == selected).ToList();
                        if (determined.Count > 0) {
                            LootReward selectedReward = determined.First();

                            if (Deities.DeityEffects.ContainsKey("goldenswirl")) {
                                UnityEngine.Object.Instantiate(Deities.DeityEffects["goldenswirl"], __instance.transform.position, Quaternion.identity);
                            }
                            int amount = selectedReward.MinDropAmount;
                            while (amount < selectedReward.MaxDropAmount) {
                                if (UnityEngine.Random.Range(0, 100) < selectedReward.ChanceForDropIncrease) {
                                    amount++;
                                    continue;
                                }
                                break;
                            }

                            GameObject prefab = PrefabManager.Instance.GetPrefab(selectedReward.Name);
                            ItemDrop id = prefab.GetComponent<ItemDrop>();
                            DamageText.instance.ShowText(DamageText.TextType.Bonus, Player.m_localPlayer.transform.position + Vector3.up * 0.2f, $"+1 {Localization.instance.Localize(id.m_itemData.m_shared.m_name)}", player: true);
                            Logger.LogInfo($"[WealthOfAges] providing +{amount} {prefab.name}");
                            DropItemsImmediate(new Dictionary<GameObject, int>() { { prefab, amount } }, __instance.transform.position, 0.5f);
                        }
                    }
                }
            }
        }
    }
}
