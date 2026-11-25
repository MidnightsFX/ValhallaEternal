using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AesirLineage.EditorTools
{
    /// <summary>
    /// Builds Aesir Lineage's effect prefabs and the asset bundle the mod embeds. Each prefab is saved as
    /// <c>al_vfx_&lt;deity&gt;_&lt;role&gt;</c>, the name the mod asks the bundle for. A recipe copies one Piloto
    /// prefab (the souls of power, their ground ring and the wings), strips anything the game can't load
    /// (Piloto's own scripts) and sets its size; every other effect is a design composed from a few particle
    /// layers (<see cref="VfxDesigns"/>). Rerun after changing either; it replaces the old prefabs.
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

            public Recipe(string name, string source, float scale = 1f)
            {
                Name = name;
                Source = source;
                Scale = scale;
            }
        }

        // Wings are half the size Piloto made them.
        private const float WingScale = 0.5f;

        // Deity names are the mod's enum names, lowercased. Roles:
        //   soul_orb                         the Soul of Power's body (the mod draws its beam, ring and rune)
        //   soul_ground                      an optional ring under it, in place of the drawn one
        //   aura (around the feet), wings (on the upper back); the mod draws crowns and weapon glows itself
        //   footstep, kill, proc, burst      one-shots
        //   and a few deity-specific ones (harvest, levelup, dome)
        private static readonly Recipe[] Recipes = {
            // Gefjun: Nature for the soul and wings; her other effects are designs.
            new Recipe("al_vfx_gefjun_soul_orb", "Nature/Orb_Nature", 1.6f),
            new Recipe("al_vfx_gefjun_wings", "Nature/Wing_Nature", WingScale * 1.5f),

            // Vör: Universe for the soul and wings; her other effects are designs.
            new Recipe("al_vfx_vor_soul_orb", "Universe/Orb_Universe", 1.6f),
            new Recipe("al_vfx_vor_wings", "Universe/wing_Universe", WingScale),

            // Baldur: Holy for the soul and wings; his other effects are designs.
            new Recipe("al_vfx_baldur_soul_orb", "Holy/Orb_Holy", 1.6f),
            new Recipe("al_vfx_baldur_wings", "Holy/wing_Holy", WingScale),

            // Skaði: Frost for the soul and wings; her other effects are designs.
            new Recipe("al_vfx_skaldi_soul_orb", "Frost/Frostmist Orb", 0.7f),
            new Recipe("al_vfx_skaldi_wings", "Frost/Wing_Frost", WingScale),

            // Syn: Fire for the soul and wings, and the Sunfire ring under her soul; her other effects are designs.
            new Recipe("al_vfx_syn_soul_orb", "Fire/Orb_Fire", 1.6f),
            new Recipe("al_vfx_syn_soul_ground", "Fire/MagicRing_Sunfire", 0.45f),
            new Recipe("al_vfx_syn_wings", "Fire/wing_fire", WingScale),

            // Freyja: Fairy for the soul and wings; her other effects are designs.
            new Recipe("al_vfx_freya_soul_orb", "Fairy/Orb_Fairy", 1.6f),
            new Recipe("al_vfx_freya_wings", "Fairy/Wing_Fairy", WingScale),

            // Hel: Mist for the soul and wings; her other effects are designs.
            new Recipe("al_vfx_hel_soul_orb", "Mist/Orb_Mist", 1.6f),
            new Recipe("al_vfx_hel_wings", "Mist/Wing_mist", WingScale),

            // Víðarr: Wind for the soul and wings; his other effects are designs.
            new Recipe("al_vfx_vidarr_soul_orb", "Wind/Orb_Wind", 1.6f),
            new Recipe("al_vfx_vidarr_wings", "Wind/Wing_Wind", WingScale),
        };

        // Effects composed in VfxDesigns; each names its own prefab.
        private static readonly Func<GameObject>[] Designs = {
            VfxDesigns.GefjunFootstep,
            VfxDesigns.GefjunAura,
            VfxDesigns.GefjunKill,
            VfxDesigns.GefjunProc,
            VfxDesigns.GefjunHarvest,
            VfxDesigns.GefjunBurst,
            VfxDesigns.VorFootstep,
            VfxDesigns.VorAura,
            VfxDesigns.VorKill,
            VfxDesigns.VorProc,
            VfxDesigns.VorBurst,
            VfxDesigns.VorLevelUp,
            VfxDesigns.BaldurFootstep,
            VfxDesigns.BaldurAura,
            VfxDesigns.BaldurKill,
            VfxDesigns.BaldurProc,
            VfxDesigns.BaldurBurst,
            VfxDesigns.SkadiFootstep,
            VfxDesigns.SkadiAura,
            VfxDesigns.SkadiKill,
            VfxDesigns.SkadiProc,
            VfxDesigns.SkadiBurst,
            VfxDesigns.SynFootstep,
            VfxDesigns.SynAura,
            VfxDesigns.SynKill,
            VfxDesigns.SynProc,
            VfxDesigns.SynBurst,
            VfxDesigns.SynDome,
            VfxDesigns.FreyjaFootstep,
            VfxDesigns.FreyjaAura,
            VfxDesigns.FreyjaKill,
            VfxDesigns.FreyjaProc,
            VfxDesigns.FreyjaBurst,
            VfxDesigns.HelFootstep,
            VfxDesigns.HelAura,
            VfxDesigns.HelKill,
            VfxDesigns.HelProc,
            VfxDesigns.HelBurst,
            VfxDesigns.VidarrFootstep,
            VfxDesigns.VidarrAura,
            VfxDesigns.VidarrKill,
            VfxDesigns.VidarrProc,
            VfxDesigns.VidarrBurst,
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
            foreach (Func<GameObject> design in Designs) {
                string problem = Save(design(), out string name);
                if (problem != null) { problems.Add(name + ": " + problem); } else { built++; }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { ShaderFolder })) {
                Label(AssetDatabase.GUIDToAssetPath(guid));
            }
            int capped = CapTextures();
            int setUp = SetUpMaterials();
            AssetDatabase.SaveAssets();
            return $"Built {built} of {Recipes.Length + Designs.Length} effect prefabs; capped {capped} textures at {MaxTextureSize}; set up {setUp} materials for the built-in pipeline." + (problems.Count > 0 ? " Problems: " + string.Join("; ", problems) : "");
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
            foreach (Func<GameObject> design in Designs) {
                GameObject composed = design();
                wanted.Add(composed.name);
                Object.DestroyImmediate(composed);
            }
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
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = effect.transform.localScale * recipe.Scale;
            Strip(root);
            return Save(root, out _);
        }

        // Saves a scene object as its prefab and destroys it. Designs are composed inactive; prefabs play on spawn.
        private static string Save(GameObject root, out string name)
        {
            name = root.name;
            root.SetActive(true);
            string path = $"{OutputFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            Object.DestroyImmediate(root);
            if (!saved) { return "could not save " + path; }
            Label(path);
            return null;
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
