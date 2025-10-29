using HarmonyLib;
using TMPro;
using UnityEngine;
using ValhallEternal.common;
using Vector3 = UnityEngine.Vector3;

namespace ValhallEternal.modules
{
    static class PlayerLevelDisplays
    {
        static GameObject levelBronze = null;
        static GameObject levelSilver = null;
        static GameObject levelGold = null;
        static GameObject levelDiamond = null;
        static GameObject levelMythic = null;

        static GameObject localPlayerVEHUD = null;
        static GameObject localPlayerLevelGO = null;

        public static void LoadAssets()
        {
            levelBronze = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/leveldisplay/level_bronze.prefab");
            levelSilver = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/leveldisplay/level_silver.prefab");
            levelGold = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/leveldisplay/level_gold.prefab");
            levelDiamond = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/leveldisplay/level_diamond.prefab");
            levelMythic = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>($"assets/leveldisplay/level_mythic.prefab");
        }

        [HarmonyPatch(typeof(Hud))]
        public static class DisplaySelfLevel
        {
            [HarmonyPatch(nameof(Hud.Awake))]
            public static void Postfix(Hud __instance)
            {
                GameObject veLocalHud = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("VELocalHud");
                if (veLocalHud != null && localPlayerVEHUD == null) {
                    localPlayerVEHUD = GameObject.Instantiate(veLocalHud, __instance.m_healthPanel.transform);
                }
                // set local hud position
                if (localPlayerVEHUD != null) {
                    Transform healthIco = __instance.m_healthPanel.transform.Find("healthicon");
                    veLocalHud.transform.localPosition = new Vector3(healthIco.position.x - ValConfig.LocalLevelDisplayOffset.Value, healthIco.position.y);
                }
            }
        }

        [HarmonyPatch(typeof(Player))]
        public static class SetupLocalPlayer
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.OnSpawned))]
            static void Postfix(Player __instance)
            {
                //if (__instance != Player.m_localPlayer) {
                //    return;
                //}
                
                // Ensure that the players Zkey value is set so that other players HUDs will be able to see this players achievements
                if (__instance.PlayerHasUniqueKey(DataObjects.CustomLevelZKey)) {
                    __instance.TryGetUniqueKeyValue(DataObjects.CustomLevelZKey, out string kv);
                    if (int.TryParse(kv, out int level) == false) {
                        level = 0;
                    }
                    __instance.m_nview.GetZDO().Set(DataObjects.CustomLevelZKey, level);
                }

                UpdateLocalLevelDisplay(__instance);
            }
        }

        public static void UpdateLocalLevelDisplay(Player player) {
            if (player.PlayerHasUniqueKey(DataObjects.CustomLevelZKey)) {
                player.TryGetUniqueKeyValue(DataObjects.CustomLevelZKey, out string playerlvl);
                Logger.LogDebug($"Checking player level |{playerlvl}|");
                if (int.TryParse(playerlvl, out int levelnum) == false)
                {
                    levelnum = 0;
                }
                SetupPlayerLevelDisplay(localPlayerVEHUD, levelnum);
            }
        }


        [HarmonyPatch(typeof(EnemyHud))]
        public static class SetupCreatureLevelDisplay
        {
            [HarmonyPatch(nameof(EnemyHud.ShowHud))]
            public static void Postfix(EnemyHud __instance, Character c)
            {
                if (c == null || !c.IsPlayer()) { return; }
                EnemyHud.HudData ehud = __instance.m_huds[c];
                if (ehud == null) { return; }

                Player otherplayer = c as Player;
                int playerVELevel = otherplayer.m_nview.GetZDO().GetInt(DataObjects.CustomLevelZKey, 0);
                if (playerVELevel > 0) {
                    // Add the players level to their hud display

                    // Build/determine the enemy hud parent object and pass that in as the location where the level should be created
                    SetupPlayerLevelDisplay(ehud.m_gui, playerVELevel);
                }
            }
        }

        public static void SetupPlayerLevelDisplay(GameObject hugGUI, int levelnum) {
            if (levelBronze == null) { LoadAssets(); }

            if (levelnum == 0) {
                // No level to display
                Logger.LogInfo("Player level set to zero or not set, removing.");
                if (localPlayerLevelGO != null) {
                    Logger.LogDebug("Removing local player level");
                    GameObject.Destroy(localPlayerLevelGO);
                }
                return;
            }

            Logger.LogDebug($"Setting player HUD with level {levelnum}");
            // This should instead determine the display based on the level configuration sent from the server and the level of the target player

            if (levelnum >= 1 && levelnum <= 10)
            {
                // Bronze
                localPlayerLevelGO = GameObject.Instantiate(levelBronze, hugGUI.transform);
            }
            else if (levelnum >= 11 && levelnum <= 20)
            {
                // Silver
                localPlayerLevelGO = GameObject.Instantiate(levelSilver, hugGUI.transform);
            }
            else if (levelnum >= 21 && levelnum <= 30)
            {
                // Gold
                localPlayerLevelGO = GameObject.Instantiate(levelGold, hugGUI.transform);
            }
            else if (levelnum >= 31 && levelnum <= 40)
            {
                // Diamond
                localPlayerLevelGO = GameObject.Instantiate(levelDiamond, hugGUI.transform);
            }
            else if (levelnum >= 41)
            {
                // Mythic
                localPlayerLevelGO = GameObject.Instantiate(levelMythic, hugGUI.transform);
            }

            TextMeshPro tmp = localPlayerLevelGO.GetComponentInChildren<TextMeshPro>();
            tmp.text = $"{levelnum}";
        }
    }
}
