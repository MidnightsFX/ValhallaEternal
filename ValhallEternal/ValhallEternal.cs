using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ValhallEternal : BaseUnityPlugin
    {
        public const string PluginGUID = "MidnightsFX.ValhallaEternal";
        public const string PluginName = "ValhallEternal";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        public static AssetBundle EmbeddedResourceBundle;
        internal static Harmony Harmony = new Harmony(PluginGUID);
        public static ManualLogSource Log;
        public static Harmony HarmonyInstance { get; private set; }
        public ValConfig cfg;

        public void Awake()
        {
            Log = this.Logger;
            cfg = new ValConfig(Config);

            EmbeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("ValhallEternal.embedded.valeternal", typeof(ValhallEternal).Assembly);
            Logger.LogInfo($"Asset Names: {string.Join(",\n", EmbeddedResourceBundle.GetAllAssetNames())}");
            Deities.LoadDietyConfigurations();
            HarmonyInstance = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
            AddLocalizations();
            SacrificeData.SetupSacrificeData();
            //PlayerLevelDisplays.LoadAssets();
            //Locations.AddLocationsToWorldGen();
            Commands.AddCommands();
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            Jotunn.Logger.LogInfo("Live eternal in Valhalla.");
            
            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }

        // This loads all localizations within the localization directory.
        // Localizations should be plain JSON objects with each of the two required entries being seperate eg:
        // "item_sword": "sword-name-here",
        // "item_sword_description": "sword-description-here",
        // the localization file itself should be a casematched language as defined by one of the "folder" language names from here:
        // https://valheim-modding.github.io/Jotunn/data/localization/language-list.html
        private void AddLocalizations()
        {
            Localization = LocalizationManager.Instance.GetLocalization();

            // Ensure localization folder exists
            var translationFolder = Path.Combine(BepInEx.Paths.ConfigPath, "ValhallEternal", "localizations");
            Directory.CreateDirectory(translationFolder);
            foreach (string embeddedResouce in typeof(ValhallEternal).Assembly.GetManifestResourceNames())
            {
                if (!embeddedResouce.Contains("localizations")) { continue; }
                // Read the localization file

                string localization = ReadEmbeddedResourceFile(embeddedResouce);
                // since I use comments in the localization that are not valid JSON those need to be stripped
                string cleaned_localization = Regex.Replace(localization, @"\/\/.*", "");
                Dictionary<string, string> internal_localization = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, string>>(cleaned_localization);
                // Just the localization name
                var localization_name = embeddedResouce.Split('.');
                if (File.Exists($"{translationFolder}/{localization_name[2]}.json"))
                {
                    string cached_translation_file = File.ReadAllText($"{translationFolder}/{localization_name[2]}.json");
                    try
                    {
                        Dictionary<string, string> cached_localization = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, string>>(cached_translation_file);
                        UpdateLocalizationWithMissingKeys(internal_localization, cached_localization);
                        Logger.LogDebug($"Reading {translationFolder}/{localization_name[2]}.json");
                        File.WriteAllText($"{translationFolder}/{localization_name[2]}.json", SimpleJson.SimpleJson.SerializeObject(cached_localization));
                        string updated_local_translation = File.ReadAllText($"{translationFolder}/{localization_name[2]}.json");
                        Localization.AddJsonFile(localization_name[2], updated_local_translation);
                    }
                    catch
                    {
                        File.WriteAllText($"{translationFolder}/{localization_name[2]}.json", cleaned_localization);
                        Logger.LogDebug($"Reading {embeddedResouce}");
                        Localization.AddJsonFile(localization_name[2], cleaned_localization);
                    }
                }
                else
                {
                    File.WriteAllText($"{translationFolder}/{localization_name[2]}.json", cleaned_localization);
                    Logger.LogDebug($"Reading {embeddedResouce}");
                    Localization.AddJsonFile(localization_name[2], cleaned_localization);
                }
                Logger.LogDebug($"Added localization: '{localization_name[2]}'");
            }
        }

        private Dictionary<string, string> UpdateLocalizationWithMissingKeys(Dictionary<string, string> internal_localization, Dictionary<string, string> cached_localization)
        {
            if (internal_localization.Keys != cached_localization.Keys)
            {
                List<string> extra_keys = cached_localization.Keys.ToList();
                foreach (KeyValuePair<string, string> entry in internal_localization)
                {
                    extra_keys.Remove(entry.Key);
                    if (!cached_localization.ContainsKey(entry.Key))
                    {
                        Logger.LogDebug($"Adding missing localization key {entry.Key}");
                        cached_localization.Add(entry.Key, entry.Value);
                    }
                }
                if (extra_keys.Count > 0)
                {
                    Logger.LogDebug($"Removing extra keys {string.Join(",", extra_keys)}.");
                    foreach (string key in extra_keys)
                    {
                        cached_localization.Remove(key);
                    }
                }
            }
            return cached_localization;
        }

        // This reads an embedded file resouce name, these are all resouces packed into the DLL
        // they all have a format following this:
        // ValheimArmory.localizations.English.json
        private string ReadEmbeddedResourceFile(string filename)
        {
            using (var stream = typeof(ValhallEternal).Assembly.GetManifestResourceStream(filename))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}