using BepInEx.Configuration;
using EternalHall;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using AesirLineage.common;

namespace AesirLineage.ui {
    /// <summary>
    /// The setup tutorial: a few pages over the choices a world most often makes, built from the shared config
    /// kit in common/Config/UI. It opens once by itself on the main menu, with a welcome page in front, and any
    /// time after from the shared Mod Config button (main menu and pause menu, for hosts and server admins).
    /// </summary>
    /// <remarks>
    /// Every page edits a staged copy; nothing reaches the config until Save. On the main menu that is this
    /// game's own config, which its worlds and a server it hosts use. In a world, a host's or remote admin's save
    /// is then sent on through Jotunn, as closing the ConfigurationManager window would.
    /// </remarks>
    internal static class QuickSetup {
        private const string LauncherEntry = "Aesir Lineage";
        // This mod's line in the shared first-run record. Never change it once shipped.
        private const string FirstRunKey = "AesirLineage";
        // Raise it to show a reworked tutorial to people who have seen this one.
        private const int FirstRunRevision = 1;

        private const float PanelW = 820f;
        private const float PanelH = 700f;
        private const float ColX = 44f;
        private const float ColW = PanelW - 2f * ColX;
        private const float PageTop = 72f;
        private const float Indent = 28f;
        private const float LabelW = 340f;
        private const float SliderW = 250f;
        private const float ValueW = 74f;
        private const float HintH = 24f;
        private const float FooterY = PanelH - 66f;

        private sealed class Page {
            internal string Title;
            internal GameObject Root;
            internal readonly List<(GameObject row, float indent)> Rows = new List<(GameObject, float)>();
        }

        private static GameObject panel;
        private static Text titleText;
        private static Text pageText;
        private static Text statusText;
        private static GameObject backButton;
        private static GameObject nextButton;
        private static GameObject finishButton;
        private static readonly List<Page> pages = new List<Page>();
        private static int current;
        private static bool tutorial;
        private static bool discardArmed;
        // What the pages have changed and not yet saved, by entry.
        private static readonly Dictionary<ConfigEntryBase, object> staged = new Dictionary<ConfigEntryBase, object>();

        internal static bool IsOpen => panel != null;

        /// <summary>From Awake: the Mod Config entry, and the tutorial's turn on the main menu.</summary>
        internal static void Init() {
            ConfigUILauncher.Init();
            ApplyRegistration();
            ConfigUIFirstRun.QueueFirstRunPopup(FirstRunKey, FirstRunRevision, ValConfig.FirstRunPopup,
                ConfigUIStartupPopups.OrderWelcome, OpenTutorial, () => IsOpen);
        }

        /// <summary>Also the handler for ShowQuickConfigButton changing.</summary>
        internal static void ApplyRegistration() {
            if (ValConfig.ShowQuickConfigButton == null) { return; }
            if (ValConfig.ShowQuickConfigButton.Value) { ConfigUILauncher.Register(LauncherEntry, Open); }
            else { ConfigUILauncher.Unregister(LauncherEntry); }
        }

        // Declines while the panel is already up: someone opened it through Mod Config while this waited its turn.
        private static bool OpenTutorial() {
            if (IsOpen) { return false; }
            Open(asTutorial: true);
            return IsOpen;
        }

        /// <summary>The launcher's entry point.</summary>
        internal static void Open() => Open(asTutorial: false);

        internal static void Open(bool asTutorial) {
            if (GUIManager.IsHeadless() || GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return; }
            Destroy();
            tutorial = asTutorial;
            try {
                Build();
            } catch (Exception e) {
                Logger.LogWarning($"The setup panel could not be built: {e}");
                Destroy();
                return;
            }
            Show(0);
        }

        /// <summary>The page shown now, from 0. For tests and screenshots.</summary>
        internal static int CurrentPage => current;
        internal static int PageCount => pages.Count;

        internal static void ShowPage(int index) {
            if (IsOpen) { Show(index); }
        }

        // Every way out of the tutorial records it as seen, including the X with nothing saved.
        internal static void Close() {
            if (tutorial) { ConfigUIFirstRun.MarkSeen(FirstRunKey, FirstRunRevision); }
            Destroy();
        }

        private static void Destroy() {
            if (panel != null) { UnityEngine.Object.Destroy(panel); }
            panel = null;
            pages.Clear();
            staged.Clear();
            discardArmed = false;
        }

        // ---------------------------------------------------------------------------------------
        // Building
        // ---------------------------------------------------------------------------------------

