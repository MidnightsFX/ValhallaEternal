using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;

namespace AesirLineage.EditorTools
{
    /// <summary>
    /// Builds Aesir Lineage's effect prefabs from the Piloto Studio pack and the asset bundle the mod
    /// embeds. Each recipe copies one Piloto prefab, strips anything the game can't load (Piloto's own
    /// scripts), sets its size and offset, and saves it as <c>al_vfx_&lt;deity&gt;_&lt;role&gt;</c>, which is
    /// the name the mod asks the bundle for. Rerun after changing a recipe; it replaces the old prefabs.
    /// </summary>
    public static class VfxBuilder
    {
        private const string Bundle = "aesirlineage";
        private const string OutputFolder = "Assets/AesirLineage/Vfx";
        private const string ShaderFolder = "Assets/AesirLineage/Shaders";
        private const string BundleOutput = "AssetBundles/StandaloneWindows";
        private const string Piloto = "Assets/Piloto Studio/Elemental VFX Mega Bundle/";

        private struct Recipe
        {
            public string Name;
            public string Source;
            public float Scale;
            public Vector3 Offset;
            public Vector3 Rotation;
            // Parts of the source whose names start with this are left out.
            public string Drop;

            public Recipe(string name, string source, float scale = 1f, Vector3 offset = default, Vector3 rotation = default, string drop = null)
            {
                Name = name;
                Source = source;
                Scale = scale;
                Offset = offset;
                Rotation = rotation;
                Drop = drop;
            }
        }

        // The mountain peaks Piloto's explosions raise; kill effects leave them out.
        private const string Mountains = "Add_Mountain";
        // Wings are half the size Piloto made them.
        private const float WingScale = 0.5f;

