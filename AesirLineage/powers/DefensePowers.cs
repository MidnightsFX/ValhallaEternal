using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    internal static class DefensePowers {
        [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
        private static class Armor {
            private static void Postfix(Player __instance, ref float __result) {
                if (__instance != Player.m_localPlayer) { return; }
                float armor = __result + PowerSet.Get(Power.Armor) + TradePowers.HoardArmor(__instance) - CraftPowers.ForeignArmorPenalty(__instance);
                __result = Mathf.Max(0f, armor) * (1f - PowerSet.Get(Power.ArmorPenalty) / 100f);
            }
        }

        // Read from the item itself, so tooltips show the boosted value too.
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBaseBlockPower), typeof(int))]
        private static class BlockPower {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                float bonus = PowerSet.Get(Power.BlockPower);
                // Tower shields are the shields that can't parry: vanilla parries only when this is above 1.
                // Valhalla Eternal tested for 0, which no shield has.
                if (__instance.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield && __instance.m_shared.m_timedBlockBonus <= 1f) {
                    bonus += PowerSet.Get(Power.TowerBlockPower);
                }
                if (bonus > 0f) { __result *= 1f + bonus / 100f; }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.GetStaggerTreshold))]
        private static class StaggerThreshold {
            private static void Postfix(Character __instance, ref float __result) {
                if (__instance == Player.m_localPlayer && PowerSet.Has(Power.StaggerThreshold, out float value)) {
                    __result *= 1f + value / 100f;
                }
            }
        }

        // Vanilla takes heat as (1 - this), so it adds a share, capped well below the 1 that means immunity.
        // The Pyre of Helheim makes the wearer immune.
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentHeatResistanceModifier))]
        private static class HeatResistance {
            private static void Postfix(Player __instance, ref float __result) {
                if (__instance != Player.m_localPlayer) { return; }
                __result += PowerSet.Has(Power.PyreOfHelheim) ? 1f : PowerSet.Get(Power.HeatResistance) / 100f;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.AddFireDamage))]
        private static class FireResist {
            private static void Prefix(Character __instance, ref float damage) {
                if (__instance == Player.m_localPlayer) { damage *= (1f - PowerSet.Get(Power.FireAndPoisonResist) / 100f) * LightPowers.AilmentFactor(Player.m_localPlayer); }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.AddPoisonDamage))]
        private static class PoisonResist {
            private static void Prefix(Character __instance, ref float damage) {
                if (__instance == Player.m_localPlayer) { damage *= (1f - PowerSet.Get(Power.FireAndPoisonResist) / 100f) * LightPowers.AilmentFactor(Player.m_localPlayer); }
            }
        }
    }
}
