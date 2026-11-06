using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AesirLineage.common;
using AesirLineage.starmap;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    internal static class Compendium {
        static readonly string HeaderColor = "#ffa64d";
        static readonly string BoonColor = "#40B850";
        static readonly string OathColor = "#CF2713";
        static readonly string DormantColor = "#8a8a8a";
        static TextsDialog diagTexts;


        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
        public static class TextsDialog_UpdateTextsList_Patch {
            public static void Postfix(TextsDialog __instance) {
                // A list set up out of sight is left alone: Auga sets up its Lore tab while the Tutorials tab shows,
                // and showing an entry there starts a coroutine on an inactive object, which Unity logs as an error.
                if (!__instance.gameObject.activeInHierarchy) { return; }
                diagTexts = __instance;
                AddDietyPrestigeExplanations(__instance);
            }

            private static void AddDietyPrestigeExplanations(TextsDialog textsDialog) {
                string entryText = BuildLineageEntry();
                // Only add the entry if it has not already been added. The list can be empty: Auga's compendium lists
                // only the texts the player knows, and a new character knows none.
                string compendiumTopic = Localization.instance.Localize($"$al_compendium_name");
                if (!textsDialog.m_texts.Exists(t => t.m_topic == compendiumTopic)) {
                    textsDialog.m_texts.Insert(0, new TextsDialog.TextInfo(compendiumTopic, entryText));
                }
            }
        }

        internal static void UpdateDietyPrestigeExplanations() {
            if (diagTexts == null) { return; }
            // Find the index of the compendium entry
            string compendiumTopic = Localization.instance.Localize($"$al_compendium_name");
            int index = diagTexts.m_texts.FindIndex(t => t.m_topic == compendiumTopic);
            if (index >= 0) {
                string entryText = BuildLineageEntry();
                // Update the entry with the new information.
                diagTexts.m_texts[index] = new TextsDialog.TextInfo(compendiumTopic, entryText);
            }
        }

        /// <summary>One power's effect at a value, e.g. "10% chance for an extra harvest when picking".</summary>
        internal static string DescribePower(Power power, float value) {
            return Localization.instance.Localize(PowerInfo.Of(power).DescriptionKey, value.ToString("0.##"));
        }

        /// <summary>How a power works, or null when the language has no extended description for it.</summary>
        internal static string PowerDetails(Power power) {
            string key = PowerInfo.Of(power).DetailsKey;
            string text = Localization.instance.Localize(key);
            // Localize answers a missing key with the key itself in brackets.
            return text == $"[{key.Substring(1)}]" ? null : text;
        }

        internal static string DeityName(Deity deity) {
            return DeityConfiguration.TryGetValue(deity, out DeityDetails details) ? Localization.instance.Localize(details.NameLocalKey) : deity.ToString();
        }

        private static string BuildLineageEntry() {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<color={HeaderColor}>$al_compendium_starmaps</color>");
            sb.AppendLine();
            bool anyDeity = false;
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                int earned = PlayerData.PointsEarned(deity);
                IReadOnlyCollection<StarNode> taken = PlayerData.Allocated(deity);
                if (earned == 0 && taken.Count == 0) { continue; }
                anyDeity = true;
                sb.AppendLine(Localization.instance.Localize("$al_compendium_points", DeityName(deity), PlayerData.PointsSpent(deity).ToString(), earned.ToString()));
                if (taken.Count > 0) {
                    sb.AppendLine("  " + string.Join(", ", taken.Select(n => n.DisplayName)));
                }
            }
            // Localized on its own: a key at the end of a line would take Windows' "\r" into its name, since Valheim's
            // key scan stops at "\n" but not "\r", and show as "[key]".
            if (!anyDeity) { sb.AppendLine(Localization.instance.Localize("$al_compendium_no_points")); }

            AppendPowers(sb, "$al_boon_section_header", BoonColor, oaths: false);
            AppendPowers(sb, "$al_oath_section_header", OathColor, oaths: true);

            return Localization.instance.Localize(sb.ToString());
        }

        private static void AppendPowers(StringBuilder sb, string header, string color, bool oaths) {
            bool wroteHeader = false;
            for (int i = 0; i < PowerInfo.Count; i++) {
                Power power = (Power)i;
                PowerInfo info = PowerInfo.Of(power);
                if (info.IsOath != oaths) { continue; }
                float value = PowerSet.Get(power);
                float dormant = PowerSet.Dormant(power);
                if (value <= 0f && dormant <= 0f) { continue; }

                if (!wroteHeader) {
                    sb.AppendLine();
                    sb.AppendLine($"<color={HeaderColor}>{header}</color>");
                    sb.AppendLine();
                    wroteHeader = true;
                }
                string sources = string.Join(", ", PowerSet.SourcesOf(power).Select(n => n.DisplayName).Concat(Boons.NamesGranting(power)));
                if (value > 0f) {
                    sb.AppendLine($"<color={color}>{info.NameKey}</color> - {DescribePower(power, value)} ({sources})");
                } else {
                    sb.AppendLine($"<color={DormantColor}>{info.NameKey}</color> - {Localization.instance.Localize("$al_power_dormant", dormant.ToString("0.##"))} ({sources})");
                }
            }
        }
    }
}
