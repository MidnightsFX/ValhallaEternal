using System.Collections.Generic;
using UnityEngine;

namespace AesirLineage.ui {
    /// <summary>
    /// Elder Futhark runes drawn in code as glowing strokes, one texture per rune, for the souls and
    /// later the star maps. Strokes are in a 0..1 box, y up.
    /// </summary>
    internal static class RuneArt {
        private static readonly Dictionary<string, Vector2[][]> Strokes = new Dictionary<string, Vector2[][]>() {
            // ᛁ
            { "isa", new[] { Line(0.5f, 0.06f, 0.5f, 0.94f) } },
            // ᚷ
            { "gebo", new[] { Line(0.18f, 0.08f, 0.82f, 0.92f), Line(0.82f, 0.08f, 0.18f, 0.92f) } },
            // ᚨ
            { "ansuz", new[] { Line(0.36f, 0.06f, 0.36f, 0.94f), Line(0.36f, 0.94f, 0.74f, 0.7f), Line(0.36f, 0.7f, 0.74f, 0.46f) } },
            // ᛊ
            { "sowilo", new[] { Path(0.66f, 0.94f, 0.3f, 0.6f, 0.7f, 0.4f, 0.34f, 0.06f) } },
            // ᛉ
            { "algiz", new[] { Line(0.5f, 0.06f, 0.5f, 0.94f), Line(0.5f, 0.52f, 0.2f, 0.88f), Line(0.5f, 0.52f, 0.8f, 0.88f) } },
            // ᚠ
            { "fehu", new[] { Line(0.36f, 0.06f, 0.36f, 0.94f), Line(0.36f, 0.62f, 0.74f, 0.88f), Line(0.36f, 0.4f, 0.74f, 0.66f) } },
            // ᚺ
            { "hagalaz", new[] { Line(0.3f, 0.06f, 0.3f, 0.94f), Line(0.7f, 0.06f, 0.7f, 0.94f), Line(0.3f, 0.64f, 0.7f, 0.36f) } },
            // ᛇ
            { "eihwaz", new[] { Line(0.5f, 0.06f, 0.5f, 0.94f), Line(0.5f, 0.94f, 0.76f, 0.72f), Line(0.5f, 0.06f, 0.24f, 0.28f) } },
        };

        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>The rune's texture, or null for a name with no strokes defined.</summary>
        internal static Texture2D Texture(string rune) {
            if (string.IsNullOrEmpty(rune)) { return null; }
            string key = rune.ToLowerInvariant();
            if (cache.TryGetValue(key, out Texture2D texture)) { return texture; }
            if (!Strokes.TryGetValue(key, out Vector2[][] strokes)) { return null; }
            texture = Draw(strokes, 128);
            cache[key] = texture;
            return texture;
        }

        private static Vector2[] Line(float x1, float y1, float x2, float y2) => new[] { new Vector2(x1, y1), new Vector2(x2, y2) };

        private static Vector2[] Path(params float[] xy) {
            Vector2[] points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) { points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]); }
            return points;
        }

        // White strokes with a soft glow around them; the material tints them.
        private static Texture2D Draw(Vector2[][] strokes, int size) {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            const float width = 0.045f;
            const float glow = 0.12f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float distance = float.MaxValue;
                    foreach (Vector2[] stroke in strokes) {
                        for (int i = 0; i + 1 < stroke.Length; i++) {
                            distance = Mathf.Min(distance, DistanceToSegment(p, stroke[i], stroke[i + 1]));
                        }
                    }
                    float core = Mathf.Clamp01((width - distance) / (1.5f / size) + 0.5f);
                    float halo = Mathf.Exp(-(distance / glow) * (distance / glow)) * 0.55f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Max(core, halo));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b) {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
