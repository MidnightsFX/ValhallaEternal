using HarmonyLib;
using Jotunn.Managers;
using Splatform;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValhallEternal.common;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.modules
{

    public static class SacrificePatches
    {
        static SacrificeUI invGUISacrifice;
        static GameObject SacrificeEnableButton = null;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class AddSacrificeUIButton
        {
            
            public static void Postfix(InventoryGui __instance)
            {
                if (SacrificeEnableButton != null) {
                    if (Player.m_localPlayer != null) {
                        SacrificeEnableButton.SetActive(Player.m_localPlayer.NoCostCheat());
                    }
                    return;
                }

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

                invGUISacrifice = SacrificeEnableButton.AddComponent<SacrificeUI>();
                bclose.onClick.AddListener(invGUISacrifice.Show);
                SacrificeEnableButton.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class HideSacrificeUI_InventoryClose
        {
            public static void Postfix()
            {
                invGUISacrifice.Hide();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class HideSacrificeUI_InventoryOpen {
            public static void Postfix() {
                if (SacrificeEnableButton != null && Player.m_localPlayer != null) {
                    SacrificeEnableButton.SetActive(Player.m_localPlayer.NoCostCheat());
                }
            }
        }
    }



    internal class SacrificeUI : MonoBehaviour, Hoverable, Interactable {

        private GameObject SacrificePanel;
        private GameObject ScrollAreaView;
        private GameObject ScrollContentArea;
        private ToggleGroup SacrificeChoiceGroup;
        private GameObject SacrificeChoiceContainer;
        private GameObject ChoiceSelectButton;
        private GameObject ManualCloseButton;
        private GameObject DeitySelectorDropdown;


        //private static Text SacrificeRequirements;
        //private static Text BoonChanges;
        //private static Text OathChanges;
        //private static Text PrestigeResetDetails;
        private Text DeityName;
        private Text DeityDescription;
        private Image DeityImage;
        private Text WarningText;

        private List<Toggle> SacrificeToggleOptions = new List<Toggle>();
        private string SelectedChoice = "none";
        private Deities.Deity SelectedDeity;

        public bool enableExclusiveDeityMode;
        public Deities.Deity ExclusiveDeity;

        public void Awake()
        {
            //_instance = this;
        }

        public void Update() {
            if (SacrificePanel == null) { return; }
            if (SacrificePanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
                // Jotunn.Logger.LogInfo("Shrine UI detected close commands.");
                Hide();
                GUIManager.BlockInput(false);
            }
        }

        public void Show()
        {
            if (SacrificePanel == null) {
                CreateStaticUIObjects();
                Deities.Deity selectDeity = Deities.Deity.Gefjun;
                if (enableExclusiveDeityMode == true) {
                    selectDeity = ExclusiveDeity;
                    DeitySelectorDropdown.SetActive(false);
                }
                SetChoiceList(selectDeity);
            }
            GUIManager.BlockInput(true);
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

        public void Toggle() {
            if (SacrificePanel != null) {
                if (SacrificePanel.activeSelf == true) {
                    Hide();
                } else {
                    Show();
                }
            } else {
                Show();
            }
        }

        public void UpdateSelectedDiety(int _actionID)
        {
            if (enableExclusiveDeityMode == true) {
                SetChoiceList(ExclusiveDeity);
                return;
            }
            
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
            bool requirementsMet = true;
            Logger.LogInfo($"Completing sacrifice choice: {SelectedChoice} for deity {SelectedDeity}");
            DataObjects.Sacrifice selectedSacrifice = SacrificeData.AllSacrifices[SelectedDeity][SelectedChoice];
            // Check requirements first
            if (selectedSacrifice.ItemRequirements != null) {

                Dictionary<string, int> playerItems = Player.m_localPlayer.m_inventory.GetItemTotalsByName();

                foreach (KeyValuePair<string, int> itemReq in selectedSacrifice.ItemRequirements) {
                    bool hasItem = playerItems.ContainsKey(itemReq.Key);
                    Logger.LogDebug($"Checking for tribute {itemReq.Value} of item {itemReq.Key} - {hasItem}");
                    if (hasItem == false) {
                        requirementsMet = false;
                        Logger.LogDebug($"Player did not have {itemReq.Value} of item {itemReq.Key}");
                        break;
                    }
                }
            }
            if (selectedSacrifice.PlayerKeyRequirements != null) {
                foreach (string key in selectedSacrifice.PlayerKeyRequirements) {
                    // TODO: config for having key removal be global vs player unique
                    bool haskey = Player.m_localPlayer.PlayerHasUniqueKey(key);
                    Logger.LogDebug($"Checking unique key {key} from player - {haskey}");
                    if (haskey == false) {
                        requirementsMet = false;
                        Logger.LogDebug($"Player did not have the required key: {key}");
                        break;
                    }
                }
            }

            if (requirementsMet == false && Player.m_localPlayer.NoCostCheat() == false) {
                Logger.LogWarning("Player did not meet the requirements, canceling.");
                WarningText.text = $"Requirements not met";
                return;
            } else {
                Logger.LogDebug("Player has met requirements.");
            }

            // Only remove the items if all requirements can be satisfied
            if (selectedSacrifice.ItemRequirements != null) {
                foreach (KeyValuePair<string, int> itemReq in selectedSacrifice.ItemRequirements) {
                    bool removedItem = Player.m_localPlayer.GetInventory().RemoveItemByPrefab(itemReq.Key, itemReq.Value);
                    if (removedItem == false) {
                        Logger.LogWarning("Unable to remove all of the required items, are you trying to cheat the deity?");
                        return;
                    }
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
                    //Player.m_localPlayer.m_knownBiome.Clear();
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

            if (selectedSacrifice.PrestigeOptions != null) {
                foreach (PrestigeEffectDetails prestigeOption in selectedSacrifice.PrestigeOptions) {
                    if (prestigeOption.PlayerMeetsPrestigeRequirements() == false) { continue; }

                    PlayerData.AddVisualPrestigeEffectOptionToPlayerConfig(prestigeOption.EffectType, prestigeOption.EffectValue);
                    Logger.LogDebug($"Adding prestige effect option {prestigeOption.EffectType} of type {prestigeOption.EffectValue} to player config.");
                    PlayerData.SetActivePrestigeEffectForPlayer(prestigeOption.EffectType, prestigeOption.EffectValue);
                }
            }

            WarningText.text = "";
            GUIManager.BlockInput(false);
            PlayerData.SavePlayerConfiguration();
            PlayerData.LoadPlayerConfiguration(Player.m_localPlayer);
            PrestigeDisplays.UpdateLocalPlayerLevelDisplay();
            Hide();
            UpdateSelectedDiety(-1); //refresh list, to show newly available options
            if (enableExclusiveDeityMode) {
                SetChoiceList(SelectedDeity);
            }
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

            var warning = GUIManager.Instance.CreateText(
                text: "",
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(335f, -350f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 14,
                color: GUIManager.Instance.ValheimYellow,
                outline: true,
                outlineColor: Color.black,
                width: 250f,
                height: 40f,
                addContentSizeFitter: false);
            warning.name = "Warning";
            WarningText = warning.GetComponent<Text>();

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




            //Logger.LogDebug("Setting up scroll entry");
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
            ScrollAreaView.GetComponentInChildren<ScrollRect>().scrollSensitivity = 1000;
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

            //Logger.LogDebug("Setting Template");
            // Setup the template for adding entries

            SacrificeChoiceContainer = Object.Instantiate(new GameObject("ChoiceTemplate"), SacrificePanel.transform);

            //Image img = SacrificeChoiceContainer.AddComponent<Image>();
            //img.color = Color.white;
            VerticalLayoutGroup vlg = SacrificeChoiceContainer.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlHeight = true;
            //vlg.childControlWidth = true;
            vlg.spacing = 5f;
            vlg.childForceExpandHeight = false;

            //le.minHeight = 240;
            //le.minWidth = 760;
            //le.preferredWidth = 760;
            //le.flexibleHeight = 1200;
            if (SacrificeChoiceContainer.GetComponent<RectTransform>() == null) { SacrificeChoiceContainer.AddComponent<RectTransform>(); }
            SacrificeChoiceContainer.layer = 5; // UI Layer
            var tf = SacrificeChoiceContainer.GetComponent<RectTransform>();
            tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 760);
            tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
            ContentSizeFitter csf = SacrificeChoiceContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            SacrificeChoiceContainer.SetActive(false);

            //Row 1 - Toggle + Name
            GameObject choiceHeaderSection = Object.Instantiate(new GameObject("ChoiceHeader"), SacrificeChoiceContainer.transform);
            choiceHeaderSection.name = "ChoiceHeader";
            HorizontalLayoutGroup hzlygch = choiceHeaderSection.AddComponent<HorizontalLayoutGroup>();
            hzlygch.childForceExpandHeight = false;
            hzlygch.childForceExpandWidth = false;
            hzlygch.childControlWidth = true;
            LayoutElement chle = choiceHeaderSection.AddComponent<LayoutElement>();
            chle.minHeight = 40;

            var toggle = GUIManager.Instance.CreateToggle(
                parent: choiceHeaderSection.transform,
                width: 40f,
                height: 40f
                );
            toggle.name = "selecter";
            toggle.transform.localPosition = new Vector3(-370, 10);
            toggle.transform.Find("Label").gameObject.SetActive(false);
            toggle.GetComponent<Toggle>().isOn = false;
            LayoutElement tle = toggle.AddComponent<LayoutElement>();
            tle.minWidth = 45;
            HorizontalLayoutGroup tglhzlg = toggle.AddComponent<HorizontalLayoutGroup>();
            tglhzlg.childForceExpandHeight = false;
            tglhzlg.childForceExpandWidth = false;
            tglhzlg.childControlWidth = true;
            GameObject tbg = toggle.transform.Find("Background").gameObject;
            LayoutElement tbglayout = tbg.AddComponent<LayoutElement>();
            tbglayout.minHeight = 40;
            tbglayout.minWidth = 40;


            var sacrificeName = GUIManager.Instance.CreateText(
                text: "Name",
                parent: choiceHeaderSection.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 30,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 660f,
                height: 80f,
                addContentSizeFitter: false);
            sacrificeName.name = "ChoiceName";
            sacrificeName.transform.localPosition = new Vector3(-240, -10);
            LayoutElement chlayout = sacrificeName.AddComponent<LayoutElement>();
            chlayout.flexibleHeight = 900;
            chlayout.minHeight = 40;
            //chlayout.minWidth = 40;

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
            LayoutElement clayout = choiceDesc.AddComponent<LayoutElement>();
            clayout.flexibleHeight = 900;
            VerticalLayoutGroup clayoutg = choiceDesc.AddComponent<VerticalLayoutGroup>();
            clayoutg.childForceExpandHeight = false;
            clayoutg.childForceExpandWidth = false;
            clayoutg.childControlWidth = true;

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
            LayoutElement dlayout = requirementDesc.AddComponent<LayoutElement>();
            dlayout.flexibleHeight = 900;
            VerticalLayoutGroup dlayoutg = requirementDesc.AddComponent<VerticalLayoutGroup>();
            dlayoutg.childForceExpandHeight = false;
            dlayoutg.childForceExpandWidth = false;
            dlayoutg.childControlWidth = true;

            GameObject itemreq = Object.Instantiate(new GameObject("ItemRequirements"), SacrificeChoiceContainer.transform);
            itemreq.name = "ItemRequirements";
            itemreq.transform.localPosition = new Vector3(-300f, 30f, 0f);
            itemreq.AddComponent<RectTransform>();
            itemreq.AddComponent<LayoutElement>();
            HorizontalLayoutGroup dlitemreq = itemreq.AddComponent<HorizontalLayoutGroup>();
            dlitemreq.childForceExpandHeight = false;
            dlitemreq.childForceExpandWidth = false;


            // Footer
            GameObject FooterSection = Object.Instantiate(new GameObject("ChoiceFooter"), SacrificeChoiceContainer.transform);
            FooterSection.name = "ChoiceFooter";
            LayoutElement flayout = FooterSection.AddComponent<LayoutElement>();
            flayout.minHeight = 50;
            VerticalLayoutGroup fvlg = FooterSection.AddComponent<VerticalLayoutGroup>();
            fvlg.childForceExpandHeight = false;
            fvlg.childForceExpandWidth = false;
            fvlg.childControlWidth = true;
            Image bkgimg = FooterSection.AddComponent<Image>();
            bkgimg.sprite = DataObjects.boonbackground;
            bkgimg.GetComponent<RectTransform>().sizeDelta = new Vector2(512, 50);
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
            //Logger.LogDebug($"Setting up {selectedDiety}.");
            foreach (KeyValuePair<string, DataObjects.Sacrifice> entry in SacrificeData.AllSacrifices[selectedDiety])
            {
                bool has_required_keys = true;
                bool has_required_boons = true;
                bool has_required_oaths = true;
                bool has_not_hit_boon_cap = true;
                // Player required to check requirements
                if (Player.m_localPlayer != null && ZoneSystem.instance != null) {
                    if (entry.Value.PlayerKeyRequirements != null) {
                        foreach (string requiredKey in entry.Value.PlayerKeyRequirements) {
                            if (!ZoneSystem.instance.GetGlobalKey(requiredKey) && !Player.m_localPlayer.PlayerHasUniqueKey(requiredKey)) {
                                has_required_keys = false;
                                break;
                            }
                        }
                    }

                    if (entry.Value.PlayerBoonRequirements != null) {
                        foreach (KeyValuePair<DataObjects.Boons, float> boon in entry.Value.PlayerBoonRequirements) {
                            if (PlayerData.localPlayerConfig.HasBoon(boon.Key, out float _) == false) {
                                has_required_boons = false;
                                break;
                            }
                        }
                    }

                    if (entry.Value.PlayerOathRequirements != null) {
                        foreach (KeyValuePair<DataObjects.Oaths, float> oath in entry.Value.PlayerOathRequirements) {
                            if (PlayerData.localPlayerConfig.HasOath(oath.Key, out float _) == false) {
                                has_required_oaths = false;
                                break;
                            }
                        }
                    }

                    if (entry.Value.PlayerBoonLimit != null) {
                        foreach (KeyValuePair<DataObjects.Boons, float> boon in entry.Value.PlayerBoonLimit) {
                            if (PlayerData.localPlayerConfig.HasBoon(boon.Key, out float value) == true && value >= boon.Value) {
                                has_not_hit_boon_cap = false;
                                break;
                            }
                        }
                    }
                }

                // Skip if failing a check
                //Logger.LogDebug($"Check for listing requirement  required_keys met: {has_required_keys}, required_boons met: {has_required_boons}, required_oaths met: {has_required_oaths}");
                if (has_required_keys == false && ValConfig.KeyRequirementsHideChoices.Value == true) { continue; }
                if (has_required_boons == false && ValConfig.BoonRequirementsHideChoices.Value == true) { continue; }
                if (has_required_oaths == false && ValConfig.OathRequirementsHideChoices.Value == true) { continue; }
                if (has_not_hit_boon_cap == false) { continue; }

                // Required items are not a hard failure
                //Logger.LogDebug("Creating Choice Container");

                var newSacrificeChoice = GameObject.Instantiate(SacrificeChoiceContainer, ScrollContentArea.transform);
                newSacrificeChoice.SetActive(true);
                var rect = newSacrificeChoice.GetComponent<RectTransform>();
                rect.localPosition = new Vector3() { x = 250, y = y_value };
                //Logger.LogDebug("Created container");

                newSacrificeChoice.transform.Find("ChoiceHeader/ChoiceName").GetComponent<Text>().text = entry.Value.Name;
                newSacrificeChoice.name = $"choice_{entry.Value.Name}";
                //Logger.LogDebug("Set choice name");

                newSacrificeChoice.transform.Find("ChoiceDesc").GetComponent<Text>().text = entry.Value.Description;
                //Logger.LogDebug("Set choice Desc");

                newSacrificeChoice.transform.Find("RequirementDesc").GetComponent<Text>().text = entry.Value.GetTotalDescription();
                //Logger.LogDebug("Set requirement Desc");

                if (entry.Value.ItemRequirements != null) {
                    //Logger.LogDebug("Setting up item requirements.");
                    Transform itemrequirementsParent = newSacrificeChoice.transform.Find("ItemRequirements");
                    if (itemrequirementsParent == null) {
                        Logger.LogWarning("Item requirements parent not found.");
                        //continue;
                    }
                    int item_x_offset = 0;
                    foreach (KeyValuePair<string, int> itemReq in entry.Value.ItemRequirements)
                    {
                        //Logger.LogDebug($"Item Requirement: {itemReq.Key} x{itemReq.Value}");
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
                                LayoutElement ilayout = itemImageGO.AddComponent<LayoutElement>();
                                ilayout.minHeight = 50;
                                ilayout.minWidth = 50;
                                itemImage.sprite = itemDrop.m_itemData.GetIcon();
                                //Logger.LogDebug("Added Icon.");

                                GameObject itemCount = GUIManager.Instance.CreateText(
                                    text: $"{itemReq.Value}",
                                    parent: itemrequirementsParent,
                                    anchorMin: new Vector2(0.5f, 0.5f),
                                    anchorMax: new Vector2(0.5f, 0.5f),
                                    position: new Vector2(-10f, 360f),
                                    font: GUIManager.Instance.AveriaSerifBold,
                                    fontSize: 14,
                                    // TODO: change this to a cool unique color
                                    color: Color.grey,
                                    outline: true,
                                    outlineColor: Color.black,
                                    width: 40f,
                                    height: 40f,
                                    addContentSizeFitter: false);
                                //Logger.LogDebug("Added Text.");
                                LayoutElement icountLE = itemCount.AddComponent<LayoutElement>();
                                icountLE.minHeight = 10;
                                icountLE.minWidth = 10;
                                itemCount.transform.localPosition = new Vector3(item_x_offset, 0f);
                                itemImageGO.transform.localPosition = new Vector3(item_x_offset, 0f);
                                item_x_offset += 45;
                                //Logger.LogDebug("Repositioned Icon and Text.");
                            }
                        }
                    }
                    //Logger.LogDebug("Set up Item requirements.");
                }
                

                var toggle = newSacrificeChoice.transform.Find("ChoiceHeader/selecter").GetComponent<Toggle>();
                toggle.group = SacrificeChoiceGroup;
                toggle.onValueChanged.AddListener((isOn) => {
                    SelectedChoice = entry.Key;
                });
                //Logger.LogDebug("Created onclick");
                SacrificeToggleOptions.Add(toggle);
                y_value -= 180;
            }
        }

        public bool Interact(Humanoid user, bool hold, bool alt) {
            Toggle();
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) {
            return false;
        }

        public string GetHoverText() {
            return Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] $ve_shrine_of_the_gods");
        }

        public string GetHoverName() {
            return Localization.instance.Localize($"$ve_tribute_shrine");
        }
    }
}
