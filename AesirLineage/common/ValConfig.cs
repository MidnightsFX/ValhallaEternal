using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AesirLineage.starmap;
using AesirLineage.ui;

namespace AesirLineage.common;

public class ValConfig
{
    public static ConfigFile cfg;
    public static ConfigEntry<bool> EnableDebugMode;
    public static ConfigEntry<string> TributeLocationBiome;
    public static ConfigEntry<int> MaxMeadowsLocations;
    public static ConfigEntry<int> MaxBlackForestLocations;
    public static ConfigEntry<int> MaxSwampLocations;
    public static ConfigEntry<int> MaxMountainLocations;
    public static ConfigEntry<int> MaxPlainsLocations;
    public static ConfigEntry<int> MaxMistlandsLocations;
    public static ConfigEntry<int> MaxAshlandsLocations;
    public static ConfigEntry<int> MaxDeepNorthLocations;
    public static ConfigEntry<float> MinDistanceBetweenTributeLocations;

    public static ConfigEntry<bool> KeyRequirementsHideChoices;
    public static ConfigEntry<bool> UseStarMapsFile;
    public static ConfigEntry<bool> UseShrinesFile;

    public static ConfigEntry<int> PointsPerSoul;
    public static ConfigEntry<int> SoulLifetimeMinutes;
    public static ConfigEntry<int> SoulReclaimRange;
    public static ConfigEntry<int> MaxAscension;
    public static ConfigEntry<int> AscensionSkillLoss;
    public static ConfigEntry<bool> AscensionForgetsRecipes;
    public static ConfigEntry<bool> AscensionReturnsToStart;
    public static ConfigEntry<bool> AscensionRemovesBossKeys;
    public static ConfigEntry<bool> AscensionAdvancesRagnarok;
    public static ConfigEntry<float> AscensionRagnarokDays;

    public static ConfigEntry<bool> EnableLooks;
    public static ConfigEntry<string> SoulBosses;

    public static ConfigEntry<int> LuckyPointsPerDeity;
    public static ConfigEntry<float> StarredKillChance;
    public static ConfigEntry<int> StarredKillMinStars;
    public static ConfigEntry<float> CraftChance;
    public static ConfigEntry<float> HarvestChance;
    public static ConfigEntry<string> BiomeDeities;
    public static ConfigEntry<string> Minibosses;
    public static ConfigEntry<int> MinibossPoints;
    public static ConfigEntry<string> RagnarokHeralds;
    public static ConfigEntry<int> RagnarokHeraldPoints;

    public static ConfigEntry<int> QuestCooldownDays;
    public static ConfigEntry<int> QuestPoints;

    public static ConfigEntry<bool> EnableProgressionLock;
    public static ConfigEntry<string> ProgressionLockSource;
    public static ConfigEntry<bool> LockEquipment;
    public static ConfigEntry<bool> LockConsumables;
    public static ConfigEntry<bool> LockCrafting;
    public static ConfigEntry<bool> LockBuilding;
    public static ConfigEntry<string> ProgressionLockItems;

    public static ConfigEntry<int> BlessingMinutes;
    public static ConfigEntry<int> InspirationMinutes;
    public static ConfigEntry<float> InspirationChance;
    public static ConfigEntry<int> InspirationMinPoints;

    public static ConfigEntry<float> LocalLevelDisplayOffset;
    public static ConfigEntry<KeyboardShortcut> StarMapKey;
    public static ConfigEntry<KeyboardShortcut> CallShipKey;
    public static ConfigEntry<bool> ShowQuickConfigButton;
    internal static ConfigEntry<FirstRunMode> FirstRunPopup;

    public const string cfgFolder = "AesirLineage";
    const string shrinesyml = "Shrines.yaml";
    const string starmapsyml = "StarMaps.yaml";
    internal static String shrinesCfgPath = Path.Combine(Paths.ConfigPath, cfgFolder, shrinesyml);
    internal static String starMapsCfgPath = Path.Combine(Paths.ConfigPath, cfgFolder, starmapsyml);

    private static CustomRPC configRPC;

    // True while this client runs on what a server sent, so leaving the server restores its own files.
    private static bool usingServerConfig;

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
        configRPC = NetworkManager.Instance.AddRPC("AL_CONFIG", OnServerRecieveConfigs, OnClientReceiveConfigs);

