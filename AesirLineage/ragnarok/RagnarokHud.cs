using HarmonyLib;
using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    public enum CountdownPosition {
        TopCenter,
        TopLeft,
        UnderMinimap,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    // The countdown to Ragnarok, where the player chose on the HUD. Shows the in-game time left and, as the end nears,
    // the spawn rate and the heralds; during Ragnarok itself, the world boss that must be killed to end it. With the
    // ring on, also when it next closes, or that the player is outside it.
    public class RagnarokHud : MonoBehaviour {
        private const float UpdateInterval = 0.5f;
        private static readonly Color Calm = new Color(0.95f, 0.85f, 0.6f);
        private static readonly Color Late = new Color(1f, 0.6f, 0.2f);
        private static readonly Color Dire = new Color(1f, 0.3f, 0.2f);

        private GameObject root;
        private Text text;
        private float nextUpdate;

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        private static class Hud_Awake_Patch {
            private static void Postfix(Hud __instance) {
                __instance.gameObject.AddComponent<RagnarokHud>();
            }
        }

        private void Start() {
            Hud hud = GetComponent<Hud>();
            if (hud == null || hud.m_rootObject == null) { return; }
            root = new GameObject("AL_RagnarokCountdown", typeof(RectTransform));
            root.transform.SetParent(hud.m_rootObject.transform, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 60f);
            text = root.AddComponent<Text>();
            text.font = GUIManager.Instance.AveriaSerifBold;
            text.fontSize = 18;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            root.SetActive(false);
            ApplyPosition();
            ValConfig.RagnarokCountdownPosition.SettingChanged += OnPositionChanged;
            ValConfig.RagnarokCountdownOffset.SettingChanged += OnPositionChanged;
        }

        private void OnDestroy() {
            ValConfig.RagnarokCountdownPosition.SettingChanged -= OnPositionChanged;
            ValConfig.RagnarokCountdownOffset.SettingChanged -= OnPositionChanged;
        }

        private void OnPositionChanged(object sender, EventArgs e) => ApplyPosition();

        // Each spot keeps clear of the vanilla HUD around it at the default GUI scale: the boss bar and raid banner
        // under the top center, the hotbar, the minimap and its status effects, the health and food bars, the stamina
        // bar and the key hints. The bottom spots grow upwards, so a second line never runs off the screen.
        private void ApplyPosition() {
            if (root == null) { return; }
            Vector2 anchor;
            Vector2 margin;
            TextAnchor alignment;
            switch (ValConfig.RagnarokCountdownPosition.Value) {
                case CountdownPosition.TopLeft:
                    anchor = new Vector2(0f, 1f); margin = new Vector2(40f, -125f); alignment = TextAnchor.UpperLeft; break;
                case CountdownPosition.UnderMinimap:
                    anchor = new Vector2(1f, 1f); margin = new Vector2(-40f, -265f); alignment = TextAnchor.UpperRight; break;
                case CountdownPosition.BottomLeft:
                    anchor = new Vector2(0f, 0f); margin = new Vector2(40f, 250f); alignment = TextAnchor.LowerLeft; break;
                case CountdownPosition.BottomCenter:
                    anchor = new Vector2(0.5f, 0f); margin = new Vector2(0f, 200f); alignment = TextAnchor.LowerCenter; break;
                case CountdownPosition.BottomRight:
                    anchor = new Vector2(1f, 0f); margin = new Vector2(-40f, 100f); alignment = TextAnchor.LowerRight; break;
                default:
                    anchor = new Vector2(0.5f, 1f); margin = new Vector2(0f, -10f); alignment = TextAnchor.UpperCenter; break;
            }
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = margin + ValConfig.RagnarokCountdownOffset.Value;
            text.alignment = alignment;
        }

        private void Update() {
            if (root == null || Time.time < nextUpdate) { return; }
            nextUpdate = Time.time + UpdateInterval;
            bool show = ValConfig.ShowRagnarokCountdown.Value && Ragnarok.Current.Active && Player.m_localPlayer != null;
            if (root.activeSelf != show) { root.SetActive(show); }
            if (show) { Refresh(); }
        }

        private void Refresh() {
            RagnarokSnapshot snapshot = Ragnarok.Current;
            Localization loc = Localization.instance;
            if (snapshot.Phase == RagnarokPhase.Ragnarok) {
                text.color = Dire;
                RagnarokWorldBosses.WorldBoss boss = RagnarokWorldBosses.Find(snapshot.WorldBossPrefab);
                text.text = loc.Localize("$al_hud_ragnarok_now") + "\n" + (boss != null
                    ? loc.Localize("$al_hud_ragnarok_boss", loc.Localize(boss.NameToken))
                    : loc.Localize(RagnarokRing.Active ? "$al_hud_ragnarok_waiting_ring" : "$al_hud_ragnarok_waiting")) + RingLine(loc);
                return;
            }

            float progress = Ragnarok.Progress;
            string line = loc.Localize("$al_hud_ragnarok_in", FormatTime(Ragnarok.SecondsLeft, Ragnarok.DayLengthSeconds));
            if (Ragnarok.InHeraldPhase) {
                text.color = Dire;
                line += "\n" + loc.Localize("$al_hud_ragnarok_heralds");
            } else {
                text.color = progress >= 0.75f ? Late : Calm;
                float rate = Ragnarok.SpawnRate(EnvMan.IsNight());
                if (rate > 1.05f) { line += "\n" + loc.Localize("$al_hud_ragnarok_spawns", rate.ToString("0.0")); }
            }
            if (RagnarokRing.LocalOutside) { text.color = Dire; }
            text.text = line + RingLine(loc);
        }

        // Outside the ring, that; otherwise when it next closes.
        private static string RingLine(Localization loc) {
            if (!RagnarokRing.Active) { return ""; }
            if (RagnarokRing.LocalOutside) { return "\n" + loc.Localize("$al_hud_ring_outside"); }
            double next = RagnarokRing.SecondsToNextStep;
            return next >= 0 ? "\n" + loc.Localize("$al_hud_ring_closes", FormatTime(next, Ragnarok.DayLengthSeconds)) : "";
        }

        // In-game time: "12d 6h", "6h 20m", "45m".
        internal static string FormatTime(double seconds, double dayLength) {
            double hourLength = dayLength / 24.0;
            int days = (int)(seconds / dayLength);
            int hours = (int)((seconds - days * dayLength) / hourLength);
            int minutes = (int)((seconds - days * dayLength - hours * hourLength) / (hourLength / 60.0));
            if (days > 0) { return $"{days}d {hours}h"; }
            if (hours > 0) { return $"{hours}h {minutes}m"; }
            return $"{minutes}m";
        }
    }
}
