using HarmonyLib;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValhallEternal.common;

namespace ValhallEternal.modules
{

    public static class SacrificePatches
    {

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class AddSacrificeUIButton
        {
            static GameObject SacrificeEnableButton = null;
            public static void Postfix(InventoryGui __instance)
            {
                if (SacrificeEnableButton != null) { return; }

                SacrificeEnableButton = GUIManager.Instance.CreateButton(
                    text: Localization.instance.Localize("$ve_sacrifice_button"),
                    parent: __instance.m_infoPanel.transform,
                    anchorMin: new Vector2(1f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    position: new Vector2(-612f, -26f),
                    width: 60f,
                    height: 60f);
                Button bclose = SacrificeEnableButton.GetComponent<Button>();
                bclose.interactable = true;

                SacrificeEnableButton.AddComponent<SacrificeUI>();
                bclose.onClick.AddListener(SacrificeUI.Instance.Show);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class HideSacrificeUI_InventoryClose
        {
            public static void Postfix()
            {
                SacrificeUI.Instance.Hide();
            }
        }
    }



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
        private static GameObject DeitySelectorDropdown;


        //private static Text SacrificeRequirements;
        //private static Text BoonChanges;
        //private static Text OathChanges;
        //private static Text PrestigeResetDetails;
        private static Text DeityName;
        private static Text DeityDescription;
        private static Image DeityImage;

        private static List<Toggle> SacrificeToggleOptions = new List<Toggle>();
        private static string SelectedChoice = "none";
        private static Deities.Deity SelectedDeity;

        public void Awake()
        {
            _instance = this;
        }

        public void Show()
        {
            if (SacrificePanel == null)
            {
                CreateStaticUIObjects();
                //LoadStaticAssets();
                SetChoiceList(Deities.Deity.Gefjun);
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

        public void UpdateSelectedDiety(int _actionID)
        {
            Dropdown dropSelector = DeitySelectorDropdown.GetComponent<Dropdown>();
            Deities.Deity selectedDiety = (Deities.Deity)System.Enum.Parse(typeof(Deities.Deity), dropSelector.options[dropSelector.value].text);

            SetChoiceList(selectedDiety);
            //dropSelector.value;
        }

        public void CompleteSacrifice()
        {
            if (SelectedChoice == "None") {
                Logger.LogWarning("No sacrifice choice selected.");
                return;
            }
            Logger.LogInfo($"Completing sacrifice choice: {SelectedChoice} for deity {SelectedDeity}");
            DataObjects.Sacrifice selectedSacrifice = SacrificeData.AllSacrifices[SelectedDeity][SelectedChoice];
            // Remove items
            if (selectedSacrifice.ItemRequirements != null) {
                foreach (KeyValuePair<string, int> itemReq in selectedSacrifice.ItemRequirements)
                {
                    Logger.LogDebug($"Removing {itemReq.Value} of item {itemReq.Key} from player inventory.");
                    Player.m_localPlayer.m_inventory.RemoveItem(itemReq.Key, itemReq.Value);
                }
            }
            // Remove players keys
            if (selectedSacrifice.PlayerKeyRequirements != null) {
                foreach (string key in selectedSacrifice.PlayerKeyRequirements)
                {
                    // TODO: config for having key removal be global vs player unique
                    Logger.LogDebug($"Removing unique key {key} from player.");
                    Player.m_localPlayer.RemoveUniqueKey(key);
                }
            }
            // Make boon changes
            if (selectedSacrifice.PlayerBoonsChanges != null) {                 
                foreach (KeyValuePair<DataObjects.Boons, float> boonChange in selectedSacrifice.PlayerBoonsChanges)
                {
                    Logger.LogDebug($"Changing player boon {boonChange.Key} by {boonChange.Value}.");
                    PlayerData.AddBoonToPlayerConfig(boonChange.Key, boonChange.Value);
                }
            }
            // Make oath changes
            if (selectedSacrifice.PlayerOathChanges != null) {
                foreach (KeyValuePair<DataObjects.Oaths, float> oathChange in selectedSacrifice.PlayerOathChanges)
                {
                    Logger.LogDebug($"Changing player oath {oathChange.Key} by {oathChange.Value}.");
                    PlayerData.AddOathToPlayerConfig(oathChange.Key, oathChange.Value);
                }
            }

            // Changes to the player data and profile
            if (selectedSacrifice.ResetPlayer != null)
            {
                if (selectedSacrifice.ResetPlayer.ResetKnownRecipes)
                {
                    // Clear known recipes and materials
                    Player.m_localPlayer.m_knownMaterial.Clear();
                    Player.m_localPlayer.m_knownRecipes.Clear();
                }
                if (selectedSacrifice.ResetPlayer.ResetSkillPercentage > 0f)
                {
                    Player.m_localPlayer.m_skills.LowerAllSkills(selectedSacrifice.ResetPlayer.ResetSkillPercentage);
                }
                if (selectedSacrifice.ResetPlayer.PrestigeLevelsGained > 0)
                {
                    PlayerData.localPlayerConfig.PlayerLevel += selectedSacrifice.ResetPlayer.PrestigeLevelsGained;
                }
                if (selectedSacrifice.ResetPlayer.TeleportToSpawn)
                {
                    GameObject startTemple = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "StartTemple").FirstOrDefault();
                    if (startTemple != null)
                    {
                        Logger.LogDebug("Teleporting player to start temple.");
                        Player.m_localPlayer.TeleportTo(startTemple.transform.position, startTemple.transform.rotation, false);
                    }
                    else
                    {
                        Logger.LogWarning("Start temple not found, teleporting to spawn point instead.");

                        Game.instance.FindSpawnPoint(out Vector3 point, out bool logoutPoint, 0f);
                        Player.m_localPlayer.TeleportTo(point, Quaternion.identity, true);
                    }
                }
            }
            PlayerData.SavePlayerConfiguration();
            PlayerData.LoadPlayerConfiguration(Player.m_localPlayer);
            SacrificeUI.Instance.Hide();
            UpdateSelectedDiety(-1); //refresh list, to show newly available options
        }

        private void LoadStaticAssets()
        {
            GameObject bareUI = ValhallEternal.EmbeddedResourceBundle.LoadAsset<GameObject>("assets/ui/sacrificeui.prefab");

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

            GameObject instance = Object.Instantiate(bareUI, GUIManager.CustomGUIFront.transform);
            GameObject panelHolder = instance.transform.Find("Panel").gameObject;
            panelHolder.transform.SetParent(SacrificePanel.transform);
            //GUIManager.Instance.ApplyWoodpanelStyle(SacrificePanel.transform);
            DeityImage = panelHolder.transform.Find("DeityImage").GetComponent<Image>();
            DeityName = panelHolder.transform.Find("DeityName").GetComponent<Text>();
            DeityDescription = panelHolder.transform.Find("DeityDesc").GetComponent<Text>();
            ManualCloseButton = panelHolder.transform.Find("Close").gameObject;
            Button bclose = ManualCloseButton.GetComponent<Button>();
            GUIManager.Instance.ApplyButtonStyle(bclose);
            bclose.interactable = true;
            bclose.onClick.AddListener(Hide);

            ChoiceSelectButton = panelHolder.transform.Find("SacrificeSelectButton").gameObject;
            Button bconfirm = ManualCloseButton.GetComponent<Button>();
            GUIManager.Instance.ApplyButtonStyle(bconfirm, 18);
            bconfirm.onClick.AddListener(CompleteSacrifice);

            // Create the dropdown for deity selection
            DeitySelectorDropdown = GUIManager.Instance.CreateDropDown(
                parent: panelHolder.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(200f, 357f),
                fontSize: 18,
                width: 200f,
                height: 40f);

            Dropdown deityDropdown = DeitySelectorDropdown.GetComponent<Dropdown>();
            deityDropdown.AddOptions(Deities.DeityOptions());
            deityDropdown.value = 2; // Gefjun default
            deityDropdown.name = "DeitySelector";
            deityDropdown.onValueChanged.AddListener(UpdateSelectedDiety);

            ScrollContentArea = panelHolder.transform.Find("OptionScroll/ScrollContent").gameObject;

            SacrificeChoiceContainer = panelHolder.transform.Find("TemplateEntry").gameObject;
            var toggle = GUIManager.Instance.CreateToggle(
                parent: SacrificeChoiceContainer.transform,
                width: 40f,
                height: 40f
            );
            toggle.name = "selecter";
            toggle.transform.Find("Label").gameObject.SetActive(false);
            toggle.GetComponent<Toggle>().isOn = false;
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


            GameObject dietyImageHolder = Object.Instantiate(new GameObject("DeityImage"), SacrificePanel.transform);
            dietyImageHolder.transform.localPosition = new Vector3(-323f, 323f);
            DeityImage = dietyImageHolder.AddComponent<Image>();
            dietyImageHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(125, 125);
            DeityImage.sprite = Deities.DeityConfiguration[Deities.Deity.Gefjun].Image;
            // TODO: add portrait outliner

            var dName  = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$ve_header_gefjun"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(11f, 360f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                // TODO: change this to a cool unique color
                color: GUIManager.Instance.ValheimYellow,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            DeityName = dName.GetComponent<Text>();
            DeityName.name = "DeityName";

            GameObject dietydesc = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$ve_description_gefjun"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-110f, 325f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 14,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 300f,
                height: 80f,
                addContentSizeFitter: false);
            DeityDescription = dietydesc.GetComponent<Text>();
            DeityDescription.resizeTextForBestFit = true;
            DeityDescription.resizeTextMaxSize = 20;
            DeityDescription.alignment = TextAnchor.MiddleCenter;
            DeityDescription.name = "DeityDesc";

            var textHeader = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$ve_sacrifice_header"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(275f, 305f),
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
                text: Localization.instance.Localize("$ve_sacrifice_header_desc"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(6f, 217f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 730f,
                height: 80f,
                addContentSizeFitter: false);
            descgo.name = "SacrificeDesc";
            Text desctextgo = descgo.GetComponent<Text>();
            desctextgo.resizeTextForBestFit = true;
            desctextgo.resizeTextMaxSize = 20;
            desctextgo.alignment = TextAnchor.UpperLeft;

            ManualCloseButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$ve_close"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(360f, 360f),
                width: 60f,
                height: 60f);
            Button bclose = ManualCloseButton.GetComponent<Button>();
            bclose.interactable = true;
            bclose.onClick.AddListener(Hide);
            //ManualCloseButton.SetActive(false);

            // Sacrifice button

            ChoiceSelectButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$ve_sacrifice_sealed"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, -350f),
                width: 300f,
                height: 60f);
            Button bselect = ChoiceSelectButton.GetComponent<Button>();
            //bselect.interactable = false;
            bselect.onClick.AddListener(CompleteSacrifice);

            Logger.LogDebug("Setting up scroll entry");
            // Scroll area
            ScrollAreaView = GUIManager.Instance.CreateScrollView(
                SacrificePanel.transform,
                showHorizontalScrollbar: false,
                showVerticalScrollbar: true,
                handleSize: 10f,
                handleDistanceToBorder: 10f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                Color.grey,
                width: 750f,
                height: 500f);
            ScrollAreaView.transform.localPosition = new Vector2 { x = 0, y = -68f };
            ScrollAreaView.GetComponentInChildren<ScrollRect>().scrollSensitivity = 200;
            ScrollContentArea = ScrollAreaView.GetComponentInChildren<ContentSizeFitter>().gameObject;
            SacrificeChoiceGroup = ScrollContentArea.AddComponent<ToggleGroup>();

            DeitySelectorDropdown = GUIManager.Instance.CreateDropDown(
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(200f, 357f),
                fontSize: 18,
                width: 200f,
                height: 40f);

            Dropdown deityDropdown = DeitySelectorDropdown.GetComponent<Dropdown>();
            deityDropdown.AddOptions(Deities.DeityOptions());
            deityDropdown.value = 2; // Gefjun default
            deityDropdown.onValueChanged.AddListener(UpdateSelectedDiety);

            Logger.LogDebug("Setting Template");
            // Setup the template for adding entries

            SacrificeChoiceContainer = Object.Instantiate(new GameObject("ChoiceTemplate"), SacrificePanel.transform);

            //Image img = SacrificeChoiceContainer.AddComponent<Image>();
            //img.color = Color.white;
            LayoutElement le = SacrificeChoiceContainer.AddComponent<LayoutElement>();
            le.minHeight = 240;
            le.minWidth = 760;
            le.preferredWidth = 760;
            if (SacrificeChoiceContainer.GetComponent<RectTransform>() == null) { SacrificeChoiceContainer.AddComponent<RectTransform>(); }
            SacrificeChoiceContainer.layer = 5; // UI Layer
            var tf = SacrificeChoiceContainer.GetComponent<RectTransform>();
            tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 760);
            tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
            ContentSizeFitter csf = SacrificeChoiceContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            SacrificeChoiceContainer.SetActive(false);



            //SacrificeChoiceContainer = GUIManager.Instance.CreateWoodpanel(
            //    parent: SacrificePanel.transform,
            //    anchorMin: new Vector2(0f, 0f),
            //    anchorMax: new Vector2(1f, 1f),
            //    position: new Vector2(0, 0),
            //    width: 750,
            //    height: 200,
            //    draggable: false);
            //SacrificeChoiceContainer.SetActive(false);

            // Background border image

            var toggle = GUIManager.Instance.CreateToggle(
                parent: SacrificeChoiceContainer.transform,
                width: 40f,
                height: 40f
                );
            toggle.name = "selecter";
            toggle.transform.localPosition = new Vector3(-345, 15);
            toggle.transform.Find("Label").gameObject.SetActive(false);
            toggle.GetComponent<Toggle>().isOn = false;
            toggle.AddComponent<LayoutElement>();


            var sacrificeName = GUIManager.Instance.CreateText(
                text: "Name",
                parent: SacrificeChoiceContainer.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(16f, 62f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 660f,
                height: 80f,
                addContentSizeFitter: false);
            sacrificeName.name = "ChoiceName";
            sacrificeName.AddComponent<LayoutElement>();

            var choiceDesc = GUIManager.Instance.CreateText(
                text: "Desc",
                parent: SacrificeChoiceContainer.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(16f, 40f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: GUIManager.Instance.ValheimBeige,
                outline: true,
                outlineColor: Color.black,
                width: 660f,
                height: 80f,
                addContentSizeFitter: false);
            choiceDesc.name = "ChoiceDesc";
            choiceDesc.AddComponent<LayoutElement>();

            var requirementDesc = GUIManager.Instance.CreateText(
                text: "Description",
                parent: SacrificeChoiceContainer.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(16f, -40f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 660f,
                height: 80f,
                addContentSizeFitter: false);
            requirementDesc.name = "RequirementDesc";
            requirementDesc.AddComponent<LayoutElement>();
            //ContentSizeFitter rdcsf = requirementDesc.GetComponent<ContentSizeFitter>();
            //rdcsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            //rdcsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            GameObject itemreq = Object.Instantiate(new GameObject("ItemRequirements"), SacrificeChoiceContainer.transform);
            itemreq.name = "ItemRequirements";
            itemreq.transform.localPosition = new Vector3(-300f, 30f, 0f);
            itemreq.AddComponent<RectTransform>();
            itemreq.AddComponent<LayoutElement>();
        }

        private void SetChoiceList(Deities.Deity selectedDiety)
        {
            // Diety info
            DeityName.text = Localization.instance.Localize(Deities.DeityConfiguration[selectedDiety].NameLocalKey);
            DeityDescription.text = Localization.instance.Localize(Deities.DeityConfiguration[selectedDiety].DescriptionLocalKey);
            DeityImage.sprite = Deities.DeityConfiguration[selectedDiety].Image;

            SacrificeToggleOptions.Clear();
            // Delete the actual gameobjects, once the toggle list is cleared
            while (ScrollContentArea.transform.childCount > 0) {
                Transform tf = ScrollContentArea.transform.GetChild(0);
                if (tf == null) { break; }
                DestroyImmediate(tf.gameObject);
            }

            SelectedDeity = selectedDiety;
            int y_value = -40;
            Logger.LogDebug($"Setting up {selectedDiety}.");
            foreach (KeyValuePair<string, DataObjects.Sacrifice> entry in SacrificeData.AllSacrifices[selectedDiety])
            {
                bool has_required_keys = true;
                bool has_required_boons = true;
                bool has_required_oaths = true;
                // Player required to check requirements
                if (Player.m_localPlayer != null && ZoneSystem.instance != null) {
                    if (entry.Value.PlayerKeyRequirements != null) {
                        foreach (string requiredKey in entry.Value.PlayerKeyRequirements)
                        {
                            if (!ZoneSystem.instance.GetGlobalKey(requiredKey) && !Player.m_localPlayer.PlayerHasUniqueKey(requiredKey))
                            {
                                has_required_keys = false;
                                break;
                            }
                        }
                    }

                    if (entry.Value.PlayerBoonRequirements != null) {
                        foreach (KeyValuePair<DataObjects.Boons, float> boon in entry.Value.PlayerBoonRequirements)
                        {
                            if (PlayerData.localPlayerConfig.HasBoon(boon.Key, out float _) == false)
                            {
                                has_required_boons = false;
                                break;
                            }
                        }
                    }

                    if (entry.Value.PlayerOathRequirements != null) {
                        foreach (KeyValuePair<DataObjects.Oaths, float> boon in entry.Value.PlayerOathRequirements)
                        {
                            if (PlayerData.localPlayerConfig.HasOath(boon.Key, out float _) == false)
                            {
                                has_required_oaths = false;
                                break;
                            }
                        }
                    }
                }

                // Skip if failing a check
                Logger.LogDebug($"Check for listing requirement  required_keys met: {has_required_keys}, required_boons met: {has_required_boons}, required_oaths met: {has_required_oaths}");
                if (has_required_keys == false && ValConfig.KeyRequirementsHideChoices.Value == true) { continue; }
                if (has_required_boons == false && ValConfig.BoonRequirementsHideChoices.Value == true) { continue; }
                if (has_required_oaths == false && ValConfig.OathRequirementsHideChoices.Value == true) { continue; }

                // Required items are not a hard failure
                Logger.LogDebug("Creating Choice Container");

                var newSacrificeChoice = GameObject.Instantiate(SacrificeChoiceContainer, ScrollContentArea.transform);
                newSacrificeChoice.SetActive(true);
                var rect = newSacrificeChoice.GetComponent<RectTransform>();
                rect.localPosition = new Vector3() { x = 250, y = y_value };
                Logger.LogDebug("Created container");

                newSacrificeChoice.transform.Find("ChoiceName").GetComponent<Text>().text = entry.Value.Name;
                newSacrificeChoice.name = $"choice_{entry.Value.Name}";
                Logger.LogDebug("Set choice name");

                newSacrificeChoice.transform.Find("ChoiceDesc").GetComponent<Text>().text = entry.Value.Description;
                Logger.LogDebug("Set choice Desc");

                newSacrificeChoice.transform.Find("RequirementDesc").GetComponent<Text>().text = entry.Value.GetTotalDescription();
                Logger.LogDebug("Set requirement Desc");

                if (entry.Value.ItemRequirements != null) {
                    Logger.LogDebug("Setting up item requirements.");
                    Transform itemrequirementsParent = newSacrificeChoice.transform.Find("ItemRequirements");
                    if (itemrequirementsParent == null) {
                        Logger.LogWarning("Item requirements parent not found.");
                        //continue;
                    }
                    int item_x_offset = 0;
                    foreach (KeyValuePair<string, int> itemReq in entry.Value.ItemRequirements)
                    {
                        Logger.LogDebug($"Item Requirement: {itemReq.Key} x{itemReq.Value}");
                        GameObject prefab = PrefabManager.Instance.GetPrefab(itemReq.Key);
                        if (prefab != null)
                        {
                            prefab.TryGetComponent<ItemDrop>(out ItemDrop itemDrop);
                            if (itemDrop != null)
                            {
                                GameObject itemImageGO = Object.Instantiate(new GameObject($"ReqItem_{itemReq.Key}"), itemrequirementsParent);
                                //itemImageGO.transform.localPosition = new Vector3(-328f, 328f);
                                Image itemImage = itemImageGO.AddComponent<Image>();
                                itemImageGO.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                                itemImage.sprite = itemDrop.m_itemData.GetIcon();
                                Logger.LogDebug("Added Icon.");

                                var itemCount = GUIManager.Instance.CreateText(
                                    text: $"{itemReq.Value}",
                                    parent: itemrequirementsParent,
                                    anchorMin: new Vector2(0.5f, 0.5f),
                                    anchorMax: new Vector2(0.5f, 0.5f),
                                    position: new Vector2(-10f, 360f),
                                    font: GUIManager.Instance.AveriaSerifBold,
                                    fontSize: 12,
                                    // TODO: change this to a cool unique color
                                    color: Color.grey,
                                    outline: true,
                                    outlineColor: Color.black,
                                    width: 40f,
                                    height: 40f,
                                    addContentSizeFitter: false);
                                Logger.LogDebug("Added Text.");
                                itemCount.transform.localPosition = new Vector3(item_x_offset, 0f);
                                itemImageGO.transform.localPosition = new Vector3(item_x_offset, 0f);
                                item_x_offset += 50;
                                Logger.LogDebug("Repositioned Icon and Text.");
                            }
                        }
                    }
                    Logger.LogDebug("Set up Item requirements.");
                }
                

                var toggle = newSacrificeChoice.transform.Find("selecter").GetComponent<Toggle>();
                toggle.group = SacrificeChoiceGroup;
                toggle.onValueChanged.AddListener((isOn) => {
                    SelectedChoice = entry.Key;
                });
                Logger.LogDebug("Created onclick");
                SacrificeToggleOptions.Add(toggle);
                y_value -= 180;
            }
        }
    }
}
