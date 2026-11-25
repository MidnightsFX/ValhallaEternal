using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Builds the Ragnarok world bosses as creature prefabs of their own, in the `aesirlineage` bundle the mod embeds and
// registers them from. Aesir Lineage > Build world boss prefabs, then Aesir Lineage > Build asset bundle; in batch
// mode, -executeMethod BossPrefabs.BuildAll does both.
//
// Each boss is AL_WorldBoss_<Key>: the game's creature components (networking, physics, Humanoid, MonsterAI) set as
// the vanilla creature it is modelled on has them (VanillaCreatures.txt), over its AssetForge body with an animator of
// its own and an attack trigger per attack. Effects are Jotunn mocks, which Jotunn swaps for the game's own when the
// mod registers the creature; the mod then adds what is particular to the boss (attacks, drops, its tuning).
// AL_WorldBoss_<Key>_Corpse is the body playing its death, for its death effects.
//
// The game's DLLs are not referenced by this project's scripts (their DisplayName attribute breaks Timeline and the
// Input System), so their components are added and set by name.
public static class BossPrefabs {
    private const string Bundle = "aesirlineage";
    private const string Root = "Assets/AesirLineage/Ragnarok/Bosses";
    private const string Mocks = "Assets/AesirLineage/Ragnarok/Mocks";
    private const string Mixamo = "Assets/AesirLineage/Ragnarok/Animations/Mixamo";
    private const string Forge = "Assets/AssetForge";
    private const string Settings = "Assets/AesirLineage/Editor/VanillaCreatures.txt";
    // The game's layers, which a prefab stores by number.
    private const int CharacterLayer = 9;
    private const int EffectLayer = 8;

    private sealed class Clip {
        public string Model;
        // The clip's name in the model; null for its only clip (Mixamo's are all "mixamo.com").
        public string Take;
        public Clip(string model, string take = null) { Model = model; Take = take; }
    }

    private sealed class Attack {
        public string Trigger;
        public Clip Clip;
        // Where in the clip the blow lands, as a fraction of it; below 0, at the clip's own "hit" event.
        public float Strike;
        public float Speed = 1f;
    }

    private sealed class Boss {
        public string Key;
        public string Vanilla;
        public float Scale;
        public string Model;
        public string Textures;
        public string Glow;
        public bool Humanoid;
        public (Clip clip, float threshold, float timeScale)[] Movement;
        public Clip Stagger;
        public Clip Death;
        public Attack[] Attacks;
        public float Radius;
        public float Height;
        public float Mass;
        public bool Gravity = true;
        public RigidbodyConstraints Constraints = RigidbodyConstraints.FreezeRotation;
        public (string bone, Color color, float range, float intensity)[] Lights = new (string, Color, float, float)[0];
        // A corpse that drops out of the air; the others die on the ground.
        public bool Falls;
    }