        private static void Build() {
            panel = ConfigUI.CreatePanel("", PanelW, PanelH, out Transform body);
            titleText = panel.GetComponentInChildren<Text>();
            panel.AddComponent<MainMenuGuard>();
            ConfigUI.AddCloseX(body, PanelW, OnCloseX);

            if (tutorial) { BuildWelcome(NewPage(body, "$al_setup_welcome_title")); }
            BuildDeeds(NewPage(body, "$al_setup_deeds_title"));
            BuildLuck(NewPage(body, "$al_setup_luck_title"));
            BuildLooks(NewPage(body, "$al_setup_looks_title"));
            BuildAscension(NewPage(body, "$al_setup_ascension_title"));
            foreach (Page page in pages) { Layout(page); }

            statusText = ConfigUI.AddText(body, ColX, FooterY - 34f, ColW, 26f, "", 15, TextAnchor.MiddleCenter, GUIManager.Instance.ValheimOrange);
            backButton = ConfigUI.AddButton(body, ColX, FooterY, 150f, "$al_setup_back", () => Show(current - 1));
            pageText = ConfigUI.AddText(body, ColX + 160f, FooterY, ColW - 2f * 160f - 170f, 40f, "", 16, TextAnchor.MiddleCenter);
            ConfigUI.AddButton(body, PanelW - ColX - 150f - 10f - 160f, FooterY, 160f, "$al_setup_save", Save);
            nextButton = ConfigUI.AddButton(body, PanelW - ColX - 150f, FooterY, 150f, "$al_setup_next", () => Show(current + 1));
            finishButton = ConfigUI.AddButton(body, PanelW - ColX - 150f, FooterY, 150f, "$al_setup_finish", Finish);
        }

        private static Page NewPage(Transform body, string title) {
            Page page = new Page { Title = title, Root = ConfigUI.NewRect("Page", body, 0f, 0f, PanelW, PanelH) };
            pages.Add(page);
            return page;
        }

        private static void Layout(Page page) {
            float y = PageTop;
            foreach ((GameObject row, float indent) in page.Rows) {
                if (row == null || !row.activeSelf) { continue; }
                ConfigUI.PositionRow(row, ColX + indent, y);
                y += ((RectTransform)row.transform).sizeDelta.y + ConfigUI.RowGap;
            }
        }

        private static GameObject Paragraph(Page page, string text, float height, float indent = 0f) {
            GameObject row = ConfigUI.AddTextRow(page.Root.transform, ColW - indent, height, text, 16, GUIManager.Instance.ValheimBeige);
            page.Rows.Add((row, indent));
            return row;
        }

        private static GameObject Hint(Page page, string text, float indent = Indent, float height = HintH) {
            GameObject row = ConfigUI.AddTextRow(page.Root.transform, ColW - indent, height, text, 14, new Color(0.72f, 0.70f, 0.64f));
            page.Rows.Add((row, indent));
            return row;
        }

        private static GameObject Divider(Page page) {
            GameObject row = ConfigUI.AddDividerRow(page.Root.transform, ColW, 14f);
            page.Rows.Add((row, 0f));
            return row;
        }

        private static GameObject ToggleRow(Page page, string label, bool value, Action<bool> onChange, float indent = 0f) {
            GameObject row = ConfigUI.AddToggleRow(page.Root.transform, ColW - indent, LabelW - indent, label, value, onChange);
            page.Rows.Add((row, indent));
            return row;
        }

        private static GameObject ToggleRow(Page page, string label, ConfigEntry<bool> entry, float indent = 0f) {
            return ToggleRow(page, label, Get(entry), on => Set(entry, on), indent);
        }

        private static GameObject SliderRow(Page page, string label, ConfigEntry<int> entry, int min, int max, float indent = Indent) {
            int value = Get(entry);
            GameObject row = ConfigUI.AddSliderRow(page.Root.transform, ColW - indent, LabelW - indent, SliderW, ValueW, label,
                min, Math.Max(max, value), Math.Max(min, value), true, v => Set(entry, Mathf.RoundToInt(v)));
            page.Rows.Add((row, indent));
            return row;
        }

        private static GameObject SliderRow(Page page, string label, ConfigEntry<float> entry, float min, float max, float indent = Indent) {
            float value = Get(entry);
            GameObject row = ConfigUI.AddSliderRow(page.Root.transform, ColW - indent, LabelW - indent, SliderW, ValueW, label,
                min, Mathf.Max(max, value), Mathf.Max(min, value), false, v => Set(entry, v));
            page.Rows.Add((row, indent));
            return row;
        }

