using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Vör's Careful Hand: gear that wears slower and lasts longer, crafting that sometimes costs nothing,
    /// masterwork items, and weapons that learn from their kills. What an item has earned is kept in its
    /// own custom data, so it travels with the item to chests, other players and saves.
    /// </summary>
    internal static class CraftPowers {
        internal const string MasterworkKey = "al_masterwork";
        internal const string KillsKey = "al_kills";
        private const float MasterworkBonus = 0.1f;
        private const int KillsPerStep = 50;
        private const float MaxWeaponLore = 20f;

        // Set while vanilla crafts, so its requirement and item calls can be told apart from others.
        private static bool crafting;
        private static bool showingCraftCost;
        private static Dictionary<string, string> upgradedData;
        private static readonly Dictionary<ItemDrop.ItemData, float> lastDurability = new Dictionary<ItemDrop.ItemData, float>();

        internal static void Reset() {
            lastDurability.Clear();
        }

        // ---- Wear ----

        /// <summary>Lasting Make, and Torchbearer's torch: gives back part of the wear on what the player wears and holds.</summary>
        internal static void Tick(Player player) {
            float refund = PowerSet.Get(Power.DurabilityLoss) / 100f;
            bool torches = PowerSet.Has(Power.Torchbearer);
            if (refund <= 0f && !torches) {
                lastDurability.Clear();
                return;
            }
            Track(player.m_rightItem, refund, torches);
            Track(player.m_leftItem, refund, torches);
            Track(player.m_chestItem, refund, torches);
            Track(player.m_legItem, refund, torches);
            Track(player.m_helmetItem, refund, torches);
            Track(player.m_shoulderItem, refund, torches);
            Track(player.m_utilityItem, refund, torches);
            if (lastDurability.Count > 32) { lastDurability.Clear(); }
        }

        private static void Track(ItemDrop.ItemData item, float refund, bool torches) {
            if (item == null || !item.m_shared.m_useDurability) { return; }
            if (lastDurability.TryGetValue(item, out float last) && item.m_durability < last) {
                float share = torches && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch ? 1f : refund;
                item.m_durability += (last - item.m_durability) * share;
            }
            lastDurability[item] = item.m_durability;
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
        private static class MaxDurability {
            private static void Postfix(ref float __result) {
                if (PowerSet.Has(Power.MaxDurability, out float more)) { __result *= 1f + more / 100f; }
            }
        }

        // ---- Crafting ----

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static class Crafting {
            private static void Prefix(InventoryGui __instance) {
                crafting = true;
                upgradedData = __instance.m_craftUpgradeItem != null ? new Dictionary<string, string>(__instance.m_craftUpgradeItem.m_customData) : null;
            }

            private static void Finalizer() {
                crafting = false;
                upgradedData = null;
            }
        }

        // Thrifty Craft: sometimes what a craft would use is left alone.
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        private static class Thrifty {
            private static bool Prefix(Player __instance) {
                if (!crafting || __instance != Player.m_localPlayer || !PowerSet.Has(Power.CraftRefund, out float chance) || Random.value * 100f >= chance) { return true; }
                PowerFx.Effect(Power.CraftRefund, VfxRole.Proc, __instance.transform.position + Vector3.up);
                __instance.Message(MessageHud.MessageType.TopLeft, "$al_thrifty_craft");
                return false;
            }
        }

        // Masterwork, and what an upgraded item had earned, set on the item the craft makes.
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
        private static class Crafted {
            private static void Postfix(ItemDrop.ItemData __result) {
                if (!crafting || __result == null) { return; }
                if (upgradedData != null) {
                    foreach (KeyValuePair<string, string> entry in upgradedData) { __result.m_customData[entry.Key] = entry.Value; }
                }
                if (!IsWeaponOrArmor(__result) || __result.m_customData.ContainsKey(MasterworkKey)) { return; }
                if (PowerSet.Has(Power.Masterwork, out float chance) && Random.value * 100f < chance) {
                    __result.m_customData[MasterworkKey] = "1";
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_masterwork_made", Localization.instance.Localize(__result.m_shared.m_name)));
                    PowerFx.Effect(Power.Masterwork, VfxRole.LevelUp, Player.m_localPlayer.transform.position, 0.8f, shared: true);
                }
            }
        }

        private static bool IsWeaponOrArmor(ItemDrop.ItemData item) {
            switch (item.m_shared.m_itemType) {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsMasterwork(ItemDrop.ItemData item) => item != null && item.m_customData.ContainsKey(MasterworkKey);

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
        private static class MasterworkDamage {
            private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
                if (IsMasterwork(__instance)) { __result.Modify(1f + MasterworkBonus); }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class MasterworkArmor {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                if (IsMasterwork(__instance)) { __result *= 1f + MasterworkBonus; }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
        private static class Tooltip {
            private static void Postfix(ItemDrop.ItemData item, ref string __result) {
                if (item == null) { return; }
                if (IsMasterwork(item)) { __result += "\n" + Localization.instance.Localize("$al_masterwork_tooltip"); }
                if (item.m_customData.TryGetValue(KillsKey, out string kills)) {
                    __result += "\n" + Localization.instance.Localize("$al_weapon_kills_tooltip", kills);
                }
            }
        }

        // Exacting: crafting and upgrading ask for more. Only recipes are touched, not building.
        [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
        private static class CraftCost {
            private static void Postfix(ref int __result) {
                if ((!crafting && !showingCraftCost) || __result <= 0 || !PowerSet.Has(Power.CraftCostPenalty, out float more)) { return; }
                __result = Mathf.CeilToInt(__result * (1f + more / 100f));
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        private static class CraftCheck {
            private static void Prefix(out bool __state) {
                __state = showingCraftCost;
                showingCraftCost = true;
            }

            private static void Finalizer(bool __state) { showingCraftCost = __state; }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
        private static class CraftCostShown {
            private static void Prefix(bool craft, out bool __state) {
                __state = showingCraftCost;
                if (craft) { showingCraftCost = true; }
            }

            private static void Finalizer(bool __state) { showingCraftCost = __state; }
        }

        // ---- Weapon-lore ----

        internal static void OnLocalKill(Player player) {
            if (!PowerSet.Has(Power.WeaponLore)) { return; }
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (weapon == null || weapon == player.m_unarmedWeapon?.m_itemData) { return; }
            int kills = weapon.m_customData.TryGetValue(KillsKey, out string count) && int.TryParse(count, out int known) ? known + 1 : 1;
            weapon.m_customData[KillsKey] = kills.ToString();
        }

        /// <summary>Weapon-lore: the bonus a weapon's kills give it, and the oath against gear made by others.</summary>
        internal static float WeaponFactor(Player player) {
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (weapon == null || weapon == player.m_unarmedWeapon?.m_itemData) { return 1f; }
            float factor = 1f;
            if (PowerSet.Has(Power.WeaponLore, out float perStep) && weapon.m_customData.TryGetValue(KillsKey, out string count) && int.TryParse(count, out int kills)) {
                factor *= 1f + Mathf.Min(MaxWeaponLore, kills / KillsPerStep * perStep) / 100f;
            }
            if (PowerSet.Has(Power.ForeignGearPenalty, out float penalty) && weapon.m_crafterID != player.GetPlayerID()) {
                factor *= Mathf.Max(0.2f, 1f - penalty / 100f);
            }
            return factor;
        }

        /// <summary>The oath against gear made by others: the armor it gives is cut.</summary>
        internal static float ForeignArmorPenalty(Player player) {
            if (!PowerSet.Has(Power.ForeignGearPenalty, out float penalty)) { return 0f; }
            long id = player.GetPlayerID();
            float lost = 0f;
            foreach (ItemDrop.ItemData piece in new[] { player.m_chestItem, player.m_legItem, player.m_helmetItem, player.m_shoulderItem }) {
                if (piece != null && piece.m_crafterID != id) { lost += piece.GetArmor() * penalty / 100f; }
            }
            return lost;
        }
    }
}
