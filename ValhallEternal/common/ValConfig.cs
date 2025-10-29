using BepInEx;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace ValhallEternal.common;

public class ValConfig
{
    public static ConfigFile cfg;
    public static ConfigEntry<bool> EnableDebugMode;
    public static ConfigEntry<string> TributeLocationBiome;
    public static ConfigEntry<int> MaxTributeLocationsGeneration;
    public static ConfigEntry<float> MinDistanceBetweenTributeLocations;

    public static ConfigEntry<float> LocalLevelDisplayOffset;

    const string cfgFolder = "Valhalla_Eternal";
    const string PrestigeCfg = "Prestige.yaml";
    internal static String prestigeCfgsPath = Path.Combine(Paths.ConfigPath, cfgFolder, PrestigeCfg);

    private static CustomRPC prestigeRPC;

    public ValConfig(ConfigFile Config)
    {
        // ensure all the config values are created
        cfg = Config;
        cfg.SaveOnConfigSet = true;
        CreateConfigValues(Config);
        SetupConfigRPCs();
        LoadYamlConfigs();
    }

    public static string GetSecondaryConfigDirectoryPath()
    {
        var patchesFolderPath = Path.Combine(Paths.ConfigPath, cfgFolder);
        var dirInfo = Directory.CreateDirectory(patchesFolderPath);

        return dirInfo.FullName;
    }

    public void SetupConfigRPCs() {
        prestigeRPC = NetworkManager.Instance.AddRPC("VALHALL_PRESTIGE", OnServerRecieveConfigs, OnClientReceivePrestigeConfigs);

        SynchronizationManager.Instance.AddInitialSynchronization(prestigeRPC, SendDeathChoices);
    }

