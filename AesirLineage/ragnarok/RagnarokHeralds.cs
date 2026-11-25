using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    // Heralds of Ragnarok: one miniboss per biome, each a larger, tougher copy of a vanilla creature with
    // its own name and a boss health bar. In the last part of a cycle the server keeps a few of them in the
    // main world, each appearing near a player there in the biome they stand in. Killing one gives star points
    // to the deity the RagnarokHeralds setting names for it (Deeds).
    //
    // The prefabs are fixed in code rather than read from config, because every machine has to register
    // the same prefabs at startup, before any server config arrives. What a herald is worth (stars and
    // health) is read from the synced config when it appears.
    internal static class RagnarokHeralds {
        internal class Herald {
            internal Heightmap.Biome Biome;
            internal string BasePrefab;
            internal float Scale;
            internal string Prefab => $"AL_Herald_{Biome}";
            internal string NameToken => $"$al_herald_{Biome.ToString().ToLowerInvariant()}";
        }

        internal static readonly List<Herald> All = new List<Herald>() {
            new Herald() { Biome = Heightmap.Biome.Meadows, BasePrefab = "Greydwarf_Elite", Scale = 1.4f },
            new Herald() { Biome = Heightmap.Biome.BlackForest, BasePrefab = "Troll", Scale = 1.3f },
            new Herald() { Biome = Heightmap.Biome.Swamp, BasePrefab = "Draugr_Elite", Scale = 1.4f },
            new Herald() { Biome = Heightmap.Biome.Mountain, BasePrefab = "Fenring", Scale = 1.4f },
            new Herald() { Biome = Heightmap.Biome.Plains, BasePrefab = "GoblinBrute", Scale = 1.3f },
            new Herald() { Biome = Heightmap.Biome.Mistlands, BasePrefab = "SeekerBrute", Scale = 1.3f },
            new Herald() { Biome = Heightmap.Biome.AshLands, BasePrefab = "Morgen", Scale = 1.25f },
            new Herald() { Biome = Heightmap.Biome.DeepNorth, BasePrefab = "StoneGolem", Scale = 1.2f },
            new Herald() { Biome = Heightmap.Biome.Ocean, BasePrefab = "Serpent", Scale = 1.3f },
        };

        // Far enough that a herald is found rather than met.
        private const float MinDistance = 70f;
        private const float MaxDistance = 160f;

        private static float nextSpawnTime;

        internal static Herald ForBiome(Heightmap.Biome biome) {
            return All.FirstOrDefault(h => h.Biome == biome);
        }

        internal static Herald Find(string name) {
            return All.FirstOrDefault(h => string.Equals(h.Biome.ToString(), name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(h.Prefab, name, StringComparison.OrdinalIgnoreCase));
        }

        internal static void ResetTimer() {
            nextSpawnTime = 0f;
        }

        // ---------------------------------------------------------------------------------------
        // Prefabs (every machine)
        // ---------------------------------------------------------------------------------------

        internal static void Init() {
            CreatureManager.OnVanillaCreaturesAvailable += CreatePrefabs;
        }

        private static void CreatePrefabs() {
            CreatureManager.OnVanillaCreaturesAvailable -= CreatePrefabs;
            foreach (Herald herald in All) {
                GameObject prefab = RagnarokCreatures.CloneAsBoss(herald.Prefab, herald.BasePrefab, herald.NameToken, herald.Scale);
                if (prefab == null) { continue; }
                prefab.AddComponent<RagnarokHerald>();
                CreatureManager.Instance.AddCreature(new CustomCreature(prefab, false));
            }
        }

        // ---------------------------------------------------------------------------------------
        // Spawning (server)
        // ---------------------------------------------------------------------------------------

        // Called every server tick while a cycle runs.
        internal static void Tick(RagnarokState state) {
            if (!Ragnarok.InHeraldPhase || ValConfig.MaxHeralds.Value <= 0) { return; }
            if (Time.realtimeSinceStartup < nextSpawnTime) { return; }
            nextSpawnTime = Time.realtimeSinceStartup + ValConfig.HeraldSpawnMinutes.Value * 60f;

            ForgetDead(state);
            if (state.Heralds.Count >= ValConfig.MaxHeralds.Value) { return; }
            List<Vector3> players = RagnarokPlacement.MainWorldPlayers();
            if (players.Count == 0) { return; }
            Vector3 near = players[UnityEngine.Random.Range(0, players.Count)];
            if (TrySpawnNear(state, near, null, out Herald herald, out _)) {
                RagnarokNetwork.BroadcastHerald(herald.Biome);
            }
        }

        // Heralds still in the world, by their saved ids.
        internal static int Alive(RagnarokState state) {
            ForgetDead(state);
            return state.Heralds.Count;
        }

        private static void ForgetDead(RagnarokState state) {
            int before = state.Heralds.Count;
            state.Heralds.RemoveAll(key => !(RagnarokPlacement.ParseId(key) is ZDOID id) || ZDOMan.instance.GetZDO(id) == null);
            if (state.Heralds.Count != before) { RagnarokSave.Save(); }
        }

        // Places a herald a short way from a point. A null choice takes the herald of the biome it lands in. Serpents
        // are only placed for a player at sea.
        internal static bool TrySpawnNear(RagnarokState state, Vector3 near, Herald choice, out Herald herald, out Vector3 position) {
            herald = null;
            position = Vector3.zero;
            WorldGenerator generator = WorldGenerator.instance;
            if (generator == null || ZNetScene.instance == null) { return false; }
            bool atSea = choice != null ? choice.Biome == Heightmap.Biome.Ocean : generator.GetHeight(near.x, near.z) < RagnarokPlacement.DeepWater;

            foreach (Vector3 point in RagnarokPlacement.PointsNear(near, atSea, MinDistance, MaxDistance)) {
                Herald candidate = choice ?? ForBiome(atSea ? Heightmap.Biome.Ocean : generator.GetBiome(point));
                ZDO zdo = candidate == null ? null : RagnarokPlacement.Create(candidate.Prefab, point, ValConfig.HeraldStars.Value + 1);
                if (zdo == null) { continue; }
                state.Heralds.Add(RagnarokPlacement.KeyOf(zdo.m_uid));
                RagnarokSave.Save();
                herald = candidate;
                position = point;
                Logger.LogInfo($"A {candidate.Biome} herald ({candidate.Prefab}) appeared at {point.x:0}, {point.y:0}, {point.z:0}.");
                return true;
            }
            return false;
        }
    }
}
