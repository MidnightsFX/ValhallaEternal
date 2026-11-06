using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using AesirLineage.common;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// Admins giving or taking away another player's star points, by the <c>al-give-points</c> and
    /// <c>al-remove-points</c> commands. Points live in each character's own save, so only that player's
    /// machine can change them; the server checks the admin and carries the change there.
    /// </summary>
    /// <remarks>
    /// The admin's machine asks the server over its own connection, so the server can check that
    /// connection against adminlist.txt the way the game's kick and ban do. A routed RPC can't be used for
    /// that: its sender writes its own id. The server then tells the player's machine over that player's
    /// connection, which only the server can send on. The player's machine reports back by routed RPC; at
    /// worst a forged report prints a line in an admin's console.
    /// </remarks>
    internal static class AdminPoints {
        private const string RequestRpc = "AL_PointsRequest";
        private const string ChangeRpc = "AL_PointsChange";
        private const string ReportRpc = "AL_PointsReport";

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static class RegisterPeerRpc {
            private static void Postfix(ZNet __instance, ZNetPeer peer) {
                if (__instance.IsServer()) {
                    peer.m_rpc.Register<string, int, int>(RequestRpc, OnRequest);
                } else {
                    peer.m_rpc.Register<int, int, long>(ChangeRpc, OnChange);
                }
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRoutedRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<string, List<string>>(ReportRpc, OnReport);
            }
        }

        /// <summary>Asks for a player's points with a deity to change by <paramref name="amount"/>, negative to take them away.</summary>
        internal static void Send(string playerName, Deity deity, int amount) {
            if (ZNet.instance == null) { return; }
            if (ZNet.instance.IsServer()) {
                // The host's own command, already checked by AdminConsoleCommand.
                Route(ZNet.GetUID(), "host", playerName, deity, amount);
                return;
            }
            ZNet.instance.GetServerRPC()?.Invoke(RequestRpc, playerName, (int)deity, amount);
        }

        // On the server, from an admin's machine.
        private static void OnRequest(ZRpc rpc, string playerName, int deity, int amount) {
            ZNetPeer admin = ZNet.instance.GetPeer(rpc);
            if (admin == null) { return; }
            string hostName = rpc.GetSocket().GetHostName();
            if (!ZNet.instance.IsAdmin(hostName)) {
                Report(admin.m_uid, "$al_cmd_server_not_admin");
                return;
            }
            if (!Enum.IsDefined(typeof(Deity), deity)) { return; }
            Route(admin.m_uid, $"{admin.m_playerName} ({hostName})", playerName, (Deity)deity, amount);
        }

        // On the server: finds the player by name and hands them the change.
        private static void Route(long admin, string adminName, string playerName, Deity deity, int amount) {
            List<ZNetPeer> peers = ZNet.instance.GetPeers().Where(peer => peer.IsReady() && string.Equals(peer.m_playerName, playerName, StringComparison.OrdinalIgnoreCase)).ToList();
            // A host's own character has no peer.
            bool host = Player.m_localPlayer != null && string.Equals(Player.m_localPlayer.GetPlayerName(), playerName, StringComparison.OrdinalIgnoreCase);
            int matches = peers.Count + (host ? 1 : 0);
            if (matches == 0) {
                Report(admin, "$al_cmd_player_offline", playerName);
                return;
            }
            if (matches > 1) {
                Report(admin, "$al_cmd_player_ambiguous", playerName);
                return;
            }
            Logger.LogInfo($"Admin {adminName} changes {playerName}'s {deity} star points by {amount}.");
            if (host) {
                Apply(deity, amount, admin);
            } else {
                peers[0].m_rpc.Invoke(ChangeRpc, (int)deity, amount, admin);
            }
        }

        // On the player's machine. Registered only on the connection to the server, so only the server sends it.
        private static void OnChange(ZRpc rpc, int deity, int amount, long admin) {
            if (!Enum.IsDefined(typeof(Deity), deity)) { return; }
            Apply((Deity)deity, amount, admin);
        }

        private static void Apply(Deity deity, int amount, long admin) {
            Player player = Player.m_localPlayer;
            if (player == null) {
                // Between bodies: anything written now would be lost when the new body loads.
                Report(admin, "$al_cmd_player_away", Game.instance?.GetPlayerProfile()?.GetName() ?? "?");
                return;
            }
            string name = player.GetPlayerName();
            string deityName = Compendium.DeityName(deity);
            bool reset = false;
            if (amount >= 0) {
                PlayerData.AddPoints(deity, amount);
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_points_given", deityName, amount.ToString()));
            } else {
                reset = PlayerData.TakePoints(deity, -amount);
                string message = Localization.instance.Localize("$al_points_taken", deityName, (-amount).ToString());
                if (reset) { message += "\n" + Localization.instance.Localize("$al_points_map_reset", deityName); }
                player.Message(MessageHud.MessageType.Center, message);
            }
            string deityToken = DeityToken(deity);
            Report(admin, "$al_cmd_player_points", name, deityToken,
                PlayerData.PointsEarned(deity).ToString(), PlayerData.PointsSpent(deity).ToString(), PlayerData.PointsAvailable(deity).ToString());
            if (reset) { Report(admin, "$al_cmd_player_map_reset", name, deityToken); }
        }

        // Sent as a token, so the admin reads the deity's name in their own language.
        private static string DeityToken(Deity deity) => DeityConfiguration.TryGetValue(deity, out DeityDetails details) ? details.NameLocalKey : deity.ToString();

        // A routed RPC to this machine's own id runs here, so a host admin is told the same way.
        private static void Report(long admin, string token, params string[] words) {
            ZRoutedRpc.instance?.InvokeRoutedRPC(admin, ReportRpc, token, words.ToList());
        }

        // On the admin's machine.
        private static void OnReport(long sender, string token, List<string> words) {
            if (global::Console.instance == null) { return; }
            string text = Localization.instance.Localize(token, words.Select(word => Localization.instance.Localize(word)).ToArray());
            global::Console.instance.Print(text);
        }
    }
}
