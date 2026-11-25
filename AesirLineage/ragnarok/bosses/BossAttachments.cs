using System.Linq;
using UnityEngine;

namespace AesirLineage.ragnarok.bosses {
    // Parts of vanilla creatures' bodies (flames, smoke, a sound loop; see BossKit.Attachment) put on a world boss's
    // bones as it spawns. They are game assets, so the boss's prefab in the bundle cannot carry them itself. Particles
    // that emitted from the vanilla creature's own mesh emit from the boss's.
    internal class BossAttachments : MonoBehaviour {
        public GameObject[] m_parts = new GameObject[0];
        public string[] m_bones = new string[0];

        private void Awake() {
            Transform[] bones = GetComponentsInChildren<Transform>(true);
            SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer skin = skins.FirstOrDefault(s => s.name.EndsWith("LOD0")) ?? skins.FirstOrDefault();
            for (int i = 0; i < m_parts.Length && i < m_bones.Length; i++) {
                Transform bone = bones.FirstOrDefault(t => t.name == m_bones[i]);
                if (m_parts[i] == null || bone == null) { continue; }
                GameObject part = Instantiate(m_parts[i], bone, false);
                part.name = m_parts[i].name;
                part.transform.localPosition = Vector3.zero;
                foreach (ParticleSystem particles in part.GetComponentsInChildren<ParticleSystem>(true)) {
                    ParticleSystem.ShapeModule shape = particles.shape;
                    if (skin != null && (shape.shapeType == ParticleSystemShapeType.SkinnedMeshRenderer || shape.shapeType == ParticleSystemShapeType.MeshRenderer)) {
                        shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
                        shape.skinnedMeshRenderer = skin;
                    }
                }
            }
        }
    }
}