    private static Boss[] Bosses() {
        // AssetForge's model with its finger weights folded into the hands.
        string modgud = $"{Root}/Modgud/Modgud.fbx";
        string sinmara = ForgeModel("Sinmara");
        string bat = $"{Root}/Hraesvelgr/Hraesvelgr.fbx";
        return new[] {
            new Boss {
                Key = "Modgud", Vanilla = "Ghost", Scale = 2.2f, Model = modgud, Textures = $"{Root}/Modgud/Textures", Glow = $"{Root}/Modgud/Modgud_Glow.png", Humanoid = true,
                Movement = new[] { (new Clip(modgud, "Idle"), 0f, 1f), (new Clip(modgud, "Walk"), 2f, 1f), (new Clip(modgud, "Run"), 4.5f, 1f) },
                Stagger = new Clip(modgud, "Hit"), Death = new Clip(modgud, "Death"),
                Attacks = new[] {
                    new Attack { Trigger = "modgud_scream", Clip = new Clip($"{Mixamo}/mutant roaring.fbx"), Strike = 0.38f, Speed = 1.3f },
                    new Attack { Trigger = "modgud_bolt", Clip = new Clip($"{Mixamo}/Standing 1H Magic Attack 01.fbx"), Strike = 0.45f, Speed = 1.1f },
                    new Attack { Trigger = "modgud_raise", Clip = new Clip($"{Mixamo}/Standing 2H Magic Area Attack 01.fbx"), Strike = 0.5f },
                },
                Radius = 0.4f, Height = 1.95f, Mass = 10f,
                // The paint left her eye sockets dark.
                Lights = new[] { ("Head", new Color(0.7f, 0.3f, 1f), 2.5f, 1.6f) },
            },
            new Boss {
                Key = "Sinmara", Vanilla = "Surtling", Scale = 2.5f, Model = sinmara, Textures = $"{Forge}/Sinmara/Textures", Glow = $"{Root}/Sinmara/Sinmara_Glow.png", Humanoid = true,
                Movement = new[] { (new Clip(sinmara, "Idle"), 0f, 1f), (new Clip(sinmara, "Walk"), 3f, 1f), (new Clip(sinmara, "Run"), 7f, 1f) },
                Stagger = new Clip(sinmara, "Hit"), Death = new Clip(sinmara, "Death"),
                Attacks = new[] {
                    new Attack { Trigger = "sinmara_burst", Clip = new Clip(sinmara, "Attack"), Strike = -1f },
                    new Attack { Trigger = "sinmara_nova", Clip = new Clip($"{Mixamo}/mutant jump attack.fbx"), Strike = 0.55f, Speed = 1.2f },
                    new Attack { Trigger = "sinmara_blink", Clip = new Clip($"{Mixamo}/mutant jumping.fbx"), Strike = 0.42f, Speed = 1.2f },
                },
                Radius = 0.45f, Height = 1.55f, Mass = 8f,
                Constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ,
                Lights = new[] { ("Spine2", new Color(0.35f, 0.65f, 1f), 4f, 2f) },
            },
            new Boss {
                Key = "Hraesvelgr", Vanilla = "Bat", Scale = 4f, Model = bat, Textures = $"{Forge}/Hraesvelgr/Textures", Humanoid = false, Falls = true,
                Movement = new[] { (new Clip(bat, "Hover"), 0f, 1f), (new Clip(bat, "Fly"), 6f, 1f), (new Clip(bat, "Fly"), 11f, 1.5f) },
                Stagger = new Clip(bat, "Stagger"), Death = new Clip(bat, "Death"),
                Attacks = new[] {
                    new Attack { Trigger = "hraesvelgr_bite", Clip = new Clip(bat, "Bite"), Strike = 0.5f },
                    new Attack { Trigger = "hraesvelgr_gale", Clip = new Clip(bat, "Gale"), Strike = 0.55f },
                    new Attack { Trigger = "hraesvelgr_call", Clip = new Clip(bat, "Call"), Strike = 0.45f },
                },
                Radius = 0.35f, Height = 0.8f, Mass = 1f, Gravity = false,
                // Its eyes, pale ice blue.
                Lights = new[] { ("Head", new Color(0.7f, 0.9f, 1f), 1.5f, 1.4f) },
            },
        };
    }

    [MenuItem("Aesir Lineage/Build world boss prefabs")]
    public static void BuildPrefabsMenu() => Debug.Log(BuildPrefabs());

    public static void BuildAll() {
        Debug.Log(BuildPrefabs());
        string bundle = AesirLineage.EditorTools.VfxBuilder.BuildBundle();
        Debug.Log(bundle);
        if (bundle.Contains("failed")) { throw new Exception(bundle); }
    }

    public static string BuildPrefabs() {
        List<string> report = new List<string>();
        foreach (Boss boss in Bosses()) { report.Add(Build(boss)); }
        AssetDatabase.SaveAssets();
        return string.Join("\n", report);
    }

    // An AssetForge humanoid's published model: the folder holds one FBX per variant picked.
    private static string ForgeModel(string name) {
        string folder = $"{Forge}/{name}";
        if (!AssetDatabase.IsValidFolder(folder)) { return null; }
        return AssetDatabase.FindAssets("t:Model", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p.Length).FirstOrDefault();
    }

