using EternalHall;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// Personal islands, with Eternal Hall: a deity's shrine gives a character an island of the deity's biome
    /// (<see cref="IslandGift"/> in <c>Shrines.yaml</c>), once, and that deity's shrines take them there after.
    /// The island's monument brings them back to the Sacrificial Stones. Off with <c>EnablePersonalIslands</c>.
    /// </summary>
    /// <remarks>
    /// Eternal Hall holds each island for an owner key, <c>aesirlineage:&lt;player id&gt;</c>, and a key holds
    /// one island, so a character has at most one. Only the server can create islands: the shrine checks the
    /// offering and keys on the player's machine, as for every other shrine choice, and asks the server, which
    /// works out the key from the asking peer's own character, so a client can only ever get its own island.
    /// The offering is taken once the server says the island was made, so a world with no room left costs
    /// nothing. The island list is synced to every client by Eternal Hall, so reading it works anywhere.
    /// </remarks>
    internal static class Islands {
        private const string KeyPrefix = "aesirlineage:";
        private const string RequestRpc = "AL_IslandRequest";
        private const string ResultRpc = "AL_IslandResult";
        // How long the shrine waits for the server before a new request may be sent.
        private const float RequestSeconds = 10f;

        private class Pending {
            public Deity Deity;
            public IslandGift Gift;
            public bool Free;
            public float At;
        }

        private static Pending pending;

        // A client learns of a new island a moment after the server's answer, so an open shrine is rebuilt then too.
        private static readonly Action IslandsChanged = SacrificeUI.RefreshOpen;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                pending = null;
                ZRoutedRpc.instance?.Register<string>(RequestRpc, OnRequest);
                ZRoutedRpc.instance?.Register<bool, bool, string, string>(ResultRpc, OnResult);
                if (IslandAPI.IsAvailable) {
                    // Leaving keeps the portal rules; otherwise Eternal Hall's own return to the Sacrificial Stones.
                    IslandAPI.RegisterMonumentHandler(KeyPrefix, null, _ => LeaveRefused());
                    IslandAPI.IslandsChanged -= IslandsChanged;
                    IslandAPI.IslandsChanged += IslandsChanged;
                }
            }
        }

        /// <summary>Whether islands can be given and visited now: the setting is on and Eternal Hall is ready.</summary>
        internal static bool Available => ValConfig.EnablePersonalIslands.Value && IslandAPI.IsAvailable && IslandAPI.IsReady;

        private static string KeyFor(long playerId) => KeyPrefix + playerId;

        /// <summary>The player's island, or null when they have none.</summary>
        internal static EHIsland Of(Player player) => player != null && IslandAPI.IsAvailable ? IslandAPI.GetIsland(KeyFor(player.GetPlayerID())) : null;

        /// <summary>The island type as Eternal Hall spells it, or null when it has no such type.</summary>
        internal static string TypeNamed(string type) {
            if (string.IsNullOrEmpty(type)) { return null; }
            return IslandAPI.GetIslandTypes().FirstOrDefault(t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>An island type (a biome name) in the player's language.</summary>
        internal static string BiomeName(string type) {
            string token = Enum.TryParse(type, true, out Heightmap.Biome biome) ? "$biome_" + biome.ToString().ToLowerInvariant() : type ?? "";
            return Localization.instance.Localize(token);
        }

        /// <summary>The deity whose shrines give islands of this type, if any.</summary>
        internal static bool TryGetGiver(string type, out Deity deity) {
            foreach (KeyValuePair<Deity, DeityShrine> entry in ShrineData.All) {
                if (string.Equals(entry.Value?.Island?.Type, type, StringComparison.OrdinalIgnoreCase)) {
                    deity = entry.Key;
                    return true;
                }
            }
            deity = default;
            return false;
        }

        // ---- Asking for an island ----

        /// <summary>
        /// On the player's machine, once the shrine has checked the offering and keys: asks the server for the
        /// island. The offering is taken when the answer says it was made.
        /// </summary>
        internal static void Ask(Deity deity, IslandGift gift, bool free) {
            Player player = Player.m_localPlayer;
            if (player == null || ZRoutedRpc.instance == null) { return; }
            if (pending != null && Time.time - pending.At < RequestSeconds) {
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_island_waiting"));
                return;
            }
            pending = new Pending() { Deity = deity, Gift = gift, Free = free, At = Time.time };
            Logger.LogInfo($"Asking the server for a {gift.Type} island from {deity}.");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, gift.Type);
        }

        // On the server: the island for the asking peer's character.
        private static void OnRequest(long sender, string type) {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) { return; }
            string error = Create(sender, type, out bool created, out string madeType);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, ResultRpc, error == null, created, error ?? "", madeType ?? type ?? "");
        }

        // Null when the island was made or already there, otherwise one of Eternal Hall's error codes.
        private static string Create(long sender, string requested, out bool created, out string madeType) {
            created = false;
            madeType = requested;
            if (!ValConfig.EnablePersonalIslands.Value) { return "disabled"; }
            if (!IslandAPI.IsAvailable) { return EHIslandErrors.NotInstalled; }
            if (!IslandAPI.IsReady) { return EHIslandErrors.NotReady; }
            string type = TypeNamed(requested);
            if (type == null) { return EHIslandErrors.UnknownType; }
            if (!TryGetCharacter(sender, out long playerId, out string playerName)) { return EHIslandErrors.InvalidOwner; }
            EHIslandRequestResult result = IslandAPI.RequestIsland(type, KeyFor(playerId), playerName);
            madeType = result.Island?.Type ?? type;
            if (!result.Success) {
                Logger.LogWarning($"Eternal Hall gave {playerName} no {type} island: {result.Error}.");
                return string.IsNullOrEmpty(result.Error) ? "failed" : result.Error;
            }
            created = result.Created;
            if (created) { Logger.LogInfo($"{playerName} was given a {type} island (island {result.Island?.Id})."); }
            return null;
        }

        // The asking peer's character, from its own player object: a peer's, or the host's own.
        private static bool TryGetCharacter(long sender, out long playerId, out string playerName) {
            playerId = 0;
            playerName = "";
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            ZDOID character = peer != null ? peer.m_characterID : sender == ZNet.GetUID() ? ZNet.instance.LocalPlayerCharacterID : ZDOID.None;
            ZDO zdo = character.IsNone() || ZDOMan.instance == null ? null : ZDOMan.instance.GetZDO(character);
            if (zdo == null) { return false; }
            playerId = zdo.GetLong(ZDOVars.s_playerID);
            playerName = zdo.GetString(ZDOVars.s_playerName);
            if (string.IsNullOrEmpty(playerName) && peer != null) { playerName = peer.m_playerName; }
            return playerId != 0;
        }

        // On the asking player's machine.
        private static void OnResult(long sender, bool success, bool created, string error, string type) {
            Player player = Player.m_localPlayer;
            Pending asked = pending;
            pending = null;
            if (player == null || asked == null) { return; }
            Localization loc = Localization.instance;
            string deityName = Compendium.DeityName(asked.Deity);
            if (success && created) {
                if (!asked.Free && !SacrificeUI.TakeItems(player, asked.Gift.Items, $"{asked.Deity}'s island")) {
                    Logger.LogWarning($"The offering for {asked.Deity}'s island was no longer there; the island was given anyway.");
                }
                DeityVfx.Share(asked.Deity, VfxRole.Burst, player.transform.position, 0.7f);
                player.Message(MessageHud.MessageType.Center, loc.Localize("$al_island_given", deityName, BiomeName(type)));
            } else if (success) {
                player.Message(MessageHud.MessageType.Center, loc.Localize("$al_island_have", BiomeName(type)));
            } else {
                string message = error switch {
                    EHIslandErrors.OwnerHasOtherType => loc.Localize("$al_island_have", BiomeName(type)),
                    EHIslandErrors.NoRoom => loc.Localize("$al_island_no_room"),
                    EHIslandErrors.NotInstalled or EHIslandErrors.NotReady or EHIslandErrors.Unsupported or "disabled" => loc.Localize("$al_island_unavailable"),
                    _ => loc.Localize("$al_island_failed", error),
                };
                player.Message(MessageHud.MessageType.Center, message);
            }
            SacrificeUI.RefreshOpen();
        }

        // ---- Travel ----

        /// <summary>Takes the player to their island, under the same rules as a portal.</summary>
        internal static void Travel(Player player) {
            EHIsland island = Of(player);
            if (island == null) { return; }
            if (!player.IsTeleportable(false)) {
                player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }
            if (!IslandAPI.TravelTo(island.OwnerKey)) {
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_island_unavailable"));
                return;
            }
            Logger.LogInfo($"Travelling to {player.GetPlayerName()}'s {island.Type} island.");
        }

        // The monument of an island this mod gave: true refuses leaving with what a portal would refuse,
        // false lets Eternal Hall take the player back to the Sacrificial Stones.
        private static bool LeaveRefused() {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsTeleportable(false)) { return false; }
            player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return true;
        }
    }
}
