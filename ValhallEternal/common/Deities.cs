using HarmonyLib;
using Ionic.Zlib;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.common
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
            Syn
        }

        internal static Dictionary<Deity, DeityDetails> DeityConfiguration = new Dictionary<Deity, DeityDetails>();

        internal static Dictionary<string, GameObject> DeityEffects = new Dictionary<string, GameObject>();

        internal static Dictionary<PrestigeEffect, Dictionary<string, PrestigeEffectDetails>> PrestigeEffects = new Dictionary<PrestigeEffect, Dictionary<string, PrestigeEffectDetails>>() {
            { PrestigeEffect.Wings, new Dictionary<string, PrestigeEffectDetails>() },
            { PrestigeEffect.Aura, new Dictionary<string, PrestigeEffectDetails>() },
            { PrestigeEffect.Footprints, new Dictionary<string, PrestigeEffectDetails>() },
        };

        public class PrestigeEffectDetails
        {
            public GameObject EffectObject { get; set; }
            public LevelTextGradiant LevelText { get; set; }
            public string Title { get; set; }
        }

        public class DeityDetails
        {
            public Sprite Image { get; set; }
            public string NameLocalKey { get; set; }
            public string DescriptionLocalKey { get; set; }

        }

        internal static void LoadDietyConfigurations()
        {
            AddDeityConfiguration(Deity.Baldur, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/baladur_nobackground.png"));
            AddDeityConfiguration(Deity.Hel, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/hel_nobackground.png"));
            AddDeityConfiguration(Deity.Gefjun, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/gefjun_nobackground.png"));
            AddDeityConfiguration(Deity.Skaldi, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/skaldi_nobackground.png"));
            AddDeityConfiguration(Deity.Freya, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/freya_nobackground.png"));
            AddDeityConfiguration(Deity.Vor, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/vor_nobackground.png"));
            AddDeityConfiguration(Deity.Syn, ValhallEternal.EmbeddedResourceBundle.LoadAsset<Sprite>("assets/art/syn_nobackground.png"));

            AddEffect("leafburstverticle", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/boons/harvestbonus.prefab"));
            AddEffect("vinepulseinward", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/boons/naturegeneric.prefab"));
            AddEffect("snowswirl", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/boons/snowswirlsoft.prefab"));
            AddEffect("goldenswirl", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/boons/goldenswirl.prefab"));

            // Unused, but here for reference
            // wings will either require rigging or a "floating" approach"
            AddPrestigeEffect(PrestigeEffect.Wings, "natureWings", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/leveldisplay/wings/naturewings.prefab"));

            AddPrestigeEffect(PrestigeEffect.Aura, "natureAura", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/leveldisplay/aura/natureaura.prefab"));
            AddPrestigeEffect(PrestigeEffect.Aura, "lightAura", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/leveldisplay/aura/lightaura.prefab"));
            AddPrestigeEffect(PrestigeEffect.Aura, "fireAura", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/leveldisplay/aura/fireaura.prefab"));
            AddPrestigeEffect(PrestigeEffect.Aura, "frostAura", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/leveldisplay/aura/frostaura.prefab"));
            AddPrestigeEffect(PrestigeEffect.Aura, "darkAura", ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/leveldisplay/aura/darkaura.prefab"));
            

        }

        internal static void AddPrestigeEffect(PrestigeEffect type, string key, GameObject asset) {
            if (type != PrestigeEffect.Aura && type != PrestigeEffect.Wings && type != PrestigeEffect.Footprints) {
                Logger.LogWarning($"Refused to add ({key}) incorrect Prestige effect as a game asset.");
                return;
            }
            if (!PrestigeEffects[type].ContainsKey(key)) {
                PrestigeEffects[type].Add(key, new PrestigeEffectDetails() { EffectObject = asset });
                PrefabManager.Instance.AddPrefab(asset);
            }
        }

        internal static void AddPrestigeLevelText(string key, string topRight, string topLeft, string bottomRight, string bottomLeft) {
            if (!PrestigeEffects[PrestigeEffect.LevelColor].ContainsKey(key)) {
                PrestigeEffects[PrestigeEffect.LevelColor].Add(key, new PrestigeEffectDetails() { LevelText = new LevelTextGradiant() { BottomLeft = bottomLeft, BottomRight = bottomRight, TopLeft = topLeft, TopRight = topRight } });
            }
        }

        internal static void AddDeityConfiguration(Deity deity, Sprite image)
        {
            DeityDetails details = new DeityDetails()
            {
                Image = image,
                NameLocalKey = $"$ve_{deity.ToString().ToLower()}_header",
                DescriptionLocalKey = $"$ve_{deity.ToString().ToLower()}_description"
            };
            DeityConfiguration.Add(deity, details);
        }

        internal static void AddEffect(string key, GameObject asset) {
            if (!DeityEffects.ContainsKey(key)) {
                DeityEffects.Add(key, asset);
                PrefabManager.Instance.AddPrefab(asset);
            }
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

        //internal static class PrestigeEffectsAvailableOnPlayer {
        //    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        //    private static void Postfix(Player __instance) {
        //        foreach (KeyValuePair<string, GameObject> kvp in PrestigeEffects[PrestigeEffect.Wings]) {
        //            GameObject added = UnityEngine.Object.Instantiate(kvp.Value, __instance.gameObject.transform);
        //            added.SetActive(false);
        //        }
        //    }
        //}
    }
}