    // Create Configuration and load it.
    private void CreateConfigValues(ConfigFile Config)
    {
        TributeLocationBiome = BindServerConfig("TributeLocation", "TributeLocationBiome", "Mistlands", "The biome which the tribute shrine will generate in.", acceptableValues: new AcceptableValueList<string>(Enum.GetNames(typeof(Heightmap.Biome))) { });
        MaxTributeLocationsGeneration = BindServerConfig("TributeLocation", "MaxTributeLocationsGeneration", 10, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MinDistanceBetweenTributeLocations = BindServerConfig("TributeLocation", "MinDistanceBetweenTributeLocations", 500f, "The minimum distance between any tribute locations. Larger values make this more spread out.", false, 0, 5000);
        LocalLevelDisplayOffset = BindServerConfig("LevelDisplay", "LocalLevelDisplayOffset", 50f, "The x pixel offset for the local players level display.");
        // Debugmode
        EnableDebugMode = Config.Bind("Client config", "EnableDebugMode", false,
            new ConfigDescription("Enables Debug logging.",
            null,
            new ConfigurationManagerAttributes { IsAdvanced = true }));
        EnableDebugMode.SettingChanged += Logger.enableDebugLogging;
        Logger.CheckEnableDebugLogging();
    }

    internal void LoadYamlConfigs()
    {
        string externalConfigFolder = ValConfig.GetSecondaryConfigDirectoryPath();
        string[] presentFiles = Directory.GetFiles(externalConfigFolder);
        bool foundDeathChoices = false;

        foreach (string configFile in presentFiles)
        {
            if (configFile.Contains(PrestigeCfg))
            {
                Logger.LogDebug($"Found Valhalla configurations: {configFile}");
                prestigeCfgsPath = configFile;
                foundDeathChoices = true;
            }
        }

        if (foundDeathChoices == false)
        {
            Logger.LogDebug("Valhalla Prestige Configs missing, recreating.");
            using (StreamWriter writetext = new StreamWriter(prestigeCfgsPath))
            {
                String header = @"#################################################
# Valhalla Eternal Prestige Configuration File
#################################################
";
                writetext.WriteLine(header);
                //writetext.WriteLine(DeathConfigurationData.DeathLevelsYamlDefaultConfig());
            }
        }

        SetupFileWatcher(PrestigeCfg);
    }

    private void SetupFileWatcher(string filtername)
    {
        FileSystemWatcher fw = new FileSystemWatcher();
        fw.Path = ValConfig.GetSecondaryConfigDirectoryPath();
        fw.NotifyFilter = NotifyFilters.LastWrite;
        fw.Filter = filtername;
        fw.Changed += new FileSystemEventHandler(UpdateConfigFileOnChange);
        fw.Created += new FileSystemEventHandler(UpdateConfigFileOnChange);
        fw.Renamed += new RenamedEventHandler(UpdateConfigFileOnChange);
        fw.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fw.EnableRaisingEvents = true;
    }

    private static void UpdateConfigFileOnChange(object sender, FileSystemEventArgs e)
    {
        if (SynchronizationManager.Instance.PlayerIsAdmin == false)
        {
            Logger.LogInfo("Player is not an admin, and not allowed to change local configuration. Ignoring.");
            return;
        }
        if (!File.Exists(e.FullPath)) { return; }

        string filetext = File.ReadAllText(e.FullPath);
        var fileInfo = new FileInfo(e.FullPath);
        Logger.LogDebug($"Filewatch changes from: ({fileInfo.Name}) {fileInfo.FullName}");
        switch (fileInfo.Name)
        {
            case PrestigeCfg:
                Logger.LogDebug("Triggering Death Choices Prestige config reload.");
                //DeathConfigurationData.UpdateDeathLevelsConfig(filetext);
                prestigeRPC.SendPackage(ZNet.instance.m_peers, SendFileAsZPackage(e.FullPath));
                break;
        }
    }

    private static IEnumerator OnClientReceivePrestigeConfigs(long sender, ZPackage package)
    {
        // Write config file
        var yaml = package.ReadString();
        //DeathConfigurationData.UpdateDeathLevelsConfig(yaml);
        //DeathConfigurationData.WriteDeathChoices();
        yield return null;
    }

    private static IEnumerator OnServerRecieveConfigs(long sender, ZPackage package)
    {
        yield return null;
    }

    private static ZPackage SendFileAsZPackage(string filepath)
    {
        string filecontents = File.ReadAllText(filepath);
        ZPackage package = new ZPackage();
        package.Write(filecontents);
        return package;
    }
    private static ZPackage SendDeathChoices()
    {
        // Read config file
        return SendFileAsZPackage("");
    }

    /// <summary>
    ///  Helper to bind configs for bool types
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="acceptableValues"></param>>
    /// <param name="advanced"></param>
    /// <returns></returns>
    public static ConfigEntry<bool> BindServerConfig(string catagory, string key, bool value, string description, AcceptableValueBase acceptableValues = null, bool advanced = false)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }

    /// <summary>
    /// Helper to bind configs for int types
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="advanced"></param>
    /// <param name="valmin"></param>
    /// <param name="valmax"></param>
    /// <returns></returns>
    public static ConfigEntry<int> BindServerConfig(string catagory, string key, int value, string description, bool advanced = false, int valmin = 0, int valmax = 150)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
            new AcceptableValueRange<int>(valmin, valmax),
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }

    /// <summary>
    /// Helper to bind configs for float types
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="advanced"></param>
    /// <param name="valmin"></param>
    /// <param name="valmax"></param>
    /// <returns></returns>
    public static ConfigEntry<float> BindServerConfig(string catagory, string key, float value, string description, bool advanced = false, float valmin = 0, float valmax = 150)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(description,
            new AcceptableValueRange<float>(valmin, valmax),
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }

    /// <summary>
    /// Helper to bind configs for strings
    /// </summary>
    /// <param name="config_file"></param>
    /// <param name="catagory"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="description"></param>
    /// <param name="advanced"></param>
    /// <returns></returns>
    public static ConfigEntry<string> BindServerConfig(string catagory, string key, string value, string description, AcceptableValueList<string> acceptableValues = null, bool advanced = false)
    {
        return cfg.Bind(catagory, key, value,
            new ConfigDescription(
                description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
            );
    }
}