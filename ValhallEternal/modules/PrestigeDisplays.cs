using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ValhallEternal.common;
using static ValhallEternal.common.DataObjects;
using Vector3 = UnityEngine.Vector3;

namespace ValhallEternal.modules
{
    internal static class PrestigeDisplays
    {
        static GameObject localPlayerVEHUD = null;
        static TextMeshProUGUI localPlayerLevelText = null;
        internal static GameObject localPlayerWings = null;
        internal static GameObject localPlayerAura = null;

        public static Dictionary<uint, PrestigeLevelHUD> extendedPlayerHUDS = new Dictionary<uint, PrestigeLevelHUD>();

        public class PrestigeLevelHUD {
            public GameObject hudroot { get; set; }
            public GameObject root { get; set; }
            public TextMeshProUGUI tmpGUI { get; set;}
        }

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
                        localPlayerVEHUD.transform.localPosition = new Vector3(x: localPlayerVEHUD.transform.localPosition.x - 108, y: localPlayerVEHUD.transform.localPosition.y - 10);
                    }
                }
            }
            if (localPlayerVEHUD != null && localPlayerLevelText == null) {
                //Transform healthIco = __instance.m_healthPanel.transform.Find("healthicon");
                //veLocalHud.transform.localPosition = new Vector3(healthIco.position.x - ValConfig.LocalLevelDisplayOffset.Value, healthIco.position.y);
                Logger.LogDebug("Finding Level TextMeshPro component.");
                Transform tform = localPlayerVEHUD.transform.Find("Level");
                if (tform != null) {
                    localPlayerLevelText = tform.GetComponent<TextMeshProUGUI>();
                } else {
                    Logger.LogDebug("Could not find Level GO");
                }
            }
        }

        public static void UpdateLocalPlayerLevelDisplay(int level = 0) {
            if (!SceneManager.GetActiveScene().name.Equals("main")) { return; }
            if (level == 0) { level = PlayerData.localPlayerConfig.PlayerLevel; }
            CreateLocalHudElements(GUIManager.CustomGUIFront.transform);
            SetupLocalPlayerLevelDisplay(localPlayerVEHUD, level);
        }

        public static void SetupPlayerWingsDisplay(string selectedWings = null) {
            if (selectedWings == null || Player.m_localPlayer == null) { return; }

            if (PrestigeDisplays.localPlayerWings != null) { GameObject.Destroy(PrestigeDisplays.localPlayerWings); }
            if (selectedWings == DataObjects.None) { return; }
            if (Deities.PrestigeEffects[DataObjects.PrestigeEffect.Wings].ContainsKey(selectedWings)) {
                // need to setup and use a visequip which links to the wings
                PrestigeDisplays.localPlayerWings = UnityEngine.GameObject.Instantiate(Deities.PrestigeEffects[PrestigeEffect.Wings][selectedWings].EffectObject, Player.m_localPlayer.gameObject.transform);
            } else {
                Logger.LogWarning($"Selected wings {selectedWings} not found in Deities.PrestigeEffects");
            }
        }

        public static void SetupPlayerAuraDisplay(string selectedAura = null) {
            if (selectedAura == null || Player.m_localPlayer == null) { return; }

            if (PrestigeDisplays.localPlayerAura != null) { GameObject.Destroy(PrestigeDisplays.localPlayerAura); }
            if (selectedAura == DataObjects.None) { return; }
            if (Deities.PrestigeEffects[DataObjects.PrestigeEffect.Aura].ContainsKey(selectedAura)) {
                // need to setup and use a visequip which links to the wings
                PrestigeDisplays.localPlayerAura = UnityEngine.GameObject.Instantiate(Deities.PrestigeEffects[PrestigeEffect.Aura][selectedAura].EffectObject, Player.m_localPlayer.gameObject.transform);
            } else {
                Logger.LogWarning($"Selected Aura {selectedAura} not found in Deities.PrestigeEffects");
            }
        }

        internal static class EnablePlayerPrestigeDisplays
        {
            [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
            public static class EnableLocalPlayerPrestigeDisplays {
                public static void Postfix(Player __instance)
                {
                    PrestigeDisplays.UpdateLocalPlayerLevelDisplay();
                    foreach(KeyValuePair<PrestigeEffect, string> kvp in PlayerData.localPlayerConfig.ActiveEffectsForPlayer) {
                        switch (kvp.Key) {
                            case PrestigeEffect.Wings:
                                PrestigeDisplays.SetupPlayerWingsDisplay(kvp.Value);
                                break;
                                //case PrestigeEffect.LevelColor:
                                //    // Set the player level color
                                //    PlayerLevelColors.ApplyPlayerLevelColor(__instance, kvp.Value);
                                //    break;
                                //case PrestigeEffect.Footprints:
                                //    // Set the player footprints
                                //    PlayerFootprints.ApplyPlayerFootprints(__instance, kvp.Value);
                                //    break;
                                case PrestigeEffect.Aura:
                                    // Set the player aura
                                    PrestigeDisplays.SetupPlayerAuraDisplay(kvp.Value);
                                    break;
                                //case PrestigeEffect.Title:
                                //    // Set the player title
                                //    PlayerTitles.ApplyPlayerTitle(__instance, kvp.Value);
                                //    break;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(EnemyHud))]
        public static class SetupOtherPlayerLevelDisplay
        {
            [HarmonyPatch(nameof(EnemyHud.ShowHud))]
            public static void Postfix(EnemyHud __instance, Character c)
            {
                if (c == null || !c.IsPlayer() || __instance == null) { return; }
                EnemyHud.HudData ehud = __instance.m_huds[c];
                if (ehud == null) { return; }

                Player otherplayer = c as Player;
                if (otherplayer == null) { return; }
                ZDO ozdo = otherplayer.m_nview.GetZDO();
                if (ozdo == null) { return; }
                uint otherplayerzid = otherplayer.GetZDOID().ID;
                int playerVELevel = ozdo.GetInt(DataObjects.CustomLevelZKey, 0);
                Logger.LogDebug($"Player {otherplayer.GetPlayerName()}-{otherplayerzid} level {playerVELevel}");
                if (extendedPlayerHUDS.ContainsKey(otherplayerzid)) {
                    // check/update level
                    if (playerVELevel == 0) {
                        extendedPlayerHUDS[otherplayerzid].root.SetActive(false);
                    } else {
                        extendedPlayerHUDS[otherplayerzid].root.SetActive(true);
                    }
                    extendedPlayerHUDS[otherplayerzid].tmpGUI.text = $"{playerVELevel}";
                } else {
                    // Create the new local hud
                    CreateEnemyHud(otherplayerzid, ehud.m_gui.transform, playerVELevel);
                }
            }
        }

        [HarmonyPatch(typeof(EnemyHud))]
        private static class CleanupOtherPlayerHUDS {
            [HarmonyPatch(nameof(EnemyHud.UpdateHuds))]
            private static void Postfix() {
                // Cleanup hud extensions that no longer reference existing huds
                if (extendedPlayerHUDS.Count == 0) { return; }
                extendedPlayerHUDS = extendedPlayerHUDS.Where(x => x.Value.root != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        public static void CreateEnemyHud(uint otherplayerzid, Transform targetTform, int otherplayerlevel) {
            Logger.LogDebug("Creating Enemy Player Hud.");
            GameObject hud = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("VERemoteHud");
            GameObject enemyHudLevel = GameObject.Instantiate(hud, targetTform);
            enemyHudLevel.name = "VERemoteHud";
            Transform tform = enemyHudLevel.transform.Find("Level");
            TextMeshProUGUI text = tform.GetComponent<TextMeshProUGUI>();
            if (otherplayerlevel == 0) { 
                enemyHudLevel.SetActive(false);
            } else {
                text.text = $"{otherplayerlevel}";
            }
            // Adjust hud location
            
            extendedPlayerHUDS.Add(otherplayerzid, new PrestigeLevelHUD() { root = enemyHudLevel, tmpGUI = text, hudroot = targetTform.gameObject });
        }

        public static void SetupLocalPlayerLevelDisplay(GameObject hugGUI, int levelnum) {
            if (levelnum == 0) {
                // No level to display
                Logger.LogInfo("Player level set to zero or not set, disabling display.");
                hugGUI.SetActive(false);
                return;
            }

            hugGUI.SetActive(true);

            // Set local player level
            if (localPlayerLevelText != null) {
                Logger.LogDebug($"Setting player HUD with level {levelnum}");
                localPlayerLevelText.text = $"{levelnum}";
                return;
            }
        }
    }
}
