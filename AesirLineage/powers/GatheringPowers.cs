using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    internal static class GatheringPowers {
        // Pickables that are treasure or materials rather than a harvest.
        internal static readonly HashSet<string> NotHarvests = new HashSet<string>() {
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
            "BlackCore",
        };

        private struct FishReward {
            public string Prefab;
            public float Weight;
            // Fishing Prosperity value needed before this can be caught.
            public float MinValue;
            public int Min;
            public int Max;
        }

        private static readonly FishReward[] FishRewards = new FishReward[] {
            new FishReward() { Prefab = "Flint", Weight = 15, MinValue = 0, Min = 1, Max = 3 },
            new FishReward() { Prefab = "Coins", Weight = 5, MinValue = 0, Min = 5, Max = 25 },
            new FishReward() { Prefab = "SerpentScale", Weight = 1, MinValue = 10, Min = 1, Max = 3 },
            new FishReward() { Prefab = "Ruby", Weight = 4, MinValue = 15, Min = 1, Max = 1 },
            new FishReward() { Prefab = "Amber", Weight = 4, MinValue = 15, Min = 1, Max = 1 },
            new FishReward() { Prefab = "AmberPearl", Weight = 3, MinValue = 25, Min = 1, Max = 1 },
            new FishReward() { Prefab = "SilverNecklace", Weight = 2, MinValue = 35, Min = 1, Max = 1 },
        };

        private static uint lastHarvestRolled;

        // Seeds of the Gods: set on a plant the player sowed while holding the power, so it keeps growing
        // anywhere even after the power is gone or another player owns it.
        private const string AnyBiomeKey = "al_anybiome";
        private const Heightmap.Biome AllBiomes = (Heightmap.Biome)(-1);

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class HarvestBounty {
            private static void Prefix(Pickable __instance, Humanoid character) {
                if (character != Player.m_localPlayer || __instance.m_picked || __instance.m_itemPrefab == null) { return; }
                if (!PowerSet.Has(Power.HarvestBounty, out float chance) || NotHarvests.Contains(__instance.m_itemPrefab.name)) { return; }
                ZDO zdo = __instance.m_nview?.GetZDO();
                if (zdo == null) { return; }
                // One roll per plant, however many times Interact fires on it.
                if (zdo.m_uid.ID == lastHarvestRolled) { return; }
                lastHarvestRolled = zdo.m_uid.ID;
                if (Random.value * 100f >= chance) { return; }

                int amount = Mathf.Max(1, __instance.m_amount);
                Vector3 position = __instance.transform.position + Vector3.up * __instance.m_spawnOffset;
                Extensions.DropItemsImmediate(new Dictionary<GameObject, int>() { { __instance.m_itemPrefab, amount } }, position, 0.5f);
                PowerFx.Effect(Power.HarvestBounty, VfxRole.Harvest, position);
                DamageText.instance.ShowText(DamageText.TextType.Bonus, position, $"+{amount}", player: true);
            }
        }

        private static uint lastHermitRolled;

        // Hermit: crops picked near the bed sometimes give a second harvest.
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class HermitHarvest {
            private static void Prefix(Pickable __instance, Humanoid character) {
                if (character != Player.m_localPlayer || __instance.m_picked || __instance.m_itemPrefab == null || !PowerSet.Has(Power.Hermit)) { return; }
                ZDO zdo = __instance.m_nview?.GetZDO();
                if (zdo == null || NotHarvests.Contains(__instance.m_itemPrefab.name) || zdo.m_uid.ID == lastHermitRolled) { return; }
                lastHermitRolled = zdo.m_uid.ID;
                Vector3 position = __instance.transform.position + Vector3.up * __instance.m_spawnOffset;
                if (!HomePowers.HermitHarvest(position)) { return; }
                int amount = Mathf.Max(1, __instance.m_amount);
                Extensions.DropItemsImmediate(new Dictionary<GameObject, int>() { { __instance.m_itemPrefab, amount } }, position, 0.5f);
                PowerFx.Effect(Power.Hermit, VfxRole.Harvest, position);
            }
        }

        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
        private static class FishingProsperity {
            private static void Prefix(Fish fish, Character owner) {
                Player local = Player.m_localPlayer;
                if (local == null || owner != local || fish == null) { return; }
                if (!PowerSet.Has(Power.FishingProsperity, out float value) || Random.value * 100f >= value) { return; }

                FishReward? picked = PickFishReward(value);
                if (picked == null) { return; }
                FishReward reward = picked.Value;
                GameObject prefab = ObjectDB.instance?.GetItemPrefab(reward.Prefab);
                if (prefab == null) { return; }
                int amount = Random.Range(reward.Min, reward.Max + 1);
                if (!local.GetInventory().AddItem(prefab, amount)) {
                    Extensions.DropItemsImmediate(new Dictionary<GameObject, int>() { { prefab, amount } }, local.transform.position, 0.5f);
                }
                PowerFx.Effect(Power.FishingProsperity, VfxRole.Harvest, fish.transform.position);
                string name = Localization.instance.Localize(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
                DamageText.instance.ShowText(DamageText.TextType.Bonus, fish.transform.position + Vector3.up * 0.2f, $"+{amount} {name}", player: true);
            }
        }

        // ---- Yoke of Gefjun: a share of hits on trees, logs and ore throw off an extra piece ----

        private static void ExtraYield(HitData hit, int minToolTier, DropTable table, bool tierZeroAllowed) {
            Player local = Player.m_localPlayer;
            if (local == null || hit == null || table == null || hit.GetAttacker() != local) { return; }
            // Clear the Land: chopping and mining hit harder.
            hit.m_damage.m_chop *= 1f + PowerSet.Get(Power.ChopDamage) / 100f;
            hit.m_damage.m_pickaxe *= 1f + PowerSet.Get(Power.MineDamage) / 100f;
            if (!PowerSet.Has(Power.GatherYield, out float chance) || hit.GetTotalDamage() <= 0f) { return; }
            if (!hit.CheckToolTier(minToolTier, tierZeroAllowed) || Random.value * 100f >= chance) { return; }
            List<GameObject> drops = table.GetDropList();
            if (drops == null || drops.Count == 0 || drops[0] == null) { return; }
            Extensions.DropItemsImmediate(new Dictionary<GameObject, int>() { { drops[0], 1 } }, hit.m_point, 0.5f);
            PowerFx.Effect(Power.GatherYield, VfxRole.Harvest, hit.m_point, 0.6f);
        }

        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
        private static class TreeYield {
            private static void Prefix(TreeBase __instance, HitData hit) { ExtraYield(hit, __instance.m_minToolTier, __instance.m_dropWhenDestroyed, true); }
        }

        [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
        private static class LogYield {
            private static void Prefix(TreeLog __instance, HitData hit) { ExtraYield(hit, __instance.m_minToolTier, __instance.m_dropWhenDestroyed, true); }
        }

        [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
        private static class RockYield {
            private static void Prefix(MineRock __instance, HitData hit) { ExtraYield(hit, __instance.m_minToolTier, __instance.m_dropItems, false); }
        }

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
        private static class DepositYield {
            private static void Prefix(MineRock5 __instance, HitData hit) { ExtraYield(hit, __instance.m_minToolTier, __instance.m_dropItems, false); }
        }

        // Other breakables only count when they are mined or chopped, so pots and crates don't.
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
        private static class DestructibleYield {
            private static void Prefix(Destructible __instance, HitData hit) {
                if (hit == null || (hit.m_skill != Skills.SkillType.Pickaxes && hit.m_skill != Skills.SkillType.WoodCutting)) { return; }
                DropOnDestroyed drops = __instance.GetComponent<DropOnDestroyed>();
                if (drops != null) { ExtraYield(hit, __instance.m_minToolTier, drops.m_dropWhenDestroyed, false); }
            }
        }

        // ---- Seeds of the Gods ----

        [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
        private static class Sown {
            private static void Postfix(Piece __instance, long uid) {
                Player local = Player.m_localPlayer;
                if (local == null || uid != local.GetPlayerID() || !PowerSet.Has(Power.SeedsOfTheGods)) { return; }
                if (__instance.GetComponent<Plant>() == null) { return; }
                __instance.m_nview?.GetZDO()?.Set(AnyBiomeKey, true);
            }
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
        private static class AnyBiome {
            private static void Prefix(Plant __instance) {
                if (__instance.m_biome == AllBiomes || __instance.m_nview?.GetZDO()?.GetBool(AnyBiomeKey, false) != true) { return; }
                // Each plant has its own copy of these, so setting them changes only this one.
                __instance.m_biome = AllBiomes;
                __instance.m_tolerateHeat = true;
                __instance.m_tolerateCold = true;
            }
        }

        // The build ghost refuses crops outside their biome; lift that while placing them.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        private static class PlaceAnywhere {
            private static void Prefix(Player __instance, out Heightmap.Biome? __state) {
                __state = null;
                if (__instance != Player.m_localPlayer || __instance.m_placementGhost == null || !PowerSet.Has(Power.SeedsOfTheGods)) { return; }
                Piece piece = __instance.m_placementGhost.GetComponent<Piece>();
                if (piece == null || piece.m_onlyInBiome == Heightmap.Biome.None || piece.GetComponent<Plant>() == null) { return; }
                __state = piece.m_onlyInBiome;
                piece.m_onlyInBiome = Heightmap.Biome.None;
            }

            private static void Postfix(Player __instance, Heightmap.Biome? __state) {
                if (__state == null || __instance.m_placementGhost == null) { return; }
                Piece piece = __instance.m_placementGhost.GetComponent<Piece>();
                if (piece != null) { piece.m_onlyInBiome = __state.Value; }
            }
        }

        private static FishReward? PickFishReward(float value) {
            float total = 0f;
            foreach (FishReward reward in FishRewards) {
                if (reward.MinValue <= value) { total += reward.Weight; }
            }
            float roll = Random.value * total;
            foreach (FishReward reward in FishRewards) {
                if (reward.MinValue > value) { continue; }
                roll -= reward.Weight;
                if (roll <= 0f) { return reward; }
            }
            return null;
        }
    }
}
