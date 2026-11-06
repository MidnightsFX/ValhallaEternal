using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Víðarr's World After: what is lost to death or Ascension coming back. Each skill's best level is
    /// kept on the character whether or not the star is held, so relearning works for losses from before
    /// it was taken. The food eaten at death is kept the same way until the grave is reached.
    /// </summary>
    internal static class AfterPowers {
        private const string BestKey = "al_bestskills";
        private const string GraveFoodKey = "al_gravefood";
        private const float LowHealth = 0.3f;
        private const float OutlastSpeed = 15f;
        private const float GoldenSeconds = 600f;
        private const float GoldenBonus = 20f;

        private static readonly Dictionary<Skills.SkillType, float> best = new Dictionary<Skills.SkillType, float>();
        private static Player bestOf;
        private static float goldenUntil;
        private static float nextOutlast;

        internal static void Reset() {
            best.Clear();
            bestOf = null;
            goldenUntil = 0f;
            nextOutlast = 0f;
        }

        private static bool Low(Player player) => player.GetHealthPercentage() < LowHealth;

        // ---- The World Renewed ----

        private static void LoadBest(Player player) {
            if (bestOf == player) { return; }
            bestOf = player;
            best.Clear();
            if (player.m_customData.TryGetValue(BestKey, out string packed)) {
                foreach (string pair in packed.Split(';')) {
                    string[] parts = pair.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int skill) && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float level)) {
                        best[(Skills.SkillType)skill] = level;
                    }
                }
            }
            Remember(player);
        }

        // Today's levels, which may be past the best kept: from before the best were kept, or raised
        // without a level-up call (Eureka sets the level directly).
        private static void Remember(Player player) {
            bool raised = false;
            foreach (KeyValuePair<Skills.SkillType, Skills.Skill> skill in player.GetSkills().m_skillData) {
                raised |= Note(skill.Key, skill.Value.m_level);
            }
            if (raised) { SaveBest(player); }
        }

        // Death and Ascension both lower skills through here, so the levels are noted first.
        [HarmonyPatch(typeof(Skills), nameof(Skills.LowerAllSkills))]
        private static class BeforeLoss {
            private static void Prefix(Skills __instance) {
                Player player = __instance.m_player;
                if (player == null || player != Player.m_localPlayer) { return; }
                bool loaded = bestOf == player;
                LoadBest(player);
                if (loaded) { Remember(player); }
            }
        }

        private static bool Note(Skills.SkillType skill, float level) {
            if (best.TryGetValue(skill, out float known) && known >= level) { return false; }
            best[skill] = level;
            return true;
        }

        private static void SaveBest(Player player) {
            player.m_customData[BestKey] = string.Join(";", best.Select(entry => $"{(int)entry.Key}:{entry.Value.ToString("0.##", CultureInfo.InvariantCulture)}"));
        }

        /// <summary>The experience bonus for a skill, in percent: relearning below its best, and Iðavöllr's gift.</summary>
        internal static float SkillBonus(Player player, Skills.SkillType skill) {
            float bonus = Time.time < goldenUntil ? GoldenBonus : 0f;
            if (!PowerSet.Has(Power.Relearn, out float relearn)) { return bonus; }
            LoadBest(player);
            if (best.TryGetValue(skill, out float most) && player.GetSkills().m_skillData.TryGetValue(skill, out Skills.Skill now) && now.m_level < most) {
                bonus += relearn;
            }
            return bonus;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
        private static class LevelUp {
            private static void Postfix(Player __instance, Skills.SkillType skill, float level) {
                if (__instance != Player.m_localPlayer) { return; }
                LoadBest(__instance);
                if (Note(skill, level)) { SaveBest(__instance); }
            }
        }

        // ---- Survivor, Hoddmímir's Wood and Outlast ----

        internal static float IncomingFactor(Player player) => Low(player) ? 1f - PowerSet.Get(Power.LastStand) / 100f : 1f;

        /// <summary>Hoddmímir's Wood: the share of the death skill loss kept.</summary>
        internal static float DeathLossKept() => 1f - PowerSet.Get(Power.KindDeath) / 100f;

        /// <summary>Outlast's speed, in percent.</summary>
        internal static float SpeedBonus(Player player) => PowerSet.Has(Power.Outlast) && Low(player) ? OutlastSpeed : 0f;

        internal static void Tick(Player player) {
            if (!PowerSet.Has(Power.Outlast, out float regen) || !Low(player) || Time.time < nextOutlast) { return; }
            nextOutlast = Time.time + 1f;
            player.Heal(player.GetMaxHealth() * regen / 100f, false);
        }

        // ---- Iðavöllr ----

        // Food is cleared later in OnDeath, after the grave is made.
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class KeepFood {
            private static void Prefix(Player __instance) {
                if (__instance != Player.m_localPlayer || !__instance.m_nview.IsOwner()) { return; }
                if (!PowerSet.Has(Power.GoldenPieces)) {
                    __instance.m_customData.Remove(GraveFoodKey);
                    return;
                }
                // m_name is the item's prefab name; foods loaded from the save have no drop prefab set.
                IEnumerable<string> eaten = __instance.m_foods
                    .Where(food => !string.IsNullOrEmpty(food.m_name))
                    .Select(food => $"{food.m_name}:{food.m_time.ToString("0.#", CultureInfo.InvariantCulture)}");
                __instance.m_customData[GraveFoodKey] = string.Join(";", eaten);
            }
        }

        // Opening one's own grave, the first time after a death.
        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
        private static class GraveReached {
            private static void Postfix(TombStone __instance, Humanoid character) {
                Player player = Player.m_localPlayer;
                if (character != player || player == null || !__instance.IsOwner() || !PowerSet.Has(Power.GoldenPieces)) { return; }
                if (!player.m_customData.TryGetValue(GraveFoodKey, out string packed)) { return; }
                player.m_customData.Remove(GraveFoodKey);
                RestoreFood(player, packed);
                goldenUntil = Time.time + GoldenSeconds;
                PowerFx.Show(player, PowerFx.Golden, GoldenSeconds);
                PowerFx.Effect(Power.GoldenPieces, VfxRole.Burst, player.transform.position, 0.8f, shared: true);
                player.Message(MessageHud.MessageType.Center, "$al_golden_pieces");
            }
        }

        // Each food back with the time it had left, into a free stomach slot.
        private static void RestoreFood(Player player, string packed) {
            foreach (string entry in packed.Split(';')) {
                string[] parts = entry.Split(':');
                if (parts.Length != 2 || player.m_foods.Count >= 3) { continue; }
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float left)) { continue; }
                ItemDrop drop = ObjectDB.instance?.GetItemPrefab(parts[0])?.GetComponent<ItemDrop>();
                if (drop == null) { continue; }
                ItemDrop.ItemData item = drop.m_itemData.Clone();
                item.m_dropPrefab = drop.gameObject;
                if (player.m_foods.Any(food => food.m_item.m_shared.m_name == item.m_shared.m_name)) { continue; }
                player.m_foods.Add(new Player.Food() {
                    m_name = parts[0],
                    m_item = item,
                    m_time = Mathf.Min(left, item.m_shared.m_foodBurnTime),
                    m_health = item.m_shared.m_food,
                    m_stamina = item.m_shared.m_foodStamina,
                    m_eitr = item.m_shared.m_foodEitr,
                });
            }
            player.UpdateFood(0f, forceUpdate: true);
        }
    }
}
