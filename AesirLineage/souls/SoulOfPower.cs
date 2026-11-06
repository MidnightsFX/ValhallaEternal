using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.modules;
using AesirLineage.starmap;
using AesirLineage.ui;
using static AesirLineage.common.Deities;
using Object = UnityEngine.Object;
using AesirLineage.visuals;

namespace AesirLineage.souls {
    /// <summary>
    /// The orb a boss leaves when a player nearby hasn't claimed it this run. Each player who reaches it
    /// and hasn't claimed it claims its favour: points in the deity's star map and the boss's claim key. It is a networked, persistent object; the boss's
    /// deity, its key and when it rose live in its ZDO, so it survives zone unloads and restarts until
    /// its lifetime runs out.
    /// </summary>
    /// <remarks>
    /// Its body is the deity's orb from the bundle (<see cref="DeityVfx"/>), with the rune glowing inside.
    /// The beam, the turning ground ring and the light are drawn in code in the deity's colours, and the
    /// deity's aura plays around its base. Without the bundle's orb, soft glows stand in for it.
    /// </remarks>
    internal sealed class SoulOfPower : MonoBehaviour, Hoverable, Interactable {
        internal const string PrefabName = "AL_SoulOfPower";
        private const string DeityKey = "al_soul_deity";
        private const string BossKey = "al_soul_boss";
        private const string BornKey = "al_soul_born";
        // The orb floats this high over where the boss fell; the ground ring sits back down at the ground.
        internal const float Height = 1.5f;
        private const float BeamHeight = 14f;
        // Players nearer than this are told a soul has risen when it appears.
        private const float AnnounceRange = 60f;

        private static GameObject prefab;

        private ZNetView nview;
        private bool ready;
        private Deity deity;
        private string bossKey;
        private float nextLifetimeCheck;
        private float claimFlash;

        private Transform visuals;
        private Transform ring;
        private Light glowLight;
        private readonly List<Transform> billboards = new List<Transform>();
        private readonly List<Material> materials = new List<Material>();
        private Material haloMaterial;
        private Color haloColor;

        internal static void RegisterPrefab() {
            prefab = PrefabManager.Instance.CreateEmptyPrefab(PrefabName);
            if (prefab == null) { return; }
            // CreateEmptyPrefab gives a cube; the soul draws itself.
            Object.DestroyImmediate(prefab.GetComponent<BoxCollider>());
            Object.DestroyImmediate(prefab.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(prefab.GetComponent<MeshFilter>());
            // The interact raycast hits this layer, and players walk through it.
            int layer = LayerMask.NameToLayer("piece_nonsolid");
            if (layer >= 0) { prefab.layer = layer; }
            prefab.AddComponent<SphereCollider>().radius = 0.6f;
            prefab.AddComponent<SoulOfPower>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, fixReference: false));
        }

