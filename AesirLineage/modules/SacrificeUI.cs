using Jotunn.Managers;
using Splatform;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using AesirLineage.common;
using static AesirLineage.common.DataObjects;
using AesirLineage.visuals;
using AesirLineage.starmap;

namespace AesirLineage.modules
{

    internal class SacrificeUI : MonoBehaviour, Hoverable, Interactable {
        private const string AscendKey = "ascend";
        private const string BlessingKey = "blessing";
        private const string QuestKey = "quest";
        private static readonly Color InfoNameColor = new Color(0.6f, 0.63f, 0.67f);
        private const float AscendConfirmSeconds = 5f;
        private static float ascendConfirmUntil;
        // A reset undoes every star of the deity's and costs an offering, so it also takes a second press.
        private const float ResetConfirmSeconds = 5f;
        private static float resetConfirmUntil;

        private GameObject SacrificePanel;
        private GameObject ScrollAreaView;
        private GameObject ScrollContentArea;
        private ToggleGroup SacrificeChoiceGroup;
        private GameObject SacrificeChoiceContainer;
        private GameObject ChoiceSelectButton;
        private GameObject ResetButton;
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
        private Text HeaderDescription;

        private List<Toggle> SacrificeToggleOptions = new List<Toggle>();
        private string SelectedChoice = "none";
        private Deities.Deity SelectedDeity;
        // Jotunn counts input blocks, so the panel releases only a block it took.
        private bool holdingInput;

        /// <summary>One entry on the shrine: what it costs and what offering it does.</summary>
        private class ShrineChoice {
            public string Key;
            public string Name;
            public string Description;
            public string Details;
            public Dictionary<string, int> Items;
            public List<string> Keys;
            public System.Action Complete;
            /// <summary>False for an entry that only says why something can't be had now; it has no toggle.</summary>
            public bool Selectable = true;
        }

        private readonly List<ShrineChoice> choices = new List<ShrineChoice>();

        public bool enableExclusiveDeityMode;
        public Deities.Deity ExclusiveDeity;

        public void Awake()
        {
            //_instance = this;
        }

        // After every Update, so the game menu has already seen input blocked and won't open on the same
        // Escape. ZInput, since the game reads keys through the new input system.
        public void LateUpdate() {
            if (SacrificePanel == null) { return; }
            // With the star map open on top, Escape closes only the star map, whichever LateUpdate runs first.
            if (ui.StarMapWindow.IsOpen || ui.StarMapWindow.LastClosedFrame == Time.frameCount) { return; }
            if (SacrificePanel.activeSelf && ZInput.GetKeyDown(KeyCode.Escape, logWarning: false)) {
                Hide();
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
            } else {
                // Points, steps and stars may have changed since it was last open.
                SetChoiceList(SelectedDeity);
            }
            if (!holdingInput) {
                GUIManager.BlockInput(true);
                holdingInput = true;
            }
            SacrificePanel.SetActive(true);
        }

        public void Hide()
        {
            if (SacrificePanel != null)
            {
                SacrificePanel.SetActive(false);
            }
            if (holdingInput) {
                holdingInput = false;
                GUIManager.BlockInput(false);
            }
        }

