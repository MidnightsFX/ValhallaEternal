using EternalHall;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// Each Ascension brings Eternal Hall's Ragnarok closer, by <c>AscensionRagnarokDays</c>. A jump that would
    /// pass the end of the countdown leaves half a day instead, so nobody is caught without warning, and a
    /// countdown already closer than that stays where it is (Eternal Hall's <see cref="RagnarokAPI"/> does both).
    /// </summary>
    /// <remarks>
    /// Ascension happens on the player's own machine, and only the server can move the countdown. The player's
    /// machine tells the server, the server decides with its own settings, and everyone is told how much closer
    /// Ragnarok came and why. Each Ascension (player, deity and level) counts once per server session, should
    /// its message arrive twice.
    /// </remarks>
    internal static class AscensionRagnarok {
        private const string AscendedRpc = "AL_Ascended";
        private const string NearerRpc = "AL_RagnarokNearer";
        // In-game days left after a jump that would have passed the end.
        internal const float MinDaysLeft = 0.5f;

        private static readonly HashSet<string> counted = new HashSet<string>();

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                counted.Clear();
                ZRoutedRpc.instance?.Register<long, string, string, int>(AscendedRpc, OnAscended);
                ZRoutedRpc.instance?.Register<string, float>(NearerRpc, OnNearer);
            }
        }

        /// <summary>Whether the next Ascension would bring Ragnarok closer, as this machine sees it.</summary>
        internal static bool WouldAdvance => ValConfig.AscensionAdvancesRagnarok.Value && ValConfig.AscensionRagnarokDays.Value > 0f
            && RagnarokAPI.IsAvailable && RagnarokAPI.IsCountingDown;

        /// <summary>On the ascending player's machine, once the Ascension is done.</summary>
        internal static void Ascended(Player player, Deity deity, int level) {
            if (ZRoutedRpc.instance == null || player == null) { return; }
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AscendedRpc,
                player.GetPlayerID(), player.GetPlayerName(), deity.ToString(), level);
        }

        // On the server.
        private static void OnAscended(long sender, long playerId, string playerName, string deity, int level) {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) { return; }
            if (!ValConfig.AscensionAdvancesRagnarok.Value || ValConfig.AscensionRagnarokDays.Value <= 0f || !RagnarokAPI.IsAvailable) { return; }
            if (!counted.Add($"{playerId}:{deity}:{level}")) { return; }
            double before = RagnarokAPI.DaysLeft;
            string reason = $"Ascension of {playerName} with {deity} to level {level}";
            if (!RagnarokAPI.AdvanceCountdown(ValConfig.AscensionRagnarokDays.Value, MinDaysLeft, reason)) {
                Logger.LogDebug($"{reason} did not move Ragnarok: its cycle is off, under way, or already closer than {MinDaysLeft} days.");
                return;
            }
            float days = (float)(before - RagnarokAPI.DaysLeft);
            Logger.LogInfo($"{reason} brought Ragnarok {days:0.##} in-game days closer.");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, NearerRpc, playerName, days);
        }

        // On every machine.
        private static void OnNearer(long sender, string playerName, float days) {
            if (Player.m_localPlayer == null || MessageHud.instance == null) { return; }
            string shown = days.ToString(days < 1f ? "0.#" : "0.##");
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                Localization.instance.Localize("$al_ragnarok_nearer", playerName, shown));
        }
    }
}
