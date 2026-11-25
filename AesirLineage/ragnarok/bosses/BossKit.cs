using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AesirLineage.ragnarok.bosses {
    // A colour to turn something to: every colour takes `Hue`, keeps its brightness, and has its saturation held
    // between the two bounds. A floor above 0 makes white and grey take the hue too, which suits a creature's body but
    // turns an effect's smoke into a wall of colour; `Glow` is the same tint with greys left grey, for effects.
    internal readonly struct Tint {
        internal readonly float Hue;
        internal readonly float MinSaturation;
        internal readonly float MaxSaturation;

        internal Tint(float hue, float minSaturation = 0f, float maxSaturation = 1f) {
            Hue = hue;
            MinSaturation = minSaturation;
            MaxSaturation = maxSaturation;
        }

        internal Tint Glow => new Tint(Hue, 0f, MaxSaturation);

        internal Color Apply(Color color) {
            Color.RGBToHSV(color, out _, out float s, out float v);
            Color result = Color.HSVToRGB(Hue, Mathf.Clamp(s, MinSaturation, MaxSaturation), v, hdr: true);
            result.a = color.a;
            return result;
        }
    }

    // What the world bosses are built from: vanilla attacks, projectiles and creatures cloned and changed, and a
    // recolour that turns a vanilla creature's look to another hue.
    internal static class BossKit {
        // The vanilla effects and sounds the bosses are built from.
        internal static class Vfx {
            // Yagluth's nova without its damage: a shockwave ring.
            internal const string Shockwave = "fx_goblinking_nova";
            // The Fuling shaman's magenta fireball.
            internal const string MagicProjectile = "GoblinShaman_projectile_fireball";
            // The Charred warlock's summoning lights, before a summon appears; Bonemass's, as it does.
            internal const string SummonTelegraph = "fx_summon_twitcher_spawn";
            internal const string SummonRise = "vfx_DraugrSpawn";
            // Brenna's fire nova: a ring and a burst of gradient-mapped flames.
            internal const string FireNova = "fx_fireskeleton_nova";
            // Brenna's nova damage, an area that hits once.
            internal const string FireAoe = "skeleton_firenova_aoe";
            // Where a boss appears at an offering altar.
            internal const string Teleport = "vfx_spawn";
            // The drake's frost bolt: straight, no gravity, made for something flying.
            internal const string WindProjectile = "hatchling_cold_projectile";
            // The frost wisp's white swirl.
            internal const string WindSwirl = "FrostWisp_Storm";
        }

        internal static class Sound {
            internal const string Scream = "sfx_ghost_attack";
            internal const string BoltLaunch = "sfx_imp_fireball_launch";
            internal const string Summon = "sfx_frozenking_spirit_summon";
            internal const string SummonRise = "sfx_DraugrSpawn";
            internal const string FireCharge = "sfx_imp_alerted";
            internal const string FireNova = "sfx_imp_fireball_explode";
            internal const string Teleport = "sfx_spawn";
            internal const string Wind = "sfx_frozenking_frozentwirl_wind";
            internal const string Screech = "sfx_hatchling_alerted";
        }

        private static GameObject container;

        // Runtime-made prefabs live under an inactive object, so nothing on them wakes until it is instantiated.
        private static Transform Container {
            get {
                if (container == null) {
                    container = new GameObject("AL_WorldBossParts");
                    container.SetActive(false);
                    Object.DontDestroyOnLoad(container);
                }
                return container.transform;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Parts
        // ---------------------------------------------------------------------------------------

        // A copy of a vanilla monster attack item, changed by `edit` and registered with the game.
        internal static GameObject Attack(string name, string basePrefab, Action<ItemDrop.ItemData.SharedData> edit) {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(name, basePrefab);
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null) { throw new InvalidOperationException($"{basePrefab} is not an item to clone"); }
            edit(item.m_itemData.m_shared);
            ItemManager.Instance.AddItem(new CustomItem(prefab, false));
            return prefab;
        }

        // A copy of a vanilla prefab (a projectile, an area effect, a visual effect), changed by `edit` and registered
        // with the game, so it can be spawned across the network.
        private static GameObject Prefab(string name, string basePrefab, Action<GameObject> edit = null) {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(name, basePrefab);
            if (prefab == null) { throw new InvalidOperationException($"{basePrefab} is not there to clone"); }
            edit?.Invoke(prefab);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
            return prefab;
        }

        // An empty, local-only prefab for a component that does its work when an attack fires it (see BlinkStrike).
        private static GameObject Local(string name) {
            GameObject prefab = new GameObject(name);
            prefab.transform.SetParent(Container, false);
            return prefab;
        }

        // A summoning "projectile": fired by an attack, it calls min to max of `creature` around the caster, up to
        // `maxAlive` of them loaded at once. Each shows `telegraph` where it will appear, `telegraphSeconds` before.
        internal static GameObject Summon(string name, GameObject creature, int min, int max, int maxAlive, float radius,
                                          EffectList telegraph, float telegraphSeconds, EffectList appear) {
            GameObject prefab = Local(name);
            SpawnAbility spawn = prefab.AddComponent<SpawnAbility>();
            spawn.m_spawnPrefab = new[] { creature };
            spawn.m_targetType = SpawnAbility.TargetType.Caster;
            spawn.m_spawnAtTarget = true;
            spawn.m_minToSpawn = min;
            // SpawnAbility's upper bound is exclusive.
            spawn.m_maxToSpawn = max + 1;
            spawn.m_maxSpawned = maxAlive;
            spawn.m_spawnRadius = radius;
            spawn.m_spawnGroundOffset = 0.2f;
            spawn.m_alertSpawnedCreature = true;
            spawn.m_levelUpSettings = new List<SpawnAbility.LevelUpSettings>();
            spawn.m_preSpawnEffects = telegraph;
            spawn.m_preSpawnDelay = telegraphSeconds;
            spawn.m_spawnEffects = appear;
            return prefab;
        }

        // A recoloured copy of a vanilla projectile with its hit effects recoloured to match, at their own size and with
        // their smoke left grey. `gravity` replaces the projectile's own, when given.
        internal static GameObject Projectile(string name, string basePrefab, Tint tint, float scale, float? gravity = null, Action<GameObject> edit = null) {
            return Recolored(name, basePrefab, tint, scale, prefab => {
                edit?.Invoke(prefab);
                Projectile projectile = prefab.GetComponent<Projectile>();
                if (projectile == null) { throw new InvalidOperationException($"{basePrefab} is not a projectile"); }
                if (gravity.HasValue) { projectile.m_gravity = gravity.Value; }
                projectile.m_hitEffects = RecoloredEffects(name + "_Hit", projectile.m_hitEffects, tint.Glow);
                projectile.m_hitWaterEffects = RecoloredEffects(name + "_HitWater", projectile.m_hitWaterEffects, tint.Glow);
            });
        }

        // The same effects, with each visual one replaced by a recoloured copy; sounds are kept as they are.
        private static EffectList RecoloredEffects(string name, EffectList effects, Tint tint) {
            if (effects?.m_effectPrefabs == null) { return new EffectList(); }
            List<EffectList.EffectData> result = new List<EffectList.EffectData>();
            for (int i = 0; i < effects.m_effectPrefabs.Length; i++) {
                EffectList.EffectData effect = effects.m_effectPrefabs[i];
                bool visual = effect.m_prefab != null && (effect.m_prefab.GetComponentInChildren<ParticleSystem>(true) != null || effect.m_prefab.GetComponentInChildren<Light>(true) != null);
                if (visual) {
                    effect = new EffectList.EffectData() {
                        m_prefab = Recolored($"{name}{i}", effect.m_prefab.name, tint, 1f),
                        m_enabled = effect.m_enabled,
                        m_variant = effect.m_variant,
                        m_attach = effect.m_attach,
                        m_follow = effect.m_follow,
                        m_inheritParentRotation = effect.m_inheritParentRotation,
                        m_inheritParentScale = effect.m_inheritParentScale,
                        m_multiplyParentVisualScale = effect.m_multiplyParentVisualScale,
                        m_randomRotation = effect.m_randomRotation,
                        m_scale = effect.m_scale,
                        m_childTransform = effect.m_childTransform,
                    };
                }
                result.Add(effect);
            }
            return new EffectList() { m_effectPrefabs = result.ToArray() };
        }

        // A copy of another prefab's look, added under `target`: its renderers and particles without its networking
        // or self-destruction, so it lives and dies with the target.
        internal static void AttachLook(GameObject target, string sourcePrefab, float scale) {
            GameObject source = PrefabManager.Instance.GetPrefab(sourcePrefab);
            if (source == null) { Logger.LogWarning($"World boss look {sourcePrefab} was not found."); return; }
            GameObject look = Object.Instantiate(source, target.transform, false);
            look.name = sourcePrefab;
            look.transform.localPosition = Vector3.zero;
            look.transform.localScale = Vector3.one * scale;
            foreach (TimedDestruction timed in look.GetComponentsInChildren<TimedDestruction>(true)) { Object.DestroyImmediate(timed); }
            foreach (ZSyncTransform sync in look.GetComponentsInChildren<ZSyncTransform>(true)) { Object.DestroyImmediate(sync); }
            foreach (ZNetView view in look.GetComponentsInChildren<ZNetView>(true)) { Object.DestroyImmediate(view); }
            foreach (ParticleSystem particles in look.GetComponentsInChildren<ParticleSystem>(true)) {
                ParticleSystem.MainModule main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.loop = true;
            }
        }

        // A creature to be summoned: a copy that is not the vanilla one, so the summon's limit counts only its own, that
        // drops nothing, so a boss is not a loot farm, that sides with the boss against players alone, and that falls
        // apart after a minute or two, so a boss's summons do not gather in the world cycle after cycle.
        internal static GameObject Minion(string name, string basePrefab, float scale, Action<GameObject> edit) {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(name, basePrefab);
            if (prefab == null || prefab.GetComponent<Character>() == null) { throw new InvalidOperationException($"{basePrefab} is not a creature to clone"); }
            prefab.transform.localScale *= scale;
            prefab.GetComponent<Character>().m_faction = Character.Faction.Boss;
            foreach (CharacterDrop drop in prefab.GetComponentsInChildren<CharacterDrop>(true)) { Object.DestroyImmediate(drop); }
            foreach (Tameable tameable in prefab.GetComponentsInChildren<Tameable>(true)) { Object.DestroyImmediate(tameable); }
            foreach (Procreation procreation in prefab.GetComponentsInChildren<Procreation>(true)) { Object.DestroyImmediate(procreation); }
            if (prefab.GetComponent<MonsterAI>() is MonsterAI ai) { ai.m_despawnInDay = false; }
            CharacterTimedDestruction lifetime = prefab.GetComponent<CharacterTimedDestruction>() ?? prefab.AddComponent<CharacterTimedDestruction>();
            lifetime.m_timeoutMin = 75f;
            lifetime.m_timeoutMax = 105f;
            lifetime.m_triggerOnAwake = true;
            edit?.Invoke(prefab);
            CreatureManager.Instance.AddCreature(new CustomCreature(prefab, false));
            return prefab;
        }

        // A copy of a vanilla effect, projectile or area turned to the tint, and made `scale` times larger. Particles set
        // to ignore their parent's scale are switched to follow it, so the whole effect grows.
        internal static GameObject Recolored(string name, string basePrefab, Tint tint, float scale, Action<GameObject> edit = null) {
            return Prefab(name, basePrefab, prefab => {
                edit?.Invoke(prefab);
                Recolor(prefab, tint);
                if (Mathf.Approximately(scale, 1f)) { return; }
                prefab.transform.localScale *= scale;
                foreach (ParticleSystem particles in prefab.GetComponentsInChildren<ParticleSystem>(true)) {
                    ParticleSystem.MainModule main = particles.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                foreach (Light light in prefab.GetComponentsInChildren<Light>(true)) { light.range *= scale; }
            });
        }

        // A teleport an attack fires as its projectile (see BlinkStrike).
        internal static GameObject Blink(string name, EffectList leave, EffectList arrive, GameObject arriveAoe) {
            GameObject prefab = Local(name);
            BlinkStrike blink = prefab.AddComponent<BlinkStrike>();
            blink.m_leaveEffects = leave;
            blink.m_arriveEffects = arrive;
            blink.m_arriveAoe = arriveAoe;
            return prefab;
        }

        // A big creature: hard to push around, and staggered only by a parry, not by the damage it takes.
        internal static void MakeHeavy(GameObject creature, float mass) {
            if (creature.GetComponent<Rigidbody>() is Rigidbody body) { body.mass = mass; }
            if (creature.GetComponent<Character>() is Character character) { character.m_staggerDamageFactor = 0f; }
        }

        internal static EffectList Effects(params string[] prefabs) {
            List<EffectList.EffectData> effects = new List<EffectList.EffectData>();
            foreach (string name in prefabs) {
                if (string.IsNullOrEmpty(name)) { continue; }
                GameObject prefab = PrefabManager.Instance.GetPrefab(name);
                if (prefab == null) {
                    Logger.LogWarning($"World boss effect {name} was not found.");
                    continue;
                }
                effects.Add(new EffectList.EffectData() { m_prefab = prefab, m_enabled = true });
            }
            return new EffectList() { m_effectPrefabs = effects.ToArray() };
        }

        internal static void SetDrops(GameObject creature, params (string item, int min, int max)[] drops) {
            CharacterDrop drop = creature.GetComponent<CharacterDrop>() ?? creature.AddComponent<CharacterDrop>();
            drop.m_drops = new List<CharacterDrop.Drop>();
            foreach ((string item, int min, int max) in drops) {
                GameObject prefab = PrefabManager.Instance.GetPrefab(item);
                if (prefab == null) { Logger.LogWarning($"World boss drop {item} was not found."); continue; }
                drop.m_drops.Add(new CharacterDrop.Drop() { m_prefab = prefab, m_amountMin = min, m_amountMax = max, m_chance = 1f, m_levelMultiplier = false });
            }
        }

        // ---------------------------------------------------------------------------------------
        // Bodies (the prefabs AesirLineageUnity builds)
        // ---------------------------------------------------------------------------------------

        private static Shader creatureShader;

        // A boss body's materials on the game's creature shader, so it is lit like the creatures around it; `glow`
        // lights the body's glow mask. The bundle can only carry Unity's own shaders.
        internal static void Look(GameObject body, Color glow) {
            if (creatureShader == null) {
                creatureShader = Shader.Find("Custom/Creature") ?? Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == "Custom/Creature");
                if (creatureShader == null) { Logger.LogWarning("The game's creature shader was not found; the world bosses keep their bundled one."); return; }
            }
            Dictionary<Material, Material> made = new Dictionary<Material, Material>();
            foreach (SkinnedMeshRenderer renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source => {
                    if (source == null || source.shader == creatureShader) { return source; }
                    if (made.TryGetValue(source, out Material done)) { return done; }
                    Material material = new Material(creatureShader) { name = source.name };
                    foreach (string texture in new[] { "_MainTex", "_BumpMap", "_EmissionMap" }) {
                        if (source.HasProperty(texture) && material.HasProperty(texture)) { material.SetTexture(texture, source.GetTexture(texture)); }
                    }
                    bool glows = source.HasProperty("_EmissionMap") && source.GetTexture("_EmissionMap") != null;
                    material.SetColor("_Color", Color.white);
                    material.SetColor("_EmissionColor", glows ? glow : Color.black);
                    if (glows) { material.EnableKeyword("_EMISSION"); }
                    made[source] = material;
                    return material;
                }).ToArray();
            }
        }

        // Death effects with the boss's corpse (its body playing its death, scaled with it) in place of any ragdoll.
        internal static EffectList WithCorpse(EffectList effects, string key, Color glow) {
            GameObject corpse = AesirLineage.EmbeddedResourceBundle?.LoadAsset<GameObject>($"AL_WorldBoss_{key}_Corpse");
            if (corpse == null) { Logger.LogWarning($"The asset bundle has no corpse for {key}."); return effects; }
            Look(corpse, glow);
            IEnumerable<EffectList.EffectData> kept = (effects?.m_effectPrefabs ?? new EffectList.EffectData[0])
                .Where(e => e.m_prefab == null || e.m_prefab.GetComponent<Ragdoll>() == null);
            EffectList.EffectData body = new EffectList.EffectData() { m_prefab = corpse, m_enabled = true, m_inheritParentScale = true };
            return new EffectList() { m_effectPrefabs = kept.Append(body).ToArray() };
        }

        // A copy of part of a vanilla creature's body (its flames, smoke or a sound loop), turned to `tint`, for
        // Attach to put on a boss's bone when it spawns.
        internal static GameObject Attachment(string vanilla, string part, Tint? tint) {
            GameObject source = PrefabManager.Instance.GetPrefab(vanilla);
            Transform found = source == null ? null : source.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == part);
            if (found == null) { Logger.LogWarning($"World boss attachment {vanilla}/{part} was not found."); return null; }
            GameObject copy = Object.Instantiate(found.gameObject, Container, false);
            copy.name = part;
            copy.SetActive(true);
            if (tint.HasValue) { Recolor(copy, tint.Value); }
            return copy;
        }

        internal static void Attach(GameObject creature, params (GameObject attachment, string bone)[] parts) {
            BossAttachments attachments = creature.GetComponent<BossAttachments>() ?? creature.AddComponent<BossAttachments>();
            foreach ((GameObject attachment, string bone) in parts.Where(p => p.attachment != null)) {
                attachments.m_parts = attachments.m_parts.Append(attachment).ToArray();
                attachments.m_bones = attachments.m_bones.Append(bone).ToArray();
            }
        }

        // ---------------------------------------------------------------------------------------
        // Recolouring
        // ---------------------------------------------------------------------------------------

        // Turns everything that draws under `root` to the tint: material colours and colour textures, particle colours,
        // trails and lights. Brightness stays, so shading and glow survive.
        internal static void Recolor(GameObject root, Tint tint) {
            Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Recolor(m, tint, materials)).ToArray();
            }
            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true)) {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = Recolor(main.startColor, tint);
                ParticleSystem.ColorOverLifetimeModule overLife = particles.colorOverLifetime;
                if (overLife.enabled) { overLife.color = Recolor(overLife.color, tint); }
                ParticleSystem.ColorBySpeedModule bySpeed = particles.colorBySpeed;
                if (bySpeed.enabled) { bySpeed.color = Recolor(bySpeed.color, tint); }
                ParticleSystem.TrailModule trails = particles.trails;
                if (trails.enabled) {
                    trails.colorOverLifetime = Recolor(trails.colorOverLifetime, tint);
                    trails.colorOverTrail = Recolor(trails.colorOverTrail, tint);
                }
                // Valheim's gradient-mapped particle shaders have no colour of their own: they take it from these.
                ParticleSystem.CustomDataModule custom = particles.customData;
                if (custom.enabled) {
                    foreach (ParticleSystemCustomData stream in new[] { ParticleSystemCustomData.Custom1, ParticleSystemCustomData.Custom2 }) {
                        if (custom.GetMode(stream) == ParticleSystemCustomDataMode.Color) {
                            custom.SetColor(stream, Recolor(custom.GetColor(stream), tint));
                        }
                    }
                }
            }
            foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true)) {
                trail.colorGradient = Recolor(trail.colorGradient, tint);
            }
            foreach (LineRenderer line in root.GetComponentsInChildren<LineRenderer>(true)) {
                line.colorGradient = Recolor(line.colorGradient, tint);
            }
            foreach (Light light in root.GetComponentsInChildren<Light>(true)) {
                light.color = tint.Apply(light.color);
            }
        }

        // Texture properties that hold colour; masks, normals and noise are left alone.
        private static readonly string[] ColorTextures = { "_MainTex", "_EmissionMap", "_BaseMap", "_EmissiveTex", "_DetailAlbedoMap" };

        private static Material Recolor(Material source, Tint tint, Dictionary<Material, Material> done) {
            if (source == null) { return null; }
            if (done.TryGetValue(source, out Material made)) { return made; }
            Material material = new Material(source) { name = source.name + "_al" };
            Shader shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++) {
                string property = shader.GetPropertyName(i);
                switch (shader.GetPropertyType(i)) {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        material.SetColor(property, tint.Apply(material.GetColor(property)));
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        if (ColorTextures.Contains(property) && material.GetTexture(property) is Texture texture) {
                            material.SetTexture(property, Recolor(texture, tint));
                        }
                        break;
                }
            }
            done[source] = material;
            return material;
        }

        private static readonly Dictionary<(Texture, Tint), Texture2D> textures = new Dictionary<(Texture, Tint), Texture2D>();

        // Game textures are not readable, so the copy is drawn through a render texture first.
        private static Texture Recolor(Texture source, Tint tint) {
            if (textures.TryGetValue((source, tint), out Texture2D made)) { return made; }
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false) { name = source.name + "_al" };
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                Color32[] pixels = copy.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) {
                    Color32 pixel = tint.Apply(pixels[i]);
                    pixel.a = pixels[i].a;
                    pixels[i] = pixel;
                }
                copy.SetPixels32(pixels);
                copy.wrapMode = source.wrapMode;
                copy.filterMode = source.filterMode;
                copy.Apply(true, true);
                textures[(source, tint)] = copy;
                return copy;
            } finally {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static ParticleSystem.MinMaxGradient Recolor(ParticleSystem.MinMaxGradient gradient, Tint tint) {
            switch (gradient.mode) {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(tint.Apply(gradient.color));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(tint.Apply(gradient.colorMin), tint.Apply(gradient.colorMax));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(Recolor(gradient.gradient, tint));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Recolor(gradient.gradientMin, tint), Recolor(gradient.gradientMax, tint));
                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(Recolor(gradient.gradient, tint)) { mode = ParticleSystemGradientMode.RandomColor };
                default:
                    return gradient;
            }
        }

        private static Gradient Recolor(Gradient gradient, Tint tint) {
            if (gradient == null) { return null; }
            Gradient result = new Gradient() { mode = gradient.mode };
            result.SetKeys(gradient.colorKeys.Select(k => new GradientColorKey(tint.Apply(k.color), k.time)).ToArray(), gradient.alphaKeys);
            return result;
        }
    }
}
