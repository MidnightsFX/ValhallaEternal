using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValhallEternal.common;
using Vector3 = UnityEngine.Vector3;

namespace ValhallEternal.modules
{
    static class PlayerLevelDisplays
    {
        static GameObject localPlayerVEHUD = null;
        static TextMeshPro localPlayerLevelText = null;

        public static void CreateLocalHudElements(Transform targetTform) {
            if (localPlayerVEHUD == null) {
                Logger.LogDebug("Creating Local Player Level Display HUD Elements.");
                GameObject veLocalHud = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("VELocalHud");
                localPlayerVEHUD = GameObject.Instantiate(veLocalHud, targetTform);
                if (Hud.m_instance != null) {
                    // This will get the X set correctly, but need to set Y
                    Transform minimapTform = Hud.m_instance.m_rootObject.transform.Find("MiniMap/small");
                    if (minimapTform != null) {
                        localPlayerVEHUD.transform.position = minimapTform.position;
                        //localPlayerVEHUD.transform.localPosition = minimapTform.localPosition;
                        localPlayerVEHUD.transform.localPosition = new Vector3(x: localPlayerVEHUD.transform.localPosition.x - 112, y: localPlayerVEHUD.transform.localPosition.y - 10);
                    }
                }
            }
            if (localPlayerVEHUD != null && localPlayerLevelText == null) {
                //Transform healthIco = __instance.m_healthPanel.transform.Find("healthicon");
                //veLocalHud.transform.localPosition = new Vector3(healthIco.position.x - ValConfig.LocalLevelDisplayOffset.Value, healthIco.position.y);
                Transform tform = localPlayerVEHUD.transform.Find("Level");
                if (tform != null)
                {
                    localPlayerLevelText = tform.GetComponent<TextMeshPro>();
                }
                else
                {
                    Logger.LogDebug("Could not find Level GO");
                }
            }
        }

        [HarmonyPatch(typeof(Player))]
        public static class DisplaySelfLevel {
            [HarmonyPatch(nameof(Player.Awake))]
            public static void Postfix(Player __instance) {
                if (__instance == null || !SceneManager.GetActiveScene().name.Equals("main")) {
                    return;
                }

                // Ensure that the players Zkey value is set so that other players HUDs will be able to see this players achievements
                if (__instance.PlayerHasUniqueKey(DataObjects.CustomLevelZKey))
                {
                    __instance.TryGetUniqueKeyValue(DataObjects.CustomLevelZKey, out string kv);
                    if (int.TryParse(kv, out int level) == false)
                    {
                        level = 0;
                    }
                    __instance.m_nview.GetZDO().Set(DataObjects.CustomLevelZKey, level);
                }
                CreateLocalHudElements(GUIManager.CustomGUIFront.transform);
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
                    Logger.LogDebug("Setting enemy hud player level");
                    GameObject enemyHud = CreateEnemyHud(ehud.m_gui.transform);
                    SetupPlayerLevelDisplay(enemyHud, playerVELevel);
                }
            }
        }

        public static GameObject CreateEnemyHud(Transform targetTform)
        {
            Logger.LogDebug("Creating Enemy Player Hud.");
            GameObject hud = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("VELocalHud");
            GameObject enemyHudLevel = GameObject.Instantiate(hud, targetTform);
            // adjust enemyHudLevel transforms to fit hud location
            return enemyHudLevel;
        }

        public static void SetupPlayerLevelDisplay(GameObject hugGUI, int levelnum) {
            if (levelnum == 0) {
                // No level to display
                Logger.LogInfo("Player level set to zero or not set, disabling display.");
                hugGUI.SetActive(false);
                return;
            }

            Logger.LogDebug($"Setting player HUD with level {levelnum}");
            hugGUI.SetActive(true);
            // This should instead determine the display based on the level configuration sent from the server and the level of the target player
            // Consider if this should have the option to convert to roman, nordic runes or language specific numbers
            Logger.LogDebug($"Checking for Level text in GO:{hugGUI.name}");


            TextMeshPro tmp = hugGUI.GetComponentInChildren<TextMeshPro>(true);
            if (tmp != null) {
                tmp.text = $"{levelnum}";
            }
        }
    }
}
