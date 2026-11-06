using BepInEx.Bootstrap;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AesirLineage.ui {
    /// <summary>
    /// Ways into the star map besides the hotkey and the shrines: a button in the row of round buttons at the top
    /// right of the inventory (compendium, skills, trophies, achievements, PvP), where Clans adds its own.
    ///
    /// The button is a copy of the trophies button, at the end of that row, which is spread out again to fit it on
    /// the board. It is added after Clans, whose buttons (one in the PvP toggle's place, its activity tracker's left of
    /// the compendium) are already in the row then and are spread out with the rest.
    ///
    /// Auga replaces that row with its own row of diamond buttons under the character's name, so under Auga the
    /// button is registered there as a link through Auga's API (vendor/Auga/API.cs). An Auga older than that API has
    /// no place for it; the hotkey still opens the star map.
    /// </summary>
    internal static class StarMapEntryPoints {
        internal const string AugaGuid = "randyknapp.mods.auga";
        private const string ButtonName = "AesirLineage_StarMapButton";
        private const string Tooltip = "$al_starmap_button";
        // Clear space kept between the row's end buttons and the board's edges, which carry iron fittings.
        private const float BoardEdge = 12f;
        private static bool warnedNoPlace;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        // Clans' Harmony ID: its buttons must be in the row before the row is spread out.
        [HarmonyAfter("MidnightsFX.Clans")]
        private static class AddInventoryButton {
            private static void Postfix(InventoryGui __instance) {
                try {
                    bool added = Chainloader.PluginInfos.ContainsKey(AugaGuid)
                        ? Auga.API.IsLoaded() && Auga.API.PlayerPanel_AddLink(ButtonName, StarMapArt.MenuIcon, Tooltip, Open)
                        : AddToRow(__instance);
                    if (!added && !warnedNoPlace) {
                        warnedNoPlace = true;
                        Logger.LogInfo("The inventory has no place for the star map button; open the star map with the 'StarMapKey' setting.");
                    }
                } catch (System.Exception e) {
                    Logger.LogWarning($"Could not add the star map button to the inventory: {e}");
                }
            }
        }

        private static void Open() {
            InventoryGui.instance?.Hide();
            StarMapWindow.Show();
        }

        private static bool AddToRow(InventoryGui gui) {
            Transform info = gui.m_info;
            RectTransform texts = info != null ? info.Find("Texts") as RectTransform : null;
            Transform trophies = info != null ? info.Find("Trophies") : null;
            if (texts == null || trophies == null) { return false; }
            if (info.Find(ButtonName) != null) { return true; }

            GameObject go = Object.Instantiate(trophies.gameObject, info, false);
            go.name = ButtonName;
            go.SetActive(true);
            // A mouse-only button: the trophies button's gamepad binding and its hint stay with the original.
            UIGamePad pad = go.GetComponent<UIGamePad>();
            if (pad != null) {
                if (pad.m_hint != null) { Object.Destroy(pad.m_hint); }
                Object.Destroy(pad);
            }
            Button button = go.GetComponent<Button>();
            // The whole event, persistent listeners included: the copy would otherwise open the trophies too.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(Open);
            Transform icon = go.transform.Find("Image") ?? go.transform.Find("Icon");
            if (icon != null && icon.TryGetComponent(out Image image)) { image.sprite = StarMapArt.MenuIcon; }
            UITooltip tooltip = go.GetComponent<UITooltip>();
            if (tooltip != null) {
                tooltip.m_text = Tooltip;
                tooltip.m_topic = "";
            }

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = texts.anchorMin;
            rect.anchorMax = texts.anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            LayOutRow((RectTransform)info, texts, rect);
            return true;
        }

        /// <summary>
        /// Spreads the row evenly across the board with the star map button last, at the game's own spacing while
        /// that fits and closer together when it does not, so no button hangs off the board's edge. Buttons sharing
        /// a slot (Clans' button where the PvP toggle was) stay together, and a hidden button keeps its slot.
        /// </summary>
        private static void LayOutRow(RectTransform info, RectTransform texts, RectTransform self) {
            float rowY = Center(texts).y;
            SortedDictionary<int, List<RectTransform>> slots = new SortedDictionary<int, List<RectTransform>>();
            foreach (Transform child in info) {
                if (child == self || !(child is RectTransform other) || other.GetComponent<Selectable>() == null) { continue; }
                if (other.anchorMin != texts.anchorMin || other.anchorMax != texts.anchorMax) { continue; }
                Vector2 center = Center(other);
                if (Mathf.Abs(center.y - rowY) > texts.rect.height / 2f) { continue; }
                int key = Mathf.RoundToInt(center.x);
                if (!slots.TryGetValue(key, out List<RectTransform> slot)) { slots[key] = slot = new List<RectTransform>(); }
                slot.Add(other);
            }

            int count = slots.Count + 1;
            float room = info.rect.width - self.rect.width - 2f * BoardEdge;
            float step = Mathf.Min(Step(info, texts), room / (count - 1));
            float middle = (0.5f - texts.anchorMin.x) * info.rect.width;
            int index = 0;
            foreach (List<RectTransform> slot in slots.Values) {
                float x = middle + (index++ - (count - 1) / 2f) * step;
                foreach (RectTransform button in slot) { button.anchoredPosition += new Vector2(x - Center(button).x, 0f); }
            }
            self.anchoredPosition = new Vector2(middle + (count - 1) / 2f * step, rowY);
        }

        // The distance between neighbours in the row: from the compendium to the skills button, right of it.
        private static float Step(Transform info, RectTransform texts) {
            RectTransform skills = info.Find("Skills") as RectTransform;
            return skills != null ? Center(skills).x - Center(texts).x : texts.rect.width * 1.5f;
        }

        private static Vector2 Center(RectTransform rect) {
            return rect.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, rect.rect.size);
        }
    }
}