    private static string Build(Boss boss) {
        if (boss.Model == null || AssetDatabase.LoadAssetAtPath<GameObject>(boss.Model) == null) {
            Debug.LogWarning($"{boss.Key}: skipped, its model is not imported yet ({boss.Model ?? Forge + "/" + boss.Key}).");
            return $"{boss.Key}: skipped, no model";
        }
        string folder = $"{Root}/{boss.Key}";
        Directory.CreateDirectory($"{folder}/Clips");
        AssetDatabase.Refresh();

        Material material = BodyMaterial(boss, folder);
        Dictionary<string, AnimationClip> made = new Dictionary<string, AnimationClip>();
        AnimationClip Copy(Clip clip, string name, float strike, bool loop) {
            if (made.TryGetValue(name, out AnimationClip done)) { return done; }
            AnimationClip copy = CopyClip(boss, clip, $"{folder}/Clips/{boss.Key}_{name}.anim", strike, loop);
            made[name] = copy;
            return copy;
        }

        AssetDatabase.DeleteAsset($"{folder}/{boss.Key}.controller");
        AssetDatabase.DeleteAsset($"{folder}/{boss.Key}_Corpse.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{folder}/{boss.Key}.controller");
        foreach (string p in new[] { "forward_speed", "sideway_speed", "turn_speed" }) { controller.AddParameter(p, AnimatorControllerParameterType.Float); }
        foreach (string p in new[] { "inWater", "onGround", "flying", "alert" }) { controller.AddParameter(p, AnimatorControllerParameterType.Bool); }
        controller.AddParameter("stagger", AnimatorControllerParameterType.Trigger);
        AnimatorControllerLayer layer = controller.layers[0];
        // CharacterAnimEvent turns a humanoid's head toward its target in OnAnimatorIK.
        layer.iKPass = boss.Humanoid;
        controller.layers = new[] { layer };
        AnimatorStateMachine machine = layer.stateMachine;

        BlendTree tree = new BlendTree { name = "Movement", blendParameter = "forward_speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.children = boss.Movement.Select(m => new ChildMotion {
            motion = Copy(m.clip, m.clip.Take, -2f, true), threshold = m.threshold, timeScale = m.timeScale,
        }).ToArray();
        AnimatorState movement = machine.AddState("Movement");
        movement.motion = tree;
        movement.tag = "idle";
        machine.defaultState = movement;

        AnimatorState stagger = machine.AddState("Stagger");
        stagger.motion = Copy(boss.Stagger, "Stagger", -2f, false);
        stagger.tag = "stagger";
        AnimatorStateTransition toStagger = machine.AddAnyStateTransition(stagger);
        toStagger.AddCondition(AnimatorConditionMode.If, 0f, "stagger");
        toStagger.duration = 0.05f;
        toStagger.canTransitionToSelf = false;
        Return(stagger, movement);

        foreach (Attack attack in boss.Attacks) {
            controller.AddParameter(attack.Trigger, AnimatorControllerParameterType.Trigger);
            AnimatorState state = machine.AddState(attack.Trigger);
            state.motion = Copy(attack.Clip, attack.Trigger, attack.Strike, false);
            state.speed = attack.Speed;
            // Character.InAttack looks for this tag.
            state.tag = "attack";
            AnimatorStateTransition to = machine.AddAnyStateTransition(state);
            to.AddCondition(AnimatorConditionMode.If, 0f, attack.Trigger);
            to.duration = 0.1f;
            to.canTransitionToSelf = false;
            Return(state, movement);
        }
        EditorUtility.SetDirty(controller);

