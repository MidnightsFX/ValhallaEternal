using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AesirLineage.starmap;

namespace AesirLineage.ui {
    internal enum NodeState {
        /// <summary>Not linked to anything the player has.</summary>
        Locked,
        /// <summary>Linked, but the player can't take it yet (points or Ascension).</summary>
        Reachable,
        /// <summary>Can be taken now.</summary>
        Available,
        Taken,
    }

    /// <summary>One star on the map: a glow, an optional ring and the star itself, over an invisible hit area.</summary>
    internal sealed class StarNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        internal StarNode Node;
        internal StarMapWindow Window;
        internal Image Glow;
        internal Image Ring;
        internal Image Core;
        internal NodeState State;

        private Color glowColor;
        private Color coreColor;
        private bool hovered;
        private float scale = 1f;
        private float celebrateUntil;
        // Each star pulses at its own phase so the map shimmers rather than blinking in unison.
        private float phase;

        private const float CelebrateSeconds = 0.6f;

        private void Awake() {
            phase = Random.value * Mathf.PI * 2f;
        }

        internal void Apply(NodeState state, Color primary, Color secondary, Color glow) {
            State = state;
            Color locked = new Color(0.55f, 0.58f, 0.66f);
            switch (state) {
                case NodeState.Taken:
                    coreColor = Color.Lerp(glow, Color.white, 0.35f);
                    glowColor = StarMapArt.WithAlpha(primary, 0.85f);
                    break;
                case NodeState.Available:
                    coreColor = Color.white;
                    glowColor = StarMapArt.WithAlpha(secondary, 0.5f);
                    break;
                case NodeState.Reachable:
                    coreColor = StarMapArt.WithAlpha(Color.Lerp(locked, secondary, 0.35f), 0.85f);
                    glowColor = StarMapArt.WithAlpha(secondary, 0.18f);
                    break;
                default:
                    coreColor = StarMapArt.WithAlpha(locked, 0.55f);
                    glowColor = StarMapArt.WithAlpha(locked, 0.08f);
                    break;
            }
            if (Ring != null) {
                Color ringColor = Node.Tier == NodeTier.Oath ? new Color(0.86f, 0.24f, 0.22f)
                    : Node.Tier == NodeTier.Apex ? glow
                    : primary;
                Ring.color = StarMapArt.WithAlpha(ringColor, state == NodeState.Locked ? 0.35f : 0.95f);
            }
            Core.color = coreColor;
            Glow.color = glowColor;
        }

        internal void Celebrate() {
            celebrateUntil = Time.unscaledTime + CelebrateSeconds;
        }

        internal void Animate(float time) {
            float target = hovered ? 1.2f : 1f;
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));

            float glowScale = 1f;
            float glowAlpha = glowColor.a;
            if (State == NodeState.Available) {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3f + phase);
                glowAlpha = Mathf.Lerp(0.3f, 0.75f, pulse);
                glowScale = 1f + 0.12f * pulse;
            } else if (State == NodeState.Taken) {
                glowAlpha *= 0.85f + 0.15f * Mathf.Sin(time * 1.6f + phase);
            }
            if (hovered) { glowAlpha = Mathf.Min(1f, glowAlpha + 0.25f); }

            float celebrate = Mathf.Clamp01((celebrateUntil - time) / CelebrateSeconds);
            if (celebrate > 0f) {
                glowScale += 1.6f * celebrate;
                glowAlpha = Mathf.Max(glowAlpha, celebrate);
            }

            transform.localScale = Vector3.one * scale;
            Glow.transform.localScale = Vector3.one * glowScale;
            Glow.color = StarMapArt.WithAlpha(glowColor, glowAlpha);
        }

        public void OnPointerEnter(PointerEventData eventData) {
            hovered = true;
            Window.OnNodeHover(this);
        }

        public void OnPointerExit(PointerEventData eventData) {
            hovered = false;
            Window.OnNodeHoverEnd(this);
        }

        // Only taking: spent points are locked until the deity's shrine resets the whole map.
        public void OnPointerClick(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Left) {
                Window.TryAllocate(this);
            }
        }
    }

    /// <summary>The map area: dragging pans, the wheel zooms towards the cursor.</summary>
    internal sealed class StarMapViewport : MonoBehaviour, IDragHandler, IScrollHandler {
        internal StarMapWindow Window;

        public void OnDrag(PointerEventData eventData) {
            Window.Pan(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData) {
            Window.ZoomAt(eventData.position, eventData.scrollDelta.y);
        }
    }
}
