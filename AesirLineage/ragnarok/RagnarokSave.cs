using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using AesirLineage.common;
using YamlDotNet.Serialization;

namespace AesirLineage.ragnarok {
    // Where the cycle hooks into the server's world load and save. The server keeps the cycle's state in a file of
    // its own beside the world.
    //
    //   ZNet.Awake        -> load the state (server)
    //   ServerLoadWorld   -> start the cycle's tick
    //   ZNet.SaveWorld    -> save it alongside the world
    internal static class RagnarokSave {
        private const string Extension = ".aesirlineage.ragnarok.yaml";

        // Server only. Null on clients and when no world is loaded.
        internal static RagnarokState State;
        private static string path;

        private static readonly ISerializer Serializer = new SerializerBuilder().Build();
        // A key an older version wrote and this one no longer has is skipped, not the whole file.
        private static readonly IDeserializer Deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class ZNet_Awake_Patch {
            private static void Prefix() {
                Ragnarok.Reset();
                RagnarokServer.Reset();
                State = null;
                path = null;
                if (!ZNet.m_isServer || ZNet.m_world == null) { return; }
                Load(ZNet.m_world);
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.ServerLoadWorld))]
        private static class ZNet_ServerLoadWorld_Patch {
            private static void Postfix(ZNet __instance) {
                if (State != null) { __instance.StartCoroutine(RagnarokServer.Run()); }
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorld))]
        private static class ZNet_SaveWorld_Patch {
            private static void Postfix() {
                Save();
            }
        }

        private static string PathFor(World world) {
            string folder = world.m_fileSource == FileHelpers.FileSource.Local
                ? SaveSystem.GetWorldsSaveRootPath(FileHelpers.FileSource.Local)
                // A cloud save cannot carry an extra file; keep it with the mod instead.
                : Path.Combine(Paths.ConfigPath, ValConfig.cfgFolder, "SavedData");
            return Path.Combine(folder, world.m_name + Extension);
        }

        private static void Load(World world) {
            path = PathFor(world);
            try {
                if (File.Exists(path)) {
                    State = Deserializer.Deserialize<RagnarokState>(File.ReadAllText(path));
                    Logger.LogInfo($"Loaded Ragnarok cycle {State?.Cycle} from {path}");
                }
            } catch (Exception e) {
                // Keep the bad copy for the admin and start a new cycle rather than refusing to load the world.
                Logger.LogError($"Could not read {path}, the Ragnarok cycle starts over: {e.Message}");
                try { File.Copy(path, path + ".broken", true); } catch { }
            }
            State ??= new RagnarokState();
            State.Heralds ??= new List<string>();
            State.WorldBossPrefab ??= "";
            State.WorldBoss ??= "";
        }

        internal static void Save() {
            if (State == null || string.IsNullOrEmpty(path)) { return; }
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, "# Aesir Lineage's Ragnarok cycle. Written by the game; edit only while the server is stopped.\n" + Serializer.Serialize(State));
                if (File.Exists(path)) { File.Delete(path); }
                File.Move(tmp, path);
            } catch (Exception e) {
                Logger.LogError($"Could not save the Ragnarok cycle to {path}: {e.Message}");
            }
        }
    }
}