        AnimatorController corpseController = AnimatorController.CreateAnimatorControllerAtPath($"{folder}/{boss.Key}_Corpse.controller");
        AnimatorState dying = corpseController.layers[0].stateMachine.AddState("Death");
        dying.motion = Copy(boss.Death, "Death", -2f, false);
        corpseController.layers[0].stateMachine.defaultState = dying;
        EditorUtility.SetDirty(corpseController);

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(boss.Model).OfType<Avatar>().FirstOrDefault();
        List<string> missing = new List<string>();
        string creature = SaveCreature(boss, material, avatar, controller, $"{folder}/AL_WorldBoss_{boss.Key}.prefab", missing);
        string corpse = SaveCorpse(boss, material, avatar, corpseController, $"{folder}/AL_WorldBoss_{boss.Key}_Corpse.prefab");
        return $"{boss.Key}: {Path.GetFileName(creature)}, {Path.GetFileName(corpse)}; clips {string.Join(", ", made.Select(c => $"{c.Key} {c.Value.length:0.00}s"))}" +
            (missing.Count > 0 ? $"; vanilla fields not found: {string.Join(", ", missing)}" : "");
    }

    private static void Return(AnimatorState from, AnimatorState to) {
        AnimatorStateTransition back = from.AddTransition(to);
        back.hasExitTime = true;
        back.exitTime = 0.92f;
        back.duration = 0.12f;
    }

    // A clip of its own beside the boss, so its events and loop can be set: the forge's events (for its own receiver)
    // are dropped, and an attack gets the game's OnAttackTrigger where the blow lands.
    private static AnimationClip CopyClip(Boss boss, Clip clip, string path, float strike, bool loop) {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(clip.Model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        AnimationClip source = clip.Take == null ? clips.FirstOrDefault() : clips.FirstOrDefault(c => c.name == clip.Take);
        if (source == null) { throw new Exception($"{boss.Key}: no clip {clip.Take ?? "(any)"} in {clip.Model}; it has {string.Join(", ", clips.Select(c => c.name))}"); }
        float hit = strike;
        if (strike < -1.5f) {
            hit = -1f;
        } else if (strike < 0f) {
            AnimationEvent forgeHit = AnimationUtility.GetAnimationEvents(source).FirstOrDefault(e => e.stringParameter == "hit");
            if (forgeHit == null) { throw new Exception($"{boss.Key}: {source.name} has no hit event to strike on"); }
            hit = forgeHit.time / source.length;
        }
        AnimationClip copy = Object.Instantiate(source);
        copy.name = Path.GetFileNameWithoutExtension(path);
        AnimationUtility.SetAnimationEvents(copy, hit >= 0f
            ? new[] { new AnimationEvent { functionName = "OnAttackTrigger", time = hit * copy.length } }
            : new AnimationEvent[0]);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(copy, settings);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    // The mod moves the body to the game's creature shader at load; this material carries its textures.
    private static Material BodyMaterial(Boss boss, string folder) {
        string[] textures = AssetDatabase.IsValidFolder(boss.Textures) ? AssetDatabase.FindAssets("t:Texture2D", new[] { boss.Textures }).Select(AssetDatabase.GUIDToAssetPath).ToArray() : new string[0];
        string albedo = textures.FirstOrDefault(t => t.EndsWith("_BaseColor.png"));
        string normal = textures.FirstOrDefault(t => t.EndsWith("_Normal.png"));
        if (albedo == null) { throw new Exception($"{boss.Key}: no base colour texture in {boss.Textures}"); }
        string path = $"{folder}/{boss.Key}_Body.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
        material.SetFloat("_Glossiness", 0.15f);
        if (normal != null) {
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
        }
        if (boss.Glow != null) {
            TextureImporter glow = (TextureImporter)AssetImporter.GetAtPath(boss.Glow);
            if (glow.sRGBTexture) {
                glow.sRGBTexture = false;
                glow.SaveAndReimport();
            }
            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(boss.Glow));
            material.SetColor("_EmissionColor", Color.white);
            material.EnableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    // The model, unpacked, with the boss's material, an Animator on its controller, and nothing of AssetForge's.
    private static GameObject Body(Boss boss, Material material, Avatar avatar, RuntimeAnimatorController controller) {
        GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(boss.Model));
        PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        body.transform.position = Vector3.zero;
        foreach (MonoBehaviour script in body.GetComponentsInChildren<MonoBehaviour>(true)) { Object.DestroyImmediate(script); }
        foreach (Collider collider in body.GetComponentsInChildren<Collider>(true)) { Object.DestroyImmediate(collider); }
        foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true)) {
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        }
        Animator animator = body.GetComponent<Animator>() ?? body.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return body;
    }

    private static Transform Bone(GameObject body, string name) {
        return body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name) ?? throw new Exception($"{body.name} has no bone {name}");
    }

    private static string SaveCreature(Boss boss, Material material, Avatar avatar, RuntimeAnimatorController controller, string path, List<string> missing) {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path)) { layer = CharacterLayer };
        try {
            root.transform.localScale = Vector3.one * boss.Scale;
            GameObject body = Body(boss, material, avatar, controller);
            body.name = "Visual";
            body.transform.SetParent(root.transform, false);
            Transform head = Bone(body, "Head");
            GameObject eye = new GameObject("EyePos") { layer = CharacterLayer };
            eye.transform.SetParent(head, false);
            eye.transform.position = head.position + Vector3.forward * 0.05f;
            foreach ((string bone, Color color, float range, float intensity) in boss.Lights) {
                Light light = new GameObject("Glow").AddComponent<Light>();
                light.transform.SetParent(Bone(body, bone), false);
                light.type = LightType.Point;
                light.color = color;
                light.range = range;
                light.intensity = intensity;
                light.shadows = LightShadows.None;
            }
            SerializedObject animEvent = Game(body, "CharacterAnimEvent");
            animEvent.FindProperty("m_headRotation").boolValue = boss.Humanoid;
            animEvent.ApplyModifiedPropertiesWithoutUndo();

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = boss.Mass;
            rigidbody.useGravity = boss.Gravity;
            rigidbody.constraints = boss.Constraints;
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = boss.Radius;
            capsule.height = boss.Height;
            capsule.center = new Vector3(0f, boss.Height * 0.5f, 0f);
            capsule.sharedMaterial = CharacterMaterial();

            Dictionary<string, SerializedObject> components = new Dictionary<string, SerializedObject>();
            foreach (string type in new[] { "ZNetView", "ZSyncTransform", "ZSyncAnimation", "Humanoid", "MonsterAI", "CharacterDrop" }) {
                components[type] = Game(root, type);
            }
            foreach ((string component, string field, string value) in VanillaSettings(boss.Vanilla)) {
                if (!components.TryGetValue(component, out SerializedObject so)) { continue; }
                if (!Set(so, field, value)) { missing.Add($"{component}.{field}"); }
            }
            SerializedObject humanoid = components["Humanoid"];
            humanoid.FindProperty("m_name").stringValue = $"$al_worldboss_{boss.Key.ToLowerInvariant()}";
            humanoid.FindProperty("m_boss").boolValue = true;
            humanoid.FindProperty("m_defeatSetGlobalKey").stringValue = "";
            humanoid.FindProperty("m_eye").objectReferenceValue = eye.transform;
            foreach (SerializedObject so in components.Values) { so.ApplyModifiedPropertiesWithoutUndo(); }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            Object.DestroyImmediate(root);
        }
        Label(path);
        return path;
    }

    private static string SaveCorpse(Boss boss, Material material, Avatar avatar, RuntimeAnimatorController controller, string path) {
        GameObject root = Body(boss, material, avatar, controller);
        try {
            root.name = Path.GetFileNameWithoutExtension(path);
            // The game's ragdolls are effects: they collide with the world and no one walks into them.
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = EffectLayer; }
            SerializedObject timed = Game(root, "TimedDestruction");
            timed.FindProperty("m_timeout").floatValue = 10f;
            timed.FindProperty("m_triggerOnAwake").boolValue = true;
            timed.ApplyModifiedPropertiesWithoutUndo();
            if (boss.Falls) {
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 50f;
                body.angularDamping = 2f;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                SphereCollider shape = root.AddComponent<SphereCollider>();
                shape.center = new Vector3(0f, boss.Height * 0.5f, 0f);
                shape.radius = boss.Radius;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        } finally {
            Object.DestroyImmediate(root);
        }
        Label(path);
        return path;
    }

    private static void Label(string path) {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        importer.assetBundleName = Bundle;
        importer.SaveAndReimport();
    }

    // A game component by its class name, as a SerializedObject to set.
    private static SerializedObject Game(GameObject target, string type) {
        Type found = Type.GetType($"{type}, assembly_valheim") ?? throw new Exception($"{type} is not loaded: is assembly_valheim.dll in Assets/Assemblies?");
        return new SerializedObject(target.AddComponent(found));
    }

    // The game's characters glide along walls and each other.
    private static PhysicsMaterial CharacterMaterial() {
        string path = $"{Root}/charactermat.physicMaterial";
        PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material != null) { return material; }
        material = new PhysicsMaterial("charactermat") {
            dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum,
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static IEnumerable<(string component, string field, string value)> VanillaSettings(string creature) {
        foreach (string line in File.ReadAllLines(Settings)) {
            if (line.StartsWith("#") || line.Trim().Length == 0) { continue; }
            string[] parts = line.Split('\t');
            if (parts.Length == 4 && parts[0] == creature) { yield return (parts[1], parts[2], parts[3]); }
        }
    }

    private static bool Set(SerializedObject so, string field, string value) {
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { return false; }
        if (value.StartsWith("{")) {
            string[] effects = value.Trim('{', '}').Split('|');
            property.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++) {
                string[] e = effects[i].Split(':');
                SerializedProperty data = property.GetArrayElementAtIndex(i);
                data.FindPropertyRelative("m_prefab").objectReferenceValue = Mock(e[0]);
                string[] names = { "m_enabled", "m_variant", "m_attach", "m_follow", "m_inheritParentRotation", "m_inheritParentScale", "m_multiplyParentVisualScale", "m_randomRotation", "m_scale" };
                for (int n = 0; n < names.Length; n++) { SetScalar(data.FindPropertyRelative(names[n]), e[n + 1]); }
            }
            return true;
        }
        if (value.StartsWith("[")) {
            string[] items = value.Trim('[', ']').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (!property.isArray) { return false; }
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) { property.GetArrayElementAtIndex(i).stringValue = items[i]; }
            return true;
        }
        return SetScalar(property, value);
    }

    private static bool SetScalar(SerializedProperty property, string value) {
        if (property == null) { return false; }
        switch (property.propertyType) {
            case SerializedPropertyType.Float: property.floatValue = float.Parse(value, CultureInfo.InvariantCulture); return true;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum: property.intValue = int.Parse(value, CultureInfo.InvariantCulture); return true;
            case SerializedPropertyType.Boolean: property.boolValue = value == "1"; return true;
            case SerializedPropertyType.String: property.stringValue = value; return true;
            default: return false;
        }
    }

    // Jotunn replaces a reference to a JVLmock_<name> object with the game's own <name> when the mod registers the
    // creature, so the bundle carries no copy of the game's effects.
    private static GameObject Mock(string name) {
        string path = $"{Mocks}/JVLmock_{name}.prefab";
        GameObject mock = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (mock != null) { return mock; }
        Directory.CreateDirectory(Mocks);
        GameObject made = new GameObject($"JVLmock_{name}");
        try {
            return PrefabUtility.SaveAsPrefabAsset(made, path);
        } finally {
            Object.DestroyImmediate(made);
        }
    }
}

