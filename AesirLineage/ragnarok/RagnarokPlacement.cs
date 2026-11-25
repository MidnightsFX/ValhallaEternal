using EternalHall;
using System.Collections.Generic;
using UnityEngine;

namespace AesirLineage.ragnarok {
    // Where heralds and world bosses appear: a short way from a player in the main world. Each is placed as a world
    // object that comes to life when a player is near, because a dedicated server cannot run a creature in an area
    // nobody has loaded.
    internal static class RagnarokPlacement {
        private const int Attempts = 30;
        // Below this a point counts as sea deep enough for the serpent, and above the next as dry land.
        internal const float DeepWater = 24f;
        internal const float DryLand = 31f;
        // Players this high are in a dungeon, which shares the world's x and z but not its ground.
        private const float DungeonHeight = 1000f;

        // Eternal Hall's islands, their surrounding sea included, are not part of the main world: Ragnarok leaves
        // them be.
        internal static bool OnIsland(Vector3 position) {
            return IslandAPI.IsAvailable && IslandAPI.GetIslandAt(position) != null;
        }

        // Where the players in the main world stand, dungeons and islands left out.
        internal static List<Vector3> MainWorldPlayers() {
            List<Vector3> positions = new List<Vector3>();
            foreach (ZNetPeer peer in ZNet.instance.GetPeers()) {
                if (!peer.IsReady()) { continue; }
                ZDO character = ZDOMan.instance.GetZDO(peer.m_characterID);
                if (character != null) { positions.Add(character.GetPosition()); }
            }
            if (!ZNet.instance.IsDedicated() && Player.m_localPlayer != null && !Player.m_localPlayer.IsDead()) {
                positions.Add(Player.m_localPlayer.transform.position);
            }
            positions.RemoveAll(p => p.y > DungeonHeight || OnIsland(p));
            return positions;
        }

        // Points `minDistance` to `maxDistance` from `near`, outside the islands: on dry land a little above the ground,
        // or at sea in water deep enough for a serpent, just under the surface.
        internal static IEnumerable<Vector3> PointsNear(Vector3 near, bool atSea, float minDistance, float maxDistance) {
            WorldGenerator generator = WorldGenerator.instance;
            if (generator == null) { yield break; }
            for (int attempt = 0; attempt < Attempts; attempt++) {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(minDistance, maxDistance);
                Vector3 point = near + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (OnIsland(point)) { continue; }
                float height = generator.GetHeight(point.x, point.z);
                if (atSea) {
                    if (height > DeepWater) { continue; }
                    point.y = ZoneSystem.instance.m_waterLevel - 1f;
                } else {
                    if (height < DryLand) { continue; }
                    point.y = height + 1f;
                }
                yield return point;
            }
        }

        // An unowned creature of this prefab at this point: the first player to come near takes it, as vanilla does
        // for any unowned creature.
        internal static ZDO Create(string prefabName, Vector3 point, int level) {
            GameObject prefab = ZNetScene.instance?.GetPrefab(prefabName);
            ZNetView view = prefab != null ? prefab.GetComponent<ZNetView>() : null;
            Character character = prefab != null ? prefab.GetComponent<Character>() : null;
            if (view == null || character == null) { return null; }

            int hash = prefabName.GetStableHashCode();
            ZDO zdo = ZDOMan.instance.CreateNewZDO(point, hash);
            zdo.Persistent = view.m_persistent;
            zdo.Type = view.m_type;
            zdo.Distant = view.m_distant;
            zdo.SetPrefab(hash);
            zdo.SetRotation(Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            zdo.Set(ZDOVars.s_level, level);
            // Written here as well as by the creature's owner when it wakes, so a player who sees it before anyone owns
            // it reads its real health.
            zdo.Set(ZDOVars.s_maxHealth, character.GetMaxHealthBase() * level);
            zdo.SetOwner(0L);
            return zdo;
        }

        // How the state saves a creature's ZDO id.
        internal static string KeyOf(ZDOID id) => $"{id.UserID}:{id.ID}";

        internal static ZDOID? ParseId(string key) {
            string[] parts = (key ?? "").Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out long user) || !uint.TryParse(parts[1], out uint id)) { return null; }
            return new ZDOID(user, id);
        }
    }
}
