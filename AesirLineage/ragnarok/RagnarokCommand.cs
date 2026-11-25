using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    // al-ragnarok: shows or changes the Ragnarok cycle, or places a herald or world boss near the admin.
    //
    // The cycle lives on the server, so the admin's machine sends the command over its own connection and the
    // server checks that connection against adminlist.txt, as AdminPoints does, then answers with what it did.
    internal class RagnarokCommand : AdminConsoleCommand, ICompletesArguments {
        private const string RequestRpc = "AL_RagnarokCommand";
        private const string ReplyRpc = "AL_RagnarokCommandReply";

        public override string Name => "al-ragnarok";
        public override string Help => "[status | now | days <in-game days> | restart | herald [biome] | boss [name]] - shows or changes the Ragnarok cycle; herald and boss place one near you without touching the cycle";

        public List<string> ArgumentOptions(int index, string[] before) {
            if (index == 0) { return new List<string>() { "status", "now", "days", "restart", "herald", "boss" }; }
            if (index == 1 && before[0] == "herald") { return RagnarokHeralds.All.Select(h => h.Biome.ToString()).ToList(); }
            if (index == 1 && before[0] == "boss") { return RagnarokWorldBosses.All.Select(b => b.Key).ToList(); }
            return null;
        }

        protected override void Execute(string[] args) {
            if (ZNet.instance == null) { return; }
            Vector3? at = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : (Vector3?)null;
            if (ZNet.instance.IsServer()) {
                Print(Run(args, at));
                return;
            }
            ZPackage request = new ZPackage();
            request.Write(args.Length);
            foreach (string arg in args) { request.Write(arg); }
            request.Write(at.HasValue);
            if (at.HasValue) { request.Write(at.Value); }
            ZNet.instance.GetServerRPC()?.Invoke(RequestRpc, request);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static class RegisterRpc {
            private static void Postfix(ZNet __instance, ZNetPeer peer) {
                if (__instance.IsServer()) {
                    peer.m_rpc.Register<ZPackage>(RequestRpc, OnRequest);
                } else {
                    peer.m_rpc.Register<ZPackage>(ReplyRpc, OnReply);
                }
            }
        }

        // On the server, from an admin's machine.
        private static void OnRequest(ZRpc rpc, ZPackage request) {
            string hostName = rpc.GetSocket().GetHostName();
            List<string> lines;
            if (!ZNet.instance.IsAdmin(hostName)) {
                lines = new List<string>() { "$al_cmd_server_not_admin" };
            } else {
                string[] args = new string[request.ReadInt()];
                for (int i = 0; i < args.Length; i++) { args[i] = request.ReadString(); }
                Vector3? at = request.ReadBool() ? request.ReadVector3() : (Vector3?)null;
                Logger.LogInfo($"Admin {hostName} runs al-ragnarok {string.Join(" ", args)}.");
                lines = Run(args, at);
            }
            ZPackage reply = new ZPackage();
            reply.Write(lines.Count);
            foreach (string line in lines) { reply.Write(line); }
            rpc.Invoke(ReplyRpc, reply);
        }

        // On the admin's machine.
        private static void OnReply(ZRpc rpc, ZPackage reply) {
            List<string> lines = new List<string>();
            int count = reply.ReadInt();
            for (int i = 0; i < count; i++) { lines.Add(reply.ReadString()); }
            Print(lines);
        }

        private static void Print(List<string> lines) {
            foreach (string line in lines) {
                string text = Localization.instance.Localize(line);
                global::Console.instance?.Print(text);
                Logger.LogInfo(text);
            }
        }

        // On the server. `at` is where the admin stands, for placing a herald or world boss.
        private static List<string> Run(string[] args, Vector3? at) {
            string message;
            switch (args.Length > 0 ? args[0] : "status") {
                case "now":
                    RagnarokServer.TriggerNow(out message);
                    break;
                case "days":
                    if (args.Length < 2 || !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float days)) {
                        message = "Usage: al-ragnarok days <in-game days left>";
                        break;
                    }
                    RagnarokServer.SetDaysLeft(days, out message);
                    break;
                case "restart":
                    RagnarokServer.Restart(out message);
                    break;
                case "herald":
                    message = PlaceHerald(args, at);
                    break;
                case "boss":
                    message = PlaceWorldBoss(args, at);
                    break;
                default:
                    return Status();
            }
            return new List<string>() { message };
        }

        private static List<string> Status() {
            RagnarokState state = RagnarokSave.State;
            if (state == null) { return new List<string>() { "No world is loaded on this server." }; }
            RagnarokSnapshot snapshot = Ragnarok.Current;
            List<string> lines = new List<string>() { $"Ragnarok cycle {state.Cycle}: {(snapshot.Active ? state.Phase.ToString() : "off")}." };
            if (!snapshot.Active) {
                lines.Add(ValConfig.RagnarokEnabled.Value ? "The cycle starts once the world has loaded." : "Turn on [Ragnarok] RagnarokEnabled in the config to start the cycle.");
                return lines;
            }
            lines.Add($"  {RagnarokHud.FormatTime(Ragnarok.SecondsLeft, Ragnarok.DayLengthSeconds)} left of {ValConfig.RagnarokCycleDays.Value} in-game days " +
                $"({Ragnarok.Progress * 100f:0.0}% through, in-game day {EnvMan.instance?.GetDay() ?? 0}).");
            lines.Add($"  Spawn rate x{Ragnarok.SpawnRate(false):0.00} by day, x{Ragnarok.SpawnRate(true):0.00} at night.");
            lines.Add($"  Herald phase {(Ragnarok.InHeraldPhase ? "under way" : "not yet")}; {RagnarokHeralds.Alive(state)} herald(s) alive.");
            if (RagnarokRing.Active) {
                int stage = RagnarokRing.Stage;
                RagnarokRing.Circle ring = RagnarokRing.At(stage);
                RagnarokRing.Circle final = RagnarokRing.Final;
                lines.Add($"  Ring step {stage} of {RagnarokRing.Steps}: {ring.Radius:0}m around {ring.Center.x:0}, {ring.Center.y:0}; " +
                    $"smallest {final.Radius:0}m around {final.Center.x:0}, {final.Center.y:0}.");
            }
            if (state.Phase != RagnarokPhase.Ragnarok) { return lines; }
            ZDO boss = RagnarokPlacement.ParseId(state.WorldBoss) is ZDOID id ? ZDOMan.instance.GetZDO(id) : null;
            lines.Add(boss == null
                ? "  The world boss rises once a player is in the main world."
                : $"  {state.WorldBossPrefab} holds Ragnarok, at {boss.GetPosition().x:0}, {boss.GetPosition().z:0}.");
            return lines;
        }

        private static string PlaceHerald(string[] args, Vector3? at) {
            RagnarokState state = RagnarokSave.State;
            if (state == null || !at.HasValue) { return "Run this in game, near where the herald should appear."; }
            RagnarokHeralds.Herald choice = null;
            if (args.Length >= 2) {
                choice = RagnarokHeralds.Find(args[1]);
                if (choice == null) { return $"No herald for '{args[1]}'. Choose one of: {string.Join(", ", RagnarokHeralds.All.Select(h => h.Biome))}."; }
            }
            if (!RagnarokHeralds.TrySpawnNear(state, at.Value, choice, out RagnarokHeralds.Herald herald, out Vector3 position)) {
                return "No place for a herald was found nearby. Try somewhere more open (or at sea for the Ocean herald).";
            }
            return $"A {herald.Biome} herald ({herald.Prefab}) waits at {position.x:0}, {position.z:0}, about {Vector3.Distance(position, at.Value):0}m away.";
        }

        private static string PlaceWorldBoss(string[] args, Vector3? at) {
            if (RagnarokSave.State == null || !at.HasValue) { return "Run this in game, near where the world boss should appear."; }
            RagnarokWorldBosses.WorldBoss choice = null;
            if (args.Length >= 2) {
                choice = RagnarokWorldBosses.Find(args[1]);
                if (choice == null) { return $"No world boss '{args[1]}'. Choose one of: {string.Join(", ", RagnarokWorldBosses.All.Select(b => b.Key))}."; }
            }
            if (!RagnarokWorldBosses.TryRaiseNear(at.Value, choice, out RagnarokWorldBosses.WorldBoss boss, out ZDO zdo)) {
                return "No place for a world boss was found nearby. Try somewhere with more dry land.";
            }
            Vector3 position = zdo.GetPosition();
            return $"{boss.Key} ({boss.Prefab}) waits at {position.x:0}, {position.z:0}, about {Vector3.Distance(position, at.Value):0}m away.";
        }
    }
}