        /// <summary>
        /// A way to earn star points that a value of 0 turns off: a toggle, and under it the rows that only matter
        /// while it is on. Turning it off stages 0; turning it back on brings back the value it had.
        /// </summary>
        private static void Source(Page page, string label, string hint, ConfigEntry<int> points, string pointsLabel, int max,
            Action<List<GameObject>> more = null) {
            int start = Get(points);
            int restore = start > 0 ? start : Math.Max(1, (int)points.DefaultValue);
            List<GameObject> under = new List<GameObject>();
            ToggleRow(page, label, start > 0, on => {
                if (on) { Set(points, restore); }
                else {
                    if (Get(points) > 0) { restore = Get(points); }
                    Set(points, 0);
                }
                foreach (GameObject row in under) { row.SetActive(on); }
                Layout(page);
            });
            if (hint != null) { Hint(page, hint); }
            GameObject amount = ConfigUI.AddSliderRow(page.Root.transform, ColW - Indent, LabelW - Indent, SliderW, ValueW, pointsLabel,
                1, Math.Max(max, restore), restore, true, v => Set(points, Mathf.RoundToInt(v)));
            page.Rows.Add((amount, Indent));
            under.Add(amount);
            more?.Invoke(under);
            foreach (GameObject row in under) { row.SetActive(start > 0); }
        }

        // ---------------------------------------------------------------------------------------
        // Pages
        // ---------------------------------------------------------------------------------------

        private static void BuildWelcome(Page page) {
            Paragraph(page, "$al_setup_welcome_intro", 84f);
            Paragraph(page, "$al_setup_welcome_setup", 70f);
            Paragraph(page, "$al_setup_welcome_later", 50f);
            Paragraph(page, "$al_setup_welcome_server", 30f);
        }

        private static void BuildDeeds(Page page) {
            Paragraph(page, "$al_setup_deeds_intro", 48f);
            Source(page, "$al_setup_souls", "$al_setup_souls_hint", ValConfig.PointsPerSoul, "$al_setup_points", 10);
            Source(page, "$al_setup_minibosses", "$al_setup_minibosses_hint", ValConfig.MinibossPoints, "$al_setup_points", 10);
            Source(page, "$al_setup_heralds", RagnarokAPI.IsAvailable ? "$al_setup_heralds_hint" : "$al_setup_heralds_hint_missing",
                ValConfig.RagnarokHeraldPoints, "$al_setup_points", 10);
            Source(page, "$al_setup_quests", "$al_setup_quests_hint", ValConfig.QuestPoints, "$al_setup_points", 10, under => {
                under.Add(SliderRow(page, "$al_setup_quest_days", ValConfig.QuestCooldownDays, 0, 30));
            });
        }

        private static void BuildLuck(Page page) {
            Paragraph(page, "$al_setup_luck_intro", 72f);
            Source(page, "$al_setup_lucky", "$al_setup_lucky_hint", ValConfig.LuckyPointsPerDeity, "$al_setup_lucky_cap", 20, under => {
                under.Add(SliderRow(page, "$al_setup_kill_chance", ValConfig.StarredKillChance, 0f, 25f));
                under.Add(SliderRow(page, "$al_setup_kill_stars", ValConfig.StarredKillMinStars, 1, 5));
                under.Add(SliderRow(page, "$al_setup_craft_chance", ValConfig.CraftChance, 0f, 10f));
                under.Add(SliderRow(page, "$al_setup_harvest_chance", ValConfig.HarvestChance, 0f, 5f));
            });
        }

        private static void BuildLooks(Page page) {
            Paragraph(page, "$al_setup_looks_intro", 72f);
            ToggleRow(page, "$al_setup_looks_enable", ValConfig.EnableLooks);
            Hint(page, "$al_setup_looks_hint", Indent, 48f);
        }

