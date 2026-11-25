using System.Collections.Generic;
using UnityEngine;

namespace AesirLineage.ui {
    /// <summary>
    /// The star map's sprites and textures, drawn in code once on first use. The shaders that make glows
    /// additive and links flow come from the bundle (see <see cref="visuals.DeityVfx"/>).
    /// </summary>
    internal static class StarMapArt {
        private static Sprite softGlow;
        private static Sprite star;
        private static Sprite ring;
        private static Sprite disc;
        private static Texture2D linkFalloff;
        private static Texture2D starfield;
        private static Texture2D nebula;
        private static Texture2D vignette;

        /// <summary>A round glow fading to nothing at the edge.</summary>
        internal static Sprite SoftGlow => softGlow ??= ToSprite(Radial(128, r => Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f)));

        /// <summary>A bright point with four long spikes and four short ones.</summary>
        internal static Sprite Star => star ??= ToSprite(StarTexture(128));

        /// <summary>A thin ring just inside the edge.</summary>
        internal static Sprite Ring => ring ??= ToSprite(Radial(128, r => Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.07f)));

        /// <summary>A filled circle with a soft edge, for tabs and badges.</summary>
        internal static Sprite Disc => disc ??= ToSprite(Radial(128, r => Mathf.Clamp01((1f - r) / 0.06f)));

        private static Sprite menuIcon;

        /// <summary>
        /// The star map's button icon: a large four-pointed star and a small one, light grey on clear and shaded
        /// like the game's own button icons, so the button's tint applies to it.
        /// </summary>
        internal static Sprite MenuIcon => menuIcon ??= ToSprite(MenuIconTexture(128));

        /// <summary>Across a link's width: opaque in the middle, clear at both sides.</summary>
        internal static Texture2D LinkFalloff => linkFalloff ??= LinkTexture(64);

        /// <summary>
        /// The same falloff along V, for a LineRenderer, whose V runs across the line's width.
        /// </summary>
        internal static Texture2D BeamFalloff => beamFalloff ??= BeamTexture(64);

        private static Texture2D beamFalloff;

        private static Texture2D BeamTexture(int height) {
            Texture2D texture = NewTexture(1, height);
            for (int y = 0; y < height; y++) {
                float t = (y + 0.5f) / height * 2f - 1f;
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Exp(-t * t * 4.5f)));
            }
            texture.Apply();
            return texture;
        }

        private static Shader glowShader;

        /// <summary>
        /// An additive material for glows in the world, from whichever particle shader the game has
        /// loaded; falls back to the sprite shader, which every build includes.
        /// </summary>
        internal static Material GlowMaterial(Texture texture, Color color) {
            if (glowShader == null) {
                foreach (string name in new[] { "Legacy Shaders/Particles/Additive", "Particles/Additive", "Mobile/Particles/Additive", "Sprites/Default" }) {
                    glowShader = Shader.Find(name);
                    if (glowShader == null) {
                        foreach (Shader loaded in Resources.FindObjectsOfTypeAll<Shader>()) {
                            if (loaded.name == name) { glowShader = loaded; break; }
                        }
                    }
                    if (glowShader != null) { break; }
                }
            }
            Material material = new Material(glowShader) { mainTexture = texture, renderQueue = 3100 };
            SetGlowColor(material, color);
            return material;
        }

        internal static void SetGlowColor(Material material, Color color) {
            // The legacy particle shaders double _TintColor; the sprite shader uses _Color as is.
            if (material.HasProperty("_TintColor")) {
                material.SetColor("_TintColor", color * 0.5f);
            } else {
                material.color = color;
            }
        }

        /// <summary>Scattered stars on transparent black, tiling.</summary>
        internal static Texture2D Starfield => starfield ??= StarfieldTexture(512, 900, 12345);

        /// <summary>Soft clouds of fractal noise in white, tinted per deity by the image showing them.</summary>
        internal static Texture2D Nebula => nebula ??= NebulaTexture(256);

        /// <summary>Clear in the middle, darkening towards the corners.</summary>
        internal static Texture2D Vignette => vignette ??= VignetteTexture(256);

        internal static Color ParseColor(string hex, Color fallback) {
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color color) ? color : fallback;
        }

        private static readonly Dictionary<string, Texture2D> runeRings = new Dictionary<string, Texture2D>();

        /// <summary>
        /// The rune repeated round a circle, upright with its foot towards the centre, between two thin
        /// rings and with a dot between each pair. White; the material or image tints it.
        /// </summary>
        internal static Texture2D RuneRing(string rune) {
            string key = rune ?? "";
            if (runeRings.TryGetValue(key, out Texture2D cached)) { return cached; }
            Texture2D glyph = RuneArt.Texture(rune);
            const int size = 256;
            const int count = 8;
            const float inner = 0.68f;
            const float outer = 0.92f;
            float sector = 2f * Mathf.PI / count;
            float middle = (inner + outer) * 0.5f;
            float band = outer - inner;
            Texture2D texture = NewTexture(size, size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    Vector2 p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    float r = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float local = angle - Mathf.Round(angle / sector) * sector;
                    float alpha = Mathf.Max(Thin(r, 0.62f), Thin(r, 0.97f));
                    if (glyph != null) {
                        // u runs against the angle so the glyphs read left to right, not mirrored.
                        float u = 0.5f - local * middle / band;
                        float v = (r - inner) / band;
                        if (u >= 0f && u <= 1f && v >= 0f && v <= 1f) { alpha = Mathf.Max(alpha, glyph.GetPixelBilinear(u, v).a); }
                    }
                    float dotX = (Mathf.Abs(local) - sector * 0.5f) * middle;
                    float dot = Mathf.Sqrt(dotX * dotX + (r - middle) * (r - middle));
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(1f - dot / 0.022f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, r > 1f ? 0f : alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            runeRings[key] = texture;
            return texture;
        }

        private static float Thin(float r, float at) => Mathf.Clamp01(1f - Mathf.Abs(r - at) / 0.012f);

        private static Texture2D apexRing;

        /// <summary>
        /// The Ascension ring through the apex stars: a fine line with a soft glow either side and small
        /// marks round it, so it is seen to turn. White; the image tints it.
        /// </summary>
        internal static Texture2D ApexRing => apexRing ??= ApexRingTexture(1024, 36);

        private static Texture2D ApexRingTexture(int size, int marks) {
            Texture2D texture = NewTexture(size, size);
            Color[] pixels = new Color[size * size];
            float line = 0.985f;
            float pixel = 2f / size;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    Vector2 p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    float r = p.magnitude;
                    float d = Mathf.Abs(r - line);
                    float alpha = Mathf.Max(Mathf.Clamp01(1f - d / (pixel * 1.5f)), 0.35f * Mathf.Clamp01(1f - d / 0.012f));
                    // Short ticks just inside the line.
                    float sector = 2f * Mathf.PI / marks;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float across = Mathf.Abs(angle - Mathf.Round(angle / sector) * sector) * r;
                    if (r < line && r > line - 0.03f && across < pixel * 1.5f) { alpha = Mathf.Max(alpha, 0.8f); }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, r > 1f ? 0f : alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        internal static Color WithAlpha(Color color, float alpha) {
            color.a = alpha;
            return color;
        }

        private static Sprite ToSprite(Texture2D texture) {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTexture(int width, int height, TextureWrapMode wrap = TextureWrapMode.Clamp) {
            return new Texture2D(width, height, TextureFormat.RGBA32, false) {
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        // White, with alpha from the distance to the centre (0 at the centre, 1 at the edge).
        private static Texture2D Radial(int size, System.Func<float, float> alphaAt) {
            Texture2D texture = NewTexture(size, size);
            Color[] pixels = new Color[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float r = Mathf.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half)) / half;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, r >= 1f ? 0f : alphaAt(r));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static readonly Vector3 MenuIconLight = new Vector3(-1f, 1f, 1.1f).normalized;

        // The game's button icons are painted, not flat: lit from the upper left, each form split into a lit and a
        // shaded plane (the trophies cup), a darker bevel along the lower-right edges (the skills valknut) and a
        // faint cloudy grain. The button's Custom/LitGui material only tints by the scene's light, so the shading
        // has to be in the sprite. Coverage is from 4x4 samples a pixel, so the points stay smooth drawn small.
        private static Texture2D MenuIconTexture(int size) {
            const int samples = 4;
            const float bevel = 0.035f;
            const float bevelShade = 0.45f;
            const float darkestFacet = 0.7f;
            const float grain = 0.16f;
            Texture2D texture = NewTexture(size, size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    int face = 0;
                    int back = 0;
                    float light = 0f;
                    for (int sy = 0; sy < samples; sy++) {
                        for (int sx = 0; sx < samples; sx++) {
                            float u = (x + (sx + 0.5f) / samples) / size;
                            float v = (y + (sy + 0.5f) / samples) / size;
                            if (MenuIconStars(u, v, out float lit)) {
                                face++;
                                light += Mathf.Lerp(darkestFacet, 1f, lit);
                            } else if (MenuIconStars(u - bevel, v + bevel, out _)) {
                                back++;
                            }
                        }
                    }
                    int covered = face + back;
                    if (covered == 0) { pixels[y * size + x] = Color.clear; continue; }
                    float u0 = (x + 0.5f) / size;
                    float v0 = (y + 0.5f) / size;
                    float cloud = 0.6f * Mathf.PerlinNoise(u0 * 6f, v0 * 6f) + 0.4f * Mathf.PerlinNoise(u0 * 14f + 3.1f, v0 * 14f + 5.3f);
                    float faceShade = face > 0 ? light / face * (1f - grain + grain * cloud) : 0f;
                    float shade = (faceShade * face + bevelShade * back) / covered;
                    pixels[y * size + x] = new Color(shade, shade, shade, covered / (float)(samples * samples));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // Whether (u, v) is on either of the icon's stars, and how lit its facet there is.
        private static bool MenuIconStars(float u, float v, out float lit) {
            return InFourPointStar(u, v, 0.56f, 0.44f, 0.42f, out lit) || InFourPointStar(u, v, 0.2f, 0.8f, 0.17f, out lit);
        }

        // Inside |x|^0.6 + |y|^0.6 <= 1, in units of the radius: points on the axes, sides curving in between.
        // Each point has a ridge along its axis; its two halves slope away from the ridge and down to the tip.
        private static bool InFourPointStar(float u, float v, float centerX, float centerY, float radius, out float lit) {
            float x = (u - centerX) / radius;
            float y = (v - centerY) / radius;
            lit = 0f;
            if (Mathf.Pow(Mathf.Abs(x), 0.6f) + Mathf.Pow(Mathf.Abs(y), 0.6f) > 1f) { return false; }
            bool across = Mathf.Abs(x) >= Mathf.Abs(y);
            Vector2 tip = across ? new Vector2(Mathf.Sign(x), 0f) : new Vector2(0f, Mathf.Sign(y));
            Vector2 side = across ? new Vector2(0f, Mathf.Sign(y)) : new Vector2(Mathf.Sign(x), 0f);
            Vector2 slope = tip * 0.35f + side;
            lit = Mathf.Clamp01(Vector3.Dot(new Vector3(slope.x, slope.y, 1f).normalized, MenuIconLight));
            return true;
        }

        private static Texture2D StarTexture(int size) {
            Texture2D texture = NewTexture(size, size);
            Color[] pixels = new Color[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r >= 1f) { pixels[y * size + x] = Color.clear; continue; }
                    float core = Mathf.Pow(Mathf.Clamp01(1f - r / 0.35f), 1.6f);
                    float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 3f) * 0.45f;
                    // Long spikes along the axes, short ones along the diagonals.
                    float axis = Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy));
                    float spikes = Mathf.Clamp01(1f - axis / 0.05f) * Mathf.Clamp01(1f - r);
                    float diagonal = Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) / 1.414f;
                    float shortSpikes = Mathf.Clamp01(1f - diagonal / 0.035f) * Mathf.Clamp01(1f - r / 0.55f) * 0.6f;
                    float alpha = Mathf.Clamp01(core + halo + spikes + shortSpikes);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D LinkTexture(int width) {
            Texture2D texture = NewTexture(width, 1);
            for (int x = 0; x < width; x++) {
                float t = (x + 0.5f) / width * 2f - 1f;
                texture.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.Exp(-t * t * 4.5f)));
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D StarfieldTexture(int size, int count, int seed) {
            Texture2D texture = NewTexture(size, size, TextureWrapMode.Repeat);
            Color[] pixels = new Color[size * size];
            System.Random random = new System.Random(seed);
            for (int i = 0; i < count; i++) {
                int cx = random.Next(size);
                int cy = random.Next(size);
                float brightness = Mathf.Pow((float)random.NextDouble(), 3f);
                // A few large stars, mostly pinpricks; slightly blue or gold.
                int radius = brightness > 0.6f ? 2 : 1;
                float warmth = (float)random.NextDouble();
                Color tint = Color.Lerp(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.92f, 0.75f), warmth);
                for (int y = -radius; y <= radius; y++) {
                    for (int x = -radius; x <= radius; x++) {
                        float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) / (radius + 0.5f));
                        int px = (cx + x + size) % size;
                        int py = (cy + y + size) % size;
                        Color existing = pixels[py * size + px];
                        float alpha = Mathf.Max(existing.a, (0.25f + 0.75f * brightness) * falloff);
                        pixels[py * size + px] = new Color(tint.r, tint.g, tint.b, alpha);
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D NebulaTexture(int size) {
            Texture2D texture = NewTexture(size, size, TextureWrapMode.Repeat);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float value = 0f;
                    float amplitude = 0.55f;
                    int frequency = 3;
                    for (int octave = 0; octave < 5; octave++) {
                        value += amplitude * TilingPerlin(u * frequency, v * frequency, frequency, (uint)octave);
                        amplitude *= 0.5f;
                        frequency *= 2;
                    }
                    float alpha = Mathf.Clamp01((value - 0.45f) * 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // Perlin noise that repeats every `period` units, so the nebula tiles without a seam.
        // Mathf.PerlinNoise does not repeat: where the texture wrapped, the clouds were cut off in straight
        // lines, and the drift carried those lines slowly across the map.
        private static float TilingPerlin(float x, float y, int period, uint seed) {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float u = Fade(fx);
            float v = Fade(fy);
            float bottom = Mathf.Lerp(Corner(x0, y0, period, seed, fx, fy), Corner(x0 + 1, y0, period, seed, fx - 1f, fy), u);
            float top = Mathf.Lerp(Corner(x0, y0 + 1, period, seed, fx, fy - 1f), Corner(x0 + 1, y0 + 1, period, seed, fx - 1f, fy - 1f), u);
            // Scaled into 0..1 the way Mathf.PerlinNoise is, which the nebula's threshold was tuned against.
            return (Mathf.Lerp(bottom, top, v) + 0.69f) / 1.483f;
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        // A lattice corner's gradient dotted with the offset from it; one of eight directions, picked by
        // hashing the corner's position wrapped to the period.
        private static float Corner(int x, int y, int period, uint seed, float dx, float dy) {
            uint hash = (uint)((x % period + period) % period) * 73856093u
                ^ (uint)((y % period + period) % period) * 19349663u
                ^ seed * 83492791u;
            hash ^= hash >> 13;
            hash *= 0x5bd1e995u;
            hash ^= hash >> 15;
            switch (hash & 7u) {
                case 0: return dx + dy;
                case 1: return -dx + dy;
                case 2: return dx - dy;
                case 3: return -dx - dy;
                case 4: return dx;
                case 5: return -dx;
                case 6: return dy;
                default: return -dy;
            }
        }

        private static Texture2D VignetteTexture(int size) {
            Texture2D texture = NewTexture(size, size);
            Color[] pixels = new Color[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 0.85f, (r - 0.45f) / 0.55f));
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