        SynchronizationManager.Instance.AddInitialSynchronization(configRPC, ConfigPackage);
    }

    // Create Configuration and load it.
    private void CreateConfigValues(ConfigFile Config)
    {
        MaxMeadowsLocations = BindServerConfig("TributeLocations", "MaxMeadowsLocations", 10, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxBlackForestLocations = BindServerConfig("TributeLocations", "MaxBlackForestLocations", 15, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxSwampLocations = BindServerConfig("TributeLocations", "MaxSwampLocations", 15, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxMountainLocations = BindServerConfig("TributeLocations", "MaxMountainLocations", 15, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxPlainsLocations = BindServerConfig("TributeLocations", "MaxPlainsLocations", 15, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxMistlandsLocations = BindServerConfig("TributeLocations", "MaxMistlandsLocations", 15, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxAshlandsLocations = BindServerConfig("TributeLocations", "MaxAshlandsLocations", 7, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MaxDeepNorthLocations = BindServerConfig("TributeLocations", "MaxDeepNorthLocations", 7, "The maximum number of tribute locations that the world generator will try to place, these are not gaurenteed.", false, 0, 50);
        MinDistanceBetweenTributeLocations = BindServerConfig("TributeLocations", "MinDistanceBetweenTributeLocations", 700f, "The minimum distance between any tribute locations. Larger values make this more spread out.", false, 0, 5000);

        LocalLevelDisplayOffset = BindServerConfig("LevelDisplay", "LocalLevelDisplayOffset", 50f, "The x pixel offset for the local players level display.");

        // Sacrifice Discovery
        KeyRequirementsHideChoices = BindServerConfig("Discovery", "KeyRequirementsHideChoices", true, "Whether or not required global or private keys cause options to be hidden if not met.");

        UseStarMapsFile = BindServerConfig("StarMaps", "UseStarMapsFile", false, $"Use {starmapsyml} instead of the built-in star maps. Clients always use the server's maps.");
        UseStarMapsFile.SettingChanged += (_, _) => {
            // Jotunn also fires this on clients when it syncs admin-only values; only the side that decides reloads.
            if (ZNet.instance != null && !ZNet.instance.IsServer()) { return; }
            StarMaps.LoadLocal();
            BroadcastConfigs();
        };
        UseShrinesFile = BindServerConfig("Shrines", "UseShrinesFile", false, $"Use {shrinesyml} instead of the built-in shrines: their blessings, dreams, resets and Ascension. Clients always use the server's shrines.");
        UseShrinesFile.SettingChanged += (_, _) => {
            if (ZNet.instance != null && !ZNet.instance.IsServer()) { return; }
            ShrineData.LoadLocal();
            BroadcastConfigs();
        };

        PointsPerSoul = BindServerConfig("SoulsOfPower", "PointsPerSoul", 1, "Star points a player gains for their deity by claiming a boss's Soul of Power, once per boss per run.", false, 0, 10);
        SoulLifetimeMinutes = BindServerConfig("SoulsOfPower", "SoulLifetimeMinutes", 30, "Minutes a Soul of Power stays after its boss dies. 0 keeps it until the world is reset.", false, 0, 1440);
        SoulReclaimRange = BindServerConfig("SoulsOfPower", "SoulReclaimRange", 60, "A boss leaves its soul only when a player this many metres from it hasn't claimed that boss's soul this run. Players this close also count as taking part in a boss's or miniboss's death, for quests and minibosses.", false, 10, 300);
        SoulBosses = BindServerConfig("SoulsOfPower", "SoulBosses",
            // Kall Fimbulbringer sets no defeat key, so his last stage is listed by its prefab name.
            "defeated_eikthyr=Gefjun,defeated_gdking=Vor,defeated_bonemass=Baldur,defeated_dragon=Skaldi,defeated_goblinking=Syn,defeated_queen=Freya,defeated_fader=Hel,FrozenKing_p3=Vidarr",
            "Which deity each boss's soul belongs to, as bosskey=Deity pairs separated by commas. The key is the global key the boss sets when it dies, or the creature's prefab name for a boss that sets none; bosses not listed leave no soul.");

        LuckyPointsPerDeity = BindServerConfig("Deeds", "LuckyPointsPerDeity", 5, "Star points each deity can give per run by chance, from starred kills, crafts and harvests together. 0 turns these chances off.", false, 0, 50);
        StarredKillChance = BindServerConfig("Deeds", "StarredKillChance", 5f, "Percent chance a qualifying starred kill gives a star point to the deity of the biome it died in. It must be hostile, not tamable, and in a biome at or past the last one whose boss you have beaten.", false, 0f, 100f);
        StarredKillMinStars = BindServerConfig("Deeds", "StarredKillMinStars", 2, "Fewest stars a kill needs to have a chance of a star point.", false, 1, 10);
        CraftChance = BindServerConfig("Deeds", "CraftChance", 1f, "Percent chance a craft or upgrade gives a star point to a random deity whose biome you have discovered.", false, 0f, 100f);
        HarvestChance = BindServerConfig("Deeds", "HarvestChance", 0.2f, "Percent chance a harvest gives a star point to the deity of the biome it grew in: picking a crop, berry, mushroom or herb, felling a tree, or breaking a rock or ore deposit.", false, 0f, 100f);
        BiomeDeities = BindServerConfig("Deeds", "BiomeDeities",
            "Meadows=Gefjun,BlackForest=Vor,Swamp=Baldur,Mountain=Skaldi,Plains=Syn,Mistlands=Freya,AshLands=Hel,DeepNorth=Vidarr",
            "Which deity hears deeds done in each biome, as Biome=Deity pairs separated by commas. A biome not listed, such as Ocean, belongs to no deity.");
        Minibosses = BindServerConfig("Deeds", "Minibosses",
            "Skeleton_Hildir=Vor,Fenring_Cultist_Hildir=Skaldi,GoblinShaman_Hildir+GoblinBruteBros=Syn,FallenValkyrie=Hel",
            "Which creatures are minibosses and whose, as prefab=Deity pairs separated by commas. Join prefabs with + to count them as one miniboss. The first kill of each per run gives its deity star points.");
        MinibossPoints = BindServerConfig("Deeds", "MinibossPoints", 1, "Star points a miniboss's first kill per run gives its deity.", false, 0, 10);
        // Eternal Hall's heralds, one per biome, named EH_Herald_<Biome>. Each goes to its biome's deity, the
        // Ocean's to Vidarr, who outlives Ragnarok.
        RagnarokHeralds = BindServerConfig("Deeds", "RagnarokHeralds",
            "EH_Herald_Meadows=Gefjun,EH_Herald_BlackForest=Vor,EH_Herald_Swamp=Baldur,EH_Herald_Mountain=Skaldi,EH_Herald_Plains=Syn,EH_Herald_Mistlands=Freya,EH_Herald_AshLands=Hel,EH_Herald_DeepNorth=Vidarr,EH_Herald_Ocean=Vidarr",
            "Eternal Hall's heralds of Ragnarok and the deity each one honours, as prefab=Deity pairs separated by commas. Unlike minibosses, every kill counts.");
        RagnarokHeraldPoints = BindServerConfig("Deeds", "RagnarokHeraldPoints", 1, "Star points each herald of Ragnarok killed gives its deity.", false, 0, 10);

        QuestCooldownDays = BindServerConfig("Quests", "QuestCooldownDays", 5, "In-game days after taking a deity's quest before its shrine gives the next one. 0 gives the next as soon as the last is done.", false, 0, 100);
        QuestPoints = BindServerConfig("Quests", "QuestPoints", 1, "Star points a shrine quest gives its deity when done.", false, 0, 10);

        EnableProgressionLock = BindServerConfig("ProgressionLock", "EnableProgressionLock", false,
            "Seals gear, food and build pieces made from a biome's materials until the boss before that biome is defeated: Black Forest materials need Eikthyr, the Swamp's need the Elder, and so on. The settings below choose what is sealed.");
        ProgressionLockSource = BindServerConfig("ProgressionLock", "ProgressionLockSource", "Personal",
            "Whose boss kills open the seals. Personal: only the bosses the player helped defeat, by landing a hit or standing nearby when it fell; Ascending seals them again when it removes boss keys. World: also every boss defeated in the world.",
            new AcceptableValueList<string>("Personal", "World"));
        LockEquipment = BindServerConfig("ProgressionLock", "LockEquipment", true, "Seals equipping weapons, armor, tools and ammo, and opening doors with a boss's key. Sealed gear a player is wearing comes off.");
        LockConsumables = BindServerConfig("ProgressionLock", "LockConsumables", true, "Seals eating food and drinking meads.");
        LockCrafting = BindServerConfig("ProgressionLock", "LockCrafting", true, "Seals crafting and upgrading at every crafting station, cooking on cooking stations and ovens, and fermenting meads.");
        LockBuilding = BindServerConfig("ProgressionLock", "LockBuilding", true, "Seals placing build pieces.");
        ProgressionLockItems = BindServerConfig("ProgressionLock", "ProgressionLockItems", "",
            "Changes which boss an item or build piece needs, as prefab=bosskey pairs separated by commas, such as SwordIron=defeated_bonemass. The boss keys are defeated_eikthyr, defeated_gdking, defeated_bonemass, defeated_dragon, defeated_goblinking, defeated_queen and defeated_fader. Leave the boss key empty to never seal the prefab. A material's boss also seals everything made from it.");

        BlessingMinutes = BindServerConfig("Blessings", "BlessingMinutes", 10, "Minutes of play a shrine's blessing lasts.", false, 1, 120);
        InspirationMinutes = BindServerConfig("Blessings", "InspirationMinutes", 30, "Minutes of play the inspiration a deity's dream leaves lasts.", false, 1, 240);
        InspirationChance = BindServerConfig("Blessings", "InspirationChance", 15f, "Percent chance of a deity's dream each time a player sleeps without an inspiration already on.", false, 0f, 100f);
        InspirationMinPoints = BindServerConfig("Blessings", "InspirationMinPoints", 4, "Star points a player must have earned with a deity before it can send them a dream.", false, 1, 100);

        MaxAscension = BindServerConfig("Ascension", "MaxAscension", 6, "How many times a character can Ascend with each deity. Levels 1 to 6 each open one apex star.", false, 1, 10);
        AscensionSkillLoss = BindServerConfig("Ascension", "AscensionSkillLoss", 50, "Percent of every skill lost on Ascending.", false, 0, 100);
        AscensionForgetsRecipes = BindServerConfig("Ascension", "AscensionForgetsRecipes", true, "Whether Ascending forgets the character's known recipes and materials.");
        AscensionReturnsToStart = BindServerConfig("Ascension", "AscensionReturnsToStart", true, "Whether Ascending sends the character back to the start temple.");
        AscensionRemovesBossKeys = BindServerConfig("Ascension", "AscensionRemovesBossKeys", true,
            "Whether Ascending removes the boss keys it requires, so the bosses must be defeated again. On a shared world these are the world's keys, so this resets boss progress for every player.");
        AscensionAdvancesRagnarok = BindServerConfig("Ascension", "AscensionAdvancesRagnarok", true,
            "With Eternal Hall's Ragnarok cycle running, each Ascension brings Ragnarok closer by AscensionRagnarokDays. If that would pass the end of the countdown, Ragnarok comes in half a day instead. Does nothing without Eternal Hall.");
        AscensionRagnarokDays = BindServerConfig("Ascension", "AscensionRagnarokDays", 3f, "In-game days each Ascension brings Ragnarok closer, with AscensionAdvancesRagnarok on.", false, 0f, 30f);

        EnableLooks = BindServerConfig("Looks", "EnableLooks", true,
            "Whether deity looks are shown: the auras, wings, footprints, weapon glows and kill effects that stars and Ascension give. Off, nobody shows any and the star map has no Looks button. Players keep the looks they earned, so turning this back on brings them back.");

        StarMapKey = Config.Bind("Client config", "StarMapKey", new KeyboardShortcut(UnityEngine.KeyCode.K),
            "Opens and closes the star maps.");
        CallShipKey = Config.Bind("Client config", "CallShipKey", new KeyboardShortcut(UnityEngine.KeyCode.Y),
            "Calls the last ship you steered to the water nearest you, with Baldur's Ship-Caller star.");

        ShowQuickConfigButton = Config.Bind("Client config", "ShowQuickConfigButton", true,
            "Shows Aesir Lineage in the Mod Config button's list, bottom right of the main menu and the pause menu, for hosts and server admins.");
        ShowQuickConfigButton.SettingChanged += (_, _) => QuickSetup.ApplyRegistration();
        FirstRunPopup = Config.Bind("Client config", "FirstRunPopup", FirstRunMode.Auto,
            "When the setup tutorial opens by itself on the main menu. Auto: once for each person on this computer, whatever the mod manager profile. ShowNextLaunch: on the next launch, then back to Auto. Never: not in this profile.");

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
        bool foundShrines = false;
        bool foundStarMaps = false;

        foreach (string configFile in presentFiles) {
            if (configFile.Contains(shrinesyml)) {
                shrinesCfgPath = configFile;
                foundShrines = true;
            }
            if (configFile.Contains(starmapsyml)) {
                starMapsCfgPath = configFile;
                foundStarMaps = true;
            }
        }

        if (foundShrines == false) {
            Logger.LogDebug("Shrines file missing, writing the built-in shrines.");
            using (StreamWriter writetext = new StreamWriter(shrinesCfgPath)) {
                writetext.WriteLine($@"#################################################
# Aesir Lineage shrines
#
# Only read when UseShrinesFile is on in the main config.
# Each deity's blessing, dream inspirations, star map reset cost and Ascension.
# A boon's id is saved on characters while it is active, so don't change it.
# Delete this file to get the current built-in shrines.
#################################################
");
                writetext.WriteLine(ShrineData.DefaultYaml());
            }
        }

        if (foundStarMaps == false) {
            Logger.LogDebug("Star maps file missing, writing the built-in maps.");
            using (StreamWriter writetext = new StreamWriter(starMapsCfgPath)) {
                writetext.WriteLine($@"#################################################
# Aesir Lineage star maps
#
# Only read when UseStarMapsFile is on in the main config.
# Delete this file to get the current built-in maps.
#################################################
");
                writetext.WriteLine(StarMaps.DefaultYaml());
            }
        }

        SetupFileWatcher(shrinesyml);
        SetupFileWatcher(starmapsyml);
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
        // The server (or a solo game) decides; a connected client's own files don't apply.
        if (ZNet.instance != null && !ZNet.instance.IsServer())
        {
            Logger.LogInfo($"{e.Name} changed, but this game is connected to a server, which decides. Ignoring.");
            return;
        }
        if (!File.Exists(e.FullPath)) { return; }

        string filetext = File.ReadAllText(e.FullPath);
        var fileInfo = new FileInfo(e.FullPath);
        Logger.LogDebug($"Filewatch changes from: ({fileInfo.Name}) {fileInfo.FullName}");
        switch (fileInfo.Name)
        {
            case shrinesyml:
                if (!UseShrinesFile.Value) {
                    Logger.LogInfo($"{shrinesyml} changed, but UseShrinesFile is off. Ignoring.");
                    return;
                }
                if (!ShrineData.ApplyYaml(filetext)) { return; }
                break;
            case starmapsyml:
                if (!UseStarMapsFile.Value) {
                    Logger.LogInfo($"{starmapsyml} changed, but UseStarMapsFile is off. Ignoring.");
                    return;
                }
                if (!StarMaps.ApplyYaml(filetext)) { return; }
                break;
            default:
                return;
        }
        BroadcastConfigs();
    }

    // Sends the server's current shrines and star maps to every connected client.
    private static void BroadcastConfigs()
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZNet.instance.m_peers.Count == 0) { return; }
        configRPC.SendPackage(ZNet.instance.m_peers, ConfigPackage());
    }

    // Sends the YAML each side loaded. Shrine YAML is written with every default value kept, so a
    // 'false' or 0 an admin set can't come back on the client as the property's initializer.
    private static ZPackage ConfigPackage()
    {
        ZPackage package = new ZPackage();
        package.Write(2);
        package.Write(shrinesyml);
        package.Write(ShrineData.ActiveYaml);
        package.Write(starmapsyml);
        package.Write(StarMaps.ActiveYaml);
        return package;
    }

    private static IEnumerator OnClientReceiveConfigs(long sender, ZPackage package)
    {
        // A server or solo game keeps its own; this is only for clients.
        if (ZNet.instance == null || ZNet.instance.IsServer()) { yield break; }

        int count = package.ReadInt();
        for (int i = 0; i < count; i++) {
            string name = package.ReadString();
            string yaml = package.ReadString();
            switch (name) {
                case shrinesyml:
                    ShrineData.ApplyYaml(yaml);
                    break;
                case starmapsyml:
                    StarMaps.ApplyYaml(yaml);
                    break;
                default:
                    Logger.LogWarning($"The server sent an unknown config '{name}'; is it running a different version?");
                    break;
            }
        }
        usingServerConfig = true;
        Logger.LogDebug("Applied the server's shrines and star maps.");
    }

    private static IEnumerator OnServerRecieveConfigs(long sender, ZPackage package)
    {
        yield return null;
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    private static class RestoreLocalConfigs {
        private static void Postfix() {
            if (!usingServerConfig) { return; }
            usingServerConfig = false;
            ShrineData.LoadLocal();
            StarMaps.LoadLocal();
        }
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