        /// <summary>Raises a soul here. Call on the machine that owns the boss; the soul is owned there too.</summary>
        internal static void Spawn(Deity deity, string bossKey, Vector3 position) {
            if (prefab == null || ZNet.instance == null) { return; }
            GameObject soul = Object.Instantiate(prefab, position, Quaternion.identity);
            ZDO zdo = soul.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null) {
                Object.Destroy(soul);
                return;
            }
            zdo.Set(DeityKey, (int)deity);
            zdo.Set(BossKey, bossKey);
            zdo.Set(BornKey, ZNet.instance.GetTime().Ticks);
            Logger.LogInfo($"A Soul of Power for {deity} rose where {bossKey} was earned.");
        }

        private void Awake() {
            nview = GetComponent<ZNetView>();
        }

        private void Start() {
            TrySetup();
        }

        // Spawn writes the ZDO just after Instantiate, so on that machine Start already sees it;
        // elsewhere the ZDO arrives with the object. Until then the soul waits.
        private void TrySetup() {
            if (ready || nview == null || !nview.IsValid()) { return; }
            ZDO zdo = nview.GetZDO();
            string key = zdo.GetString(BossKey, "");
            if (string.IsNullOrEmpty(key)) { return; }
            bossKey = key;
            deity = (Deity)zdo.GetInt(DeityKey, 0);
            ready = true;
            try {
                BuildVisuals();
            } catch (Exception e) {
                // A missing shader or effect costs the looks, not the soul.
                Logger.LogWarning($"Could not build the Soul of Power's visuals: {e.Message}");
            }
            Announce(zdo);
        }

        private void Announce(ZDO zdo) {
            Player player = Player.m_localPlayer;
            long born = zdo.GetLong(BornKey, 0L);
            if (player == null || born == 0L || ZNet.instance == null) { return; }
            if (new TimeSpan(ZNet.instance.GetTime().Ticks - born).TotalSeconds > 15) { return; }
            if (Vector3.Distance(player.transform.position, transform.position) > AnnounceRange) { return; }
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_soul_rises", Compendium.DeityName(deity)));
        }

        private void Update() {
            if (!ready) {
                TrySetup();
                return;
            }
            if (visuals != null) { Animate(); }
            if (Time.time >= nextLifetimeCheck) {
                nextLifetimeCheck = Time.time + 5f;
                CheckLifetime();
            }
        }

        // Only the owner removes it, once it has been there longer than the server allows.
        private void CheckLifetime() {
            if (!nview.IsValid() || !nview.IsOwner() || ZNet.instance == null) { return; }
            int minutes = ValConfig.SoulLifetimeMinutes.Value;
            if (minutes <= 0) { return; }
            long born = nview.GetZDO().GetLong(BornKey, 0L);
            if (born == 0L) { return; }
            if (new TimeSpan(ZNet.instance.GetTime().Ticks - born).TotalMinutes >= minutes) {
                ZNetScene.instance.Destroy(gameObject);
            }
        }

        // ---- Interaction ----

        public string GetHoverName() {
            return ready ? Localization.instance.Localize("$al_soul_name", Compendium.DeityName(deity)) : "";
        }

        public string GetHoverText() {
            if (!ready) { return ""; }
            string action = PlayerData.HasClaimedSoul(bossKey)
                ? $"<color=#9aa0aa>{Localization.instance.Localize("$al_soul_claimed")}</color>"
                : Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] ") + Localization.instance.Localize("$al_soul_accept", Compendium.DeityName(deity));
            return $"{GetHoverName()}\n{action}";
        }

        public float GetHoverOffset() {
            return 0f;
        }

        public bool Interact(Humanoid user, bool hold, bool alt) {
            if (hold || !ready || user != Player.m_localPlayer) { return false; }
            if (PlayerData.HasClaimedSoul(bossKey)) {
                user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_soul_claimed"));
                return false;
            }
            int points = ValConfig.PointsPerSoul.Value;
            PlayerData.ClaimSoul(bossKey, deity, points);
            DeityVfx.Share(deity, VfxRole.Burst, user.transform.position, 0.8f);
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_soul_granted", Compendium.DeityName(deity), points.ToString()));
            claimFlash = 1f;
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) {
            return false;
        }

        // ---- Visuals ----

        private void BuildVisuals() {
            StarMaps.TryGet(deity, out StarMap map);
            Color primary = DeityVfx.Primary(deity);
            Color glow = DeityVfx.Glow(deity);

            visuals = new GameObject("Visuals").transform;
            visuals.SetParent(transform, false);

            haloColor = StarMapArt.WithAlpha(primary, 0.55f);
            if (DeityVfx.Attach(deity, VfxRole.SoulOrb, visuals, Vector3.zero, Quaternion.identity) != null) {
                // The orb brings its own body; a wide, faint glow sets it apart from the world around it.
                haloColor = StarMapArt.WithAlpha(primary, 0.3f);
                haloMaterial = Billboard("HaloOuter", visuals, StarMapArt.SoftGlow.texture, haloColor, 4f);
            } else {
                haloMaterial = Billboard("HaloOuter", visuals, StarMapArt.SoftGlow.texture, haloColor, 3.4f);
                Billboard("Halo", visuals, StarMapArt.SoftGlow.texture, StarMapArt.WithAlpha(Color.Lerp(glow, Color.white, 0.5f), 0.9f), 1.5f);
                Billboard("Core", visuals, StarMapArt.Star.texture, Color.white, 0.9f);
            }
            Texture2D rune = RuneArt.Texture(map?.Definition.Rune);
            if (rune != null) {
                Billboard("Rune", visuals, rune, StarMapArt.WithAlpha(Color.Lerp(primary, Color.white, 0.35f), 1f), 0.75f);
            }

            // On the ground: the deity's own ring where it has one, else a turning ring of its runes; and its aura.
            Vector3 ground = new Vector3(0f, 0.04f - Height, 0f);
            if (DeityVfx.Attach(deity, VfxRole.SoulGround, transform, ground, Quaternion.identity) == null) {
                ring = Quad("Ring", transform, StarMapArt.RuneRing(map?.Definition.Rune), StarMapArt.WithAlpha(primary, 0.85f), 3f);
                ring.localPosition = ground;
                ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            DeityVfx.Attach(deity, VfxRole.Aura, transform, new Vector3(0f, -Height, 0f), Quaternion.identity);

            LineRenderer beam = new GameObject("Beam").AddComponent<LineRenderer>();
            beam.transform.SetParent(visuals, false);
            beam.useWorldSpace = false;
            beam.positionCount = 2;
            beam.SetPosition(0, Vector3.zero);
            beam.SetPosition(1, Vector3.up * BeamHeight);
            beam.widthCurve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 0.08f));
            beam.colorGradient = new Gradient() {
                colorKeys = new[] { new GradientColorKey(glow, 0f), new GradientColorKey(primary, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) },
            };
            Material beamMaterial = DeityVfx.GlowMaterial(StarMapArt.BeamFalloff, Color.white);
            materials.Add(beamMaterial);
            beam.material = beamMaterial;
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.receiveShadows = false;

            glowLight = new GameObject("Light").AddComponent<Light>();
            glowLight.transform.SetParent(visuals, false);
            glowLight.type = LightType.Point;
            glowLight.color = glow;
            glowLight.range = 8f;
            glowLight.intensity = 1.5f;
            glowLight.shadows = LightShadows.None;
        }

        private Material Billboard(string name, Transform parent, Texture texture, Color color, float size) {
            Transform quad = Quad(name, parent, texture, color, size);
            billboards.Add(quad);
            return quad.GetComponent<MeshRenderer>().sharedMaterial;
        }

        private Transform Quad(string name, Transform parent, Texture texture, Color color, float size) {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.layer = gameObject.layer;
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = Vector3.one * size;
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            Material material = DeityVfx.GlowMaterial(texture, color);
            StarMapArt.SetGlowColor(material, color);
            materials.Add(material);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad.transform;
        }

        private void Animate() {
            float time = Time.time;
            // Once this player has its favour the soul dims for them, so others can still see it is theirs to take.
            float strength = ready && PlayerData.HasClaimedSoul(bossKey) ? 0.35f : 1f;
            claimFlash = Mathf.Max(0f, claimFlash - Time.deltaTime * 1.5f);

            visuals.localPosition = new Vector3(0f, Mathf.Sin(time * 1.4f) * 0.12f, 0f);
            if (ring != null) { ring.Rotate(0f, 0f, 18f * Time.deltaTime, Space.Self); }
            if (glowLight != null) {
                glowLight.intensity = (1.3f + 0.4f * Mathf.Sin(time * 2.3f)) * strength + claimFlash * 4f;
            }
            if (haloMaterial != null) {
                float pulse = 0.8f + 0.2f * Mathf.Sin(time * 1.9f);
                StarMapArt.SetGlowColor(haloMaterial, StarMapArt.WithAlpha(haloColor, haloColor.a * pulse * strength + claimFlash * 0.5f));
            }

            Transform camera = GameCamera.instance != null ? GameCamera.instance.transform : null;
            if (camera == null) { return; }
            foreach (Transform billboard in billboards) {
                billboard.rotation = Quaternion.LookRotation(billboard.position - camera.position);
            }
        }

        private void OnDestroy() {
            foreach (Material material in materials) {
                if (material != null) { Object.Destroy(material); }
            }
        }
    }
}
