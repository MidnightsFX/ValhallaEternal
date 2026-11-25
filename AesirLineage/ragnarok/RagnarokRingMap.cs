using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace AesirLineage.ragnarok {
    // The ring on the map and the minimap: red where it has closed, yellow where it is still to close, and a brighter
    // line where it closes next. Its last circle and everything past the main world's ocean stay clear.
    //
    // A texture over the whole area the map texture covers, on an image laid over each map image that copies its uvRect
    // every frame, so it pans and zooms with the map. It is redrawn only when the ring changes.
    public class RagnarokRingMap : MonoBehaviour {
        private const int Size = 1024;
        private const float EdgePixels = 1.5f;
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Closed = new Color32(220, 40, 25, 64);
        private static readonly Color32 Closing = new Color32(255, 210, 50, 30);
        private static readonly Color32 NextEdge = new Color32(255, 150, 30, 190);

        private Minimap minimap;
        private RawImage large;
        private RawImage small;
        private Texture2D texture;
        private int drawnStage = -1;
        private Vector2 drawnCenter;
        private float drawnRadius;

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
        private static class Minimap_Start_Patch {
            private static void Postfix(Minimap __instance) {
                __instance.gameObject.AddComponent<RagnarokRingMap>();
            }
        }

        private void Start() {
            minimap = GetComponent<Minimap>();
            large = Overlay(minimap.m_mapImageLarge);
            small = Overlay(minimap.m_mapImageSmall);
        }

        private void OnDestroy() {
            if (texture != null) { Destroy(texture); }
        }

        // First among the map image's children, so anything else drawn over the map stays on top of it.
        private static RawImage Overlay(RawImage map) {
            if (map == null) { return null; }
            GameObject overlay = new GameObject("AL_RagnarokRing", typeof(RectTransform));
            overlay.transform.SetParent(map.transform, false);
            overlay.transform.SetAsFirstSibling();
            RectTransform rect = overlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage image = overlay.AddComponent<RawImage>();
            image.raycastTarget = false;
            overlay.SetActive(false);
            return image;
        }

        private void LateUpdate() {
            if (minimap == null) { return; }
            bool show = RagnarokRing.Active;
            if (show) { Redraw(); }
            Follow(large, minimap.m_mapImageLarge, show);
            Follow(small, minimap.m_mapImageSmall, show);
        }

        private static void Follow(RawImage overlay, RawImage map, bool show) {
            if (overlay == null || map == null) { return; }
            if (overlay.gameObject.activeSelf != show) { overlay.gameObject.SetActive(show); }
            if (show) { overlay.uvRect = map.uvRect; }
        }

        private void Redraw() {
            int stage = RagnarokRing.Stage;
            RagnarokRing.Circle final = RagnarokRing.Final;
            if (texture != null && stage == drawnStage && final.Center == drawnCenter && final.Radius == drawnRadius) { return; }
            drawnStage = stage;
            drawnCenter = final.Center;
            drawnRadius = final.Radius;
            if (texture == null) {
                texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "AL_RagnarokRing", wrapMode = TextureWrapMode.Clamp };
                if (large != null) { large.texture = texture; }
                if (small != null) { small.texture = texture; }
            }

            // The map texture's pixels are m_pixelSize metres, centred on the world's origin (Minimap.WorldToMapPoint).
            float span = minimap.m_textureSize * minimap.m_pixelSize;
            float edge = EdgePixels * span / Size;
            float worldEdge = RagnarokRing.EdgeRadius * RagnarokRing.EdgeRadius;
            RagnarokRing.Circle current = RagnarokRing.At(stage);
            bool hasNext = stage < RagnarokRing.Steps;
            RagnarokRing.Circle next = RagnarokRing.At(stage + 1);

            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++) {
                float z = ((y + 0.5f) / Size - 0.5f) * span;
                for (int x = 0; x < Size; x++) {
                    float wx = ((x + 0.5f) / Size - 0.5f) * span;
                    if (wx * wx + z * z > worldEdge) {
                        pixels[y * Size + x] = Clear;
                        continue;
                    }
                    Vector2 point = new Vector2(wx, z);
                    Color32 color = Vector2.Distance(point, current.Center) > current.Radius ? Closed
                        : Vector2.Distance(point, final.Center) > final.Radius ? Closing
                        : Clear;
                    if (hasNext) {
                        float line = 1f - Mathf.Abs(Vector2.Distance(point, next.Center) - next.Radius) / edge;
                        if (line > 0f) { color = Color32.Lerp(color, NextEdge, line); }
                    }
                    pixels[y * Size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
        }
    }
}
