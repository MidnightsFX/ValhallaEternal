using HarmonyLib;
using Ionic.Zlib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static AesirLineage.common.DataObjects;

namespace AesirLineage.common
{
    public static class Deities
    {
        public enum Deity
        {
            Baldur,
            Hel,
            Gefjun,
            Skaldi,
            Freya,
            Vor,
            Syn,
            // Souls, summons and shrine prefabs store a deity as its number, so new deities only go last.
            Vidarr
        }

        internal static Dictionary<Deity, DeityDetails> DeityConfiguration = new Dictionary<Deity, DeityDetails>();

        public class DeityDetails
        {
            public Sprite Image { get; set; }
            public string NameLocalKey { get; set; }
            public string DescriptionLocalKey { get; set; }

        }

        internal static void LoadDietyConfigurations()
        {
            AddDeityConfiguration(Deity.Baldur, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/baladur_nobackground.png"));
            AddDeityConfiguration(Deity.Hel, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/hel_nobackground.png"));
            AddDeityConfiguration(Deity.Gefjun, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/gefjun_nobackground.png"));
            AddDeityConfiguration(Deity.Skaldi, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/skaldi_nobackground.png"));
            AddDeityConfiguration(Deity.Freya, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/freya_nobackground.png"));
            AddDeityConfiguration(Deity.Vor, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/vor_nobackground.png"));
            AddDeityConfiguration(Deity.Syn, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/syn_nobackground.png"));
            AddDeityConfiguration(Deity.Vidarr, AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/vidarr_nobackground.png") ?? RuneSprite("eihwaz"));
        }

        // Stands in for a portrait missing from the bundle, as in a bundle built before the art was added.
        private static Sprite RuneSprite(string rune) {
            Texture2D texture = ui.RuneArt.Texture(rune);
            return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        internal static void AddDeityConfiguration(Deity deity, Sprite image)
        {
            DeityDetails details = new DeityDetails()
            {
                Image = image,
                NameLocalKey = $"$al_{deity.ToString().ToLower()}_header",
                DescriptionLocalKey = $"$al_{deity.ToString().ToLower()}_description"
            };
            DeityConfiguration.Add(deity, details);
        }

        /// <summary>
        /// A setting's "name=Deity, ..." pairs, such as "defeated_eikthyr=Gefjun", into name to deity. Names
        /// match whatever their case; bad pairs are skipped with a warning naming the setting.
        /// </summary>
        internal static Dictionary<string, Deity> ParsePairs(string raw, string setting) {
            Dictionary<string, Deity> result = new Dictionary<string, Deity>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) { return result; }
            foreach (string pair in raw.Split(',')) {
                string[] parts = pair.Split('=');
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0])
                    || !Enum.TryParse(parts[1].Trim(), ignoreCase: true, out Deity deity) || !Enum.IsDefined(typeof(Deity), deity)) {
                    if (!string.IsNullOrWhiteSpace(pair)) { Logger.LogWarning($"{setting}: can't read '{pair.Trim()}'; expected name=Deity."); }
                    continue;
                }
                result[parts[0].Trim()] = deity;
            }
            return result;
        }

        // Returns a list of Dropdown options for all of the configured Dieties
        public static List<Dropdown.OptionData> DeityOptions()
        {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (Deity diety in DeityConfiguration.Keys.ToList())
            {
                options.Add(new Dropdown.OptionData() { text = diety.ToString() });
            }
            return options;
        }
    }
}
