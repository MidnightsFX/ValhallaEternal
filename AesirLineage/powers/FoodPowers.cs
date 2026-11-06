using HarmonyLib;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    internal static class FoodPowers {
        private const float FeastRange = 6f;

        /// <summary>The health the player's food gives right now, before any bonus.</summary>
        internal static float FoodHealth(Player player) {
            float total = 0f;
            foreach (Player.Food food in player.GetFoods()) { total += food.m_health; }
            return total;
        }

        private static float StrongestFoodHealth(Player player) {
            float best = 0f;
            foreach (Player.Food food in player.GetFoods()) { best = Mathf.Max(best, food.m_health); }
            return best;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        private static class FoodValues {
            private static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr) {
                if (__instance != Player.m_localPlayer) { return; }

                // The bonus is on what food gives, not on the base health and stamina everyone has.
                if (PowerSet.Has(Power.FoodBonus, out float bonus)) {
                    float factor = 1f + bonus / 100f;
                    hp = __instance.m_baseHP + (hp - __instance.m_baseHP) * factor;
                    stamina = __instance.m_baseStamina + (stamina - __instance.m_baseStamina) * factor;
                    eitr *= factor;
                }
                // Sick-bed's oath: Hel's plate is Hunger, and food gives less.
                if (PowerSet.Has(Power.FoodValuePenalty, out float less)) {
                    float kept = 1f - less / 100f;
                    hp = __instance.m_baseHP + (hp - __instance.m_baseHP) * kept;
                    stamina = __instance.m_baseStamina + (stamina - __instance.m_baseStamina) * kept;
                    eitr *= kept;
                }
                HuntPowers.MeatBonus(__instance, ref hp, ref stamina);
                eitr += PowerSet.Get(Power.EitrBase);
                // Hearty Meal: the strongest food counts for more. Hearth-Mother: a share of a nearby ally's food.
                if (PowerSet.Has(Power.HeartyMeal, out float hearty)) { hp += StrongestFoodHealth(__instance) * hearty / 100f; }
                hp += AllyPowers.HearthShareFor(__instance);

                hp *= 1f - PowerSet.Get(Power.MaxHealthPenalty) / 100f;
                stamina *= 1f - PowerSet.Get(Power.MaxStaminaPenalty) / 100f;
                eitr *= 1f - PowerSet.Get(Power.MaxEitrPenalty) / 100f;
            }
        }

        // Food burns down on a timer that UpdateFood feeds with dt; a faster timer means food lasts less
        // long. The timer is nudged rather than dt scaled, because dt also paces food's health regen.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
        private static class FoodDuration {
            private static void Prefix(Player __instance, float dt) {
                if (__instance != Player.m_localPlayer || dt <= 0f) { return; }
                float rate = (1f + PowerSet.Get(Power.FoodDurationPenalty) / 100f) / (1f + PowerSet.Get(Power.FoodDuration) / 100f);
                if (rate != 1f) { __instance.m_foodUpdateTimer += dt * Game.m_foodRate * (rate - 1f); }
            }
        }

        // Gefjun's Feast heals players nearby by a share of the food; Beloved gives them the mead's effect too.
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        private static class Sharing {
            private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result) {
                if (!__result || __instance != Player.m_localPlayer || item == null) { return; }
                bool feast = PowerSet.Has(Power.FeastShare, out float share) && item.m_shared.m_food > 0f;
                StatusEffect mead = PowerSet.Has(Power.Beloved) ? item.m_shared.m_consumeStatusEffect : null;
                if (!feast && mead == null) { return; }
                foreach (Player ally in PowerFx.AlliesNear(__instance.transform.position, FeastRange).ToArray()) {
                    if (feast) { ally.Heal(item.m_shared.m_food * share / 100f, true); }
                    if (mead != null) { ally.GetSEMan().AddStatusEffect(mead.NameHash(), resetTime: true); }
                    PowerFx.Effect(feast ? Power.FeastShare : Power.Beloved, VfxRole.Harvest, ally.transform.position, shared: true);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
        private static class NourishmentEffect {
            private static void Postfix(Player __instance, bool __result) {
                if (!__result || __instance != Player.m_localPlayer || !PowerSet.Has(Power.FoodBonus)) { return; }
                PowerFx.Effect(Power.FoodBonus, VfxRole.Harvest, __instance.transform.position, 1.2f);
            }
        }
    }
}