        // Deity names are the mod's enum names, lowercased. Roles:
        //   soul_orb                         the Soul of Power's body (the mod draws its beam, ring and rune)
        //   soul_ground                      an optional ring under it, in place of the drawn one
        //   aura (around the feet), wings (on the upper back); the mod draws crowns and weapon glows itself
        //   footstep, kill, proc, burst      one-shots
        //   and a few deity-specific ones (harvest, levelup, dome)
        private static readonly Recipe[] Recipes = {
            // Gefjun: Nature
            new Recipe("al_vfx_gefjun_soul_orb", "Nature/Orb_Nature", 1.6f),
            new Recipe("al_vfx_gefjun_aura", "Nature/NatureAura", 1.1f, new Vector3(0f, 0.55f, 0f)),
            new Recipe("al_vfx_gefjun_wings", "Nature/Wing_Nature", WingScale * 1.5f),
            new Recipe("al_vfx_gefjun_footstep", "Nature/Impact_Nature", 0.3f),
            new Recipe("al_vfx_gefjun_kill", "Nature/VFX_NatureExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_gefjun_proc", "Nature/Impact_Nature", 0.7f),
            new Recipe("al_vfx_gefjun_burst", "Nature/AOE_Explosion_Nature", 0.6f),
            new Recipe("al_vfx_gefjun_harvest", "Nature/Leaf Spray", 0.8f),

            // Vör: Universe
            new Recipe("al_vfx_vor_soul_orb", "Universe/Orb_Universe", 1.6f),
            new Recipe("al_vfx_vor_aura", "Universe/UniverseAura", 1.1f, new Vector3(0f, 0.55f, 0f)),
            new Recipe("al_vfx_vor_wings", "Universe/wing_Universe", WingScale),
            new Recipe("al_vfx_vor_footstep", "Universe/Impact_Universe", 0.3f),
            new Recipe("al_vfx_vor_kill", "Universe/VFX_UniverseExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_vor_proc", "Universe/Impact_Universe", 0.7f),
            new Recipe("al_vfx_vor_burst", "Universe/AOE_Explosion_Universe", 0.6f),
            new Recipe("al_vfx_vor_levelup", "Arcane/VFX_LevelUpLevelUp_Arcane", 0.8f),

            // Baldur: Holy
            new Recipe("al_vfx_baldur_soul_orb", "Holy/Orb_Holy", 1.6f),
            new Recipe("al_vfx_baldur_aura", "Holy/HolyAura", 1.1f, new Vector3(0f, 0.55f, 0f)),
            new Recipe("al_vfx_baldur_wings", "Holy/wing_Holy", WingScale),
            new Recipe("al_vfx_baldur_footstep", "Holy/Impact_Holy", 0.3f),
            new Recipe("al_vfx_baldur_kill", "Holy/VFX_HolyExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_baldur_proc", "Holy/Impact_Holy", 0.7f),
            new Recipe("al_vfx_baldur_burst", "Holy/AOE_Explosion_Holy", 0.6f),

            // Skaði: Frost
            new Recipe("al_vfx_skaldi_soul_orb", "Frost/Frostmist Orb", 0.7f),
            new Recipe("al_vfx_skaldi_aura", "Frost/FrostAura", 1.1f, new Vector3(0f, 0.55f, 0f)),
            new Recipe("al_vfx_skaldi_wings", "Frost/Wing_Frost", WingScale),
            new Recipe("al_vfx_skaldi_footstep", "Frost/Impact_Frost", 0.3f),
            new Recipe("al_vfx_skaldi_kill", "Frost/VFX_FrostExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_skaldi_proc", "Frost/Impact_Frost", 0.7f),
            new Recipe("al_vfx_skaldi_burst", "Frost/AOE_Explosion_Frost", 0.6f),

            // Syn: Fire, and the Sunfire rune ring
            new Recipe("al_vfx_syn_soul_orb", "Fire/Orb_Fire", 1.6f),
            new Recipe("al_vfx_syn_soul_ground", "Fire/MagicRing_Sunfire", 0.45f),
            new Recipe("al_vfx_syn_aura", "Fire/MagicRing_Sunfire", 0.28f, new Vector3(0f, 0.03f, 0f)),
            new Recipe("al_vfx_syn_wings", "Fire/wing_fire", WingScale),
            new Recipe("al_vfx_syn_footstep", "Fire/Impact_Fire", 0.3f),
            new Recipe("al_vfx_syn_kill", "Fire/VFX_FireExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_syn_proc", "Fire/Impact_Fire", 0.7f),
            new Recipe("al_vfx_syn_burst", "Fire/AOE_Explosion_Fire", 0.6f),
            new Recipe("al_vfx_syn_dome", "Fire/Solar Protection Dome", 1f),

            // Freyja: Fairy, and the celestial lotus
            new Recipe("al_vfx_freya_soul_orb", "Fairy/Orb_Fairy", 1.6f),
            new Recipe("al_vfx_freya_aura", "MegaBundle Exclusives/Aura_Celestial_Lotus", 0.8f, new Vector3(0f, 0.03f, 0f)),
            new Recipe("al_vfx_freya_wings", "Fairy/Wing_Fairy", WingScale),
            new Recipe("al_vfx_freya_footstep", "Fairy/Impact_Fairy", 0.3f),
            new Recipe("al_vfx_freya_kill", "Fairy/VFX_FairyExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_freya_proc", "Fairy/Impact_Fairy", 0.7f),
            new Recipe("al_vfx_freya_burst", "Fairy/AOE_Explosion_Fairy", 0.6f),

            // Hel: Mist
            new Recipe("al_vfx_hel_soul_orb", "Mist/Orb_Mist", 1.6f),
            new Recipe("al_vfx_hel_aura", "Mist/MistAura", 1.1f, new Vector3(0f, 0.55f, 0f)),
            new Recipe("al_vfx_hel_wings", "Mist/Wing_mist", WingScale),
            new Recipe("al_vfx_hel_footstep", "Mist/Impact_Mist", 0.3f),
            new Recipe("al_vfx_hel_kill", "Mist/VFX_MistExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_hel_proc", "Mist/Impact_Mist", 0.7f),
            new Recipe("al_vfx_hel_burst", "Mist/AOE_Explosion_Mist", 0.6f),

            // Víðarr: Wind
            new Recipe("al_vfx_vidarr_soul_orb", "Wind/Orb_Wind", 1.6f),
            new Recipe("al_vfx_vidarr_aura", "Wind/WindAura", 1.1f, new Vector3(0f, 0.55f, 0f)),
            new Recipe("al_vfx_vidarr_wings", "Wind/Wing_Wind", WingScale),
            new Recipe("al_vfx_vidarr_footstep", "Wind/Impact_Wind", 0.3f),
            new Recipe("al_vfx_vidarr_kill", "Wind/VFX_WindExplosion", 0.45f, drop: Mountains),
            new Recipe("al_vfx_vidarr_proc", "Wind/Impact_Wind", 0.7f),
            new Recipe("al_vfx_vidarr_burst", "Wind/AOE_Explosion_Wind", 0.6f),
        };

        [MenuItem("Aesir Lineage/Build effect prefabs")]
        public static void BuildPrefabsMenu() => Debug.Log(BuildPrefabs());

        [MenuItem("Aesir Lineage/Build asset bundle")]
        public static void BuildBundleMenu() => Debug.Log(BuildBundle());

        /// <summary>Writes every recipe's prefab and labels it and the mod's shaders for the bundle.</summary>
        public static string BuildPrefabs()
        {
            Directory.CreateDirectory(OutputFolder);
            RemoveStale();
            List<string> problems = new List<string>();
            int built = 0;
            foreach (Recipe recipe in Recipes) {
                string problem = Build(recipe);
                if (problem != null) { problems.Add(recipe.Name + ": " + problem); } else { built++; }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { ShaderFolder })) {
                Label(AssetDatabase.GUIDToAssetPath(guid));
            }
            int capped = CapTextures();
            int setUp = SetUpMaterials();
            AssetDatabase.SaveAssets();
            return $"Built {built} of {Recipes.Length} effect prefabs; capped {capped} textures at {MaxTextureSize}; set up {setUp} materials for the built-in pipeline." + (problems.Count > 0 ? " Problems: " + string.Join("; ", problems) : "");
        }