        private void OnDestroy() {
            if (holdingInput) {
                holdingInput = false;
                GUIManager.BlockInput(false);
            }
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
            ShrineChoice choice = choices.Find(c => c.Key == SelectedChoice);
            if (choice == null || !choice.Selectable) {
                WarningText.text = Localization.instance.Localize("$al_shrine_choose");
                return;
            }
            Player player = Player.m_localPlayer;
            if (player == null) { return; }
            bool free = player.NoCostCheat();
            if (!free && (!HasItems(player, choice.Items) || !HasKeys(player, choice.Keys))) {
                WarningText.text = Localization.instance.Localize("$al_shrine_requirements_not_met");
                return;
            }
            // Ascension can't be undone, so it takes a second offer within a few seconds.
            if (choice.Key == AscendKey && Time.time > ascendConfirmUntil) {
                ascendConfirmUntil = Time.time + AscendConfirmSeconds;
                WarningText.text = Localization.instance.Localize("$al_ascend_confirm");
                return;
            }
            ascendConfirmUntil = 0f;
            if (!free && !TakeItems(player, choice.Items, choice.Key)) { return; }
            Logger.LogInfo($"Offering {choice.Key} to {SelectedDeity}.");
            WarningText.text = "";
            DeityVfx.Share(SelectedDeity, VfxRole.Burst, player.transform.position, 0.7f);
            choice.Complete();
            // Blessings keep the shrine open, showing what changed; Ascension closes it.
            if (SacrificePanel != null && SacrificePanel.activeSelf) {
                SetChoiceList(SelectedDeity);
            }
        }

        /// <summary>
        /// The shrine's reset button: every one of the deity's stars gives its points back, for the shrine's
        /// reset offering. Spent points are otherwise locked, so the first press says what it costs and only
        /// a second press within a few seconds goes through.
        /// </summary>
        public void ResetStarMap() {
            Player player = Player.m_localPlayer;
            if (player == null) { return; }
            Localization loc = Localization.instance;
            Deities.Deity deity = SelectedDeity;
            string deityName = Compendium.DeityName(deity);
            if (PlayerData.Allocated(deity).Count == 0) {
                WarningText.text = loc.Localize("$al_shrine_reset_none", deityName);
                return;
            }
            // A deity left out of Shrines.yaml asks for nothing, so its points are never locked for good.
            Dictionary<string, int> cost = ShrineData.All.TryGetValue(deity, out DeityShrine shrine) && shrine != null ? shrine.ResetCost : null;
            bool free = player.NoCostCheat() || cost == null || cost.Count == 0;
            if (!free && !HasItems(player, cost)) {
                WarningText.text = loc.Localize("$al_shrine_reset_short", ItemList(cost));
                return;
            }
            if (Time.time > resetConfirmUntil) {
                resetConfirmUntil = Time.time + ResetConfirmSeconds;
                WarningText.text = loc.Localize("$al_shrine_reset_confirm", deityName, PlayerData.PointsSpent(deity).ToString());
                if (!free) { WarningText.text += " " + loc.Localize("$al_shrine_reset_cost", ItemList(cost)); }
                return;
            }
            resetConfirmUntil = 0f;
            if (!free && !TakeItems(player, cost, "reset")) { return; }
            Logger.LogInfo($"Resetting {deity}'s star map at the shrine.");
            WarningText.text = "";
            DeityVfx.Share(deity, VfxRole.Burst, player.transform.position, 0.7f);
            ResetStars(deity);
            SetChoiceList(deity);
        }

        // Called once HasItems has passed, so every item is known to be there before any is taken.
        private bool TakeItems(Player player, Dictionary<string, int> items, string what) {
            if (items == null) { return true; }
            foreach (KeyValuePair<string, int> item in items) {
                if (!player.GetInventory().RemoveItemByPrefab(item.Key, item.Value)) {
                    Logger.LogWarning($"Could not take {item.Value} {item.Key} for {SelectedDeity}'s {what}.");
                    return false;
                }
            }
            return true;
        }

        // "Eikthyr trophy x1, ..." in the player's language, for the reset button's messages.
        private static string ItemList(Dictionary<string, int> items) {
            return string.Join(", ", items.Select(item => {
                GameObject prefab = PrefabManager.Instance.GetPrefab(item.Key);
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                string name = drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : item.Key;
                return $"{name} x{item.Value}";
            }));
        }

        private static bool HasItems(Player player, Dictionary<string, int> items) {
            if (items == null || items.Count == 0) { return true; }
            Dictionary<string, int> held = player.m_inventory.GetItemTotalsByName();
            return items.All(item => held.TryGetValue(item.Key, out int count) && count >= item.Value);
        }

