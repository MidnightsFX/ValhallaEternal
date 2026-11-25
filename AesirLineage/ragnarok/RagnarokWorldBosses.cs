using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AesirLineage.ragnarok {
    // The world bosses that end a Ragnarok cycle, one chosen at random each time. Each is a creature prefab of its own
    // from the mod's asset bundle (built in AesirLineageUnity, Aesir Lineage > Build world boss prefabs), given attacks
    // built from vanilla ones (see bosses/). Like the heralds they are fixed in code, because every machine has to
    // register the same prefabs at startup, before any server config arrives.
    internal static class RagnarokWorldBosses {
        // Close enough that the boss is in the area the player has loaded, and comes for them at once.
        private const float MinDistance = 35f;
        private const float MaxDistance = 70f;

        internal class WorldBoss {
            internal string Key;
            internal Action<GameObject> Build;
            // False when the bundle has no prefab for it; it is then never raised.
            internal bool Registered;
            internal string Prefab => $"AL_WorldBoss_{Key}";
            internal string NameToken => $"$al_worldboss_{Key.ToLowerInvariant()}";
        }

        internal static readonly List<WorldBoss> All = new List<WorldBoss>() {
            new WorldBoss() { Key = "Modgud", Build = bosses.Modgud.Build },
            new WorldBoss() { Key = "Sinmara", Build = bosses.Sinmara.Build },
            new WorldBoss() { Key = "Hraesvelgr", Build = bosses.Hraesvelgr.Build },
        };

        internal static WorldBoss Find(string name) {
            return All.FirstOrDefault(b => string.Equals(b.Key, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(b.Prefab, name, StringComparison.OrdinalIgnoreCase));
        }

        // ---------------------------------------------------------------------------------------
        // Prefabs (every machine)
        // ---------------------------------------------------------------------------------------

        internal static void Init() {
            CreatureManager.OnVanillaCreaturesAvailable += CreatePrefabs;
        }

        private static void CreatePrefabs() {
            CreatureManager.OnVanillaCreaturesAvailable -= CreatePrefabs;
            foreach (WorldBoss boss in All) {
                GameObject prefab = AesirLineage.EmbeddedResourceBundle?.LoadAsset<GameObject>(boss.Prefab);
                if (prefab == null) {
                    Logger.LogWarning($"The mod's asset bundle has no {boss.Prefab}; Ragnarok will not raise {boss.Key}.");
                    continue;
                }
                try {
                    boss.Build(prefab);
                } catch (Exception e) {
                    Logger.LogWarning($"Building the world boss {boss.Key} failed; it will lack some of its attacks or effects: {e}");
                }
                // Hunts the nearest player, as raiders do, rather than waiting to be found, and like vanilla's bosses
                // fights only players: any other faction would have it chasing seagulls and greydwarfs instead.
                prefab.GetComponent<MonsterAI>().m_enableHuntPlayer = true;
                prefab.GetComponent<Character>().m_faction = Character.Faction.Boss;
                prefab.AddComponent<RagnarokWorldBoss>();
                // The prefab's effects are Jotunn mocks of vanilla ones, resolved here.
                CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                boss.Registered = true;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Raising (server)
        // ---------------------------------------------------------------------------------------

        // A random world boss near a random player in the main world, on dry land. With the ring on, only near a player
        // inside its last circle, and inside it.
        internal static bool TryRaiseNearPlayer(out WorldBoss boss, out ZDO zdo) {
            boss = null;
            zdo = null;
            List<Vector3> players = RagnarokPlacement.MainWorldPlayers();
            Func<Vector3, bool> allowed = null;
            if (RagnarokRing.Active) {
                RagnarokRing.Circle final = RagnarokRing.Final;
                allowed = final.Contains;
                players.RemoveAll(p => !final.Contains(p));
            }
            while (players.Count > 0) {
                int pick = UnityEngine.Random.Range(0, players.Count);
                if (TryRaiseNear(players[pick], null, out boss, out zdo, allowed)) { return true; }
                players.RemoveAt(pick);
            }
            return false;
        }

        // The chosen world boss, or a random one, a short way from a point on dry land.
        internal static bool TryRaiseNear(Vector3 near, WorldBoss choice, out WorldBoss boss, out ZDO zdo, Func<Vector3, bool> allowed = null) {
            List<WorldBoss> ready = All.Where(b => b.Registered).ToList();
            boss = choice ?? (ready.Count > 0 ? ready[UnityEngine.Random.Range(0, ready.Count)] : null);
            zdo = null;
            if (ZNetScene.instance == null || boss == null || !boss.Registered) { return false; }
            foreach (Vector3 point in RagnarokPlacement.PointsNear(near, false, MinDistance, MaxDistance)) {
                if (allowed != null && !allowed(point)) { continue; }
                zdo = RagnarokPlacement.Create(boss.Prefab, point, 1);
                if (zdo != null) {
                    Logger.LogInfo($"The world boss {boss.Key} ({boss.Prefab}) appeared at {point.x:0}, {point.y:0}, {point.z:0}.");
                    return true;
                }
            }
            return false;
        }
    }
}
