using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using AesirLineage.common;
using AesirLineage.starmap;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// A deity's dream: when a player sleeps with no inspiration on, a deity they have earned enough points
    /// with may send a dream, which leaves one of its inspirations when they wake.
    /// </summary>
    /// <remarks>
    /// The dream is chosen where the game shows its own dream text while sleeping, and replaces that text.
    /// A boss's dream cinematic plays there too, and is left alone. The inspiration starts on waking, in
    /// <c>Player.SetSleeping</c>.
    /// </remarks>
    internal static class Dreams {
        private static Deity dreamDeity;
        private static Boon dreamt;

        /// <summary>Set by <c>al-dream</c>: the next sleep brings a dream whatever the chance.</summary>
        internal static bool ForceNext;

        /// <summary>The deities that can send the player a dream, each with its weight: the points earned with it.</summary>
        internal static List<(Deity deity, int weight)> Eligible() {
            List<(Deity, int)> eligible = new List<(Deity, int)>();
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                int points = PlayerData.PointsEarned(deity);
                if (points < ValConfig.InspirationMinPoints.Value) { continue; }
                if (!ShrineData.All.TryGetValue(deity, out DeityShrine shrine) || shrine == null || shrine.Inspirations.Count == 0) { continue; }
                eligible.Add((deity, Math.Max(1, points)));
            }
            return eligible;
        }

        // Whether tonight brings a dream, and whose. Only one inspiration can be on at a time.
        private static bool TryDream(out Deity deity, out Boon boon) {
            deity = default;
            boon = null;
            if (Player.m_localPlayer == null || Boons.Active.Any(active => active.Kind == BoonKind.Inspiration)) { return false; }
            List<(Deity deity, int weight)> eligible = Eligible();
            if (eligible.Count == 0) { return false; }
            if (!ForceNext && UnityEngine.Random.value * 100f >= ValConfig.InspirationChance.Value) { return false; }
            ForceNext = false;

            int roll = UnityEngine.Random.Range(0, eligible.Sum(entry => entry.weight));
            foreach ((Deity candidate, int weight) in eligible) {
                roll -= weight;
                if (roll >= 0) { continue; }
                deity = candidate;
                break;
            }
            List<Boon> inspirations = ShrineData.All[deity].Inspirations;
            boon = inspirations[UnityEngine.Random.Range(0, inspirations.Count)];
            return true;
        }

        // The boon's dream line, or a plain one when it has none or its translation is missing.
        private static string DreamLine(Deity deity, Boon boon) {
            string line = boon.Dream;
            if (!string.IsNullOrEmpty(line) && line.StartsWith("$")) { line = Localization.instance.Localize(line); }
            if (string.IsNullOrEmpty(line) || line.StartsWith("[")) {
                line = Localization.instance.Localize("$al_dream_plain", Compendium.DeityName(deity));
            }
            return line;
        }

        // Sleep starts afresh, so a dream from a sleep that never ended doesn't carry over.
        [HarmonyPatch(typeof(Player), nameof(Player.SetSleeping))]
        private static class Sleep {
            private static void Postfix(Player __instance, bool sleep) {
                if (__instance != Player.m_localPlayer) { return; }
                if (sleep) {
                    dreamt = null;
                    return;
                }
                if (dreamt == null) { return; }
                Boons.Start(dreamDeity, BoonKind.Inspiration, dreamt);
                dreamt = null;
            }
        }

        // After the game has played a boss's cinematic or picked its own dream text. The game shows its
        // text by fading the field in and out; when it picked none, this starts that itself.
        [HarmonyPatch(typeof(SleepText), nameof(SleepText.ShowDreamText))]
        private static class DreamText {
            private static void Postfix(SleepText __instance) {
                if (CinematicsManager.IsPlaying() || !TryDream(out Deity deity, out Boon boon)) { return; }
                dreamDeity = deity;
                dreamt = boon;
                __instance.m_dreamField.text = DreamLine(deity, boon);
                if (__instance.m_dreamField.enabled) { return; }
                __instance.m_dreamField.enabled = true;
                __instance.Invoke(nameof(SleepText.DelayedCrossFadeStart), 0.1f);
                __instance.Invoke(nameof(SleepText.HideDreamText), 6.5f);
            }
        }
    }
}
