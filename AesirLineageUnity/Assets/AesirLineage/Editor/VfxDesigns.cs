using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AesirLineage.EditorTools
{
    /// <summary>
    /// Effects composed from a few simple particle layers on Piloto's materials, in place of whole Piloto
    /// prefabs: each deity's footstep, aura, kill and power effects are one motif of its own, kept small.
    /// </summary>
    public static class VfxDesigns
    {
        private const string Materials = "Assets/Piloto Studio/Materials/";

        /// <summary>One particle system of an effect. Sizes are metres, times seconds, speeds metres per second.</summary>
        public sealed class Layer
        {
            public string Name;
            public string Material;
            public ParticleSystemRenderMode Render = ParticleSystemRenderMode.Billboard;
            // Lifts the quad by this many of its own sizes, so a blade's base sits where it spawned.
            public float PivotUp;

            public bool Loop;
            public float Duration = 1f;
            public int Burst;
            public float Rate;
            public float RatePerMetre;
            public int Max = 40;

            public Vector2 Life = new Vector2(1f, 1f);
            public Vector2 Size = new Vector2(0.1f, 0.1f);
            // When set, particles are this tall and Size wide.
            public Vector2 Height;
            // For stretched billboards: how many of its own sizes long a particle is drawn along its flight.
            public float Stretch = 1f;
            public Vector2 Speed;
            public float Gravity;
            public bool RandomRotation;
            // Seconds before the layer starts.
            public float Delay;
            // Turns per second either way, at random.
            public float Spin;
            public float Noise;
            public float Drag;
            // Turns per second round the effect's upright axis.
            public float Orbit;

            public ParticleSystemShapeType Shape = ParticleSystemShapeType.Sphere;
            public float Radius = 0.01f;
            // How far in from the edge particles spawn: 1 fills the shape, 0 is its edge only.
            public float Thickness = 1f;
            // A burst spaced evenly round the shape's circle rather than at random.
            public bool Spread;
            public float Angle;
            // Moves and turns the layer itself within the effect.
            public Vector3 Offset;
            public Vector3 Rotation;
            public Vector3 ShapeRotation;
            // Particles drawn flat in the layer's own plane, so they turn with the effect, instead of facing the camera.
            public bool FaceLayer;
            // Particles turned so their face looks along the way they first move: spawned on a ring and sent outward,
            // each faces the centre with its widest side.
            public bool FaceMotion;
            // For a texture holding a square grid of this many drawings a side: each particle shows one at random, or
            // with Animate plays through them all over its life.
            public int Tiles;
            public bool Animate;

            // Start colours are picked between these, then shift toward OverEnd over the particle's life.
            public Color ColorA = Color.white;
            public Color ColorB = Color.white;
            public Color OverEnd = Color.white;
            public float Alpha = 1f;
            public float FadeIn = 0.1f;
            public float FadeOut = 0.4f;
            // The fraction of life spent growing from nothing.
            public float Grow;
            // A tail behind each particle lasting this fraction of its life, 0 for none; it tapers from TrailWidth
            // metres and shifts toward TrailEnd as it fades.
            public float Trail;
            public float TrailWidth = 0.02f;
            public float TrailAlpha = 0.8f;
            public Color TrailEnd = Color.white;
            // Drawn with the soft additive line unless set; an alpha-blended material makes a dark trail.
            public string TrailMaterial;
        }

        /// <summary>A scene object holding the layers, inactive so it doesn't play until copied or saved.</summary>
        public static GameObject Compose(string name, params Layer[] layers)
        {
            GameObject root = new GameObject(name);
            root.SetActive(false);
            foreach (Layer layer in layers) { Build(root.transform, layer); }
            return root;
        }

        private static void Build(Transform root, Layer layer)
        {
            GameObject go = new GameObject(layer.Name);
            go.transform.SetParent(root, false);
            go.transform.SetLocalPositionAndRotation(layer.Offset, Quaternion.Euler(layer.Rotation));
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = layer.Duration;
            main.loop = layer.Loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(layer.Life.x, layer.Life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(layer.Speed.x, layer.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(layer.Size.x, layer.Size.y);
            if (layer.Height.y > 0f) {
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(layer.Size.x, layer.Size.y);
                main.startSizeY = new ParticleSystem.MinMaxCurve(layer.Height.x, layer.Height.y);
                main.startSizeZ = new ParticleSystem.MinMaxCurve(layer.Size.x, layer.Size.y);
            }
            main.startRotation = layer.RandomRotation ? new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI) : new ParticleSystem.MinMaxCurve(0f);
            main.startDelay = layer.Delay;
            main.startColor = new ParticleSystem.MinMaxGradient(layer.ColorA, layer.ColorB);
            main.gravityModifier = layer.Gravity;
            // Worn effects leave their particles where they were made, so an aura trails rather than following the body.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = layer.Max;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = layer.Rate;
            emission.rateOverDistance = layer.RatePerMetre;
            emission.SetBursts(layer.Burst > 0 ? new[] { new ParticleSystem.Burst(0f, layer.Burst) } : new ParticleSystem.Burst[0]);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = layer.Shape;
            shape.radius = layer.Radius;
            shape.radiusThickness = layer.Thickness;
            shape.angle = layer.Angle;
            if (layer.Spread) { shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread; }
            shape.rotation = layer.ShapeRotation;
            shape.alignToDirection = layer.FaceMotion;

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(layer.OverEnd, 1f) },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(layer.Alpha, Mathf.Max(0.001f, layer.FadeIn)),
                    new GradientAlphaKey(layer.Alpha, Mathf.Min(0.999f, 1f - layer.FadeOut)),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = gradient;

            if (layer.Grow > 0f) {
                ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(layer.Grow, 1f), new Keyframe(1f, 1f)));
            }
            if (layer.Spin > 0f) {
                ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
                rotation.enabled = true;
                float spin = layer.Spin * 2f * Mathf.PI;
                rotation.z = new ParticleSystem.MinMaxCurve(-spin, spin);
            }
            if (layer.Noise > 0f) {
                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = layer.Noise;
                noise.frequency = 0.6f;
                noise.scrollSpeed = 0.2f;
                noise.quality = ParticleSystemNoiseQuality.Medium;
            }
            if (layer.Orbit != 0f) {
                ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.orbitalY = layer.Orbit * 2f * Mathf.PI;
            }
            if (layer.Tiles > 1) {
                ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
                sheet.enabled = true;
                sheet.numTilesX = layer.Tiles;
                sheet.numTilesY = layer.Tiles;
                // A frame picked between two constants is picked once per particle and held for its life.
                sheet.frameOverTime = layer.Animate
                    ? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f))
                    : new ParticleSystem.MinMaxCurve(0f, 0.999f);
                sheet.cycleCount = 1;
            }
            if (layer.Drag > 0f) {
                ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.drag = layer.Drag;
                // Unity scales drag by particle size unless told not to, which leaves small particles barely slowed.
                limit.multiplyDragByParticleSize = false;
            }

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            string path = Materials + layer.Material + ".mat";
            renderer.sharedMaterial = layer.Material.StartsWith("AL/") ? Own(layer.Material) : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (renderer.sharedMaterial == null) { Debug.LogWarning($"{root.name}/{layer.Name}: no material at {path}"); }
            renderer.renderMode = layer.Render;
            if (layer.FaceLayer || layer.FaceMotion) { renderer.alignment = ParticleSystemRenderSpace.Local; }
            renderer.pivot = new Vector3(0f, layer.PivotUp, 0f);
            // Unity draws no particle wider than half the screen by default, which shrank wide rings seen up close.
            renderer.maxParticleSize = 10f;
            if (layer.Render == ParticleSystemRenderMode.Stretch) {
                renderer.lengthScale = layer.Stretch;
                renderer.velocityScale = 0f;
            }
            if (layer.Trail > 0f) {
                ParticleSystem.TrailModule trails = system.trails;
                trails.enabled = true;
                trails.mode = ParticleSystemTrailMode.PerParticle;
                trails.lifetime = layer.Trail;
                trails.minVertexDistance = 0.02f;
                trails.worldSpace = true;
                trails.dieWithParticles = true;
                trails.sizeAffectsWidth = false;
                trails.widthOverTrail = new ParticleSystem.MinMaxCurve(layer.TrailWidth, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                trails.inheritParticleColor = true;
                Gradient fade = new Gradient();
                fade.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(layer.TrailEnd, 1f) },
                    new[] { new GradientAlphaKey(layer.TrailAlpha, 0f), new GradientAlphaKey(0f, 1f) });
                trails.colorOverTrail = fade;
                trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
                renderer.trailMaterial = Own(layer.TrailMaterial ?? Line);
            }
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Materials of our own, saved beside the prefabs so the bundle carries them with the effects.
        private const string OwnMaterials = "Assets/AesirLineage/Vfx/Materials";
        private const int TextureSize = 128;

        /// <summary>A soft round glow, with the falloff of the mod's own SoftGlow.</summary>
        public const string Glow = "AL/Glow";
        /// <summary>A soft line across its width, for particles' trails.</summary>
        public const string Line = "AL/Line";
        /// <summary>A shaft of light, brightest low down, with fine streaks running up through it.</summary>
        public const string Beam = "AL/Beam";
        /// <summary>A glint: a bright core with four long thin spikes and four faint short ones.</summary>
        public const string Glint = "AL/Glint";
        /// <summary>A ring of light with sixteen rays round it, long and short in turn.</summary>
        public const string SunWheel = "AL/SunWheel";
        /// <summary>A glowing footprint, toe up the texture: a soft-filled sole with a brighter rim and a faint halo.</summary>
        public const string Footprint = "AL/Footprint";
        /// <summary>A snowflake: six arms, each with two pairs of side branches, round a small hexagon.</summary>
        public const string Snowflake = "AL/Snowflake";
        /// <summary>The soft glow drawn alpha blended on World Alpha, so it whitens what is behind it like mist.</summary>
        public const string Mist = "AL/Mist";
        /// <summary>A hexagonal shield plate: a bright rim, a bevel line inside it, a rivet in each corner, a round boss
        /// and a faint fill.</summary>
        public const string Ward = "AL/Ward";
        /// <summary>A chip of stone, shaded and speckled in grey, drawn alpha blended so it reads as solid.</summary>
        public const string Stone = "AL/Stone";
        /// <summary>A flake of ash: a ragged, grainy shape, white so the particle colour sets its shade.</summary>
        public const string Ash = "AL/Ash";
        /// <summary>Piloto's wispy smoke trail turned to shadow, alpha blended, for dark trails.</summary>
        public const string ShadowTrail = "AL/ShadowTrail";
        /// <summary>Diamond dust: a tall rhombus of ice with a bright rim, a facet down it and across it, a glint at its
        /// centre and a faint halo.</summary>
        public const string Diamond = "AL/Diamond";
        /// <summary>A shard of ice: a hexagonal column seen side on, pointed at both ends, one face lit.</summary>
        public const string Shard = "AL/Shard";
        /// <summary>A curtain of the northern lights: a wavering bright lower edge with rays rising from it, teal
        /// fading to violet as it climbs, drawn as light.</summary>
        public const string Aurora = "AL/Aurora";

        private static Material Own(string key)
        {
            if (key == Glow) { return Additive("al_glow", "al_softglow", (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Radius(u, v)), 2.2f)); }
            if (key == Line) { return Additive("al_line", "al_line", (u, v) => Mathf.Exp(-(2f * v - 1f) * (2f * v - 1f) * 4.5f)); }
            if (key == Beam) { return Additive("al_beam", "al_beam", BeamAlpha); }
            if (key == Glint) { return Additive("al_glint", "al_glint", GlintAlpha); }
            if (key == SunWheel) { return Additive("al_sunwheel", "al_sunwheel", SunWheelAlpha); }
            if (key == Footprint) { return Additive("al_footprint", "al_footprint", FootprintAlpha); }
            if (key == Snowflake) { return Additive("al_snowflake", "al_snowflake", SnowflakeAlpha); }
            if (key == Mist) { return Additive("al_mist", "al_softglow", (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Radius(u, v)), 2.2f), "World Alpha"); }
            if (key == Ward) { return Additive("al_ward", "al_ward", WardAlpha); }
            if (key == Stone) { return Painted("al_stone", "al_stone", StoneColor, "World Alpha"); }
            if (key == Ash) { return Additive("al_ash", "al_ash", AshAlpha, "World Alpha"); }
            if (key == ShadowTrail) { return Shadowed("al_shadowtrail", "Tx_SmokeTrail_06.png"); }
            if (key == Diamond) { return Additive("al_diamond", "al_diamond", DiamondAlpha); }
            if (key == Shard) { return Additive("al_shard", "al_shard", ShardAlpha); }
            if (key == Aurora) { return Painted("al_aurora", "al_aurora", AuroraColor, "World Additive", 256, 256); }
            if (key.StartsWith(AlphaPrefix)) { return SpriteMaterial(key.Substring(AlphaPrefix.Length), "World Alpha", "al_"); }
            return key.StartsWith(ShinePrefix) ? SpriteMaterial(key.Substring(ShinePrefix.Length), "World Additive", "al_shine_") : null;
        }

        // A white texture of our own on one of the mod's world shaders (World Additive unless named); the particle
        // colour tints it.
        private static Material Additive(string material, string texture, System.Func<float, float, float> alphaAt, string shader = "World Additive")
            => Painted(material, texture, (u, v) => new Color(1f, 1f, 1f, alphaAt(u, v)), shader);

        private static Material Painted(string material, string texture, System.Func<float, float, Color> colorAt, string shader, int width = TextureSize, int height = TextureSize)
        {
            string path = $"{OwnMaterials}/{material}.mat";
            Material saved = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (saved != null) { return saved; }
            Material made = new Material(Shader.Find("AesirLineage/" + shader)) { mainTexture = Baked(texture, colorAt, width, height) };
            AssetDatabase.CreateAsset(made, path);
            return made;
        }

        // A Piloto texture drawn in light on black turned to a white shadow mask, its brightness becoming alpha, so the
        // particle colour can draw it dark. Its faint background is cut away and its edges softened, as either left the
        // quad showing as a dark rectangle.
        private static Material Shadowed(string material, string piloto) => FromPiloto(material, piloto, (c, u, v) => {
            float alpha = Mathf.Clamp01((c.grayscale - 0.15f) / 0.7f);
            alpha *= Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) / 0.1f);
            return new Color(1f, 1f, 1f, alpha);
        });

        // A copy of a Piloto texture, each pixel converted, on World Alpha at half the source's size.
        private static Material FromPiloto(string material, string piloto, System.Func<Color, float, float, Color> convert)
        {
            Material saved = AssetDatabase.LoadAssetAtPath<Material>($"{OwnMaterials}/{material}.mat");
            if (saved != null) { return saved; }
            // Loaded from the file, as the imported texture isn't readable.
            Texture2D source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes("Assets/Piloto Studio/Textures/" + piloto));
            Material made = Painted(material, material, (u, v) => convert(source.GetPixelBilinear(u, v), u, v),
                "World Alpha", source.width / 2, source.height / 2);
            Object.DestroyImmediate(source);
            return made;
        }

        // Each pixel from a function of its centre in a 0..1 box, y up.
        private static Texture2D Baked(string name, System.Func<float, float, Color> colorAt, int width = TextureSize, int height = TextureSize)
        {
            string path = $"{OwnMaterials}/{name}.png";
            Texture2D saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (saved != null) { return saved; }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    texture.SetPixel(x, y, colorAt((x + 0.5f) / width, (y + 0.5f) / height));
                }
            }
            Directory.CreateDirectory(OwnMaterials);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static float Radius(float u, float v) => Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) / 0.5f;

        private static float Band(float x, float centre, float width) => Mathf.Exp(-((x - centre) / width) * ((x - centre) / width));

        private static float BeamAlpha(float u, float v)
        {
            float body = Band(u, 0.5f, 0.17f);
            float streaks = 0.5f * Band(u, 0.38f, 0.03f) + 0.6f * Band(u, 0.52f, 0.025f) + 0.4f * Band(u, 0.64f, 0.035f);
            float along = Mathf.SmoothStep(0f, 1f, v / 0.08f) * (1f - Mathf.SmoothStep(0f, 1f, (v - 0.35f) / 0.65f));
            return Mathf.Clamp01(body * 0.6f + streaks * body) * along;
        }

        private static float GlintAlpha(float u, float v)
        {
            float dx = (u - 0.5f) / 0.5f;
            float dy = (v - 0.5f) / 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r >= 1f) { return 0f; }
            float core = Mathf.Pow(Mathf.Clamp01(1f - r / 0.22f), 2f);
            float halo = Mathf.Pow(1f - r, 4f) * 0.35f;
            float spikes = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy)) / 0.025f) * Mathf.Pow(1f - r, 1.2f);
            float diagonal = Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) / 1.414f;
            float shortSpikes = Mathf.Clamp01(1f - diagonal / 0.02f) * Mathf.Clamp01(1f - r / 0.4f) * 0.5f;
            return Mathf.Clamp01(core + halo + spikes + shortSpikes);
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t);
        }

        private static float WardAlpha(float u, float v)
        {
            Vector2 p = new Vector2((u - 0.5f) / 0.5f, (v - 0.5f) / 0.5f);
            float qx = Mathf.Abs(p.x);
            float qy = Mathf.Abs(p.y);
            // Below 0 inside a flat-topped hexagon, whose corners lie 0.9 out at every sixth of a turn from the right.
            float d = Mathf.Max(qy, qx * 0.866025f + qy * 0.5f) - 0.78f;
            float r = p.magnitude;
            float rim = Mathf.Exp(-(d / 0.035f) * (d / 0.035f));
            float bevel = Mathf.Exp(-((d + 0.16f) / 0.015f) * ((d + 0.16f) / 0.015f)) * 0.45f;
            float fill = d < 0f ? 0.08f + 0.1f * (1f - r) : Mathf.Exp(-(d / 0.08f) * (d / 0.08f)) * 0.35f;
            float boss = Mathf.Exp(-((r - 0.15f) / 0.025f) * ((r - 0.15f) / 0.025f)) * 0.7f + Mathf.Clamp01(1f - r / 0.1f) * 0.45f;
            float rivets = 0f;
            for (int k = 0; k < 6; k++) {
                float angle = k * Mathf.PI / 3f;
                float gap = Vector2.Distance(p, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.8f);
                rivets = Mathf.Max(rivets, Mathf.Pow(Mathf.Clamp01(1f - gap / 0.05f), 1.5f) * 0.9f);
            }
            return Mathf.Clamp01(rim + bevel + fill + boss + rivets);
        }

        private static readonly Vector2[] StoneOutline = {
            new Vector2(0.2f, 0.36f), new Vector2(0.42f, 0.12f), new Vector2(0.74f, 0.2f), new Vector2(0.88f, 0.48f),
            new Vector2(0.76f, 0.82f), new Vector2(0.44f, 0.9f), new Vector2(0.18f, 0.66f),
        };

        // Lit from the upper left, with a ridge between two faces, darker edges and a little speckle.
        private static Color StoneColor(float u, float v)
        {
            Vector2 p = new Vector2(u, v);
            bool inside = true;
            float edge = float.MaxValue;
            for (int i = 0; i < StoneOutline.Length; i++) {
                Vector2 a = StoneOutline[i];
                Vector2 b = StoneOutline[(i + 1) % StoneOutline.Length];
                if ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) < 0f) { inside = false; }
                edge = Mathf.Min(edge, Segment(p, a, b));
            }
            float alpha = inside ? Mathf.Clamp01(edge / 0.015f) : 0f;
            Vector2 n = p - new Vector2(0.52f, 0.5f);
            float light = 0.6f + 0.35f * Vector2.Dot(n.normalized, new Vector2(-0.64f, 0.77f)) * Mathf.Clamp01(n.magnitude / 0.35f);
            if ((p.x - 0.42f) * 0.78f - (p.y - 0.12f) * 0.33f > 0f) { light -= 0.12f; }
            float speckle = (Mathf.Abs(Mathf.Sin(u * 913.7f + v * 371.3f) * 43758.5453f) % 1f - 0.5f) * 0.14f;
            float b2 = Mathf.Clamp01((light + speckle) * (0.75f + 0.25f * Mathf.Clamp01(edge / 0.05f)));
            return new Color(b2, b2, b2, alpha);
        }

        private static float AshAlpha(float u, float v)
        {
            Vector2 p = new Vector2((u - 0.5f) / 0.5f, (v - 0.5f) / 0.5f * 1.3f);
            float angle = Mathf.Atan2(p.y, p.x);
            // Many small lobes of similar size: a few large ones read as a moth's wings rather than a flake.
            float edge = 0.6f + 0.06f * Mathf.Sin(3f * angle + 0.7f) + 0.06f * Mathf.Sin(5f * angle + 2.1f) + 0.05f * Mathf.Sin(7f * angle + 0.3f)
                + 0.04f * Mathf.Sin(11f * angle + 1.3f) + 0.03f * Mathf.Sin(17f * angle + 4f);
            float body = Mathf.SmoothStep(0f, 1f, (edge - p.magnitude) / 0.08f);
            float grain = Mathf.PerlinNoise(u * 9f + 3.1f, v * 9f + 7.7f);
            return Mathf.Clamp01(body * (0.55f + 0.45f * grain));
        }

        private static float DiamondAlpha(float u, float v)
        {
            Vector2 p = new Vector2((u - 0.5f) / 0.5f, (v - 0.5f) / 0.5f);
            float r = p.magnitude;
            // Below 1 inside a tall rhombus half 0.32 wide and 0.8 high.
            float d = Mathf.Abs(p.x) / 0.32f + Mathf.Abs(p.y) / 0.8f;
            float inside = d < 1f ? 1f : 0f;
            float rim = Mathf.Exp(-((d - 1f) / 0.07f) * ((d - 1f) / 0.07f));
            float fill = inside * (0.2f + 0.35f * (1f - d));
            float facets = inside * (0.45f * Mathf.Exp(-(p.x / 0.03f) * (p.x / 0.03f)) + 0.35f * Mathf.Exp(-((p.y - 0.18f) / 0.03f) * ((p.y - 0.18f) / 0.03f)));
            float halo = 0.22f * Mathf.Exp(-(r / 0.55f) * (r / 0.55f));
            float glint = Mathf.Exp(-(r / 0.09f) * (r / 0.09f));
            return Mathf.Clamp01(rim + fill + facets + halo + glint);
        }

        private static float ShardAlpha(float u, float v)
        {
            Vector2 p = new Vector2((u - 0.5f) / 0.5f, (v - 0.5f) / 0.5f);
            float ax = Mathf.Abs(p.x);
            float ay = Mathf.Abs(p.y);
            // A hexagonal column seen side on: straight sides to 0.55 of its height, then pointed ends.
            float halfWidth = 0.24f * (ay < 0.55f ? 1f : Mathf.Max(0f, (0.9f - ay) / 0.35f));
            float d = ax - halfWidth;
            float rim = Mathf.Exp(-(d / 0.05f) * (d / 0.05f)) * (ay < 0.92f ? 1f : 0f);
            float inside = d < 0f ? 1f : 0f;
            // One face catches the light, the other is in shade, and two edges run down the column between them.
            float fill = inside * (p.x < 0f ? 0.45f : 0.22f);
            float edges = inside * 0.5f * Mathf.Exp(-((ax - 0.08f) / 0.025f) * ((ax - 0.08f) / 0.025f));
            return Mathf.Clamp01(rim + fill + edges);
        }

        private static Color AuroraColor(float u, float v)
        {
            float lower = 0.12f + 0.06f * Mathf.Sin(2f * Mathf.PI * (u * 1.3f + 0.2f)) + 0.03f * Mathf.Sin(2f * Mathf.PI * u * 3.7f);
            float rays = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(u * 55f + 3f * Mathf.Sin(u * 9f)), 2f);
            float above = v > lower ? Mathf.Exp(-(v - lower) / 0.6f) * (0.5f + 0.5f * rays) : 0f;
            float edge = Mathf.Exp(-((v - lower) / 0.025f) * ((v - lower) / 0.025f));
            float sides = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) / 0.18f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.25f);
            Color color = Color.Lerp(Hex("#7FF0C8"), Hex("#B49CFF"), Mathf.Clamp01((v - lower) / 0.4f));
            color = Color.Lerp(color, Color.white, edge * 0.5f);
            color.a = Mathf.Clamp01(Mathf.Max(above, edge * 0.9f) * sides);
            return color;
        }

        private static float SnowflakeAlpha(float u, float v)
        {
            Vector2 p = new Vector2((u - 0.5f) / 0.5f, (v - 0.5f) / 0.5f);
            float distance = float.MaxValue;
            for (int k = 0; k < 6; k++) {
                float angle = k * Mathf.PI / 3f;
                Vector2 arm = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = Mathf.Min(distance, Segment(p, Vector2.zero, arm * 0.88f));
                Vector2 next = new Vector2(Mathf.Cos(angle + Mathf.PI / 3f), Mathf.Sin(angle + Mathf.PI / 3f));
                distance = Mathf.Min(distance, Segment(p, arm * 0.14f, next * 0.14f));
                foreach ((float along, float length) in new[] { (0.38f, 0.22f), (0.62f, 0.16f) }) {
                    Vector2 root = arm * along;
                    foreach (float side in new[] { -1f, 1f }) {
                        float branch = angle + side * Mathf.PI / 3f;
                        distance = Mathf.Min(distance, Segment(p, root, root + new Vector2(Mathf.Cos(branch), Mathf.Sin(branch)) * length));
                    }
                }
            }
            float core = Mathf.Clamp01((0.028f - distance) / (3f / TextureSize) + 0.5f);
            float halo = Mathf.Exp(-(distance / 0.07f) * (distance / 0.07f)) * 0.5f;
            return Mathf.Max(core, halo);
        }

        // Above 0 inside an ellipse, 1 at its centre.
        private static float Inside(float u, float v, float cx, float cy, float rx, float ry)
        {
            float x = (u - cx) / rx;
            float y = (v - cy) / ry;
            return 1f - Mathf.Sqrt(x * x + y * y);
        }

        private static float FootprintAlpha(float u, float v)
        {
            float ball = Inside(u, v, 0.5f, 0.68f, 0.3f, 0.26f);
            float arch = Inside(u, v, 0.53f, 0.46f, 0.2f, 0.22f);
            float heel = Inside(u, v, 0.49f, 0.22f, 0.22f, 0.18f);
            float d = Mathf.Max(ball, Mathf.Max(arch, heel));
            if (d <= 0f) { return Mathf.Exp(-(d / 0.12f) * (d / 0.12f)) * 0.35f; }
            float fill = Mathf.SmoothStep(0f, 1f, d / 0.25f) * 0.6f;
            float rim = Band(d, 0.07f, 0.06f) * 0.5f;
            return Mathf.Clamp01(fill + rim + 0.35f);
        }

        private static float SunWheelAlpha(float u, float v)
        {
            float dx = (u - 0.5f) / 0.5f;
            float dy = (v - 0.5f) / 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.62f) / 0.035f) + Band(r, 0.62f, 0.12f) * 0.35f;
            float turn = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 1f) * 16f;
            int index = Mathf.RoundToInt(turn);
            float reach = index % 2 == 0 ? 0.96f : 0.84f;
            float along = (r - 0.66f) / (reach - 0.66f);
            float ray = along > 0f && along < 1f ? Mathf.Clamp01(1f - Mathf.Abs(turn - index) / (0.14f * (1f - along))) * (1f - along) : 0f;
            return Mathf.Clamp01(ring + ray);
        }

        // A Piloto texture drawn plainly on World Alpha. Some of Piloto's own materials need per-particle data these
        // layers don't give them: Grass_Thick draws nothing without the custom vertex streams that drive its dissolve.
        private const string AlphaPrefix = "AL/Alpha:";
        public static string Sprite(string texture) => AlphaPrefix + texture;

        // A Piloto texture drawn as light on World Additive: its own shading shows through as it glows.
        private const string ShinePrefix = "AL/Shine:";
        public static string Shine(string texture) => ShinePrefix + texture;

        private static Material SpriteMaterial(string texture, string shader, string prefix)
        {
            string path = $"{OwnMaterials}/{prefix}{Path.GetFileNameWithoutExtension(texture).ToLowerInvariant()}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) { return material; }
            Directory.CreateDirectory(OwnMaterials);
            material = new Material(Shader.Find("AesirLineage/" + shader)) {
                mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Piloto Studio/Textures/" + texture),
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            color.a = alpha;
            return color;
        }

        // ---- Gefjun: plough, harvest and the household. Wheat gold, meadow green, pollen. ----

        private static readonly Color GefjunGold = Hex("#E8C35A");
        private static readonly Color GefjunGreen = Hex("#7DBE5A");
        private static readonly Color GefjunGlow = Hex("#FFF2B0");

        // Tufts of wheat-gold grass growing up out of the ground and fading.
        private static Layer GefjunTufts(int count, float radius, Vector2 life, Vector2 size) => new Layer {
            Name = "Tufts", Material = Sprite("GrassThick.png"),
            Render = ParticleSystemRenderMode.VerticalBillboard, PivotUp = 0.5f,
            Burst = count, Life = life, Size = size, Max = count,
            Shape = ParticleSystemShapeType.Circle, Radius = radius, Thickness = 0.4f, ShapeRotation = new Vector3(90f, 0f, 0f),
            ColorA = GefjunGlow, ColorB = GefjunGold, OverEnd = Color.Lerp(Color.white, GefjunGreen, 0.35f),
            FadeIn = 0.02f, FadeOut = 0.3f, Grow = 0.15f,
        };

        // Specks of pollen drifting up from a disc on the ground.
        private static Layer GefjunPollen(int burst, float rate, float radius, Vector2 life, Vector2 speed, float alpha = 1f) => new Layer {
            Name = "Pollen", Material = Glow,
            Burst = burst, Rate = rate, Max = 60, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.07f, 0.13f), Speed = speed, Gravity = -0.02f, Noise = 0.25f,
            Shape = ParticleSystemShapeType.Cone, Angle = 20f, Radius = radius, Thickness = 1f, ShapeRotation = new Vector3(-90f, 0f, 0f),
            ColorA = GefjunGlow, ColorB = GefjunGold, OverEnd = GefjunGold, Alpha = alpha, FadeIn = 0.2f, FadeOut = 0.4f,
        };

        // Single leaves turning in the air.
        private static Layer GefjunLeaves(int burst, float rate, float radius, Vector2 speed, float gravity) => new Layer {
            Name = "Leaves", Material = Sprite("Chinese/leaf.png"),
            Burst = burst, Rate = rate, Max = 20, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = new Vector2(1.2f, 2f), Size = new Vector2(0.12f, 0.2f), Speed = speed, Gravity = gravity, Drag = 0.25f,
            RandomRotation = true, Spin = 0.4f, Noise = 0.15f,
            Shape = ParticleSystemShapeType.Sphere, Radius = radius,
            ColorA = GefjunGreen, ColorB = Color.Lerp(GefjunGreen, GefjunGold, 0.6f), OverEnd = Color.Lerp(Color.white, GefjunGold, 0.5f),
            FadeIn = 0.05f, FadeOut = 0.35f,
        };

        public static GameObject GefjunFootstep() => Compose("al_vfx_gefjun_footstep",
            GefjunTufts(1, 0.01f, new Vector2(1.8f, 2.2f), new Vector2(0.3f, 0.36f)),
            GefjunPollen(2, 0f, 0.08f, new Vector2(0.8f, 1.2f), new Vector2(0.1f, 0.25f)));

        public static GameObject GefjunAura()
        {
            Layer pollen = GefjunPollen(0, 6f, 1.1f, new Vector2(2.5f, 4f), new Vector2(0.03f, 0.12f), 0.9f);
            pollen.RatePerMetre = 2f;
            pollen.Thickness = 0.55f;
            Layer leaves = GefjunLeaves(0, 0.7f, 0.9f, new Vector2(0f, 0.05f), 0.03f);
            leaves.Offset = new Vector3(0f, 1.2f, 0f);
            leaves.Life = new Vector2(3f, 4f);
            return Compose("al_vfx_gefjun_aura", pollen, leaves);
        }

        public static GameObject GefjunKill() => Compose("al_vfx_gefjun_kill",
            GefjunTufts(4, 0.45f, new Vector2(1.4f, 1.8f), new Vector2(0.55f, 0.75f)),
            GefjunPollen(10, 0f, 0.35f, new Vector2(1.2f, 1.8f), new Vector2(0.3f, 0.6f)),
            new Layer {
                Name = "Ground", Material = Glow, Render = ParticleSystemRenderMode.HorizontalBillboard,
                Burst = 1, Max = 1, Life = new Vector2(0.9f, 0.9f), Size = new Vector2(1.4f, 1.4f), Offset = new Vector3(0f, 0.03f, 0f),
                ColorA = GefjunGold, ColorB = GefjunGold, OverEnd = GefjunGold, Alpha = 0.6f, FadeIn = 0.1f, FadeOut = 0.7f, Grow = 0.3f,
            });

        public static GameObject GefjunProc() => Compose("al_vfx_gefjun_proc",
            GefjunLeaves(3, 0f, 0.15f, new Vector2(0.6f, 1.2f), 0.15f),
            GefjunPollen(5, 0f, 0.15f, new Vector2(0.5f, 0.8f), new Vector2(0.4f, 0.8f)));

        public static GameObject GefjunHarvest() => Compose("al_vfx_gefjun_harvest",
            GefjunLeaves(5, 0f, 0.2f, new Vector2(0.8f, 1.4f), 0.2f),
            GefjunPollen(6, 0f, 0.2f, new Vector2(0.8f, 1.2f), new Vector2(0.4f, 0.9f)));

        // A wave of pollen rolling out over the ground to the power's reach (5 m at scale 1), a few leaves lifting.
        public static GameObject GefjunBurst()
        {
            Layer wave = GefjunPollen(36, 0f, 0.3f, new Vector2(0.9f, 1.1f), new Vector2(4.5f, 5f));
            wave.Name = "Wave";
            wave.Shape = ParticleSystemShapeType.Circle;
            wave.Thickness = 0f;
            wave.Noise = 0f;
            wave.Drag = 0.08f;
            wave.Gravity = 0f;
            wave.Size = new Vector2(0.12f, 0.18f);
            wave.Offset = new Vector3(0f, 0.15f, 0f);
            wave.ShapeRotation = new Vector3(90f, 0f, 0f);
            wave.FadeIn = 0.05f;
            wave.FadeOut = 0.5f;
            return Compose("al_vfx_gefjun_burst", wave, GefjunLeaves(8, 0f, 0.8f, new Vector2(0.8f, 1.6f), 0.2f));
        }

        // ---- Vör: wisdom, the one from whom nothing is hidden. The forest round Mímir's well: spirit leaves, wisps of
        // light and roots of light, white and blue fading to forest green. ----

        private static readonly Color VorWhite = Hex("#EAF6FF");
        private static readonly Color VorBlue = Hex("#7FD0FF");
        private static readonly Color VorForest = Hex("#7FC79A");
        private static readonly Vector3 Up = new Vector3(-90f, 0f, 0f);

        // A forest light: a white-blue spark on a wandering path, its tail fading to forest green.
        private static Layer VorWisps(int burst, float rate, Vector2 life, Vector2 speed, float trail) => new Layer {
            Name = "Wisps", Material = Glow,
            Burst = burst, Rate = rate, Max = 30, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.07f, 0.1f), Speed = speed, Noise = 0.5f,
            Shape = ParticleSystemShapeType.Cone, Angle = 25f, Radius = 0.05f, ShapeRotation = Up,
            ColorA = VorWhite, ColorB = VorBlue, OverEnd = VorBlue,
            FadeIn = 0.1f, FadeOut = 0.35f,
            Trail = trail, TrailWidth = 0.025f, TrailAlpha = 0.8f, TrailEnd = VorForest,
        };

        // Pale leaves of light, the leaf's veins showing through the glow.
        private static Layer VorLeaves(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Leaves", Material = Shine("Chinese/leaf.png"),
            Burst = burst, Rate = rate, Max = 12, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.13f, 0.18f), Speed = speed, Drag = 0.4f,
            RandomRotation = true, Spin = 0.25f, Noise = 0.15f,
            Shape = ParticleSystemShapeType.Cone, Angle = 30f, Radius = 0.1f, ShapeRotation = Up,
            ColorA = Color.Lerp(VorWhite, VorForest, 0.4f), ColorB = Color.Lerp(VorWhite, VorBlue, 0.3f), OverEnd = VorBlue,
            Alpha = 0.9f, FadeIn = 0.15f, FadeOut = 0.5f, Grow = 0.1f,
        };

        private static Layer VorFlash(float size, float alpha, float height) => new Layer {
            Name = "Flash", Material = Glow, Burst = 1, Max = 1, Life = new Vector2(0.3f, 0.3f), Size = new Vector2(size, size),
            Offset = new Vector3(0f, height, 0f),
            ColorA = VorWhite, ColorB = VorWhite, OverEnd = VorBlue, Alpha = alpha, FadeIn = 0.05f, FadeOut = 0.8f,
        };

        // A leaf of light lifts from each step as a wisp curls up beside it.
        public static GameObject VorFootstep()
        {
            Layer leaf = VorLeaves(1, 0f, new Vector2(1.1f, 1.3f), new Vector2(0.15f, 0.25f));
            leaf.Offset = new Vector3(0f, 0.2f, 0f);
            Layer wisp = VorWisps(1, 0f, new Vector2(0.7f, 0.9f), new Vector2(0.5f, 0.7f), 0.6f);
            wisp.Offset = new Vector3(0f, 0.15f, 0f);
            return Compose("al_vfx_vor_footstep", leaf, wisp);
        }

        // Wisps wander low round the feet and now and then a leaf drifts down. All of it is left in the world,
        // so walking leaves trails of light behind.
        public static GameObject VorAura()
        {
            Layer wisps = VorWisps(0, 1.4f, new Vector2(2.2f, 3f), new Vector2(0.05f, 0.15f), 0.35f);
            wisps.Noise = 0.18f;
            wisps.Angle = 30f;
            wisps.Radius = 0.9f;
            wisps.Thickness = 0.4f;
            wisps.Offset = new Vector3(0f, 0.25f, 0f);
            wisps.RatePerMetre = 0.6f;
            Layer leaves = VorLeaves(0, 0.5f, new Vector2(3f, 4f), new Vector2(0f, 0.05f));
            leaves.Gravity = 0.02f;
            leaves.Shape = ParticleSystemShapeType.Sphere;
            leaves.Radius = 0.9f;
            leaves.Offset = new Vector3(0f, 1.4f, 0f);
            return Compose("al_vfx_vor_aura", wisps, leaves);
        }

        // Roots of light rise round the fallen as from Mímir's well, leaves of light lift away, and a pale light flares.
        public static GameObject VorKill()
        {
            Layer roots = VorWisps(5, 0f, new Vector2(1.1f, 1.3f), new Vector2(1.9f, 2.3f), 0.85f);
            roots.Name = "Roots";
            roots.Angle = 18f;
            roots.Radius = 0.35f;
            roots.Thickness = 0.2f;
            roots.Spread = true;
            roots.Noise = 0.7f;
            roots.Drag = 1f;
            roots.Size = new Vector2(0.06f, 0.08f);
            roots.TrailWidth = 0.035f;
            Layer leaves = VorLeaves(4, 0f, new Vector2(1.4f, 1.8f), new Vector2(0.5f, 0.9f));
            leaves.Angle = 50f;
            leaves.Radius = 0.2f;
            leaves.Offset = new Vector3(0f, 0.5f, 0f);
            return Compose("al_vfx_vor_kill", roots, leaves, VorFlash(0.9f, 0.5f, 0.5f));
        }

        public static GameObject VorProc()
        {
            Layer wisps = VorWisps(2, 0f, new Vector2(0.5f, 0.6f), new Vector2(1.4f, 1.8f), 0.6f);
            wisps.Shape = ParticleSystemShapeType.Sphere;
            wisps.Drag = 2f;
            return Compose("al_vfx_vor_proc", wisps, VorFlash(0.45f, 0.7f, 0f));
        }

        // Wisps sweep out in a spiral over the ground to the power's reach (5 m at scale 1), leaves lifting behind them.
        public static GameObject VorBurst()
        {
            Layer sweep = VorWisps(12, 0f, new Vector2(1f, 1.1f), new Vector2(4.4f, 4.6f), 0.5f);
            sweep.Name = "Sweep";
            sweep.Shape = ParticleSystemShapeType.Circle;
            sweep.ShapeRotation = new Vector3(90f, 0f, 0f);
            sweep.Radius = 0.3f;
            sweep.Thickness = 0f;
            sweep.Spread = true;
            sweep.Noise = 0f;
            sweep.Drag = 0.08f;
            sweep.Orbit = 0.25f;
            sweep.Offset = new Vector3(0f, 0.4f, 0f);
            sweep.FadeIn = 0.05f;
            sweep.FadeOut = 0.4f;
            Layer leaves = VorLeaves(6, 0f, new Vector2(1.4f, 1.8f), new Vector2(0.8f, 1.4f));
            leaves.Angle = 40f;
            leaves.Radius = 0.6f;
            return Compose("al_vfx_vor_burst", sweep, leaves);
        }

        // The flash of insight (boons, Ascension, Eureka, Völuspá, Masterwork): wisps spiral up round the player,
        // drawing a helix of light, as leaves of light drift down.
        public static GameObject VorLevelUp()
        {
            Layer spiral = VorWisps(8, 0f, new Vector2(1.3f, 1.5f), new Vector2(1f, 1.2f), 0.45f);
            spiral.Name = "Spiral";
            spiral.Angle = 0f;
            spiral.Radius = 0.6f;
            spiral.Thickness = 0f;
            spiral.Spread = true;
            spiral.Noise = 0.1f;
            spiral.Orbit = 0.6f;
            spiral.Offset = new Vector3(0f, 0.1f, 0f);
            Layer leaves = VorLeaves(6, 0f, new Vector2(1.6f, 2f), new Vector2(0.2f, 0.4f));
            leaves.Shape = ParticleSystemShapeType.Sphere;
            leaves.Radius = 0.5f;
            leaves.Gravity = 0.05f;
            leaves.Offset = new Vector3(0f, 1.6f, 0f);
            return Compose("al_vfx_vor_levelup", spiral, leaves, VorFlash(1.1f, 0.4f, 1.5f));
        }

        // ---- Baldur: light and purity, the god nothing would harm. Sunlight in straight lines: shafts of light, rays
        // and a sun-wheel, with flecks of burnished gold like his gorget's. White gold warming to sun gold. ----

        private static readonly Color BaldurWhite = Hex("#FFF4D6");
        private static readonly Color BaldurSun = Hex("#F5B83D");

        // A shaft of sunlight standing on the ground.
        private static Layer BaldurBeam(int burst, float rate, Vector2 life, Vector2 width, Vector2 height, float alpha) => new Layer {
            Name = "Beam", Material = Beam, Render = ParticleSystemRenderMode.VerticalBillboard, PivotUp = 0.5f,
            Burst = burst, Rate = rate, Max = 12, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = width, Height = height,
            ColorA = BaldurWhite, ColorB = Color.Lerp(BaldurWhite, BaldurSun, 0.3f), OverEnd = BaldurSun,
            Alpha = alpha, FadeIn = 0.15f, FadeOut = 0.55f,
        };

        // Flecks of burnished gold turning in the light.
        private static Layer BaldurFlecks(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Flecks", Material = Glint,
            Burst = burst, Rate = rate, Max = 30, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.09f, 0.14f), Speed = speed, RandomRotation = true, Spin = 0.2f, Noise = 0.08f,
            Shape = ParticleSystemShapeType.Cone, Angle = 20f, Radius = 0.1f, ShapeRotation = Up,
            ColorA = BaldurSun, ColorB = Color.Lerp(BaldurSun, BaldurWhite, 0.5f), OverEnd = BaldurSun,
            FadeIn = 0.2f, FadeOut = 0.45f, Grow = 0.2f,
        };

        // Straight rays of light thrown out from a point, as from the sun.
        private static Layer BaldurRays(int burst, Vector2 life, Vector2 speed, float stretch) => new Layer {
            Name = "Rays", Material = Glow, Render = ParticleSystemRenderMode.Stretch, Stretch = stretch,
            Burst = burst, Max = burst, Life = life, Size = new Vector2(0.06f, 0.08f), Speed = speed, Drag = 3f,
            Shape = ParticleSystemShapeType.Sphere, Radius = 0.05f,
            ColorA = Color.white, ColorB = BaldurWhite, OverEnd = BaldurSun, FadeIn = 0.02f, FadeOut = 0.6f,
        };

        // The sun-wheel, lying level and opening out.
        private static Layer BaldurWheel(float size, Vector2 life, float alpha, float height, float grow) => new Layer {
            Name = "Wheel", Material = SunWheel, Render = ParticleSystemRenderMode.HorizontalBillboard,
            Burst = 1, Max = 1, Life = life, Size = new Vector2(size, size), Offset = new Vector3(0f, height, 0f),
            RandomRotation = true,
            ColorA = BaldurWhite, ColorB = BaldurWhite, OverEnd = BaldurSun, Alpha = alpha, FadeIn = 0.05f, FadeOut = 0.6f, Grow = grow,
        };

        // A glowing footprint on the ground, toe the way the player walks (the mod turns the effect with them),
        // fading as a gold fleck lifts from it.
        public static GameObject BaldurFootstep() => Compose("al_vfx_baldur_footstep",
            new Layer {
                Name = "Print", Material = Footprint, FaceLayer = true, Rotation = new Vector3(90f, 0f, 0f), Offset = new Vector3(0f, 0.03f, 0f),
                Burst = 1, Max = 1, Life = new Vector2(1.6f, 1.6f), Size = new Vector2(0.22f, 0.22f), Height = new Vector2(0.32f, 0.32f),
                ColorA = BaldurWhite, ColorB = BaldurWhite, OverEnd = BaldurSun, Alpha = 0.85f, FadeIn = 0.05f, FadeOut = 0.6f,
            },
            BaldurFlecks(1, 0f, new Vector2(0.6f, 0.9f), new Vector2(0.15f, 0.3f)));

        // Gold flecks drift about a metre out like dust in a sunbeam, left in the world as the player walks on.
        public static GameObject BaldurAura()
        {
            Layer flecks = BaldurFlecks(0, 6f, new Vector2(2f, 3f), new Vector2(0.03f, 0.1f));
            flecks.Radius = 1f;
            flecks.Thickness = 0.6f;
            flecks.Offset = new Vector3(0f, 0.2f, 0f);
            return Compose("al_vfx_baldur_aura", flecks);
        }

        // A shaft of sunlight falls on the fallen, the sun-wheel flashes open over them like a halo and gold flecks rise.
        public static GameObject BaldurKill()
        {
            Layer beam = BaldurBeam(1, 0f, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(3.2f, 3.2f), 0.8f);
            beam.FadeIn = 0.08f;
            beam.FadeOut = 0.7f;
            Layer flecks = BaldurFlecks(6, 0f, new Vector2(0.9f, 1.3f), new Vector2(0.5f, 0.9f));
            flecks.Radius = 0.3f;
            flecks.Offset = new Vector3(0f, 0.3f, 0f);
            return Compose("al_vfx_baldur_kill", beam, BaldurWheel(1.4f, new Vector2(0.9f, 0.9f), 0.8f, 0.95f, 0.35f), flecks);
        }

        // A glint and six short rays.
        public static GameObject BaldurProc() => Compose("al_vfx_baldur_proc",
            new Layer {
                Name = "Glint", Material = Glint, Burst = 1, Max = 1, Life = new Vector2(0.3f, 0.3f), Size = new Vector2(0.5f, 0.5f),
                RandomRotation = true, ColorA = BaldurWhite, ColorB = BaldurWhite, OverEnd = BaldurSun, FadeIn = 0.05f, FadeOut = 0.7f,
            },
            BaldurRays(6, new Vector2(0.25f, 0.3f), new Vector2(2.5f, 3f), 5f));

        // The sun-wheel opens out over the ground to the power's reach (5 m at scale 1), rays running out with it.
        public static GameObject BaldurBurst()
        {
            Layer rays = BaldurRays(16, new Vector2(0.7f, 0.8f), new Vector2(5f, 5.5f), 8f);
            rays.Shape = ParticleSystemShapeType.Circle;
            rays.ShapeRotation = new Vector3(90f, 0f, 0f);
            rays.Spread = true;
            rays.Thickness = 0f;
            rays.Drag = 0.3f;
            rays.Offset = new Vector3(0f, 0.4f, 0f);
            // The wheel's ring sits at 0.62 of its radius, so 16 m across puts it at the 5 m reach.
            return Compose("al_vfx_baldur_burst", BaldurWheel(16f, new Vector2(0.9f, 0.9f), 0.7f, 0.4f, 1f), rays);
        }

        // ---- Skaði: winter, the hunt and the mountains. Snow and frost, hard-edged and falling: six-armed snowflakes
        // that fall and turn, a breath of frost mist and a flash of cold at the moment of impact. Ice blue, white and
        // steel. Set at about half the weight of her old Piloto effects. ----

        private static readonly Color SkadiIce = Hex("#8FD3FF");
        private static readonly Color SkadiFrost = Hex("#CFEFFF");

        // Snowflakes, thrown up or drifting, that fall and turn.
        private static Layer SkadiFlakes(int burst, float rate, Vector2 life, Vector2 speed, float size) => new Layer {
            Name = "Flakes", Material = Snowflake,
            Burst = burst, Rate = rate, Max = 60, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(size * 0.7f, size), Speed = speed, Gravity = 0.04f, Drag = 1f,
            RandomRotation = true, Spin = 0.15f, Noise = 0.1f,
            Shape = ParticleSystemShapeType.Cone, Angle = 40f, Radius = 0.1f, ShapeRotation = Up,
            ColorA = Color.white, ColorB = SkadiFrost, OverEnd = SkadiIce, FadeIn = 0.1f, FadeOut = 0.4f, Grow = 0.1f,
        };

        // Frost mist rolling low.
        private static Layer SkadiMist(int burst, float rate, float size, Vector2 life, float alpha) => new Layer {
            Name = "Mist", Material = Mist,
            Burst = burst, Rate = rate, Max = 30, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(size * 0.8f, size), Speed = new Vector2(0f, 0.1f), RandomRotation = true, Spin = 0.05f,
            Shape = ParticleSystemShapeType.Sphere, Radius = 0.1f, Offset = new Vector3(0f, 0.15f, 0f),
            ColorA = Color.white, ColorB = SkadiFrost, OverEnd = SkadiFrost, Alpha = alpha, FadeIn = 0.2f, FadeOut = 0.6f, Grow = 0.3f,
        };

        // A flash of cold light.
        private static Layer SkadiFlash(float size, float alpha, float height, float life) => new Layer {
            Name = "Flash", Material = Glow, Burst = 1, Max = 1, Life = new Vector2(life, life), Size = new Vector2(size, size),
            Offset = new Vector3(0f, height, 0f),
            ColorA = SkadiFrost, ColorB = SkadiFrost, OverEnd = SkadiIce, Alpha = alpha, FadeIn = 0.05f, FadeOut = 0.8f,
        };

        // Each step kicks up snow in a puff of frost and a small flash of cold.
        public static GameObject SkadiFootstep()
        {
            Layer flakes = SkadiFlakes(12, 0f, new Vector2(0.9f, 1.3f), new Vector2(0.6f, 1.2f), 0.1f);
            flakes.Gravity = 0.06f;
            return Compose("al_vfx_skaldi_footstep",
                flakes,
                SkadiMist(1, 0f, 0.6f, new Vector2(0.9f, 0.9f), 0.35f),
                SkadiFlash(0.5f, 0.45f, 0.1f, 0.25f));
        }

        // Snow appears at random round the top half of her body and falls, and frost mist rolls along the ground. Both
        // are left in the world as she walks on.
        public static GameObject SkadiAura()
        {
            Layer flakes = SkadiFlakes(0, 9f, new Vector2(2.5f, 3.5f), new Vector2(0f, 0.05f), 0.13f);
            flakes.Shape = ParticleSystemShapeType.Sphere;
            flakes.Radius = 0.6f;
            flakes.Thickness = 0.5f;
            flakes.Offset = new Vector3(0f, 1.35f, 0f);
            flakes.RatePerMetre = 2f;
            Layer mist = SkadiMist(0, 1.2f, 1.2f, new Vector2(2.5f, 3.5f), 0.22f);
            mist.Shape = ParticleSystemShapeType.Circle;
            mist.ShapeRotation = new Vector3(90f, 0f, 0f);
            mist.Radius = 1f;
            mist.Thickness = 0.4f;
            return Compose("al_vfx_skaldi_aura", flakes, mist);
        }

        // A flash of cold over the fallen, snow thrown up and falling, and frost mist rolling out.
        public static GameObject SkadiKill()
        {
            Layer flakes = SkadiFlakes(18, 0f, new Vector2(1.4f, 2f), new Vector2(1.8f, 3.5f), 0.14f);
            flakes.Offset = new Vector3(0f, 0.6f, 0f);
            flakes.Angle = 60f;
            flakes.Gravity = 0.15f;
            flakes.Drag = 1.2f;
            Layer mist = SkadiMist(6, 0f, 2f, new Vector2(1.3f, 1.6f), 0.45f);
            mist.Shape = ParticleSystemShapeType.Circle;
            mist.ShapeRotation = new Vector3(90f, 0f, 0f);
            mist.Radius = 0.4f;
            mist.Speed = new Vector2(0.8f, 1.3f);
            mist.Drag = 1f;
            return Compose("al_vfx_skaldi_kill", flakes, mist, SkadiFlash(2.2f, 0.5f, 0.6f, 0.35f));
        }

        // A flash of cold and one large snowflake spinning as ten small ones scatter.
        public static GameObject SkadiProc()
        {
            Layer flake = SkadiFlakes(1, 0f, new Vector2(0.5f, 0.5f), Vector2.zero, 0.4f);
            flake.Name = "Flake";
            flake.Spin = 0.5f;
            flake.Gravity = 0f;
            Layer scatter = SkadiFlakes(10, 0f, new Vector2(0.6f, 0.9f), new Vector2(1.2f, 2f), 0.09f);
            scatter.Name = "Scatter";
            scatter.Shape = ParticleSystemShapeType.Sphere;
            scatter.Drag = 2.5f;
            return Compose("al_vfx_skaldi_proc", flake, scatter, SkadiFlash(0.7f, 0.5f, 0f, 0.25f));
        }

        // Snow swirls out in a spiral to the power's reach (5 m at scale 1) over a rolling wave of frost mist, and frost
        // patterns spread across the ground behind it.
        public static GameObject SkadiBurst()
        {
            Layer wave = SkadiMist(24, 0f, 1.2f, new Vector2(1f, 1.1f), 0.3f);
            wave.Name = "Wave";
            wave.Shape = ParticleSystemShapeType.Circle;
            wave.ShapeRotation = new Vector3(90f, 0f, 0f);
            wave.Radius = 0.3f;
            wave.Thickness = 0f;
            wave.Spread = true;
            wave.Speed = new Vector2(4.5f, 4.5f);
            wave.Drag = 0.08f;
            Layer swirl = SkadiFlakes(48, 0f, new Vector2(1.1f, 1.3f), new Vector2(3.5f, 4.5f), 0.18f);
            swirl.Name = "Swirl";
            swirl.Shape = ParticleSystemShapeType.Circle;
            swirl.ShapeRotation = new Vector3(90f, 0f, 0f);
            swirl.Radius = 0.3f;
            swirl.Thickness = 0.2f;
            swirl.Spread = true;
            swirl.Orbit = 0.35f;
            swirl.Spin = 0.4f;
            swirl.Gravity = 0.02f;
            swirl.Drag = 0.1f;
            swirl.Offset = new Vector3(0f, 0.5f, 0f);
            Layer frost = SkadiFlakes(10, 0f, new Vector2(1.6f, 2f), Vector2.zero, 1.4f);
            frost.Name = "Frost";
            frost.Render = ParticleSystemRenderMode.HorizontalBillboard;
            frost.Shape = ParticleSystemShapeType.Circle;
            frost.ShapeRotation = new Vector3(90f, 0f, 0f);
            frost.Radius = 4f;
            frost.Thickness = 1f;
            frost.Offset = new Vector3(0f, 0.03f, 0f);
            frost.Size = new Vector2(0.8f, 1.4f);
            frost.Gravity = 0f;
            frost.Noise = 0f;
            frost.Spin = 0f;
            frost.Alpha = 0.6f;
            frost.Grow = 0.25f;
            frost.Delay = 0.1f;
            return Compose("al_vfx_skaldi_burst", wave, swirl, frost);
        }

        // ---- Syn: the gatekeeper who refuses entry. The warden's ward: hexagonal ward plates of bronze light, embers
        // from the hall fire she guards, and chips of stone knocked up by a step that will not be moved. Bronze, ember
        // red and stone, at about half the weight of her old Piloto effects. ----

        private static readonly Color SynBronze = Hex("#B07A3A");
        private static readonly Color SynEmber = Hex("#C0392B");
        private static readonly Color SynGlow = Hex("#FFD9A0");
        private static readonly Color SynFire = Hex("#FF8A3D");
        private static readonly Color SynStone = Hex("#8C8274");

        // Ward plates flickering up and fading.
        private static Layer SynWards(int burst, float rate, Vector2 life, Vector2 size, float alpha) => new Layer {
            Name = "Wards", Material = Ward,
            Burst = burst, Rate = rate, Max = 40, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = size,
            ColorA = SynGlow, ColorB = Color.Lerp(SynGlow, SynBronze, 0.5f), OverEnd = SynBronze,
            Alpha = alpha, FadeIn = 0.1f, FadeOut = 0.5f, Grow = 0.15f,
        };

        // Ward plates lighting up flat in the ground, like the floor of her hall, out to the radius.
        private static Layer SynFloor(float rate, float radius, Vector2 size, float alpha)
        {
            Layer floor = SynWards(0, rate, new Vector2(1.4f, 2f), size, alpha);
            floor.Name = "Floor";
            floor.Render = ParticleSystemRenderMode.HorizontalBillboard;
            floor.Shape = ParticleSystemShapeType.Circle;
            floor.ShapeRotation = new Vector3(90f, 0f, 0f);
            floor.Radius = radius;
            floor.Offset = new Vector3(0f, 0.04f, 0f);
            floor.FadeIn = 0.3f;
            floor.Grow = 0f;
            return floor;
        }

        // Embers rising and flickering, short tails behind them.
        private static Layer SynEmbers(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Embers", Material = Glow,
            Burst = burst, Rate = rate, Max = 60, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.04f, 0.07f), Speed = speed, Gravity = -0.05f, Noise = 0.6f,
            Shape = ParticleSystemShapeType.Cone, Angle = 25f, Radius = 0.15f, ShapeRotation = Up,
            ColorA = SynGlow, ColorB = SynFire, OverEnd = SynEmber, FadeIn = 0.05f, FadeOut = 0.5f,
            Trail = 0.25f, TrailWidth = 0.02f, TrailAlpha = 0.7f, TrailEnd = SynEmber,
        };

        // Chips of stone knocked up and falling back.
        private static Layer SynStones(int burst, Vector2 life, Vector2 speed, Vector2 size) => new Layer {
            Name = "Stones", Material = Stone,
            Burst = burst, Max = burst, Life = life, Size = size, Speed = speed, Gravity = 0.6f, RandomRotation = true, Spin = 1.2f,
            Shape = ParticleSystemShapeType.Cone, Angle = 35f, Radius = 0.12f, ShapeRotation = Up,
            ColorA = SynStone, ColorB = Color.Lerp(SynStone, SynBronze, 0.4f), OverEnd = SynStone, FadeIn = 0.02f, FadeOut = 0.3f,
        };

        // Dust thrown up off the stone.
        private static Layer SynDust(int burst, float size, Vector2 life, float alpha) => new Layer {
            Name = "Dust", Material = Mist,
            Burst = burst, Max = Mathf.Max(1, burst), Life = life, Size = new Vector2(size * 0.8f, size), Speed = new Vector2(0.1f, 0.3f),
            RandomRotation = true, Shape = ParticleSystemShapeType.Sphere, Radius = 0.1f, Offset = new Vector3(0f, 0.1f, 0f),
            ColorA = Color.Lerp(SynStone, Color.white, 0.4f), ColorB = Color.Lerp(SynStone, SynBronze, 0.3f), OverEnd = SynStone,
            Alpha = alpha, FadeIn = 0.1f, FadeOut = 0.6f, Grow = 0.4f,
        };

        private static Layer SynFlash(float size, float alpha, float height, float life) => new Layer {
            Name = "Flash", Material = Glow, Burst = 1, Max = 1, Life = new Vector2(life, life), Size = new Vector2(size, size),
            Offset = new Vector3(0f, height, 0f),
            ColorA = SynGlow, ColorB = SynGlow, OverEnd = SynBronze, Alpha = alpha, FadeIn = 0.05f, FadeOut = 0.8f,
        };

        // Each step lands like a stone set in place: a ward plate pressed into the ground under the foot, chips knocked
        // up in a puff of dust and a few embers lifting.
        public static GameObject SynFootstep()
        {
            Layer stamp = SynWards(1, 0f, new Vector2(0.8f, 0.8f), new Vector2(0.42f, 0.42f), 0.6f);
            stamp.Name = "Stamp";
            stamp.Max = 1;
            stamp.FaceLayer = true;
            stamp.Rotation = new Vector3(90f, 0f, 0f);
            stamp.Offset = new Vector3(0f, 0.03f, 0f);
            stamp.FadeIn = 0.05f;
            stamp.FadeOut = 0.6f;
            stamp.Grow = 0.1f;
            return Compose("al_vfx_syn_footstep",
                stamp,
                SynStones(8, new Vector2(0.45f, 0.6f), new Vector2(1.2f, 2f), new Vector2(0.05f, 0.09f)),
                SynDust(1, 0.8f, new Vector2(0.8f, 0.8f), 0.55f),
                SynEmbers(4, 0f, new Vector2(0.5f, 0.8f), new Vector2(0.4f, 0.9f)));
        }

        // Ward plates light up flat in the ground round her feet and fade, and embers rise from among them. Both are
        // left in the world as she walks on, so moving lays a short path of plates behind her.
        public static GameObject SynAura()
        {
            Layer floor = SynFloor(3f, 1.1f, new Vector2(0.5f, 0.65f), 0.5f);
            floor.Thickness = 0.6f;
            floor.RatePerMetre = 1f;
            Layer embers = SynEmbers(0, 4f, new Vector2(0.8f, 1.3f), new Vector2(0.3f, 0.7f));
            embers.Shape = ParticleSystemShapeType.Circle;
            embers.ShapeRotation = new Vector3(90f, 0f, 0f);
            embers.Radius = 0.9f;
            embers.Thickness = 0.5f;
            embers.Offset = new Vector3(0f, 0.1f, 0f);
            return Compose("al_vfx_syn_aura", floor, embers);
        }

        // The door shuts: a ring of ward plates rises round the fallen like a shield wall, stone chips burst in a puff
        // of dust and embers lift.
        public static GameObject SynKill()
        {
            Layer wards = SynWards(8, 0f, new Vector2(1f, 1.1f), new Vector2(0.5f, 0.6f), 0.35f);
            wards.Shape = ParticleSystemShapeType.Circle;
            wards.ShapeRotation = new Vector3(90f, 0f, 0f);
            wards.Radius = 0.9f;
            wards.Thickness = 0f;
            wards.Spread = true;
            wards.Gravity = -0.1f;
            wards.Offset = new Vector3(0f, 0.5f, 0f);
            Layer embers = SynEmbers(14, 0f, new Vector2(0.7f, 1.1f), new Vector2(0.8f, 1.6f));
            embers.Angle = 45f;
            embers.Drag = 1f;
            embers.Offset = new Vector3(0f, 0.3f, 0f);
            Layer dust = SynDust(4, 1.1f, new Vector2(1f, 1.4f), 0.35f);
            dust.Radius = 0.4f;
            return Compose("al_vfx_syn_kill",
                wards,
                SynStones(14, new Vector2(0.6f, 0.9f), new Vector2(2f, 3.5f), new Vector2(0.07f, 0.14f)),
                dust,
                embers,
                SynFlash(2f, 0.5f, 0.5f, 0.35f));
        }

        // A ward plate flashes where the blow landed in a puff of dust as embers scatter.
        public static GameObject SynProc()
        {
            Layer ward = SynWards(1, 0f, new Vector2(0.4f, 0.4f), new Vector2(0.65f, 0.65f), 0.85f);
            ward.Name = "Ward";
            ward.FadeIn = 0.05f;
            ward.FadeOut = 0.6f;
            Layer embers = SynEmbers(10, 0f, new Vector2(0.4f, 0.6f), new Vector2(1.5f, 2.5f));
            embers.Shape = ParticleSystemShapeType.Sphere;
            embers.Drag = 2f;
            return Compose("al_vfx_syn_proc", ward, SynDust(2, 0.8f, new Vector2(0.7f, 0.9f), 0.3f), embers);
        }

        // A shield wall pushes out: a ring of ward plates rolls out to the power's reach (5 m at scale 1), embers are
        // thrown up across the ground and stone bursts from the centre.
        public static GameObject SynBurst()
        {
            Layer wall = SynWards(20, 0f, new Vector2(1f, 1f), new Vector2(0.6f, 0.7f), 0.6f);
            wall.Name = "Wall";
            wall.Shape = ParticleSystemShapeType.Circle;
            wall.ShapeRotation = new Vector3(90f, 0f, 0f);
            wall.Radius = 0.4f;
            wall.Thickness = 0f;
            wall.Spread = true;
            wall.Speed = new Vector2(4.5f, 4.5f);
            wall.Drag = 0.08f;
            wall.Offset = new Vector3(0f, 0.6f, 0f);
            wall.FadeIn = 0.05f;
            Layer embers = SynEmbers(30, 0f, new Vector2(0.8f, 1.2f), new Vector2(1f, 2.5f));
            embers.Radius = 3f;
            return Compose("al_vfx_syn_burst", wall, embers,
                SynStones(16, new Vector2(0.6f, 0.9f), new Vector2(2.5f, 4f), new Vector2(0.08f, 0.15f)));
        }

        // The rune dome of Doors of Valhalla, while the player blocks: ward plates flicker over a dome 1.25 m out at
        // scale 1 (the mod scales it to the dome's reach), more light flat in the ground it covers, and embers rise round
        // its edge.
        public static GameObject SynDome()
        {
            Layer wards = SynWards(0, 12f, new Vector2(1f, 1.4f), new Vector2(0.35f, 0.45f), 0.4f);
            wards.Shape = ParticleSystemShapeType.Hemisphere;
            wards.ShapeRotation = Up;
            wards.Radius = 1.25f;
            wards.Thickness = 0f;
            Layer floor = SynFloor(5f, 1.2f, new Vector2(0.4f, 0.5f), 0.3f);
            Layer embers = SynEmbers(0, 6f, new Vector2(0.8f, 1.2f), new Vector2(0.3f, 0.7f));
            embers.Shape = ParticleSystemShapeType.Circle;
            embers.ShapeRotation = new Vector3(90f, 0f, 0f);
            embers.Radius = 1.25f;
            embers.Thickness = 0f;
            return Compose("al_vfx_syn_dome", wards, floor, embers);
        }

        // ---- Freyja: the seiðkona in her falcon cloak, who chooses half the slain. Feathers shed from the cloak, tears
        // of red gold, and violet motes of seiðr. Rose, amber gold and violet. ----

        private static readonly Color FreyjaRose = Hex("#E86A9A");
        private static readonly Color FreyjaBlush = Hex("#FFD8E6");
        private static readonly Color FreyjaAmber = Hex("#F2B33D");
        private static readonly Color FreyjaRedGold = Hex("#E5803A");
        private static readonly Color FreyjaViolet = Hex("#C9A0FF");

        // Feathers from her cloak, each one of Piloto's four drawn as rose light (alpha blended, its grey shading
        // dulled them to petals), rocking and drifting down as feathers fall.
        private static Layer FreyjaFeathers(int burst, float rate, Vector2 life, Vector2 size) => new Layer {
            Name = "Feathers", Material = Shine("FeathersPickbook.png"), Tiles = 2,
            Burst = burst, Rate = rate, Max = 40, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = size, Speed = new Vector2(0.2f, 0.5f), Gravity = 0.08f, Drag = 1.5f, Noise = 0.3f,
            RandomRotation = true, Spin = 0.25f,
            ColorA = Color.Lerp(FreyjaBlush, FreyjaRose, 0.35f), ColorB = FreyjaRose, OverEnd = Color.Lerp(FreyjaRose, FreyjaViolet, 0.6f),
            FadeIn = 0.1f, FadeOut = 0.4f,
        };

        // Tears of red gold: Piloto's droplets drawn as amber light, falling.
        private static Layer FreyjaTears(int burst, Vector2 life, Vector2 speed, Vector2 size) => new Layer {
            Name = "Tears", Material = Shine("Droplets_Pickbook.png"), Tiles = 2,
            Burst = burst, Max = Mathf.Max(1, burst), Life = life, Size = size, Speed = speed, Gravity = 0.5f, Drag = 1f,
            RandomRotation = true, Shape = ParticleSystemShapeType.Cone, Angle = 35f, Radius = 0.1f, ShapeRotation = Up,
            ColorA = FreyjaAmber, ColorB = FreyjaRedGold, OverEnd = FreyjaRedGold, FadeIn = 0.05f, FadeOut = 0.4f,
        };

        // Motes of seiðr: soft violet and rose lights drifting.
        private static Layer FreyjaMotes(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Motes", Material = Glow,
            Burst = burst, Rate = rate, Max = 40, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.09f, 0.16f), Speed = speed, Noise = 0.25f,
            Shape = ParticleSystemShapeType.Cone, Angle = 20f, Radius = 0.1f, ShapeRotation = Up,
            ColorA = FreyjaViolet, ColorB = Color.Lerp(FreyjaViolet, FreyjaRose, 0.5f), OverEnd = FreyjaViolet,
            FadeIn = 0.2f, FadeOut = 0.5f,
        };

        private static Layer FreyjaGlow(float size, float alpha, float height, float life) => new Layer {
            Name = "Glow", Material = Glow, Burst = 1, Max = 1, Life = new Vector2(life, life), Size = new Vector2(size, size),
            Offset = new Vector3(0f, height, 0f),
            ColorA = FreyjaBlush, ColorB = FreyjaRose, OverEnd = FreyjaViolet, Alpha = alpha, FadeIn = 0.05f, FadeOut = 0.7f,
        };

        // She treads lightly: a feather lies where her foot fell, two more lift off and drift down, and a soft rose glow.
        public static GameObject FreyjaFootstep()
        {
            Layer laid = FreyjaFeathers(1, 0f, new Vector2(0.9f, 0.9f), new Vector2(0.38f, 0.38f));
            laid.Name = "Laid";
            laid.Max = 1;
            laid.FaceLayer = true;
            laid.Rotation = new Vector3(90f, 0f, 0f);
            laid.Offset = new Vector3(0f, 0.03f, 0f);
            laid.Speed = Vector2.zero;
            laid.Gravity = 0f;
            laid.Noise = 0f;
            laid.Spin = 0f;
            laid.FadeIn = 0.05f;
            laid.FadeOut = 0.5f;
            Layer lifted = FreyjaFeathers(2, 0f, new Vector2(0.9f, 1.1f), new Vector2(0.12f, 0.16f));
            lifted.Speed = new Vector2(0.8f, 1.2f);
            lifted.Shape = ParticleSystemShapeType.Cone;
            lifted.Angle = 35f;
            lifted.ShapeRotation = Up;
            lifted.Drag = 2.5f;
            return Compose("al_vfx_freya_footstep", laid, lifted, FreyjaGlow(0.55f, 0.4f, 0.05f, 0.5f));
        }

        // Now and then a feather falls from round her shoulders and drifts to the ground, and motes of seiðr rise low
        // round her feet. Both are left in the world as she walks on.
        public static GameObject FreyjaAura()
        {
            Layer feathers = FreyjaFeathers(0, 1.4f, new Vector2(2.5f, 3.2f), new Vector2(0.16f, 0.24f));
            feathers.Shape = ParticleSystemShapeType.Circle;
            feathers.ShapeRotation = new Vector3(90f, 0f, 0f);
            feathers.Radius = 0.7f;
            feathers.Thickness = 0.5f;
            feathers.Offset = new Vector3(0f, 1.5f, 0f);
            feathers.Speed = new Vector2(0.05f, 0.2f);
            feathers.RatePerMetre = 0.4f;
            Layer motes = FreyjaMotes(0, 5f, new Vector2(1.5f, 2.5f), new Vector2(0.15f, 0.35f));
            motes.Shape = ParticleSystemShapeType.Circle;
            motes.ShapeRotation = new Vector3(90f, 0f, 0f);
            motes.Radius = 0.9f;
            motes.Thickness = 0.5f;
            motes.Offset = new Vector3(0f, 0.1f, 0f);
            motes.Gravity = -0.02f;
            return Compose("al_vfx_freya_aura", feathers, motes);
        }

        // The chooser of the slain: feathers burst up from the fallen as from a falcon taking wing and drift down, a
        // rose light rises from the body to Fólkvangr, motes of seiðr lift round it and tears of red gold fall.
        public static GameObject FreyjaKill()
        {
            Layer feathers = FreyjaFeathers(14, 0f, new Vector2(1.6f, 2.2f), new Vector2(0.22f, 0.32f));
            feathers.Speed = new Vector2(2f, 3.2f);
            feathers.Shape = ParticleSystemShapeType.Cone;
            feathers.Angle = 55f;
            feathers.Radius = 0.4f;
            feathers.ShapeRotation = Up;
            feathers.Drag = 2.5f;
            feathers.Offset = new Vector3(0f, 0.3f, 0f);
            Layer chosen = FreyjaMotes(1, 0f, new Vector2(1.3f, 1.3f), new Vector2(1.3f, 1.3f));
            chosen.Name = "Chosen";
            chosen.Size = new Vector2(0.3f, 0.3f);
            chosen.Angle = 0f;
            chosen.Noise = 0f;
            chosen.ColorA = FreyjaBlush;
            chosen.ColorB = FreyjaRose;
            chosen.Alpha = 0.85f;
            chosen.Offset = new Vector3(0f, 0.4f, 0f);
            chosen.Trail = 0.3f;
            chosen.TrailWidth = 0.1f;
            chosen.TrailAlpha = 0.5f;
            chosen.TrailEnd = FreyjaViolet;
            Layer motes = FreyjaMotes(10, 0f, new Vector2(1.2f, 1.8f), new Vector2(0.3f, 0.7f));
            motes.Shape = ParticleSystemShapeType.Circle;
            motes.ShapeRotation = new Vector3(90f, 0f, 0f);
            motes.Radius = 0.9f;
            motes.Gravity = -0.05f;
            motes.Offset = new Vector3(0f, 0.2f, 0f);
            Layer tears = FreyjaTears(8, new Vector2(0.8f, 1.1f), new Vector2(1.5f, 2.5f), new Vector2(0.09f, 0.13f));
            tears.Offset = new Vector3(0f, 0.4f, 0f);
            return Compose("al_vfx_freya_kill", feathers, chosen, motes, tears, FreyjaGlow(2f, 0.45f, 0.4f, 0.5f));
        }

        // Where a power strikes: a few feathers flung out from the blow, tears of red gold and a rose glow.
        public static GameObject FreyjaProc()
        {
            Layer feathers = FreyjaFeathers(6, 0f, new Vector2(0.9f, 1.2f), new Vector2(0.16f, 0.22f));
            feathers.Speed = new Vector2(1.5f, 2.5f);
            feathers.Shape = ParticleSystemShapeType.Sphere;
            feathers.Radius = 0.1f;
            feathers.Drag = 3f;
            Layer tears = FreyjaTears(4, new Vector2(0.6f, 0.8f), new Vector2(1f, 1.8f), new Vector2(0.06f, 0.09f));
            tears.Shape = ParticleSystemShapeType.Sphere;
            return Compose("al_vfx_freya_proc", feathers, tears, FreyjaGlow(0.7f, 0.5f, 0f, 0.35f));
        }

        // A beat of the falcon cloak: a ring of feathers sweeps out across the ground to the power's reach (5 m at scale
        // 1), tears of red gold are thrown up and motes of seiðr rise.
        public static GameObject FreyjaBurst()
        {
            Layer wing = FreyjaFeathers(24, 0f, new Vector2(1.2f, 1.4f), new Vector2(0.22f, 0.3f));
            wing.Name = "Wingbeat";
            wing.Shape = ParticleSystemShapeType.Circle;
            wing.ShapeRotation = new Vector3(90f, 0f, 0f);
            wing.Radius = 0.4f;
            wing.Thickness = 0f;
            wing.Spread = true;
            wing.Speed = new Vector2(5f, 5.5f);
            wing.Drag = 0.6f;
            wing.Gravity = 0f;
            wing.Offset = new Vector3(0f, 0.5f, 0f);
            Layer tears = FreyjaTears(16, new Vector2(0.9f, 1.2f), new Vector2(2.5f, 4f), new Vector2(0.08f, 0.12f));
            tears.Radius = 1.5f;
            Layer motes = FreyjaMotes(20, 0f, new Vector2(1.2f, 1.8f), new Vector2(0.4f, 0.9f));
            motes.Shape = ParticleSystemShapeType.Circle;
            motes.ShapeRotation = new Vector3(90f, 0f, 0f);
            motes.Radius = 3f;
            return Compose("al_vfx_freya_burst", wing, tears, motes);
        }

        // ---- Hel: the queen of the dead, half living and half dead. Shadow: dark smoke curling low over the ground,
        // wisps coiling up out of it trailing dark ribbons, and ash. Near-black with a cast of corpse blue, and only a
        // few faint souls for light. Piloto's void tentacles were tried for shadows reaching up, but read as black grass. ----

        private static readonly Color HelShadow = Hex("#07080B");
        private static readonly Color HelDusk = Hex("#1A2130");
        private static readonly Color HelBone = Hex("#E8E2D0");
        private static readonly Color HelAshDark = Hex("#2A2C33");
        private static readonly Color HelSoul = Hex("#CFE8FF");
        private static readonly Color HelCorpse = Hex("#5A7FA8");

        // Dark smoke: Piloto's wispy smoke sheet played through as the puff lives, so each one curls and frays.
        private static Layer HelSmoke(int burst, float rate, Vector2 life, Vector2 size, float alpha) => new Layer {
            Name = "Smoke", Material = Sprite("tex_WispySmoke01_8x8.png"), Tiles = 8, Animate = true,
            Burst = burst, Rate = rate, Max = 40, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = size, Speed = new Vector2(0.1f, 0.3f), Gravity = -0.02f, Noise = 0.2f,
            RandomRotation = true, Spin = 0.1f, Shape = ParticleSystemShapeType.Sphere, Radius = 0.2f,
            ColorA = HelShadow, ColorB = HelDusk, OverEnd = HelShadow, Alpha = alpha, FadeIn = 0.15f, FadeOut = 0.5f, Grow = 0.3f,
        };

        // Wisps of shadow darting and curling, each drawing a ribbon of dark smoke behind it.
        private static Layer HelWisps(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Wisps", Material = Glow,
            Burst = burst, Rate = rate, Max = 20, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.02f, 0.03f), Speed = speed, Noise = 0.9f, Drag = 1.2f,
            Shape = ParticleSystemShapeType.Cone, Angle = 40f, Radius = 0.25f, ShapeRotation = Up,
            ColorA = HelShadow, ColorB = HelDusk, OverEnd = HelShadow, FadeIn = 0.1f, FadeOut = 0.4f,
            Trail = 0.6f, TrailWidth = 0.22f, TrailAlpha = 0.75f, TrailEnd = HelShadow, TrailMaterial = ShadowTrail,
        };

        // Flakes of ash, dark more often than pale, drifting slowly and turning.
        private static Layer HelAsh(int burst, float rate, Vector2 life, Vector2 size) => new Layer {
            Name = "Ash", Material = Ash,
            Burst = burst, Rate = rate, Max = 60, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = size, Speed = new Vector2(0.2f, 0.5f), Gravity = -0.02f, Drag = 1f, Noise = 0.35f,
            RandomRotation = true, Spin = 0.4f,
            ColorA = Color.Lerp(HelBone, HelAshDark, 0.45f), ColorB = HelShadow, OverEnd = HelAshDark, FadeIn = 0.1f, FadeOut = 0.5f,
        };

        // A few faint souls sinking into the dark.
        private static Layer HelSouls(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Souls", Material = Glow,
            Burst = burst, Rate = rate, Max = 20, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.06f, 0.1f), Speed = speed, Gravity = 0.4f, Noise = 0.2f,
            Shape = ParticleSystemShapeType.Cone, Angle = 25f, Radius = 0.2f, ShapeRotation = Up,
            ColorA = HelSoul, ColorB = HelCorpse, OverEnd = HelCorpse, Alpha = 0.6f, FadeIn = 0.15f, FadeOut = 0.4f,
            Trail = 0.3f, TrailWidth = 0.03f, TrailAlpha = 0.4f, TrailEnd = HelCorpse,
        };

        // Shadow curls up from each step and frays, and a few flakes of ash lift with it.
        public static GameObject HelFootstep()
        {
            Layer smoke = HelSmoke(2, 0f, new Vector2(0.9f, 1.1f), new Vector2(0.45f, 0.6f), 0.8f);
            smoke.Radius = 0.08f;
            smoke.Offset = new Vector3(0f, 0.25f, 0f);
            Layer ash = HelAsh(3, 0f, new Vector2(0.8f, 1.1f), new Vector2(0.06f, 0.09f));
            ash.Shape = ParticleSystemShapeType.Cone;
            ash.Angle = 35f;
            ash.Radius = 0.12f;
            ash.ShapeRotation = Up;
            ash.Speed = new Vector2(0.6f, 1f);
            ash.Drag = 2f;
            return Compose("al_vfx_hel_footstep", smoke, ash);
        }

        // Shadow creeps low round her feet, wisps of it coil up beside her trailing dark, and ash lifts. All are left in
        // the world as she walks on, so moving leaves a trail of shadow.
        public static GameObject HelAura()
        {
            Layer smoke = HelSmoke(0, 3.5f, new Vector2(1.6f, 2.2f), new Vector2(0.6f, 0.9f), 0.55f);
            smoke.Shape = ParticleSystemShapeType.Circle;
            smoke.ShapeRotation = new Vector3(90f, 0f, 0f);
            smoke.Radius = 0.7f;
            smoke.Offset = new Vector3(0f, 0.3f, 0f);
            smoke.Speed = new Vector2(0.05f, 0.15f);
            smoke.RatePerMetre = 1.2f;
            Layer wisps = HelWisps(0, 1.2f, new Vector2(1.4f, 1.8f), new Vector2(0.3f, 0.6f));
            wisps.Radius = 0.7f;
            wisps.Thickness = 0.4f;
            wisps.TrailWidth = 0.15f;
            wisps.TrailAlpha = 0.6f;
            wisps.Offset = new Vector3(0f, 0.1f, 0f);
            Layer ash = HelAsh(0, 4f, new Vector2(2f, 3f), new Vector2(0.08f, 0.13f));
            ash.Shape = ParticleSystemShapeType.Circle;
            ash.ShapeRotation = new Vector3(90f, 0f, 0f);
            ash.Radius = 0.9f;
            ash.Thickness = 0.5f;
            ash.Speed = new Vector2(0.05f, 0.15f);
            ash.Gravity = -0.03f;
            ash.Offset = new Vector3(0f, 0.1f, 0f);
            return Compose("al_vfx_hel_aura", smoke, wisps, ash);
        }

        // Her death mark: shadow billows up round the fallen and rolls out low, wisps coil up out of it trailing dark
        // ribbons, and a few faint souls sink into it.
        public static GameObject HelKill()
        {
            Layer smoke = HelSmoke(10, 0f, new Vector2(1.2f, 1.6f), new Vector2(0.8f, 1.1f), 0.52f);
            smoke.Shape = ParticleSystemShapeType.Circle;
            smoke.ShapeRotation = new Vector3(90f, 0f, 0f);
            smoke.Radius = 0.3f;
            smoke.Speed = new Vector2(0.8f, 1.2f);
            smoke.Drag = 1.2f;
            smoke.Offset = new Vector3(0f, 0.5f, 0f);
            Layer wisps = HelWisps(8, 0f, new Vector2(1.1f, 1.4f), new Vector2(1.6f, 2.4f));
            wisps.TrailWidth = 0.3f;
            wisps.TrailAlpha = 0.52f;
            wisps.Offset = new Vector3(0f, 0.3f, 0f);
            Layer souls = HelSouls(3, 0f, new Vector2(1f, 1.2f), new Vector2(1.4f, 1.8f));
            souls.Alpha = 0.42f;
            souls.Offset = new Vector3(0f, 0.4f, 0f);
            return Compose("al_vfx_hel_kill", smoke, wisps, souls);
        }

        // Where a power strikes: a puff of shadow bursts and frays, two wisps dart off trailing dark, and ash scatters.
        public static GameObject HelProc()
        {
            Layer smoke = HelSmoke(3, 0f, new Vector2(0.6f, 0.8f), new Vector2(0.4f, 0.55f), 0.75f);
            smoke.Speed = new Vector2(0.4f, 0.8f);
            smoke.Drag = 2f;
            Layer wisps = HelWisps(2, 0f, new Vector2(0.6f, 0.8f), new Vector2(1.5f, 2.5f));
            wisps.Shape = ParticleSystemShapeType.Sphere;
            wisps.Radius = 0.1f;
            Layer ash = HelAsh(6, 0f, new Vector2(0.7f, 1f), new Vector2(0.05f, 0.08f));
            ash.Speed = new Vector2(1f, 2f);
            ash.Drag = 2.5f;
            return Compose("al_vfx_hel_proc", smoke, wisps, ash);
        }

        // A wave of shadow rolls out low over the ground to the power's reach (5 m at scale 1), and wisps coil up from
        // it as it passes, trailing dark.
        public static GameObject HelBurst()
        {
            Layer wave = HelSmoke(24, 0f, new Vector2(1.1f, 1.4f), new Vector2(0.8f, 1.1f), 0.6f);
            wave.Name = "Wave";
            wave.Shape = ParticleSystemShapeType.Circle;
            wave.ShapeRotation = new Vector3(90f, 0f, 0f);
            wave.Radius = 0.4f;
            wave.Thickness = 0f;
            wave.Spread = true;
            wave.Speed = new Vector2(5f, 5.5f);
            wave.Drag = 0.6f;
            wave.Gravity = 0f;
            wave.Offset = new Vector3(0f, 0.45f, 0f);
            Layer wisps = HelWisps(10, 0f, new Vector2(1f, 1.3f), new Vector2(1.2f, 2f));
            wisps.Radius = 2.5f;
            wisps.Thickness = 0.3f;
            wisps.Delay = 0.2f;
            Layer ash = HelAsh(16, 0f, new Vector2(1.2f, 1.6f), new Vector2(0.08f, 0.13f));
            ash.Shape = ParticleSystemShapeType.Circle;
            ash.ShapeRotation = new Vector3(90f, 0f, 0f);
            ash.Radius = 2.5f;
            ash.Speed = new Vector2(0.5f, 1f);
            return Compose("al_vfx_hel_burst", wave, wisps, ash);
        }

        // ---- Víðarr: the silent god of the Deep North, its bitter cold and endless snow. Skaði has the falling snow,
        // so his is mystical frost: diamond dust (ice crystals hanging in air too cold to hold them, glittering as they
        // turn), curtains of the northern lights, and shards of ice. The teal sparkles of his first aura, made frostier.
        // Ice white and cyan, with the aurora's green and violet. ----

        private static readonly Color VidarrIce = Hex("#E6F7FF");
        private static readonly Color VidarrCyan = Hex("#9FE8FF");
        private static readonly Color VidarrTeal = Hex("#7FF0C8");
        private static readonly Color VidarrDeep = Hex("#3E7FA8");

        // Diamond dust: tiny ice crystals hanging in the air and turning, so they glitter.
        private static Layer VidarrDust(int burst, float rate, Vector2 life, Vector2 speed) => new Layer {
            Name = "Dust", Material = Diamond,
            Burst = burst, Rate = rate, Max = 80, Loop = rate > 0f, Duration = rate > 0f ? 5f : 1f,
            Life = life, Size = new Vector2(0.05f, 0.09f), Speed = speed, Drag = 1.5f, Noise = 0.15f,
            RandomRotation = true, Spin = 0.5f, Shape = ParticleSystemShapeType.Sphere, Radius = 0.2f,
            ColorA = VidarrIce, ColorB = VidarrCyan, OverEnd = VidarrTeal, FadeIn = 0.15f, FadeOut = 0.45f,
        };

        // Shards of ice, glinting as they tumble.
        private static Layer VidarrShards(int burst, Vector2 life, Vector2 speed, Vector2 size) => new Layer {
            Name = "Shards", Material = Shard,
            Burst = burst, Max = Mathf.Max(1, burst), Life = life, Size = size, Speed = speed, Gravity = 0.4f, Drag = 1.5f,
            RandomRotation = true, Spin = 0.8f,
            Shape = ParticleSystemShapeType.Cone, Angle = 40f, Radius = 0.15f, ShapeRotation = Up,
            ColorA = VidarrIce, ColorB = VidarrCyan, OverEnd = VidarrDeep, FadeIn = 0.03f, FadeOut = 0.35f,
        };

        // A ring of curtains of the northern lights standing up from the ground, teal at their foot and violet above,
        // each turned to face the centre with its widest side as it moves outward.
        private static Layer VidarrAurora(int burst, float radius, Vector2 speed, Vector2 life, Vector2 width, Vector2 height, float alpha) => new Layer {
            Name = "Aurora", Material = Aurora, PivotUp = 0.5f, FaceMotion = true,
            Burst = burst, Max = burst, Life = life, Size = width, Height = height, Speed = speed,
            Shape = ParticleSystemShapeType.Circle, ShapeRotation = new Vector3(90f, 0f, 0f), Radius = radius, Thickness = 0f, Spread = true,
            Alpha = alpha, FadeIn = 0.25f, FadeOut = 0.5f, Grow = 0.2f,
        };

        // A puff of diamond dust glitters up from each step in a breath of cold light and hangs a moment, and a shard of
        // ice flicks away.
        public static GameObject VidarrFootstep()
        {
            Layer dust = VidarrDust(8, 0f, new Vector2(0.7f, 1f), new Vector2(0.5f, 1f));
            dust.Shape = ParticleSystemShapeType.Cone;
            dust.Angle = 35f;
            dust.Radius = 0.1f;
            dust.ShapeRotation = Up;
            dust.Drag = 3f;
            return Compose("al_vfx_vidarr_footstep", dust,
                VidarrShards(1, new Vector2(0.5f, 0.6f), new Vector2(1f, 1.5f), new Vector2(0.08f, 0.1f)),
                VidarrFlash(0.5f, 0.4f, 0.1f, 0.45f));
        }

        private static Layer VidarrFlash(float size, float alpha, float height, float life) => new Layer {
            Name = "Flash", Material = Glow, Burst = 1, Max = 1, Life = new Vector2(life, life), Size = new Vector2(size, size),
            Offset = new Vector3(0f, height, 0f),
            ColorA = VidarrCyan, ColorB = VidarrIce, OverEnd = VidarrTeal, Alpha = alpha, FadeIn = 0.05f, FadeOut = 0.8f,
        };

        // Diamond dust hangs glittering in the air round him, out of reach of his body, and is left in the world as he
        // walks on.
        public static GameObject VidarrAura()
        {
            // A shell round the body rather than a ring, so the dust hangs at every height but never on him.
            Layer dust = VidarrDust(0, 12f, new Vector2(2f, 2.8f), new Vector2(0.02f, 0.08f));
            dust.Size = new Vector2(0.07f, 0.11f);
            dust.Radius = 1.1f;
            dust.Thickness = 0.4f;
            dust.Offset = new Vector3(0f, 0.9f, 0f);
            dust.Noise = 0.1f;
            dust.RatePerMetre = 2f;
            return Compose("al_vfx_vidarr_aura", dust);
        }

        // The cold claims the fallen: a few curtains of the northern lights flare up round it facing in, diamond dust
        // swirls up through them and shards of ice are thrown out glinting.
        public static GameObject VidarrKill()
        {
            Layer aurora = VidarrAurora(4, 0.8f, new Vector2(0.2f, 0.3f), new Vector2(1.1f, 1.3f), new Vector2(0.9f, 1.1f), new Vector2(1.2f, 1.6f), 0.6f);
            Layer dust = VidarrDust(30, 0f, new Vector2(1.1f, 1.5f), new Vector2(1.2f, 2f));
            dust.Shape = ParticleSystemShapeType.Cone;
            dust.Angle = 30f;
            dust.Radius = 0.6f;
            dust.ShapeRotation = Up;
            dust.Orbit = 0.35f;
            dust.Offset = new Vector3(0f, 0.2f, 0f);
            Layer shards = VidarrShards(6, new Vector2(0.7f, 0.9f), new Vector2(2f, 3f), new Vector2(0.1f, 0.16f));
            shards.Offset = new Vector3(0f, 0.4f, 0f);
            return Compose("al_vfx_vidarr_kill", aurora, dust, shards);
        }

        // Where a power strikes: a burst of diamond dust glitters out from the blow with a few shards of ice and a cold
        // flash.
        public static GameObject VidarrProc()
        {
            Layer dust = VidarrDust(14, 0f, new Vector2(0.6f, 0.9f), new Vector2(1f, 2f));
            dust.Drag = 3f;
            Layer shards = VidarrShards(3, new Vector2(0.5f, 0.7f), new Vector2(1.5f, 2.5f), new Vector2(0.08f, 0.12f));
            shards.Shape = ParticleSystemShapeType.Sphere;
            return Compose("al_vfx_vidarr_proc", dust, shards, VidarrFlash(0.6f, 0.6f, 0f, 0.3f));
        }

        // A ring of the northern lights, every curtain facing the player, sweeps out across the ground to the power's
        // reach (5 m at scale 1), diamond dust glittering up behind it. Enough curtains, wide enough, to stay a nearly
        // unbroken ring as it widens.
        public static GameObject VidarrBurst()
        {
            Layer aurora = VidarrAurora(22, 0.4f, new Vector2(4.5f, 4.5f), new Vector2(1f, 1.2f), new Vector2(1.4f, 1.7f), new Vector2(1.1f, 1.4f), 0.55f);
            aurora.Drag = 0.08f;
            Layer dust = VidarrDust(40, 0f, new Vector2(1f, 1.4f), new Vector2(0.5f, 1.2f));
            dust.Shape = ParticleSystemShapeType.Cone;
            dust.Angle = 20f;
            dust.Radius = 3f;
            dust.ShapeRotation = Up;
            dust.Delay = 0.15f;
            return Compose("al_vfx_vidarr_burst", aurora, dust);
        }
    }
}
