using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AesirLineage.EditorTools
{
    /// <summary>
    /// Lays effect prefabs out in a row in a scratch scene, against a dark backdrop, with their particles
    /// advanced to a given time, so they can be compared in the Scene view. A tuning aid only.
    /// </summary>
    public static class VfxPreview
    {
        private const string BackdropName = "VfxPreviewBackdrop";

        /// <summary>Clears the scene and shows the prefabs left to right; returns what was placed.</summary>
        public static List<string> Row(string[] prefabPaths, float spacing = 4f, float height = 1.5f, float simulate = 1.5f, float viewSize = 8f, float scale = 1f, bool figures = true)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            foreach (GameObject go in scene.GetRootGameObjects()) {
                if (go.name != BackdropName) { Object.DestroyImmediate(go); }
            }
            EnsureBackdrop(prefabPaths.Length * spacing);

            List<string> placed = new List<string>();
            for (int i = 0; i < prefabPaths.Length; i++) {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null) {
                    placed.Add("missing: " + prefabPaths[i]);
                    continue;
                }
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(i * spacing, height, 0f);
                instance.transform.localScale *= scale;
                placed.Add(instance.name);
                if (figures) { Figure(new Vector3(i * spacing, 0f, 0f)); }
            }
            Simulate(simulate);

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null) {
                view.showGrid = false;
                view.pivot = new Vector3((prefabPaths.Length - 1) * spacing * 0.5f, height, 0f);
                view.rotation = Quaternion.Euler(8f, 0f, 0f);
                view.size = viewSize;
                view.Repaint();
            }
            return placed;
        }

        /// <summary>Advances every root particle system in the scene to the given time.</summary>
        public static void Simulate(float time)
        {
            foreach (ParticleSystem system in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) {
                if (system.transform.parent == null || system.transform.parent.GetComponentInParent<ParticleSystem>() == null) {
                    system.Simulate(time, true, true);
                }
            }
            SceneView.lastActiveSceneView?.Repaint();
        }

        // A dim stand-in for a player: 1.8 m tall, feet at the given point, facing the camera (-Z).
        private static void Figure(Vector3 feet)
        {
            GameObject figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            figure.name = "Figure";
            Object.DestroyImmediate(figure.GetComponent<Collider>());
            figure.transform.position = feet + Vector3.up * 0.9f;
            figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.35f);
            figure.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.16f, 0.15f, 0.14f) };
        }

        // ---- Shots: frames of an effect as players see it in game, saved as one PNG strip ----

        private const string StageName = "VfxStage";
        private const int ShotWidth = 640;
        private const int ShotHeight = 480;
        /// <summary>Where a kill or proc on the stage's boar-sized victim plays.</summary>
        public static readonly Vector3 VictimCenter = new Vector3(0.7f, 0.45f, 2.4f);

        /// <summary>
        /// A fresh scene with a meadow-toned stage: a player at the origin facing +Z, a boar-sized victim
        /// ahead, and three cameras: "GameCam" where Valheim's third-person camera sits, "SideCam" as
        /// another player standing to the side would see it, "CloseCam" a couple of metres from the victim.
        /// There is no bloom, so glows read dimmer than in game.
        /// </summary>
        public static void Stage()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform stage = new GameObject(StageName).transform;
            Shader lit = Shader.Find("Standard");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(stage, false);
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = new Material(lit) { color = new Color(0.25f, 0.3f, 0.17f) };

            Light sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(stage, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
            RenderSettings.fog = false;

            Transform player = new GameObject("Player").transform;
            player.SetParent(stage, false);
            Body("Body", player, new Vector3(0f, 0.9f, 0f), Quaternion.identity, new Vector3(0.5f, 0.9f, 0.35f), new Color(0.35f, 0.3f, 0.26f), lit);
            Body("Victim", stage, new Vector3(0.7f, 0.32f, 2.4f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.55f, 0.55f, 0.6f), new Color(0.3f, 0.22f, 0.18f), lit);

            Shooter("GameCam", stage, new Vector3(0.9f, 2.1f, -3.2f), new Vector3(0.4f, 0.9f, 2f));
            Shooter("SideCam", stage, new Vector3(5f, 1.9f, -1.2f), new Vector3(0f, 0.6f, -1.6f));
            Shooter("CloseCam", stage, new Vector3(1.9f, 1.15f, 0.9f), new Vector3(0.6f, 0.35f, 2.3f));
        }

        /// <summary>Plays a one-shot at a point and saves a strip of frames, one per age in seconds.</summary>
        public static string OneShot(GameObject effect, Vector3 at, float scale, float[] ages, string camera, string file)
        {
            GameObject instance = Spawn(effect, at, scale);
            List<Texture2D> tiles = new List<Texture2D>();
            foreach (float age in ages) {
                SimulateTo(instance, age);
                tiles.Add(Grab(camera));
            }
            Object.DestroyImmediate(instance);
            return SaveStrip(tiles, file);
        }

        /// <summary>
        /// The player jogging toward +Z with an effect left at each step (alternate feet, the newest a tenth of a
        /// second old). One frame from each camera.
        /// </summary>
        public static string Steps(GameObject effect, string file, float speed = 4f, float interval = 0.36f, int count = 9)
        {
            List<GameObject> spawned = new List<GameObject>();
            for (int k = 0; k < count; k++) {
                float age = 0.1f + k * interval;
                GameObject step = Spawn(effect, new Vector3(k % 2 == 0 ? 0.12f : -0.12f, 0f, -age * speed), 1f);
                SimulateTo(step, age);
                spawned.Add(step);
            }
            List<Texture2D> tiles = new List<Texture2D> { Grab("GameCam"), Grab("SideCam") };
            foreach (GameObject go in spawned) { Object.DestroyImmediate(go); }
            return SaveStrip(tiles, file);
        }

        /// <summary>
        /// An effect worn at the player's feet: settled while standing, then after jogging up to the same
        /// spot, so particles left in the world show as a trail. Frames: standing and jogging, each from both cameras.
        /// </summary>
        public static string Worn(GameObject effect, string file, float settle = 3f, float jog = 1.5f, float speed = 4f)
        {
            Transform player = GameObject.Find(StageName + "/Player").transform;
            List<Texture2D> tiles = new List<Texture2D>();
            const float dt = 1f / 30f;

            GameObject worn = Object.Instantiate(effect, player, false);
            worn.SetActive(true);
            Advance(worn, player, settle, dt, 0f, true);
            tiles.Add(Grab("GameCam"));
            tiles.Add(Grab("SideCam"));
            Object.DestroyImmediate(worn);

            player.position = new Vector3(0f, 0f, -speed * jog);
            worn = Object.Instantiate(effect, player, false);
            worn.SetActive(true);
            Advance(worn, player, settle, dt, 0f, true);
            Advance(worn, player, jog, dt, speed, false);
            tiles.Add(Grab("GameCam"));
            tiles.Add(Grab("SideCam"));
            Object.DestroyImmediate(worn);
            player.position = Vector3.zero;
            return SaveStrip(tiles, file);
        }

        private static void Advance(GameObject effect, Transform mover, float seconds, float dt, float speed, bool restart)
        {
            List<ParticleSystem> roots = Roots(effect);
            if (restart) {
                foreach (ParticleSystem system in roots) { system.Simulate(0f, true, true, true); }
            }
            for (float t = 0f; t < seconds; t += dt) {
                mover.position += Vector3.forward * speed * dt;
                foreach (ParticleSystem system in roots) { system.Simulate(dt, true, false, true); }
            }
        }

        private static GameObject Spawn(GameObject effect, Vector3 at, float scale)
        {
            GameObject instance = Object.Instantiate(effect, at, Quaternion.identity);
            instance.SetActive(true);
            instance.transform.localScale *= scale;
            return instance;
        }

        private static void SimulateTo(GameObject effect, float age)
        {
            foreach (ParticleSystem system in Roots(effect)) { system.Simulate(age, true, true, true); }
        }

        private static List<ParticleSystem> Roots(GameObject effect)
        {
            List<ParticleSystem> roots = new List<ParticleSystem>();
            foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true)) {
                Transform parent = system.transform.parent;
                if (parent == null || parent.GetComponentInParent<ParticleSystem>(true) == null) { roots.Add(system); }
            }
            return roots;
        }

        private static void Body(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Color color, Shader lit)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = name;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(parent, false);
            body.transform.SetLocalPositionAndRotation(position, rotation);
            body.transform.localScale = scale;
            body.GetComponent<MeshRenderer>().sharedMaterial = new Material(lit) { color = color };
        }

        private static void Shooter(string name, Transform parent, Vector3 position, Vector3 lookAt)
        {
            Camera camera = new GameObject(name).AddComponent<Camera>();
            camera.transform.SetParent(parent, false);
            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.48f, 0.55f);
            camera.allowHDR = false;
            camera.enabled = false;
        }

        private static Texture2D Grab(string camera)
        {
            Camera shooter = GameObject.Find(StageName + "/" + camera).GetComponent<Camera>();
            RenderTexture target = RenderTexture.GetTemporary(new RenderTextureDescriptor(ShotWidth, ShotHeight, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 4 });
            shooter.targetTexture = target;
            shooter.Render();
            shooter.targetTexture = null;
            RenderTexture.active = target;
            Texture2D tile = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tile.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tile.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            return tile;
        }

        private static string SaveStrip(List<Texture2D> tiles, string file)
        {
            Texture2D strip = new Texture2D(ShotWidth * tiles.Count, ShotHeight, TextureFormat.RGB24, false);
            for (int i = 0; i < tiles.Count; i++) {
                strip.SetPixels(i * ShotWidth, 0, ShotWidth, ShotHeight, tiles[i].GetPixels());
                Object.DestroyImmediate(tiles[i]);
            }
            strip.Apply();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            System.IO.File.WriteAllBytes(file, strip.EncodeToPNG());
            Object.DestroyImmediate(strip);
            return file;
        }

        private static void EnsureBackdrop(float width)
        {
            GameObject backdrop = GameObject.Find(BackdropName);
            if (backdrop == null) {
                backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backdrop.name = BackdropName;
                Material material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.02f, 0.02f, 0.04f) };
                backdrop.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            backdrop.transform.position = new Vector3(width * 0.5f, 5f, 15f);
            backdrop.transform.localScale = new Vector3(width + 200f, 120f, 1f);
        }
    }
}