// Import settings for the clips and models the bosses are built from, applied whenever they import.
public class BossModelImport : AssetPostprocessor {
    private const string Mixamo = "Assets/AesirLineage/Ragnarok/Animations/Mixamo/";
    private const string Bat = "Assets/AesirLineage/Ragnarok/Bosses/Hraesvelgr/Hraesvelgr.fbx";
    private const string Modgud = "Assets/AesirLineage/Ragnarok/Bosses/Modgud/Modgud.fbx";
    private static readonly string[] Loops = { "Hover", "Fly", "Idle", "Walk", "Run" };

    private bool Ours => assetPath.StartsWith(Mixamo) || assetPath == Bat || assetPath == Modgud;

    private void OnPreprocessModel() {
        ModelImporter importer = (ModelImporter)assetImporter;
        // Flames attached to a boss emit from its skinned mesh, which the player can only read if it is kept readable.
        if (assetPath.StartsWith("Assets/AssetForge/") || assetPath == Bat || assetPath == Modgud) { importer.isReadable = true; }
        if (!Ours) { return; }
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = true;
        importer.animationType = assetPath == Bat ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.autoGenerateAvatarMappingIfUnspecified = true;
    }

    private void OnPreprocessAnimation() {
        if (!Ours) { return; }
        ModelImporter importer = (ModelImporter)assetImporter;
        // The rest-pose take is only there to give Unity the model's T-pose.
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations.Where(c => !c.takeName.EndsWith("_TPose")).ToArray();
        foreach (ModelImporterClipAnimation clip in clips) {
            if (assetPath == Bat || assetPath == Modgud) {
                // Blender writes takes as "<armature>|<action>".
                clip.name = clip.takeName.Substring(clip.takeName.IndexOf('|') + 1);
                clip.loopTime = Loops.Contains(clip.name);
                if (assetPath == Modgud) {
                    // In place, as AssetForge makes them: the hips' sway stays in the pose.
                    clip.lockRootRotation = clip.lockRootHeightY = clip.lockRootPositionXZ = true;
                    clip.keepOriginalOrientation = clip.keepOriginalPositionY = clip.keepOriginalPositionXZ = true;
                }
            } else {
                // Turning and height stay in the pose; travel goes to root motion, which the game's animators drop,
                // so a leaping attack leaps in place.
                clip.lockRootRotation = clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = false;
                clip.keepOriginalOrientation = clip.keepOriginalPositionY = true;
                clip.loopTime = false;
            }
        }
        importer.clipAnimations = clips;
    }
}
