using HarmonyLib;
using UnityEngine;

namespace AesirLineage.ragnarok {
    // Server -> client traffic for the Ragnarok cycle, on each client's own connection, so a client only takes cycle
    // updates from its server.
    //
    //   State      the cycle and its ring (on joining, and whenever it changes)
    //   Herald     a herald appeared, by biome, for the message
    //   WorldBoss  the world boss rose (by prefab, with its biome) or was slain, for the message
    internal static class RagnarokNetwork {
        private const string StateRpc = "AL_RagnarokState";
        private const string HeraldRpc = "AL_RagnarokHerald";
        private const string WorldBossRpc = "AL_RagnarokWorldBoss";

        private const int StateFormat = 3;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static class ZNet_OnNewConnection_Patch {
            private static void Postfix(ZNet __instance, ZNetPeer peer) {
                if (__instance.IsServer()) { return; }
                peer.m_rpc.Register<ZPackage>(StateRpc, OnClientState);
                peer.m_rpc.Register<int>(HeraldRpc, OnClientHerald);
                peer.m_rpc.Register<string, int, bool>(WorldBossRpc, OnClientWorldBoss);
            }
        }

        // Before the client spawns, so its HUD and spawns have the cycle from the start.
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
        private static class ZNet_RPC_PeerInfo_Patch {
            private static void Postfix(ZNet __instance, ZRpc rpc) {
                if (!__instance.IsServer() || RagnarokSave.State == null) { return; }
                ZNetPeer peer = __instance.GetPeer(rpc);
                if (peer == null || !peer.IsReady()) { return; }
                peer.m_rpc.Invoke(StateRpc, WriteState(Ragnarok.Current));
            }
        }

        // ---------------------------------------------------------------------------------------
        // State
        // ---------------------------------------------------------------------------------------

        // Server: makes the snapshot live here and sends it to every client.
        internal static void BroadcastState(RagnarokSnapshot snapshot) {
            Ragnarok.Publish(snapshot);
            if (ZNet.instance == null || !ZNet.instance.IsServer()) { return; }
            foreach (ZNetPeer peer in ZNet.instance.GetPeers()) {
                if (peer.IsReady()) { peer.m_rpc.Invoke(StateRpc, WriteState(snapshot)); }
            }
        }

        private static ZPackage WriteState(RagnarokSnapshot snapshot) {
            ZPackage package = new ZPackage();
            package.Write(StateFormat);
            package.Write(snapshot.Active);
            package.Write((int)snapshot.Phase);
            package.Write(snapshot.Cycle);
            package.Write(snapshot.StartSeconds);
            package.Write(snapshot.EndSeconds);
            package.Write(snapshot.WorldBossPrefab);
            package.Write(snapshot.RingCenter.x);
            package.Write(snapshot.RingCenter.y);
            package.Write(snapshot.RingRadius);
            return package;
        }

        private static void OnClientState(ZRpc rpc, ZPackage package) {
            int format = package.ReadInt();
            if (format != StateFormat) {
                Logger.LogError($"The server sent the Ragnarok cycle in format {format}, this client understands {StateFormat}. Update Aesir Lineage on both.");
                return;
            }
            Ragnarok.Publish(new RagnarokSnapshot(package.ReadBool(), (RagnarokPhase)package.ReadInt(), package.ReadInt(),
                package.ReadDouble(), package.ReadDouble(), package.ReadString(),
                new Vector2(package.ReadSingle(), package.ReadSingle()), package.ReadSingle()));
        }

        // ---------------------------------------------------------------------------------------
        // Heralds and the world boss
        // ---------------------------------------------------------------------------------------

        internal static void BroadcastHerald(Heightmap.Biome biome) {
            foreach (ZNetPeer peer in ZNet.instance.GetPeers()) {
                if (peer.IsReady()) { peer.m_rpc.Invoke(HeraldRpc, (int)biome); }
            }
            if (!ZNet.instance.IsDedicated()) { ShowHerald(biome); }
        }

        private static void OnClientHerald(ZRpc rpc, int biome) {
            ShowHerald((Heightmap.Biome)biome);
        }

        private static void ShowHerald(Heightmap.Biome biome) {
            string name = Localization.instance.Localize(RagnarokHeralds.ForBiome(biome)?.NameToken ?? "");
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$al_herald_appeared", name, Ragnarok.BiomeName(biome)));
        }

        internal static void BroadcastWorldBoss(RagnarokWorldBosses.WorldBoss boss, Heightmap.Biome biome, bool slain) {
            foreach (ZNetPeer peer in ZNet.instance.GetPeers()) {
                if (peer.IsReady()) { peer.m_rpc.Invoke(WorldBossRpc, boss.Prefab, (int)biome, slain); }
            }
            if (!ZNet.instance.IsDedicated()) { ShowWorldBoss(boss.Prefab, biome, slain); }
        }

        private static void OnClientWorldBoss(ZRpc rpc, string prefab, int biome, bool slain) {
            ShowWorldBoss(prefab, (Heightmap.Biome)biome, slain);
        }

        private static void ShowWorldBoss(string prefab, Heightmap.Biome biome, bool slain) {
            string name = Localization.instance.Localize(RagnarokWorldBosses.Find(prefab)?.NameToken ?? prefab);
            string text = slain
                ? Localization.instance.Localize("$al_worldboss_slain", name)
                : Localization.instance.Localize("$al_worldboss_rises", name, Ragnarok.BiomeName(biome));
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, text);
        }
    }
}