        private static string[] EffectPrefabs()
        {
            string[] effects = AssetDatabase.FindAssets("t:Prefab", new[] { OutputFolder });
            for (int i = 0; i < effects.Length; i++) { effects[i] = AssetDatabase.GUIDToAssetPath(effects[i]); }
            return effects;
        }

        // Piloto made its materials in URP. The game's built-in pipeline reads its own blend modes and
        // transparency keyword, which Unity only writes when it validates a material (in the inspector, or on
        // some reimports). A material never validated goes into the bundle opaque, and its particles draw as
        // solid squares, black where the texture is dark. Set up every effect material as the inspector would.
        private static int SetUpMaterials()
        {
            int changed = 0;
            foreach (string path in AssetDatabase.GetDependencies(EffectPrefabs(), true)) {
                Material material = path.EndsWith(".mat") ? AssetDatabase.LoadAssetAtPath<Material>(path) : null;
                if (material == null || !material.HasProperty("_BUILTIN_Surface")) { continue; }
                string before = SurfaceState(material);
                BuiltInBaseShaderGUI.SetupSurface(material);
                if (SurfaceState(material) != before) {
                    EditorUtility.SetDirty(material);
                    changed++;
                }
            }
            return changed;
        }

        private static string SurfaceState(Material material) =>
            $"{string.Join(" ", material.shaderKeywords)} {material.GetFloat("_BUILTIN_SrcBlend")} {material.GetFloat("_BUILTIN_DstBlend")} {material.GetFloat("_BUILTIN_ZWrite")} {material.renderQueue}";

        // Piloto ships textures up to 2048 square; effects a few metres across don't need more than
        // this, and the bundle is embedded in the mod's DLL. Only the Windows override is set.
        private const int MaxTextureSize = 512;

        private static int CapTextures()
        {
            int capped = 0;
            foreach (string path in AssetDatabase.GetDependencies(EffectPrefabs(), true)) {
                if (!path.StartsWith("Assets/Piloto Studio/") || !(AssetImporter.GetAtPath(path) is TextureImporter importer)) { continue; }
                TextureImporterPlatformSettings windows = importer.GetPlatformTextureSettings("Standalone");
                if (windows.overridden && windows.maxTextureSize <= MaxTextureSize) { continue; }
                windows.overridden = true;
                windows.maxTextureSize = Mathf.Min(MaxTextureSize, importer.maxTextureSize);
                windows.format = TextureImporterFormat.Automatic;
                windows.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(windows);
                importer.SaveAndReimport();
                capped++;
            }
            return capped;
        }

        /// <summary>Builds every labelled bundle for Windows into the folder the mod's build copies from.</summary>
        public static string BuildBundle()
        {
            Directory.CreateDirectory(BundleOutput);
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(BundleOutput, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            if (manifest == null) { return "Bundle build failed; see the console."; }
            FileInfo file = new FileInfo(Path.Combine(BundleOutput, Bundle));
            return $"Built {string.Join(", ", manifest.GetAllAssetBundles())}; {Bundle} is {file.Length / 1024f / 1024f:0.0} MB.";
        }

        // Prefabs left from recipes that have since been removed.
        private static void RemoveStale()
        {
            HashSet<string> wanted = new HashSet<string>();
            foreach (Recipe recipe in Recipes) { wanted.Add(recipe.Name); }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { OutputFolder })) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!wanted.Contains(Path.GetFileNameWithoutExtension(path))) { AssetDatabase.DeleteAsset(path); }
            }
        }

        private static string Build(Recipe recipe)
        {
            string sourcePath = Piloto + recipe.Source + ".prefab";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) { return "no source at " + sourcePath; }

            GameObject root = new GameObject(recipe.Name);
            GameObject effect = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(effect, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            effect.transform.SetParent(root.transform, false);
            effect.transform.localPosition = recipe.Offset;
            effect.transform.localRotation = Quaternion.Euler(recipe.Rotation);
            effect.transform.localScale = effect.transform.localScale * recipe.Scale;
            if (recipe.Drop != null) { Drop(root, recipe.Drop); }
            Strip(root);

            string path = $"{OutputFolder}/{recipe.Name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            Object.DestroyImmediate(root);
            if (!saved) { return "could not save " + path; }
            Label(path);
            return null;
        }

        private static void Drop(GameObject root, string prefix)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true)) {
                // A part inside one already dropped is gone with it.
                if (part != null && part.name.StartsWith(prefix)) { Object.DestroyImmediate(part.gameObject); }
            }
        }

        // The game has none of Piloto's scripts, so they would load as missing scripts. Particles keep
        // working without them. Shadows from effects are only a cost.
        private static void Strip(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                foreach (MonoBehaviour behaviour in child.GetComponents<MonoBehaviour>()) {
                    Object.DestroyImmediate(behaviour);
                }
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (ParticleSystem system in root.GetComponentsInChildren<ParticleSystem>(true)) {
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = true;
                // Scale with the recipe rather than staying at the source's size.
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        private static void Label(string assetPath)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null && importer.assetBundleName != Bundle) {
                importer.assetBundleName = Bundle;
                importer.SaveAndReimport();
            }
        }
    }
}
