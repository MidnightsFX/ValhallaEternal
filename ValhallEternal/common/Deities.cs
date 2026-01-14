using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    }
}
