using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ValhallEternal.common;

namespace ValhallEternal.modules {
    public static class PrestigeAndCosmeticUI {

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class AddSacrificeUIButton {
            static GameObject SacrificeEnableButton = null;
            public static void Postfix(InventoryGui __instance) {
                // This only gets added once you have prestiged
                if (PlayerData.localPlayerConfig.PlayerLevel > 0 == false) { return; }
                if (SacrificeEnableButton != null) { return; }

                SacrificeEnableButton = GUIManager.Instance.CreateButton(
                    text: Localization.instance.Localize("$ve_prestige_options"),
                    parent: __instance.m_infoPanel.transform,
                    anchorMin: new Vector2(1f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    position: new Vector2(-672f, -26f),
                    width: 60f,
                    height: 60f);
                Button bclose = SacrificeEnableButton.GetComponent<Button>();
                bclose.interactable = true;

                SacrificeEnableButton.AddComponent<PrestigeUI>();
                bclose.onClick.AddListener(PrestigeUI.Instance.Show);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class HideSacrificeUI_InventoryClose {
            public static void Postfix() {
                PrestigeUI.Instance.Hide();
            }
        }

        internal class PrestigeUI : MonoBehaviour {
            public static PrestigeUI Instance => _instance ??= new PrestigeUI();
            private static PrestigeUI _instance;

            private static GameObject PrestigePanel;

            public void Awake() {
                _instance = this;
            }

            public void Show() {
                if (PrestigePanel == null) {
                    CreateStaticUIObjects();
                    //LoadStaticAssets();
                }
                PrestigePanel.SetActive(true);
            }

            public void Hide() {
                // Logger.LogDebug("Closing");
                if (PrestigePanel != null) {
                    PrestigePanel.SetActive(false);
                }
                GUIManager.BlockInput(false);
            }

            private void CreateStaticUIObjects() {
                if (GUIManager.Instance == null || !GUIManager.CustomGUIFront) {
                    Logger.LogWarning("GUIManager not setup, skipping static object creation.");
                    return;
                }

                // Create the panel object
                PrestigePanel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 800,
                    height: 800,
                    draggable: true);
                // Hide it right away
                PrestigePanel.SetActive(false);
            }
            }
    }
}
