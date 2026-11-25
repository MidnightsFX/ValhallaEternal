using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    // Marks a herald's prefab, so a patch can tell a herald from the creature it was drawn from.
    public class RagnarokHerald : MonoBehaviour { }

    // Marks a world boss's prefab, likewise.
    public class RagnarokWorldBoss : MonoBehaviour { }

    // What heralds and world bosses share: each is a vanilla creature cloned into a boss of its own.
    internal static class RagnarokCreatures {
        // A copy of the vanilla creature with its own name and a boss health bar, that is fought and never kept.
        // Null, with a warning, when the creature is not there to copy.
        internal static GameObject CloneAsBoss(string prefabName, string basePrefab, string nameToken, float scale) {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(prefabName, basePrefab);
            Character character = prefab != null ? prefab.GetComponent<Character>() : null;
            if (character == null) {
                Logger.LogWarning($"{basePrefab} is not available to clone; there will be no {prefabName}.");
                return null;
            }
            character.m_name = nameToken;
            character.m_boss = true;
            character.m_defeatSetGlobalKey = "";
            prefab.transform.localScale *= scale;
            foreach (Tameable tameable in prefab.GetComponentsInChildren<Tameable>(true)) { Object.DestroyImmediate(tameable); }
            foreach (Procreation procreation in prefab.GetComponentsInChildren<Procreation>(true)) { Object.DestroyImmediate(procreation); }
            foreach (Growup growup in prefab.GetComponentsInChildren<Growup>(true)) { Object.DestroyImmediate(growup); }
            if (prefab.GetComponent<MonsterAI>() is MonsterAI ai) { ai.m_despawnInDay = false; }
            return prefab;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.GetMaxHealthBase))]
        private static class Character_GetMaxHealthBase_Patch {
            private static void Postfix(Character __instance, ref float __result) {
                // Asked of a character already destroyed, whose components can no longer be read.
                if (__instance == null) { return; }
                if (__instance.TryGetComponent(out RagnarokHerald _)) {
                    __result *= ValConfig.HeraldHealthMultiplier.Value;
                } else if (__instance.TryGetComponent(out RagnarokWorldBoss _)) {
                    __result *= ValConfig.WorldBossHealthMultiplier.Value;
                }
            }
        }
    }
}