        // Boss keys live in ZoneSystem's global keys in vanilla, and in VentureValheim's per-player
        // private keys when its private-key mode is on (VV patches GetGlobalKey to answer for those).
        private static bool HasKeys(Player player, List<string> keys) {
            if (keys == null || keys.Count == 0) { return true; }
            return keys.All(key => (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key)) || player.PlayerHasUniqueKey(key));
        }

        private void Bless(Deities.Deity deity, Boon blessing) {
            PlayerData.RecordBlessing(deity);
            Boons.Start(deity, BoonKind.Blessing, blessing);
        }

        private void ResetStars(Deities.Deity deity) {
            PlayerData.ResetDeity(deity);
            Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$al_shrine_reset_done", Compendium.DeityName(deity)));
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
                text: Localization.instance.Localize("$al_gefjun_header"),
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
                text: Localization.instance.Localize("$al_gefjun_description"),
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
                text: Localization.instance.Localize("$al_sacrifice_header"),
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

            GameObject starMapButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$al_starmap_shrine_button"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(275f, 262f),
                width: 160f,
                height: 36f);
            starMapButton.GetComponent<Button>().onClick.AddListener(() => ui.StarMapWindow.Show(SelectedDeity));

            GameObject descgo = GUIManager.Instance.CreateText(
                text: Localization.instance.Localize("$al_sacrifice_header_desc"),
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
            HeaderDescription = desctextgo;
            desctextgo.resizeTextForBestFit = true;
            desctextgo.resizeTextMaxSize = 20;
            desctextgo.alignment = TextAnchor.UpperLeft;

            // Between the Ask button and the panel's edge; the reset's confirmation runs to a few lines.
            var warning = GUIManager.Instance.CreateText(
                text: "",
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(275f, -350f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 14,
                color: GUIManager.Instance.ValheimYellow,
                outline: true,
                outlineColor: Color.black,
                width: 240f,
                height: 64f,
                addContentSizeFitter: false);
            warning.name = "Warning";
            WarningText = warning.GetComponent<Text>();
            WarningText.resizeTextForBestFit = true;
            WarningText.resizeTextMinSize = 10;
            WarningText.resizeTextMaxSize = 14;
            WarningText.alignment = TextAnchor.MiddleLeft;

            ManualCloseButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$al_close"),
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
                text: Localization.instance.Localize("$al_sacrifice_sealed"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, -350f),
                width: 300f,
                height: 60f);
            Button bselect = ChoiceSelectButton.GetComponent<Button>();
            //bselect.interactable = false;
            bselect.onClick.AddListener(CompleteSacrifice);

            ResetButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$al_shrine_reset_button"),
                parent: SacrificePanel.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(-275f, -350f),
                width: 200f,
                height: 50f);
            ResetButton.name = "ResetStarMap";
            ResetButton.GetComponent<Button>().onClick.AddListener(ResetStarMap);




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
            SelectedChoice = "none";
            resetConfirmUntil = 0f;
            ResetButton.GetComponent<Button>().interactable = PlayerData.Allocated(selectedDiety).Count > 0;
            BuildChoices(selectedDiety);

