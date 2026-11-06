using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.ui;
using static AesirLineage.common.Deities;
using Object = UnityEngine.Object;

namespace AesirLineage.visuals {
    /// <summary>What an effect is for. Each deity has its own prefab per role where the bundle has one.</summary>
    internal enum VfxRole {
        SoulOrb,
        SoulGround,
        Aura,
        Wings,
        Footstep,
        Kill,
        Proc,
        Burst,
        Harvest,
        LevelUp,
        Dome,
    }

    /// <summary>
    /// Each deity's effects, built from the Piloto Studio pack in the Unity project
    /// (<c>Assets/AesirLineage/Editor/VfxBuilder.cs</c>) and loaded from the bundle as
    /// <c>al_vfx_&lt;deity&gt;_&lt;role&gt;</c>. A missing prefab falls back to a burst drawn in the deity's
    /// colours, so a server's custom maps or a stale bundle cost looks, never errors.
    /// </summary>
    internal static class DeityVfx {
        private const string RpcName = "AL_Vfx";
        // Shared effects only play for players this close.
        private const float ShareRange = 80f;
        // One-shots are removed after their particles are done, within these bounds.
        private const float MinLifetime = 0.5f;
        private const float MaxLifetime = 8f;

        private static readonly Dictionary<(Deity, VfxRole), GameObject> prefabs = new Dictionary<(Deity, VfxRole), GameObject>();
        private static readonly Dictionary<GameObject, float> lifetimes = new Dictionary<GameObject, float>();

        internal static Shader UIAdditive { get; private set; }
        internal static Shader UILinkFlow { get; private set; }
        internal static Shader WorldAdditive { get; private set; }

