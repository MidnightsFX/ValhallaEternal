using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons
{
    internal static class QualityNourishment
    {
        // REWRITE- should give a BONUS based on players food, having individual food items give extra value would require modifying the prefabs

        [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
        public static class IncreaseFoodValue
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions /*, ILGenerator generator*/)
            {
                var codeMatcher = new CodeMatcher(instructions);
                codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Player), nameof(Player.m_foods))),
                    new CodeMatch(OpCodes.Ldloc_S)
                    //new CodeMatch(OpCodes.Callvirt)
                    ).Advance(2).InsertAndAdvance(
                    Transpilers.EmitDelegate(FoodWithBonus)
                    ).ThrowIfNotMatch("Unable to patch Food Bonus values.");

                return codeMatcher.Instructions();
            }

            public static Player.Food FoodWithBonus(Player.Food newfood)
            {
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.QualityNourishment, out float foodBonus))
                {
                    float bonusFactor = 1 + (foodBonus / 100f);
                    newfood.m_stamina *= bonusFactor;
                    newfood.m_eitr *= bonusFactor;
                    newfood.m_health *= bonusFactor;
                    Logger.LogDebug($"QualityNourishment increased values: Health:{newfood.m_health}, Stamina:{newfood.m_stamina}, Eitr:{newfood.m_eitr}");
                }
                return newfood;
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), argumentTypes: new Type[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int) })]
        public static class ShowFoodValueBonus
        {
            public static void Prefix(ItemDrop.ItemData __instance, Tuple<float, float, float> __state)
            {
                if (__instance == null) { return; }
                __state = new Tuple<float, float, float>(0, 0,0);
                float bonusFactor = 0;
                if (PlayerData.HasBoonWithValue(DataObjects.Boons.QualityNourishment, out float foodBonus))
                {
                    bonusFactor = 1 + (foodBonus / 100f);
                }
                if (bonusFactor > 0)
                {
                    __state = new Tuple<float, float, float> (__instance.m_shared.m_food, __instance.m_shared.m_foodStamina, __instance.m_shared.m_foodEitr);

                    __instance.m_shared.m_food *= bonusFactor;
                    __instance.m_shared.m_foodStamina *= bonusFactor;
                    __instance.m_shared.m_foodEitr *= bonusFactor;
                }
            }
            public static void Postfix(ItemDrop.ItemData __instance, Tuple<float, float, float> __state)
            {
                if (__state == null) { return; }
                if (__state.Item1 > 0) { __instance.m_shared.m_food = __state.Item1; }
                if (__state.Item2 > 0) { __instance.m_shared.m_foodStamina = __state.Item2; }
                if (__state.Item3 > 0) { __instance.m_shared.m_foodEitr = __state.Item3; }
            }
        }
    }
}
