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
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.modules {
    public static class PrestigeAndCosmeticUI {

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class AddSacrificeUIButton {
            static GameObject SacrificeEnableButton = null;
            public static void Postfix(InventoryGui __instance) {
                // This only gets added once you have prestiged
                if (PlayerData.localPlayerConfig.PlayerLevel == 0) { return; }
                if (SacrificeEnableButton != null) { return; }

                SacrificeEnableButton = GUIManager.Instance.CreateButton(
                    text: Localization.instance.Localize("$ve_prestige_options"),
                    parent: __instance.m_infoPanel.transform,
                    anchorMin: new Vector2(1f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    position: new Vector2(-628f, -90f),
                    width: 90f,
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

            private static GameObject WingSelectorDropdown;
            private static GameObject AuraSelectorDropdown;
            private static GameObject ManualCloseButton;
            //private static GameObject ApplyChangesButton;

            public void Awake() {
                _instance = this;
            }

            public void Show() {
                if (PrestigePanel == null) {
                    CreateStaticUIObjects();
                    //LoadStaticAssets();
                }
                PrestigePanel.SetActive(true);
                EnableDisablePrestigeSelctions();
            }

            public void Hide() {
                // Logger.LogDebug("Closing");
                if (PrestigePanel != null) {
                    PrestigePanel.SetActive(false);
                }
                GUIManager.BlockInput(false);
            }

            public static void EnableDisablePrestigeSelctions() {
                if (PlayerData.localPlayerConfig.AvailableEffectsForPlayer == null) { return; }
                Logger.LogDebug($"Enabling usable prestige options in UI: wings-{PlayerData.localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(PrestigeEffect.Wings)} Aura-{PlayerData.localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(PrestigeEffect.Aura)}.");
                bool enableWings = PlayerData.localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(PrestigeEffect.Wings) && PlayerData.localPlayerConfig.AvailableEffectsForPlayer[PrestigeEffect.Wings].Count > 1;
                WingSelectorDropdown.SetActive(enableWings);
                bool enableAura = PlayerData.localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(PrestigeEffect.Aura) && PlayerData.localPlayerConfig.AvailableEffectsForPlayer[PrestigeEffect.Aura].Count > 1;
                AuraSelectorDropdown.SetActive(enableAura);
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
                    width: 300,
                    height: 400,
                    draggable: true);
                // Hide it right away
                PrestigePanel.SetActive(false);

                WingSelectorDropdown = GUIManager.Instance.CreateDropDown(
                    parent: PrestigePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 0f),
                    fontSize: 18,
                    width: 200f,
                    height: 40f);

                Dropdown wingselectordd = WingSelectorDropdown.GetComponent<Dropdown>();
                wingselectordd.AddOptions(PlayerData.ListPlayerAvailablePrestigeEffect(PrestigeEffect.Wings));
                wingselectordd.onValueChanged.AddListener(UpdateSelectedWings);
                //WingSelectorDropdown.SetActive(false);

                AuraSelectorDropdown = GUIManager.Instance.CreateDropDown(
                    parent: PrestigePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, -50f),
                    fontSize: 18,
                    width: 200f,
                    height: 40f);

                Dropdown auraSelectorDD = AuraSelectorDropdown.GetComponent<Dropdown>();
                auraSelectorDD.AddOptions(PlayerData.ListPlayerAvailablePrestigeEffect(PrestigeEffect.Aura));
                auraSelectorDD.onValueChanged.AddListener(UpdateSelectedAura);
                //AuraSelectorDropdown.SetActive(false);


                ManualCloseButton = GUIManager.Instance.CreateButton(
                    text: Localization.instance.Localize("$ve_close"),
                    parent: PrestigePanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(108f, 158f),
                    width: 60f,
                    height: 60f);
                Button bclose = ManualCloseButton.GetComponent<Button>();
                bclose.interactable = true;
                bclose.onClick.AddListener(Hide);
                //ManualCloseButton.SetActive(false);

                // Sacrifice button

                //ApplyChangesButton = GUIManager.Instance.CreateButton(
                //    text: Localization.instance.Localize("$ve_apply_cosmetics"),
                //    parent: PrestigePanel.transform,
                //    anchorMin: new Vector2(0.5f, 0.5f),
                //    anchorMax: new Vector2(0.5f, 0.5f),
                //    position: new Vector2(0f, -350f),
                //    width: 300f,
                //    height: 60f);
                //Button bselect = ApplyChangesButton.GetComponent<Button>();
                ////bselect.interactable = false;
                //bselect.onClick.AddListener(ApplySelectedEffects);
            }

            public void UpdateSelectedWings(int _actionID) {
                Dropdown dropSelector = WingSelectorDropdown.GetComponent<Dropdown>();
                string selectWings = dropSelector.options[dropSelector.value].text;
                PrestigeDisplays.SetupPlayerWingsDisplay(selectWings);
            }

            public void UpdateSelectedAura(int _actionID) {
                Dropdown dropSelector = WingSelectorDropdown.GetComponent<Dropdown>();
                string selectedAura = dropSelector.options[dropSelector.value].text;
                PrestigeDisplays.SetupPlayerAuraDisplay(selectedAura);
            }

            public void ApplySelectedEffects() {
                Dropdown dropSelector = WingSelectorDropdown.GetComponent<Dropdown>();
                
            }
        }
    }
}