        internal static void Load() {
            // A dedicated server draws nothing, and there every shader would report itself unsupported.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) {
                Logger.LogDebug("No graphics device: deity effects are not loaded.");
                return;
            }
            AssetBundle bundle = AesirLineage.EmbeddedResourceBundle;
            UIAdditive = bundle.LoadAsset<Shader>("AL_UIAdditive");
            UILinkFlow = bundle.LoadAsset<Shader>("AL_UILinkFlow");
            WorldAdditive = bundle.LoadAsset<Shader>("AL_WorldAdditive");
            int loaded = 0;
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                foreach (VfxRole role in Enum.GetValues(typeof(VfxRole))) {
                    GameObject prefab = bundle.LoadAsset<GameObject>(AssetName(deity, role));
                    if (prefab == null) { continue; }
                    CheckShaders(prefab);
                    prefabs[(deity, role)] = prefab;
                    lifetimes[prefab] = Lifetime(prefab);
                    loaded++;
                }
            }
            Logger.LogInfo($"Loaded {loaded} deity effects.");
        }

        internal static string AssetName(Deity deity, VfxRole role) {
            string name = role switch {
                VfxRole.SoulOrb => "soul_orb",
                VfxRole.SoulGround => "soul_ground",
                _ => role.ToString().ToLowerInvariant(),
            };
            return $"al_vfx_{deity.ToString().ToLowerInvariant()}_{name}";
        }

        internal static bool Has(Deity deity, VfxRole role) => prefabs.ContainsKey((deity, role));

        /// <summary>A lasting effect parented to something: an aura, wings, the soul's body. Null if the deity has none.</summary>
        internal static GameObject Attach(Deity deity, VfxRole role, Transform parent, Vector3 localPosition, Quaternion localRotation) {
            if (!prefabs.TryGetValue((deity, role), out GameObject prefab)) { return null; }
            GameObject instance = Object.Instantiate(prefab, parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

        /// <summary>Plays a one-shot here only.</summary>
        internal static void Play(Deity deity, VfxRole role, Vector3 position, float scale = 1f) {
            bool large = role == VfxRole.Burst || role == VfxRole.Kill || role == VfxRole.Dome;
            // Only some deities have a harvest or level-up effect of their own; the rest use their general one.
            if (prefabs.TryGetValue((deity, role), out GameObject prefab)
                || prefabs.TryGetValue((deity, large ? VfxRole.Burst : VfxRole.Proc), out prefab)) {
                GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
                if (scale != 1f) { instance.transform.localScale *= scale; }
                Object.Destroy(instance, lifetimes[prefab]);
                return;
            }
            FallbackBurst(Glow(deity), Primary(deity), position, large ? scale * 2f : scale);
        }

        /// <summary>Plays a one-shot for every player nearby, this one included.</summary>
        internal static void Share(Deity deity, VfxRole role, Vector3 position, float scale = 1f) {
            if (ZRoutedRpc.instance == null) {
                Play(deity, role, position, scale);
                return;
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RpcName, (int)deity, (int)role, position, scale);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<int, int, Vector3, float>(RpcName, OnShared);
            }
        }

        private static void OnShared(long sender, int deity, int role, Vector3 position, float scale) {
            // Everyone gets the call, servers without a player too.
            Player local = Player.m_localPlayer;
            if (local == null || Vector3.Distance(local.transform.position, position) > ShareRange) { return; }
            if (!Enum.IsDefined(typeof(Deity), deity) || !Enum.IsDefined(typeof(VfxRole), role)) { return; }
            Play((Deity)deity, (VfxRole)role, position, Mathf.Clamp(scale, 0.1f, 5f));
        }

        // ---- Colours ----

        internal static Color Primary(Deity deity) => Palette(deity, p => p.Primary);
        internal static Color Secondary(Deity deity) => Palette(deity, p => p.Secondary);
        internal static Color Glow(Deity deity) => Palette(deity, p => p.Glow);

        private static Color Palette(Deity deity, Func<DeityPalette, string> pick) {
            DeityPalette palette = StarMaps.TryGet(deity, out StarMap map) ? map.Definition.Palette : null;
            return StarMapArt.ParseColor(palette != null ? pick(palette) : null, Color.white);
        }

        /// <summary>An additive glow material for effects drawn in code, faded by fog where the bundle's shader loaded.</summary>
        internal static Material GlowMaterial(Texture texture, Color color) {
            if (WorldAdditive == null || !WorldAdditive.isSupported) { return StarMapArt.GlowMaterial(texture, color); }
            Material material = new Material(WorldAdditive) { mainTexture = texture };
            material.SetColor("_Color", color);
            return material;
        }

        // ---- Fallbacks and safety ----

        // Shader Graph shaders from the Piloto pack should run in the game's built-in pipeline; if one
        // doesn't on some machine, its materials get the plain additive shader instead of turning pink.
        private static void CheckShaders(GameObject prefab) {
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true)) {
                foreach (Material material in renderer.sharedMaterials) {
                    if (material == null || material.shader == null || material.shader.isSupported) { continue; }
                    Logger.LogWarning($"{prefab.name}: shader {material.shader.name} is not supported here; using a plain glow.");
                    Texture texture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                    material.shader = WorldAdditive != null && WorldAdditive.isSupported ? WorldAdditive : Shader.Find("Sprites/Default");
                    if (texture != null) { material.mainTexture = texture; }
                }
            }
        }

        private static float Lifetime(GameObject prefab) {
            float longest = 0f;
            foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>(true)) {
                ParticleSystem.MainModule main = system.main;
                longest = Mathf.Max(longest, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
            }
            return Mathf.Clamp(longest, MinLifetime, MaxLifetime);
        }

        private static readonly List<Material> fallbackMaterials = new List<Material>();

        // A spray of soft glows in the deity's colours, built in code.
        private static void FallbackBurst(Color inner, Color outer, Vector3 position, float scale) {
            GameObject go = new GameObject("AL_Burst");
            go.transform.position = position;
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * scale, 4f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f * scale, 0.45f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(inner, outer);
            main.gravityModifier = -0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f * scale;
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            if (fallbackMaterials.Count == 0) { fallbackMaterials.Add(GlowMaterial(StarMapArt.SoftGlow.texture, Color.white)); }
            renderer.sharedMaterial = fallbackMaterials[0];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            system.Play();
            Object.Destroy(go, 1.5f);
        }
    }
}
