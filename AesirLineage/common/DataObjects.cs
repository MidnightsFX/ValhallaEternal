using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AesirLineage.modules;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static AesirLineage.common.Deities;

namespace AesirLineage.common
{
    public static class DataObjects {
        public static readonly string CustomLevelZKey = "ALLevel";
        public static readonly string CustomDataKey = "AL_DATA";
        /// <summary>Boss keys whose soul the player has claimed since their last Ascension, comma-separated.</summary>
        public static readonly string ClaimsZKey = "AL_Claims";
        /// <summary>The player's looks as "Aura=Gefjun|Wings=Hel", read by other clients.</summary>
        public static readonly string CosmeticsZKey = "AL_Cosmetics";
        public static IDeserializer yamldeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        /// <summary>
        /// For character saves, which outlive the fields that get removed: what it doesn't know is skipped,
        /// rather than the whole save failing to load. Config files keep the strict one, so typos show.
        /// </summary>
        public static IDeserializer saveDeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
        public static ISerializer yamlserializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build();
        public static ISerializer yamlserializerJsonCompat = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).JsonCompatible().Build();

        public static Sprite boonbackground;
        public static Sprite hastenDeath;

        public static void LoadAssets()
        {
            boonbackground = AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/bottom_border_divider.png");
            hastenDeath = AesirLineage.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/hastenDeath.png");

            powers.KillPowers.RegisterStatusEffects();
            powers.PowerFx.RegisterStatusEffects();
            Boons.RegisterStatusEffects();
            Quests.RegisterStatusEffects();
            visuals.DeityVfx.Load();
        }

        public static readonly string HarvestingBonusColor = "#ffc87c";
        public static readonly string FishBountyBonusColor = "#619CFF";

        public static readonly string None = "None";

        public interface IProbability
        {
            public string Name { get; set; }
            public float SelectionWeight { get; set; }
        }

        public class LevelTextGradiant
        {
            public string TopLeft { get; set; }
            public string BottomLeft { get; set; }
            public string TopRight { get; set; }
            public string BottomRight { get; set; }
        }
    }
}
