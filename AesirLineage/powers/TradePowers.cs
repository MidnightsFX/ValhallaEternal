using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Freyja's Tears of Gold: better prices from traders, coins that come easier and weigh nothing,
    /// treasure in chests opened the first time, and a hoard that guards and strengthens its keeper.
    /// </summary>
    internal static class TradePowers {
        internal const string CoinsName = "$item_coins";
        private const string GersemiKey = "al_gersemi";
        private const float HoardCoinsPerStep = 200f;
        private const float MaxHoardArmor = 10f;
        private const float GoldHungerCoinsPerStep = 100f;
        private const float MaxGoldHunger = 25f;
        // Tears of Red Gold: coins paid for each point of damage turned aside.
        private const float CoinsPerPoint = 5f;

        internal static int Coins(Player player) => player.GetInventory().CountItems(CoinsName);

        /// <summary>Hoard: armor from the coins the player carries.</summary>
        internal static float HoardArmor(Player player) {
            if (!PowerSet.Has(Power.Hoard, out float perStep)) { return 0f; }
            return Mathf.Min(MaxHoardArmor, Mathf.Floor(Coins(player) / HoardCoinsPerStep) * perStep);
        }

        /// <summary>Gold-hunger: the damage bonus from the coins the player carries, in percent.</summary>
        internal static float GoldHungerBonus(Player player) {
            if (!PowerSet.Has(Power.GoldHunger, out float perStep)) { return 0f; }
            return Mathf.Min(MaxGoldHunger, Mathf.Floor(Coins(player) / GoldHungerCoinsPerStep) * perStep);
        }

        /// <summary>Tears of Red Gold: pays part of a hit in coins instead of health. Returns the damage left.</summary>
        internal static float PayInGold(Player player, float damage) {
            if (damage <= 0f || !PowerSet.Has(Power.RedGoldTears, out float share)) { return damage; }
            int coins = Coins(player);
            if (coins <= 0) { return damage; }
            float turned = Mathf.Min(damage * share / 100f, coins / CoinsPerPoint);
            int paid = Mathf.CeilToInt(turned * CoinsPerPoint);
            if (paid <= 0) { return damage; }
            player.GetInventory().RemoveItem(CoinsName, Mathf.Min(paid, coins));
            PowerFx.Effect(Power.RedGoldTears, VfxRole.Proc, player.GetCenterPoint(), 0.5f);
            return damage - turned;
        }

        // ---- Traders ----

        // Each trader item's discounted copy, kept so the store's selection still finds the same object.
        private static readonly Dictionary<Trader.TradeItem, Trader.TradeItem> discounts = new Dictionary<Trader.TradeItem, Trader.TradeItem>();

        // Dwarves' Bargain: what traders sell costs less. Copies are handed out so the trader's own prices stay.
        [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems))]
        private static class Buying {
            private static void Postfix(ref List<Trader.TradeItem> __result) {
                if (__result == null || !PowerSet.Has(Power.TradePrice, out float cheaper)) { return; }
                List<Trader.TradeItem> discounted = new List<Trader.TradeItem>(__result.Count);
                foreach (Trader.TradeItem item in __result) {
                    if (!discounts.TryGetValue(item, out Trader.TradeItem copy)) {
                        copy = new Trader.TradeItem() {
                            m_prefab = item.m_prefab,
                            m_stack = item.m_stack,
                            m_requiredGlobalKey = item.m_requiredGlobalKey,
                            m_levelUpEffect = item.m_levelUpEffect,
                            m_buyPlayerEffects = item.m_buyPlayerEffects,
                            m_icon = item.m_icon,
                            m_name = item.m_name,
                            m_tooltip = item.m_tooltip,
                            m_buyKey = item.m_buyKey,
                            m_incrementKey = item.m_incrementKey,
                            m_incrementAmount = item.m_incrementAmount,
                        };
                        discounts[item] = copy;
                    }
                    copy.m_price = Mathf.Max(1, Mathf.FloorToInt(item.m_price * (1f - cheaper / 100f)));
                    discounted.Add(copy);
                }
                __result = discounted;
            }
        }

        // Dwarves' Bargain: valuables sell for more, paid as extra coins after the sale.
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SellItem))]
        private static class Selling {
            private static void Prefix(StoreGui __instance, out int __state) {
                ItemDrop.ItemData item = __instance.GetSellableItem();
                __state = item != null ? item.m_shared.m_value * item.m_stack : 0;
            }

            private static void Postfix(StoreGui __instance, int __state) {
                Player local = Player.m_localPlayer;
                if (__state <= 0 || local == null || !PowerSet.Has(Power.TradePrice, out float more)) { return; }
                int extra = Mathf.FloorToInt(__state * more / 100f);
                if (extra > 0) { local.GetInventory().AddItem(__instance.m_coinPrefab.gameObject, extra); }
            }
        }

        // ---- Coins ----

        // Gleam: coins picked up count for a little more.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
        private static class Gleam {
            private static void Prefix(Humanoid __instance, GameObject go, out int __state) {
                __state = 0;
                if (__instance != Player.m_localPlayer || go == null || !PowerSet.Has(Power.CoinFind)) { return; }
                ItemDrop drop = go.GetComponent<ItemDrop>();
                if (drop != null && drop.m_itemData.m_shared.m_name == CoinsName && !drop.m_itemData.m_pickedUp) { __state = drop.m_itemData.m_stack; }
            }

            private static void Postfix(Humanoid __instance, bool __result, int __state) {
                if (!__result || __state <= 0) { return; }
                int extra = Mathf.FloorToInt(__state * PowerSet.Get(Power.CoinFind) / 100f + Random.value);
                GameObject coins = ObjectDB.instance?.GetItemPrefab("Coins");
                if (extra > 0 && coins != null) { __instance.GetInventory().AddItem(coins, extra); }
            }
        }

        // Hoard: coins weigh nothing.
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
        private static class Weightless {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                if (__instance.m_shared.m_name == CoinsName && PowerSet.Has(Power.Hoard)) { __result = 0f; }
            }
        }

        // The oath of Gold-hunger: coins don't go to the grave.
        [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
        private static class CoinsLost {
            private static void Prefix(Player __instance) {
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.LoseCoinsOnDeath)) { return; }
                int coins = Coins(__instance);
                if (coins > 0) { __instance.GetInventory().RemoveItem(CoinsName, coins); }
            }
        }

        // ---- Gersemi: treasure in a world chest, the first time it is opened ----

        [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
        private static class Treasure {
            private static void Prefix(Container __instance, bool granted) {
                if (!granted || Player.m_localPlayer == null || !PowerSet.Has(Power.Gersemi, out float chance)) { return; }
                ZNetView nview = __instance.m_nview;
                if (nview == null || !nview.IsOwner() || __instance.m_defaultItems == null || __instance.m_defaultItems.m_drops.Count == 0) { return; }
                Piece piece = __instance.GetComponent<Piece>();
                if (piece != null && piece.GetCreator() != 0L) { return; }
                ZDO zdo = nview.GetZDO();
                if (zdo.GetBool(GersemiKey, false)) { return; }
                zdo.Set(GersemiKey, true);
                if (Random.value * 100f >= chance) { return; }
                (GameObject prefab, int amount) = KillPowers.RollValuable();
                if (prefab == null || !__instance.GetInventory().AddItem(prefab, amount)) { return; }
                __instance.Save();
                PowerFx.Effect(Power.Gersemi, VfxRole.Harvest, __instance.transform.position + Vector3.up);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$al_gersemi_found");
            }
        }
    }
}