        private static void BuildAscension(Page page) {
            Paragraph(page, "$al_setup_ascension_intro", 72f);
            SliderRow(page, "$al_setup_ascension_max", ValConfig.MaxAscension, 1, 10, 0f);
            SliderRow(page, "$al_setup_ascension_skills", ValConfig.AscensionSkillLoss, 0, 100, 0f);
            ToggleRow(page, "$al_setup_ascension_recipes", ValConfig.AscensionForgetsRecipes);
            ToggleRow(page, "$al_setup_ascension_start", ValConfig.AscensionReturnsToStart);
            ToggleRow(page, "$al_setup_ascension_keys", ValConfig.AscensionRemovesBossKeys);
            Hint(page, "$al_setup_ascension_keys_hint");
            Divider(page);

            bool on = Get(ValConfig.AscensionAdvancesRagnarok);
            List<GameObject> under = new List<GameObject>();
            ToggleRow(page, "$al_setup_ragnarok", on, value => {
                Set(ValConfig.AscensionAdvancesRagnarok, value);
                foreach (GameObject row in under) { row.SetActive(value); }
                Layout(page);
            });
            Hint(page, RagnarokAPI.IsAvailable ? "$al_setup_ragnarok_hint" : "$al_setup_ragnarok_hint_missing", Indent, 48f);
            under.Add(SliderRow(page, "$al_setup_ragnarok_days", ValConfig.AscensionRagnarokDays, 0.5f, 10f));
            foreach (GameObject row in under) { row.SetActive(on); }
        }

        // ---------------------------------------------------------------------------------------
        // Navigation and saving
        // ---------------------------------------------------------------------------------------

        private static void Show(int index) {
            if (pages.Count == 0) { return; }
            current = Mathf.Clamp(index, 0, pages.Count - 1);
            for (int i = 0; i < pages.Count; i++) { pages[i].Root.SetActive(i == current); }
            titleText.text = ConfigUI.L(pages[current].Title);
            pageText.text = $"{current + 1} / {pages.Count}";
            bool last = current == pages.Count - 1;
            backButton.SetActive(current > 0);
            nextButton.SetActive(!last);
            finishButton.SetActive(last);
            discardArmed = false;
            ShowStatus();
        }

        private static void ShowStatus(string text = null) {
            if (statusText == null) { return; }
            statusText.text = text != null ? ConfigUI.L(text) : (Unsaved() ? ConfigUI.L("$al_setup_unsaved") : "");
        }

        private static bool Unsaved() => staged.Any(change => !Equals(change.Key.BoxedValue, change.Value));

        private static void OnCloseX() {
            if (Unsaved() && !discardArmed) {
                discardArmed = true;
                ShowStatus("$al_setup_discard");
                return;
            }
            Close();
        }

        private static void Finish() {
            Save();
            Close();
        }

        /// <summary>Writes every staged change to the config, then sends it on to the server or the players.</summary>
        internal static void Save() {
            int changed = 0;
            foreach (KeyValuePair<ConfigEntryBase, object> change in staged) {
                if (Equals(change.Key.BoxedValue, change.Value)) { continue; }
                change.Key.BoxedValue = change.Value;
                changed++;
            }
            staged.Clear();
            discardArmed = false;
            if (changed > 0) {
                Logger.LogInfo($"Setup saved {changed} setting(s).");
                SendOn();
            }
            ShowStatus(changed > 0 ? "$al_setup_saved" : "$al_setup_nothing");
        }

        private static T Get<T>(ConfigEntry<T> entry) => staged.TryGetValue(entry, out object value) ? (T)value : entry.Value;

        private static void Set<T>(ConfigEntry<T> entry, T value) {
            staged[entry] = value;
            discardArmed = false;
            ShowStatus();
        }

        // On the main menu, hides the menu while the panel is up: it stays live underneath, and Enter in one of the
        // panel's value boxes would otherwise press its selected button and start the game.
        private sealed class MainMenuGuard : MonoBehaviour {
            private GameObject hidden;

            private void Awake() {
                FejdStartup startup = FejdStartup.instance;
                if (startup != null && startup.m_mainMenu != null && startup.m_mainMenu.activeSelf) {
                    hidden = startup.m_mainMenu;
                    hidden.SetActive(false);
                }
            }

            private void OnDestroy() {
                if (hidden != null) { hidden.SetActive(true); }
                hidden = null;
            }
        }

        private static MethodInfo syncChanged;
        private static bool syncResolved;

        // Jotunn sends changed admin settings on when the ConfigurationManager window closes; this is that same
        // step, reached by reflection: a remote admin's change goes to the server, a host's to its players.
        private static void SendOn() {
            if (ZNet.instance == null) { return; }
            if (!syncResolved) {
                syncResolved = true;
                syncChanged = AccessTools.Method(typeof(SynchronizationManager), "SynchronizeChangedConfig");
                if (syncChanged == null) { Logger.LogWarning("Jotunn's SynchronizeChangedConfig was not found, so saved settings reach other players only when they next join."); }
            }
            try {
                syncChanged?.Invoke(SynchronizationManager.Instance, null);
            } catch (Exception e) {
                Logger.LogWarning($"Saved settings could not be sent on: {e.Message}");
            }
        }
    }
}
