using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ValhallEternal.common;

namespace ValhallEternal.modules
{
    internal class SacrificeUI : MonoBehaviour
    {
        // UI instance for this player
        public static SacrificeUI Instance => _instance ??= new SacrificeUI();
        private static SacrificeUI _instance;

        private static GameObject SacrificePanel;
        private static GameObject ScrollAreaView;
        private static GameObject ScrollContentArea;
        private static ToggleGroup SacrificeChoiceGroup;
        private static GameObject SacrificeChoiceContainer;
        private static GameObject ChoiceSelectButton;
        private static GameObject ManualCloseButton;

        private static Text Description1;
        private static Text Description2;
        private static Text Description3;
        private static Text Description4;

        private static List<Toggle> SacrificeToggleOptions = new List<Toggle>();
        private static string SelectedChoice = "none";

        public void Awake()
        {

        }

        public void Show()
        {
            if (SacrificePanel == null)
            {
                CreateStaticUIObjects();
            }
            SacrificePanel.SetActive(true);
        }

        public void Hide()
        {
            // Logger.LogDebug("Closing");
            if (SacrificePanel != null)
            {
                SacrificePanel.SetActive(false);
            }
            GUIManager.BlockInput(false);
        }

        private void CreateStaticUIObjects()
        {
            if (GUIManager.Instance == null || !GUIManager.CustomGUIFront)
            {
                Logger.LogWarning("GUIManager not setup, skipping static object creation.");
                return;
            }

            // Create the panel object
            SacrificePanel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 800,
                height: 800,
                draggable: true);
            // Hide it right away
            SacrificePanel.SetActive(false);

            var textHeader = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$selection_header"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(50f, 360f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            textHeader.name = "Sacrifice";

            GameObject descgo = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$selection_description"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 315f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 560f,
                height: 60f,
                addContentSizeFitter: false);
            var descgotext = descgo.GetComponent<Text>();
            descgotext.resizeTextForBestFit = true;
            descgotext.resizeTextMaxSize = 20;
            descgotext.alignment = TextAnchor.MiddleCenter;

            ManualCloseButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$close"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(360f, 360f),
                width: 60f,
                height: 60f);
            Button bclose = ManualCloseButton.GetComponent<Button>();
            bclose.interactable = true;
            bclose.onClick.AddListener(Hide);
            ManualCloseButton.SetActive(false);

            var deathpenaltyTitle = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$death_penalty_header"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(100f, 220f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 400f,
                height: 40f,
                addContentSizeFitter: false);
            deathpenaltyTitle.name = "DeathPenaltyTitle";

            var deathpenalty = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$death_penalty_description"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(150f, 110f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 18,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 500f,
                height: 200f,
                addContentSizeFitter: false);
            deathpenalty.name = "DeathPenaltyDesc";
            Description1 = deathpenalty.GetComponent<Text>();
            Description1.resizeTextForBestFit = true;
            Description1.resizeTextMaxSize = 18;
            //DeathPenaltyDescription.verticalOverflow = VerticalWrapMode.Overflow;

            var xpTitle = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$xp_header"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(100f, 30f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 400f,
                height: 40f,
                addContentSizeFitter: false);
            xpTitle.name = "xpModifiersTitle";

            var xpMod = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$xp_mod_description"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(150f, -80f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 16,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 500f,
                height: 200f,
                addContentSizeFitter: false);
            xpMod.name = "xpModifiersDesc";
            Description2 = xpMod.GetComponent<Text>();
            Description2.resizeTextForBestFit = true;
            Description2.resizeTextMaxSize = 18;

            var lootTitle = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$loot_header"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(100f, -130f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 400f,
                height: 40f,
                addContentSizeFitter: false);
            lootTitle.name = "lootModifiersTitle";

            var lootDesc = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$loot_modifier_description"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(150f, -240f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 16,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 500f,
                height: 200f,
                addContentSizeFitter: false);
            lootDesc.name = "lootModifersDesc";
            Description3 = lootDesc.GetComponent<Text>();
            Description3.resizeTextForBestFit = true;
            Description3.resizeTextMaxSize = 18;

            var harvestTitle = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$harvest_header"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(100f, -260f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 22,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 400f,
                height: 40f,
                addContentSizeFitter: false);
            harvestTitle.name = "harvestModifiersTitle";

            var harvestDesc = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$harvest_modifier_description"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(150f, -370f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 16,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 500f,
                height: 200f,
                addContentSizeFitter: false);
            harvestDesc.name = "harvestModifersDesc";
            Description4 = harvestDesc.GetComponent<Text>();
            Description4.resizeTextForBestFit = true;
            Description4.resizeTextMaxSize = 18;

            ChoiceSelectButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$deathchoice_select"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-240f, -290f),
                width: 200f,
                height: 80f);
            Button bchoice = ChoiceSelectButton.GetComponent<Button>();
            bchoice.interactable = true;
            //bchoice.onClick.AddListener(MakePlayerDeathSelection);

            Logger.LogDebug("Setting up scroll entry");
            // Scroll area
            ScrollAreaView = GUIManager.Instance.CreateScrollView(SacrificePanel.transform, false, true, 10f, 10f, GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.grey, 200f, 400f);
            ScrollAreaView.transform.localPosition = new Vector2 { x = -260, y = -30 };
            ScrollContentArea = ScrollAreaView.GetComponentInChildren<ContentSizeFitter>().gameObject;
            SacrificeChoiceGroup = ScrollContentArea.AddComponent<ToggleGroup>();

            Logger.LogDebug("Setting up death choice template entry");

            SacrificeChoiceContainer = new GameObject("DeathChoice");
            SacrificeChoiceContainer.transform.SetParent(SacrificePanel.transform);
            SacrificeChoiceContainer.transform.position = SacrificePanel.transform.position;
            SacrificeChoiceContainer.SetActive(false);

            var toggleGo = GUIManager.Instance.CreateToggle(
                parent: SacrificeChoiceContainer.transform,
                width: 40f,
                height: 40f
                );
            toggleGo.transform.localPosition = new Vector2(-220f, 0f);
            toggleGo.name = "selecter";
            toggleGo.transform.Find("Label").gameObject.SetActive(false);
            toggleGo.GetComponent<Toggle>().isOn = false;

            var deathSettingName = GUIManager.Instance.CreateText(
                text: "Name",
                parent: SacrificeChoiceContainer.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-20f, 0f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: GUIManager.Instance.ValheimYellow,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            deathSettingName.name = "ChoiceName";
        }

        private void SetChoiceList()
        {
            SacrificeToggleOptions.Clear();
            int y_value = -50;
            //Logger.LogDebug($"Setting up {DeathChoices.Count} death styles.");
            foreach (var entry in SacrificeData.AllSacrifices)
            {
                var newDeathChoice = GameObject.Instantiate(SacrificeChoiceContainer, ScrollContentArea.transform);
                //Logger.LogDebug("Created container");
                var selector = newDeathChoice.transform.Find("selecter");
                //Logger.LogDebug($"Finding selector... null? {selector == null}");
                selector.Find("Label").GetComponent<Text>().text = entry.Name;
                //Logger.LogDebug("Set label text");
                newDeathChoice.transform.Find("ChoiceName").GetComponent<Text>().text = entry.Description;
                //Logger.LogDebug("Set display name");
                var toggle = selector.GetComponent<Toggle>();
                toggle.group = SacrificeChoiceGroup;
                toggle.onValueChanged.AddListener((isOn) => {
                    //Logger.LogDebug("Setting up onclock");
                    Description1.GetComponent<Text>().text = entry
                    //Logger.LogDebug("Set death description");
                    Description2.GetComponent<Text>().text = entry.Value.GetSkillModiferDescription();
                    //Logger.LogDebug("Set xp mod");
                    Description3.GetComponent<Text>().text = entry.Value.GetLootModifiersDescription();
                    //Logger.LogDebug("Set loot mod");
                    Description4.GetComponent<Text>().text = entry.Value.GetResourceModiferDescription();
                    //Logger.LogDebug("Set harvest mod");
                    SelectedChoice = entry.Key;
                });
                //Logger.LogDebug("Created onclick");
                newDeathChoice.SetActive(true);
                newDeathChoice.transform.localPosition = new Vector3() { x = 260, y = y_value };
                SacrificeToggleOptions.Add(toggle);
                y_value -= 50;
            }
        }
    }
}
