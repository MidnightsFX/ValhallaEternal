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
