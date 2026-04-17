using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;

namespace ValhallEternal.modules {
    internal static class Compendium {
        static readonly string HeaderColor = "#ffa64d";
        static readonly string BoonColor = "#40B850";
        static readonly string OathColor = "#CF2713";
        //static string positiveColor = Color.green.ToString();
        static TextsDialog diagTexts;


        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
        public static class TextsDialog_UpdateTextsList_Patch {
            public static void Postfix(TextsDialog __instance) {
                diagTexts = __instance;
                AddDietyPrestigeExplanations(__instance);
            }

            private static void AddDietyPrestigeExplanations(TextsDialog textsDialog) {
                string entryText = BuildPrestigeOathBoonCompendiumEntry();
                // Only add the entry if it has not already been added.
                string compendiumTopic = Localization.instance.Localize($"$ve_compendium_name");
                if (textsDialog.m_texts[0].m_topic != compendiumTopic) {
                    textsDialog.m_texts.Insert(0, new TextsDialog.TextInfo(compendiumTopic, entryText));
                }
            }
        }

        internal static void UpdateDietyPrestigeExplanations() {
            if (diagTexts == null) { return; }
            // Find the index of the compendium entry
            string compendiumTopic = Localization.instance.Localize($"$ve_compendium_name");
            int index = diagTexts.m_texts.FindIndex(t => t.m_topic == compendiumTopic);
            if (index >= 0) {
                string entryText = BuildPrestigeOathBoonCompendiumEntry();
                // Update the entry with the new information.
                diagTexts.m_texts[index] = new TextsDialog.TextInfo(compendiumTopic, entryText);
            }
        }

        private static string BuildPrestigeOathBoonCompendiumEntry() {
            StringBuilder sb = new StringBuilder();
            if (PlayerData.localPlayerConfig.TotalBoons.Count > 0) {
                sb.AppendLine($"<color={HeaderColor}>{Localization.instance.Localize("$ve_boon_section_header")}</color>");
                sb.AppendLine();
            }
            foreach (KeyValuePair<DataObjects.Boons, float> kvp in PlayerData.localPlayerConfig.TotalBoons) {
                sb.AppendLine(Localization.instance.Localize($"{DataObjects.LocalizeBoon(kvp.Key)} ($ve_level <color={BoonColor}>{kvp.Value}</color>) - {DataObjects.LocalizeBoonDesc(kvp.Key)}"));
            }

            // Section break
            if (sb.Length > 0) { sb.AppendLine(); }

            if (PlayerData.localPlayerConfig.TotalOaths.Count > 0) {
                sb.AppendLine($"<color={HeaderColor}>{Localization.instance.Localize("$ve_oath_section_header")}</color>");
                sb.AppendLine();
            }
            foreach (KeyValuePair<DataObjects.Oaths, float> kvp in PlayerData.localPlayerConfig.TotalOaths) {
                sb.AppendLine(Localization.instance.Localize($"{DataObjects.LocalizeOath(kvp.Key)} ($ve_level <color={OathColor}>{kvp.Value}</color>) - {DataObjects.LocalizeOathDesc(kvp.Key, kvp.Value)}"));
            }
            Logger.LogDebug($"Built compendium entry:\n{sb}");
            return Localization.instance.Localize(sb.ToString());
        }
    }
}
