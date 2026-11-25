using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    // Creatures spawn faster as Ragnarok nears. The spawn system runs on whichever machine owns the zone,
    // usually a client, so every machine scales its own spawns from the synced cycle and config.
    //
    // A rate of 3 means a spawner tries three times as often and allows three times as many of its creature
    // nearby; scaling only one of the two would barely show, since spawners mostly sit at their cap. Raids
    // keep their own pace. The spawn lists are shared, so the values are put back once the update is done.
    internal static class RagnarokSpawns {
        [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList))]
        private static class SpawnSystem_UpdateSpawnList_Patch {
            private static void Prefix(SpawnSystem __instance, List<SpawnSystem.SpawnData> spawners, bool eventSpawners, out Vector2[] __state) {
                __state = null;
                if (eventSpawners || spawners == null || !Ragnarok.Current.Active) { return; }
                float rate = Ragnarok.SpawnRate(EnvMan.IsNight());
                if (Mathf.Approximately(rate, 1f)) { return; }
                if (!ValConfig.ScaleIslandSpawns.Value && RagnarokPlacement.OnIsland(__instance.transform.position)) { return; }

                __state = new Vector2[spawners.Count];
                for (int i = 0; i < spawners.Count; i++) {
                    SpawnSystem.SpawnData spawner = spawners[i];
                    __state[i] = new Vector2(spawner.m_spawnInterval, spawner.m_maxSpawned);
                    spawner.m_spawnInterval = Mathf.Max(1f, spawner.m_spawnInterval / rate);
                    if (spawner.m_maxSpawned > 0) { spawner.m_maxSpawned = Mathf.Max(1, Mathf.RoundToInt(spawner.m_maxSpawned * rate)); }
                }
            }

            // A finalizer rather than a postfix, so the shared values go back even if the update throws.
            private static void Finalizer(List<SpawnSystem.SpawnData> spawners, Vector2[] __state) {
                if (__state == null) { return; }
                for (int i = 0; i < __state.Length && i < spawners.Count; i++) {
                    spawners[i].m_spawnInterval = __state[i].x;
                    spawners[i].m_maxSpawned = (int)__state[i].y;
                }
            }
        }
    }
}
