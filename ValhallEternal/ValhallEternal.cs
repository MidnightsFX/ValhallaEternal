using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Reflection;
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
            Logger.LogInfo($"Asset Names: {string.Join(",", EmbeddedResourceBundle.GetAllAssetNames())}");
            HarmonyInstance = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            //PlayerLevelDisplays.LoadAssets();
            Locations.AddLocationsToWorldGen();
            Commands.AddCommands();
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            Jotunn.Logger.LogInfo("Live eternal in Valhalla.");
            
            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }
    }
}