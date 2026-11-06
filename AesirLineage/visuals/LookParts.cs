using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using AesirLineage.starmap;
using AesirLineage.ui;
using static AesirLineage.common.Deities;

namespace AesirLineage.visuals {
    /// <summary>Materials for the looks drawn in code, one per deity and use, shared by every player.</summary>
    internal static class LookMaterials {
        private static readonly Dictionary<(Deity, string), Material> cache = new Dictionary<(Deity, string), Material>();

        internal static Material Get(Deity deity, string use, Texture texture, Color color) {
            if (cache.TryGetValue((deity, use), out Material material) && material != null) { return material; }
            material = DeityVfx.GlowMaterial(texture, color);
            StarMapArt.SetGlowColor(material, color);
            cache[(deity, use)] = material;
            return material;
        }

        internal static string Rune(Deity deity) => StarMaps.TryGet(deity, out StarMap map) ? map.Definition.Rune : null;

        internal static Transform Quad(string name, Transform parent, Material material, float size) {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = Vector3.one * size;
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad.transform;
        }
    }

    /// <summary>
    /// The crown: the deity's rune circling above the head, with a thin halo under the glyphs. Each
    /// Ascension with the deity adds a glyph and widens the circle. Hidden for now: the crown slot is
    /// left out of <see cref="Cosmetics.Slots"/>, so nothing creates one.
    /// </summary>
    internal sealed class RuneCrown : MonoBehaviour {
        private const int BaseCount = 5;
        private const int MaxCount = 13;
        private const float Radius = 0.27f;
        private const float AboveHead = 0.32f;
        private const float GlyphSize = 0.13f;
        private const float DegreesPerSecond = 35f;

        private Player player;
        private Transform halo;
        private Transform[] glyphs;
        private float radius;

        internal static GameObject Create(Deity deity, Player player, int ascension) {
            GameObject go = new GameObject("AL_RuneCrown");
            go.transform.SetParent(player.transform, false);
            RuneCrown crown = go.AddComponent<RuneCrown>();
            crown.player = player;
            crown.glyphs = new Transform[Mathf.Min(MaxCount, BaseCount + ascension)];
            crown.radius = Radius * (1f + 0.05f * ascension);
            Texture rune = (Texture)RuneArt.Texture(LookMaterials.Rune(deity)) ?? StarMapArt.Star.texture;
            Material glyph = LookMaterials.Get(deity, "crown_glyph", rune, Color.Lerp(DeityVfx.Glow(deity), Color.white, 0.3f));
            Material ring = LookMaterials.Get(deity, "crown_ring", StarMapArt.Ring.texture, StarMapArt.WithAlpha(DeityVfx.Primary(deity), 0.8f));
            crown.halo = LookMaterials.Quad("Halo", go.transform, ring, crown.radius * 2.3f);
            for (int i = 0; i < crown.glyphs.Length; i++) {
                crown.glyphs[i] = LookMaterials.Quad("Glyph", go.transform, glyph, GlyphSize);
            }
            return go;
        }

        private void LateUpdate() {
            if (player == null) { return; }
            Transform head = player.m_head;
            transform.position = (head != null ? head.position : player.transform.position + Vector3.up * 1.75f) + Vector3.up * AboveHead;
            transform.rotation = Quaternion.identity;
            float spin = Time.time * DegreesPerSecond;
            halo.localRotation = Quaternion.Euler(90f, spin, 0f);
            Camera camera = Utils.GetMainCamera();
            for (int i = 0; i < glyphs.Length; i++) {
                float angle = (spin + i * 360f / glyphs.Length) * Mathf.Deg2Rad;
                glyphs[i].localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.04f * Mathf.Sin(Time.time * 2f + i), Mathf.Sin(angle) * radius);
                if (camera != null) { glyphs[i].rotation = Quaternion.LookRotation(glyphs[i].position - camera.transform.position, Vector3.up); }
            }
        }
    }

    /// <summary>
    /// Sparks in the deity's colours streaming off the weapon in the right hand. They are emitted from
    /// the weapon's bounding box rather than its mesh, since the game's meshes can't be read at runtime.
    /// </summary>
    internal sealed class WeaponGlow : MonoBehaviour {
        internal Deity Deity;
        internal int Ascension;
        private VisEquipment equipment;
        private GameObject boundItem;
        private GameObject sparks;

        private void Awake() {
            equipment = GetComponent<VisEquipment>();
        }

        /// <summary>Follows weapon changes; called a couple of times a second.</summary>
        internal void Track() {
            GameObject item = equipment != null ? equipment.m_rightItemInstance : null;
            if (item == boundItem && (item == null || sparks != null)) { return; }
            if (sparks != null) { Destroy(sparks); }
            boundItem = item;
            if (item != null) { sparks = Build(item); }
        }

        private void OnDestroy() {
            if (sparks != null) { Destroy(sparks); }
        }

        private GameObject Build(GameObject item) {
            Renderer target = null;
            Bounds bounds = default;
            foreach (MeshFilter filter in item.GetComponentsInChildren<MeshFilter>()) {
                if (filter.sharedMesh == null) { continue; }
                target = filter.GetComponent<Renderer>();
                bounds = filter.sharedMesh.bounds;
                break;
            }
            if (target == null) {
                SkinnedMeshRenderer skinned = item.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinned == null || skinned.sharedMesh == null) { return null; }
                target = skinned;
                bounds = skinned.sharedMesh.bounds;
            }

            GameObject go = new GameObject("AL_WeaponGlow");
            go.transform.SetParent(target.transform, false);
            go.transform.localPosition = bounds.center;
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(DeityVfx.Glow(Deity), DeityVfx.Primary(Deity));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 120 + 30 * Ascension;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 45f + 10f * Ascension;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = bounds.size;
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = LookMaterials.Get(Deity, "weapon_spark", StarMapArt.SoftGlow.texture, Color.white);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
            return go;
        }
    }

    /// <summary>Glowing footprints that fade a few seconds after each step.</summary>
    internal sealed class Footprint : MonoBehaviour {
        private const float Lasts = 3f;
        private const int MaxPrints = 48;
        private static readonly Queue<Footprint> prints = new Queue<Footprint>();
        private static MaterialPropertyBlock block;

        private Renderer mark;
        private Color color;
        private float born;

        internal static void Leave(Deity deity, Vector3 point, Vector3 forward) {
            while (prints.Count >= MaxPrints) {
                Footprint oldest = prints.Dequeue();
                if (oldest != null) { Destroy(oldest.gameObject); }
            }
            forward.y = 0f;
            Material material = LookMaterials.Get(deity, "footprint", StarMapArt.SoftGlow.texture, Color.white);
            Transform quad = LookMaterials.Quad("AL_Footprint", null, material, 1f);
            quad.position = point + Vector3.up * 0.04f;
            quad.rotation = Quaternion.LookRotation(Vector3.down, forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward);
            quad.localScale = new Vector3(0.26f, 0.42f, 1f);
            Footprint footprint = quad.gameObject.AddComponent<Footprint>();
            footprint.mark = quad.GetComponent<Renderer>();
            footprint.color = DeityVfx.Primary(deity);
            footprint.born = Time.time;
            prints.Enqueue(footprint);
        }

        private void Update() {
            float age = (Time.time - born) / Lasts;
            if (age >= 1f) {
                Destroy(gameObject);
                return;
            }
            block ??= new MaterialPropertyBlock();
            Color now = StarMapArt.WithAlpha(color, 0.7f * (1f - age * age));
            block.SetColor("_Color", now);
            block.SetColor("_TintColor", now);
            mark.SetPropertyBlock(block);
        }
    }
}
