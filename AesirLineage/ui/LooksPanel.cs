using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AesirLineage.modules;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.ui {
    /// <summary>
    /// The star map's side panel for choosing looks: one row per cosmetic slot, stepping through the
    /// deities whose look the player has for it, and None.
    /// </summary>
    internal sealed class LooksPanel : MonoBehaviour {
        internal const float Width = 380f;
        private const float RowHeight = 58f;
        private const float Top = 70f;

        private class Row {
            public CosmeticSlot Slot;
            public Text Value;
            public Button Previous;
            public Button Next;
        }

        private readonly List<Row> rows = new List<Row>();
        private Text hint;

        internal static LooksPanel Create(RectTransform parent) {
            GameObject go = new GameObject("Looks", typeof(RectTransform));
            go.layer = 5;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-24f, 0f);
            rt.sizeDelta = new Vector2(Width, Top + Cosmetics.Slots.Length * RowHeight + 90f);
            Image back = go.AddComponent<Image>();
            back.color = new Color(0.03f, 0.035f, 0.06f, 0.94f);
            back.raycastTarget = true;
            LooksPanel panel = go.AddComponent<LooksPanel>();
            panel.Build(rt);
            return panel;
        }

        private void Build(RectTransform rt) {
            Text title = NewText(rt, "Title", GUIManager.Instance.NorseBold ?? GUIManager.Instance.AveriaSerifBold, 30, GUIManager.Instance.ValheimBeige, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(Width - 20f, 44f));
            title.text = Localization.instance.Localize("$al_looks_title");

            for (int i = 0; i < Cosmetics.Slots.Length; i++) {
                CosmeticSlot slot = Cosmetics.Slots[i];
                float y = -Top - i * RowHeight - RowHeight * 0.5f;
                Row row = new Row() { Slot = slot };
                Text label = NewText(rt, $"Label_{slot}", GUIManager.Instance.AveriaSerifBold, 17, GUIManager.Instance.ValheimBeige, TextAnchor.MiddleLeft);
                Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(20f + 75f, y), new Vector2(150f, 30f));
                label.text = Localization.instance.Localize($"$al_slot_{slot}");
                row.Previous = ArrowButton(rt, "<", new Vector2(-142f, y));
                row.Next = ArrowButton(rt, ">", new Vector2(-28f, y));
                row.Value = NewText(rt, $"Value_{slot}", GUIManager.Instance.AveriaSerifBold, 17, Color.white, TextAnchor.MiddleCenter);
                Place(row.Value.rectTransform, new Vector2(1f, 1f), new Vector2(-85f, y), new Vector2(84f, 30f));
                row.Previous.onClick.AddListener(() => Step(row, -1));
                row.Next.onClick.AddListener(() => Step(row, 1));
                rows.Add(row);
            }

            hint = NewText(rt, "Hint", GUIManager.Instance.AveriaSerif, 14, new Color(0.6f, 0.62f, 0.68f), TextAnchor.UpperCenter);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(Width - 36f, 70f));
        }

        /// <summary>Shows what each slot wears now and which can be changed.</summary>
        internal void Refresh() {
            bool any = false;
            foreach (Row row in rows) {
                List<Deity> available = Cosmetics.Available(row.Slot);
                any |= available.Count > 0;
                Deity? shown = Cosmetics.Effective(row.Slot);
                row.Value.text = shown == null ? Localization.instance.Localize("$al_look_none") : Compendium.DeityName(shown.Value);
                row.Value.color = shown == null ? new Color(0.6f, 0.62f, 0.68f) : Color.Lerp(DeityVfx.Primary(shown.Value), Color.white, 0.35f);
                row.Previous.interactable = available.Count > 0;
                row.Next.interactable = available.Count > 0;
            }
            hint.text = Localization.instance.Localize(any ? "$al_looks_hint" : "$al_looks_none_yet");
        }

        // None, then each available deity in turn.
        private void Step(Row row, int direction) {
            List<string> options = new List<string>() { Cosmetics.None };
            foreach (Deity deity in Cosmetics.Available(row.Slot)) { options.Add(deity.ToString()); }
            if (options.Count < 2) { return; }
            Deity? shown = Cosmetics.Effective(row.Slot);
            int index = shown == null ? 0 : Mathf.Max(0, options.IndexOf(shown.Value.ToString()));
            int next = (index + direction + options.Count) % options.Count;
            PlayerData.ChooseCosmetic(row.Slot, options[next]);
            Refresh();
        }

        private static Button ArrowButton(Transform parent, string text, Vector2 position) {
            GameObject go = GUIManager.Instance.CreateButton(
                text: text,
                parent: parent,
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                position: position,
                width: 40f,
                height: 40f);
            return go.GetComponent<Button>();
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size) {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static Text NewText(Transform parent, string name, Font font, int size, Color color, TextAnchor anchor) {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }
    }
}