            Player player = Player.m_localPlayer;
            Dictionary<string, int> held = player != null ? player.m_inventory.GetItemTotalsByName() : new Dictionary<string, int>();
            int y_value = -40;
            foreach (ShrineChoice choice in choices)
            {
                GameObject entry = GameObject.Instantiate(SacrificeChoiceContainer, ScrollContentArea.transform);
                entry.SetActive(true);
                entry.GetComponent<RectTransform>().localPosition = new Vector3() { x = 250, y = y_value };
                entry.name = $"choice_{choice.Key}";
                Text name = entry.transform.Find("ChoiceHeader/ChoiceName").GetComponent<Text>();
                name.text = choice.Name;
                entry.transform.Find("ChoiceDesc").GetComponent<Text>().text = choice.Description;
                entry.transform.Find("RequirementDesc").GetComponent<Text>().text = choice.Details;
                AddItemIcons(entry.transform.Find("ItemRequirements"), choice.Items, held);

                Toggle toggle = entry.transform.Find("ChoiceHeader/selecter").GetComponent<Toggle>();
                if (!choice.Selectable) {
                    toggle.gameObject.SetActive(false);
                    name.color = InfoNameColor;
                    y_value -= 180;
                    continue;
                }
                toggle.group = SacrificeChoiceGroup;
                string key = choice.Key;
                toggle.onValueChanged.AddListener((isOn) => {
                    if (isOn) { SelectedChoice = key; }
                });
                SacrificeToggleOptions.Add(toggle);
                y_value -= 180;
            }
        }

        /// <summary>
        /// The deity's quest and blessing (or why they can't be had now), and Ascension until the player has
        /// ascended with the deity as often as allowed. The star map's reset has its own button.
        /// </summary>
        private void BuildChoices(Deities.Deity deity) {
            choices.Clear();
            string deityName = Compendium.DeityName(deity);
            HeaderDescription.text = Localization.instance.Localize("$al_shrine_intro", deityName);
            if (Quests.BossFor(deity) != null) {
                HeaderDescription.text += "\n" + Localization.instance.Localize("$al_shrine_quests_done", deityName, PlayerData.QuestsDone(deity).ToString());
            }
            if (ValConfig.LuckyPointsPerDeity.Value > 0) {
                HeaderDescription.text += "\n" + Localization.instance.Localize("$al_shrine_lucky", deityName,
                    Deeds.LuckyPoints(deity).ToString(), ValConfig.LuckyPointsPerDeity.Value.ToString());
            }
            if (!ShrineData.All.TryGetValue(deity, out DeityShrine shrine) || shrine == null) { return; }
            Player player = Player.m_localPlayer;

            ShrineChoice quest = QuestChoice(deity);
            if (quest != null) { choices.Add(quest); }
            if (shrine.Blessing != null) { choices.Add(BlessingChoice(deity, shrine.Blessing)); }

            AscensionTribute ascension = shrine.Ascension;
            if (ascension != null && Ascension.CanAscend(deity)
                && (player == null || HasKeys(player, ascension.RequiredKeys) || !ValConfig.KeyRequirementsHideChoices.Value)) {
                int next = PlayerData.AscensionLevel(deity) + 1;
                choices.Add(new ShrineChoice() {
                    Key = AscendKey,
                    Name = Localization.instance.Localize("$al_ascend_name", deityName, next.ToString(), Ascension.Max.ToString()),
                    Description = Localization.instance.Localize("$al_ascend_desc", deityName),
                    Details = AscensionDetails(deity, ascension, next, player),
                    Items = ascension.Items,
                    Keys = ascension.RequiredKeys,
                    Complete = () => {
                        Ascension.Ascend(Player.m_localPlayer, deity, ascension);
                        Hide();
                    },
                });
            }
        }

        // The deity's quest to take, or the one under way, or how long until the next. None when no boss is
        // mapped to the deity.
        private ShrineChoice QuestChoice(Deities.Deity deity) {
            string underWay = Quests.UnderWay(deity);
            string boss = underWay ?? Quests.BossFor(deity);
            if (boss == null) { return null; }
            Localization loc = Localization.instance;
            string deityName = Compendium.DeityName(deity);
            string bossName = Quests.BossName(boss);
            int days = Quests.DaysUntilNext(deity);
            ShrineChoice choice = new ShrineChoice() {
                Key = QuestKey,
                Name = loc.Localize("$al_quest_name", bossName),
                Details = loc.Localize("$al_grants_points", $"<color=#b9f2ff>{ValConfig.QuestPoints.Value}</color>", deityName),
            };
            if (underWay != null) {
                choice.Selectable = false;
                choice.Description = loc.Localize("$al_quest_under_way", deityName, bossName);
            } else if (days > 0) {
                choice.Selectable = false;
                choice.Description = loc.Localize(days == 1 ? "$al_quest_tomorrow" : "$al_quest_wait", deityName, days.ToString());
            } else {
                choice.Description = loc.Localize("$al_quest_desc", deityName, bossName);
                choice.Details += "\n" + loc.Localize("$al_quest_no_limit");
                choice.Complete = () => Quests.Take(deity);
            }
            return choice;
        }

        // The blessing to take, or, when it can't be had now, why: the deity's quest isn't done this run,
        // or the deity has blessed the player today already.
        private ShrineChoice BlessingChoice(Deities.Deity deity, Boon blessing) {
            Localization loc = Localization.instance;
            string deityName = Compendium.DeityName(deity);
            StringBuilder details = new StringBuilder();
            details.AppendLine($"<color=#b9f2ff>{Boons.Describe(blessing)}</color>");
            details.AppendLine(loc.Localize("$al_blessing_lasts", Boons.Minutes(BoonKind.Blessing).ToString()));
            ShrineChoice choice = new ShrineChoice() { Key = BlessingKey, Name = Boons.DisplayName(blessing) };

            if (!Quests.BlessingOpen(deity)) {
                choice.Selectable = false;
                choice.Description = loc.Localize("$al_blessing_locked", deityName);
            } else if (PlayerData.BlessedToday(deity)) {
                choice.Selectable = false;
                float left = Boons.SecondsLeft(deity, BoonKind.Blessing);
                choice.Description = left > 0f
                    ? loc.Localize("$al_blessing_active", Mathf.CeilToInt(left / 60f).ToString())
                    : loc.Localize("$al_blessing_tomorrow", deityName);
            } else {
                choice.Description = loc.Localize("$al_blessing_desc", deityName);
                choice.Items = blessing.Offering;
                if (blessing.Offering.Count > 0) { details.Append(loc.Localize("$al_item_req")); }
                choice.Complete = () => Bless(deity, blessing);
            }
            choice.Details = details.ToString();
            return choice;
        }

        // What the next Ascension gives, then what it costs, then what it needs.
        private static string AscensionDetails(Deities.Deity deity, AscensionTribute ascension, int level, Player player) {
            Localization loc = Localization.instance;
            string deityName = Compendium.DeityName(deity);
            StringBuilder sb = new StringBuilder();
            if (ascension.Points != 0) {
                sb.AppendLine(loc.Localize("$al_grants_points", $"<color=#b9f2ff>{ascension.Points}</color>", deityName));
            }
            foreach (StarNode star in Ascension.StarsOpenedAt(deity, level)) {
                sb.AppendLine($"<color=#c9a0ff>{loc.Localize("$al_ascend_opens", star.DisplayName)}</color>");
            }
            IReadOnlyList<CosmeticSlot> looks = Ascension.LooksAt(level);
            if (looks.Count > 0 && ValConfig.EnableLooks.Value) {
                string names = string.Join(", ", looks.Select(slot => loc.Localize($"$al_slot_{slot}")));
                sb.AppendLine($"<color=#c9a0ff>{loc.Localize("$al_ascend_keeps", deityName, names)}</color>");
            }
            sb.AppendLine(loc.Localize("$al_ascend_souls"));
            sb.AppendLine();
            if (ValConfig.AscensionSkillLoss.Value > 0) {
                sb.AppendLine($"<color=#ff4040>{loc.Localize("$al_ascend_cost_skills", ValConfig.AscensionSkillLoss.Value.ToString())}</color>");
            }
            if (ValConfig.AscensionForgetsRecipes.Value) { sb.AppendLine($"<color=#ff4040>{loc.Localize("$al_ascend_cost_recipes")}</color>"); }
            if (ValConfig.AscensionReturnsToStart.Value) { sb.AppendLine($"<color=#ff4040>{loc.Localize("$al_ascend_cost_start")}</color>"); }
            if (ValConfig.AscensionRemovesBossKeys.Value) { sb.AppendLine($"<color=#ff4040>{loc.Localize("$al_ascend_cost_keys")}</color>"); }
            if (AscensionRagnarok.WouldAdvance) {
                sb.AppendLine($"<color=#ff4040>{loc.Localize("$al_ascend_cost_ragnarok", ValConfig.AscensionRagnarokDays.Value.ToString("0.##"))}</color>");
            }
            AppendKeys(sb, ascension.RequiredKeys, player);
            if (ascension.Items.Count > 0) { sb.Append(loc.Localize("$al_item_req")); }
            return sb.ToString();
        }

        private static void AppendKeys(StringBuilder sb, List<string> keys, Player player) {
            if (keys == null || keys.Count == 0) { return; }
            sb.AppendLine(Localization.instance.Localize("$al_requires"));
            foreach (string key in keys) {
                bool met = player != null && HasKeys(player, new List<string>() { key });
                sb.AppendLine($"<color={(met ? "#b9f2ff" : "#ff4040")}>  {key}</color>");
            }
        }

        // Each item's icon with "held/needed" under it, red while the player is short.
        private void AddItemIcons(Transform parent, Dictionary<string, int> items, Dictionary<string, int> held) {
            if (parent == null || items == null) { return; }
            foreach (KeyValuePair<string, int> item in items) {
                held.TryGetValue(item.Key, out int have);
                Color countColor = have >= item.Value ? GUIManager.Instance.ValheimYellow : new Color(1f, 0.35f, 0.3f);
                GameObject prefab = PrefabManager.Instance.GetPrefab(item.Key);
                ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;

                GameObject icon = new GameObject($"ReqItem_{item.Key}", typeof(RectTransform));
                icon.transform.SetParent(parent, false);
                LayoutElement iconLayout = icon.AddComponent<LayoutElement>();
                iconLayout.minHeight = 50;
                iconLayout.minWidth = 50;
                if (itemDrop != null) {
                    icon.AddComponent<Image>().sprite = itemDrop.m_itemData.GetIcon();
                } else {
                    // A typo in Shrines.yaml, or an item from a mod that isn't installed: show the name
                    // rather than leave a gap that looks free.
                    Logger.LogWarning($"Shrine item '{item.Key}' is not a known item.");
                    Text missing = icon.AddComponent<Text>();
                    missing.font = GUIManager.Instance.AveriaSerif;
                    missing.fontSize = 11;
                    missing.color = countColor;
                    missing.alignment = TextAnchor.MiddleCenter;
                    missing.text = item.Key;
                }

                GameObject count = GUIManager.Instance.CreateText(
                    text: $"{have}/{item.Value}",
                    parent: parent,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: Vector2.zero,
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 14,
                    color: countColor,
                    outline: true,
                    outlineColor: Color.black,
                    width: 50f,
                    height: 40f,
                    addContentSizeFitter: false);
                LayoutElement countLayout = count.AddComponent<LayoutElement>();
                countLayout.minHeight = 10;
                countLayout.minWidth = 10;
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
            return $"{GetHoverName()}\n" + Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] $al_shrine_pray");
        }

        public string GetHoverName() {
            return enableExclusiveDeityMode
                ? Localization.instance.Localize("$al_shrine_of", Compendium.DeityName(ExclusiveDeity))
                : Localization.instance.Localize("$al_shrine_name");
        }

        public float GetHoverOffset() {
            // Hoverable gained this member in the Sept 2026 game update. Every vanilla implementer
            // returns a serialized m_hoverOffset; zero keeps the shrine's hover text where it was.
            return 0f;
        }
    }
}
