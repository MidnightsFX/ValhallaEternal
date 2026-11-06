using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Eternal Hall public island and Ragnarok API (version 3). See API/README.md.

namespace EternalHall
{
    /// <summary>
    /// Public API for Eternal Hall islands. Copy this file into your project and declare a soft dependency
    /// on Eternal Hall to hand out islands from your own mod (a clan hall, a quest reward, ...):
    /// <code>[BepInDependency("MidnightsFX.EternalHall", BepInDependency.DependencyFlags.SoftDependency)]</code>
    /// All access is through reflection, so your plugin never needs a hard reference to the Eternal Hall
    /// assembly. Check <see cref="IsAvailable"/> before calling anything else, and call everything from the
    /// main thread.
    ///
    /// Islands you request are held by an owner key you choose, such as <c>"clans:1234"</c>; a key holds at
    /// most one island. Creating, renaming and releasing islands only works on the server (or host), so
    /// check on your own server who may have one before asking. Reading islands works everywhere.
    /// </summary>
    public static class IslandAPI
    {
        private static readonly Type Receiver;
        private static readonly MethodInfo GetApiVersionMethod;
        private static readonly MethodInfo IsReadyMethod;
        private static readonly MethodInfo GetIslandTypesMethod;
        private static readonly MethodInfo RequestIslandMethod;
        private static readonly MethodInfo RequestIslandWithLocationsMethod;
        private static readonly MethodInfo RenameIslandMethod;
        private static readonly MethodInfo ReleaseIslandMethod;
        private static readonly MethodInfo GetIslandMethod;
        private static readonly MethodInfo GetIslandsMethod;
        private static readonly MethodInfo GetIslandAtMethod;
        private static readonly MethodInfo TravelToMethod;
        private static readonly MethodInfo RegisterMonumentHandlerMethod;
        private static readonly MethodInfo AddIslandsChangedListenerMethod;
        private static readonly MethodInfo RemoveIslandsChangedListenerMethod;

        /// <summary>True when Eternal Hall is loaded and the API is callable.</summary>
        public static bool IsAvailable => Receiver != null;

        /// <summary>The installed Eternal Hall's API version, or 0 when it is not installed.</summary>
        public static int ApiVersion => GetApiVersionMethod?.Invoke(null, null) is int version ? version : 0;

        static IslandAPI()
        {
            Receiver = Type.GetType("EternalHall.IslandAPIReceiver, EternalHall");
            if (Receiver == null)
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "EternalHall");
                Receiver = assembly?.GetType("EternalHall.IslandAPIReceiver");
            }
            if (Receiver == null) return;
            GetApiVersionMethod = Find("GetApiVersion");
            IsReadyMethod = Find("IsReady");
            GetIslandTypesMethod = Find("GetIslandTypes");
            RequestIslandMethod = Find("RequestIsland");
            RequestIslandWithLocationsMethod = Find("RequestIslandWithLocations");
            RenameIslandMethod = Find("RenameIsland");
            ReleaseIslandMethod = Find("ReleaseIsland");
            GetIslandMethod = Find("GetIsland");
            GetIslandsMethod = Find("GetIslands");
            GetIslandAtMethod = Find("GetIslandAt");
            TravelToMethod = Find("TravelTo");
            RegisterMonumentHandlerMethod = Find("RegisterMonumentHandler");
            AddIslandsChangedListenerMethod = Find("AddIslandsChangedListener");
            RemoveIslandsChangedListenerMethod = Find("RemoveIslandsChangedListener");
        }

        private static MethodInfo Find(string name)
        {
            return Receiver.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        }

        /// <summary>
        /// True once this machine has the world's island list: on the server when the world has loaded, on a
        /// client when the server has sent it. False on the main menu.
        /// </summary>
        public static bool IsReady => IsReadyMethod?.Invoke(null, null) is bool ready && ready;

        /// <summary>The island types that can be requested, as biome names: Meadows, BlackForest, Swamp,
        /// Mountain, Plains, Mistlands, AshLands.</summary>
        public static List<string> GetIslandTypes()
        {
            return GetIslandTypesMethod?.Invoke(null, null) as List<string> ?? new List<string>();
        }

        /// <summary>
        /// <b>Server only.</b> Returns the key's island of this type, creating it if the key has none yet. A new
        /// island is ready to travel to as soon as this returns, and every client learns about it shortly after.
        /// </summary>
        /// <param name="islandType">One of <see cref="GetIslandTypes"/>, case-insensitive.</param>
        /// <param name="ownerKey">Your key for the owner, prefixed with your mod's name (<c>"clans:1234"</c>).</param>
        /// <param name="ownerName">Shown on the island's monument, for example the clan's name. When the key
        /// already has this island, a different name renames it.</param>
        /// <returns>The result; check <see cref="EHIslandRequestResult.Success"/> and, on failure,
        /// <see cref="EHIslandRequestResult.Error"/> (one of <see cref="EHIslandErrors"/>). A key that already holds
        /// an island of another type fails with <see cref="EHIslandErrors.OwnerHasOtherType"/> and that island.</returns>
        public static EHIslandRequestResult RequestIsland(string islandType, string ownerKey, string ownerName)
        {
            if (RequestIslandMethod == null) return EHIslandRequestResult.Failed(EHIslandErrors.NotInstalled);
            Dictionary<string, object> values = RequestIslandMethod.Invoke(null, new object[] { islandType, ownerKey, ownerName }) as Dictionary<string, object>;
            return values == null ? EHIslandRequestResult.Failed(EHIslandErrors.NotInstalled) : EHIslandRequestResult.From(values);
        }

        /// <summary>
        /// <b>Server only.</b> As <see cref="RequestIsland(string, string, string)"/>, but a newly created island gets
        /// the locations you ask for besides its monument, instead of the ones the server's config asks for (by
        /// default none). An island the key already holds is left as it is. Needs API version 2; an older Eternal
        /// Hall fails with <see cref="EHIslandErrors.Unsupported"/>.
        /// </summary>
        /// <param name="locations">How many locations, and which. Null behaves as the three-argument call.</param>
        public static EHIslandRequestResult RequestIsland(string islandType, string ownerKey, string ownerName, EHIslandLocations locations)
        {
            if (locations == null) return RequestIsland(islandType, ownerKey, ownerName);
            if (RequestIslandMethod == null) return EHIslandRequestResult.Failed(EHIslandErrors.NotInstalled);
            if (RequestIslandWithLocationsMethod == null) return EHIslandRequestResult.Failed(EHIslandErrors.Unsupported);
            object[] arguments = { islandType, ownerKey, ownerName, locations.Count, new List<string>(locations.Names ?? new List<string>()) };
            Dictionary<string, object> values = RequestIslandWithLocationsMethod.Invoke(null, arguments) as Dictionary<string, object>;
            return values == null ? EHIslandRequestResult.Failed(EHIslandErrors.NotInstalled) : EHIslandRequestResult.From(values);
        }

        /// <summary><b>Server only.</b> Changes the name shown on the key's island, for example after a clan is
        /// renamed. Returns false when the key has no island.</summary>
        public static bool RenameIsland(string ownerKey, string ownerName)
        {
            return RenameIslandMethod?.Invoke(null, new object[] { ownerKey, ownerName }) is bool ok && ok;
        }

        /// <summary>
        /// <b>Server only.</b> The key gives up its island, so it can request a new one. The island and
        /// everything built on it stay in the world with no owner (admins can still reach it); it is never
        /// deleted. Returns false when the key has no island.
        /// </summary>
        public static bool ReleaseIsland(string ownerKey)
        {
            return ReleaseIslandMethod?.Invoke(null, new object[] { ownerKey }) is bool ok && ok;
        }

        /// <summary>The key's island, or null when it has none. Works on clients.</summary>
        public static EHIsland GetIsland(string ownerKey)
        {
            return EHIsland.From(GetIslandMethod?.Invoke(null, new object[] { ownerKey }) as Dictionary<string, object>);
        }

        /// <summary>Every island whose key starts with the prefix, for example <c>"clans:"</c>. Works on clients.</summary>
        public static List<EHIsland> GetIslands(string ownerKeyPrefix)
        {
            if (!(GetIslandsMethod?.Invoke(null, new object[] { ownerKeyPrefix }) is List<Dictionary<string, object>> list)) return new List<EHIsland>();
            return list.Select(EHIsland.From).Where(i => i != null).ToList();
        }

        /// <summary>
        /// The island at a world position (anywhere in its slot, its surrounding sea included), or null. Covers
        /// players' boss islands too, whose <see cref="EHIsland.OwnerKey"/> is empty. Use it to tell whether a
        /// player is standing on your island.
        /// </summary>
        public static EHIsland GetIslandAt(Vector3 position)
        {
            return EHIsland.From(GetIslandAtMethod?.Invoke(null, new object[] { position }) as Dictionary<string, object>);
        }

        /// <summary>
        /// <b>Client.</b> Teleports the local player to the key's island. Checks nothing about what they carry,
        /// so apply your own rules first. Returns false when the key has no island or the teleport did not start.
        /// </summary>
        public static bool TravelTo(string ownerKey)
        {
            return TravelToMethod?.Invoke(null, new object[] { ownerKey }) is bool ok && ok;
        }

        /// <summary>
        /// Every island has a monument at its centre that, by default, teleports whoever uses it back to the
        /// Sacrificial Stones, with no item checks. For islands whose key starts with
        /// <paramref name="ownerKeyPrefix"/>, Eternal Hall calls <paramref name="onUse"/> with the island's key
        /// instead, on the client that used it. Return true when you dealt with the use (refusing counts), or
        /// false to fall back to the Sacrificial Stones. Register on every client; registering a prefix again
        /// replaces it.
        /// </summary>
        /// <param name="useText">The action shown on the monument's hover, a $token or plain text, or null for
        /// the default.</param>
        public static void RegisterMonumentHandler(string ownerKeyPrefix, string useText, Func<string, bool> onUse)
        {
            RegisterMonumentHandlerMethod?.Invoke(null, new object[] { ownerKeyPrefix, useText, onUse });
        }

        /// <summary>Raised on every machine after its island list changes: an island created, renamed or
        /// released, or a client receiving the list.</summary>
        public static event Action IslandsChanged
        {
            add { AddIslandsChangedListenerMethod?.Invoke(null, new object[] { value }); }
            remove { RemoveIslandsChangedListenerMethod?.Invoke(null, new object[] { value }); }
        }
    }

    /// <summary>
    /// Public API for Eternal Hall's Ragnarok cycle, in the same file and on the same terms as <see cref="IslandAPI"/>:
    /// reflection only, main thread only. Needs API version 3; against an older Eternal Hall <see cref="IsAvailable"/>
    /// is false and every call does nothing.
    /// </summary>
    public static class RagnarokAPI
    {
        private static readonly MethodInfo IsCountingDownMethod;
        private static readonly MethodInfo GetDaysLeftMethod;
        private static readonly MethodInfo AdvanceCountdownMethod;

        /// <summary>True when an Eternal Hall with the Ragnarok API is loaded.</summary>
        public static bool IsAvailable => AdvanceCountdownMethod != null;

        static RagnarokAPI()
        {
            Type receiver = Type.GetType("EternalHall.RagnarokAPIReceiver, EternalHall");
            if (receiver == null)
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "EternalHall");
                receiver = assembly?.GetType("EternalHall.RagnarokAPIReceiver");
            }
            if (receiver == null) return;
            IsCountingDownMethod = receiver.GetMethod("IsCountingDown", BindingFlags.Public | BindingFlags.Static);
            GetDaysLeftMethod = receiver.GetMethod("GetDaysLeft", BindingFlags.Public | BindingFlags.Static);
            AdvanceCountdownMethod = receiver.GetMethod("AdvanceCountdown", BindingFlags.Public | BindingFlags.Static);
        }

        /// <summary>True while the world's Ragnarok cycle is on and counting down. Works on clients.</summary>
        public static bool IsCountingDown => IsCountingDownMethod?.Invoke(null, null) is bool counting && counting;

        /// <summary>In-game days until Ragnarok, or -1 while it is not counting down. Works on clients.</summary>
        public static double DaysLeft => GetDaysLeftMethod?.Invoke(null, null) is double days ? days : -1;

        /// <summary>
        /// <b>Server only.</b> Brings Ragnarok <paramref name="days"/> in-game days closer, but never closer than
        /// <paramref name="minDaysLeft"/> from now; a countdown that already is closer stays where it is. Every client
        /// sees the new countdown at once. Returns whether it moved: false when the cycle is off, already under way,
        /// or this is not the server.
        /// </summary>
        /// <param name="reason">Written to the server's log, such as <c>"Ascension of Erik"</c>.</param>
        public static bool AdvanceCountdown(float days, float minDaysLeft, string reason)
        {
            return AdvanceCountdownMethod?.Invoke(null, new object[] { days, minDaysLeft, reason }) is bool moved && moved;
        }
    }

    /// <summary>One island, as Eternal Hall reported it when you asked.</summary>
    public sealed class EHIsland
    {
        public int Id { get; private set; }
        /// <summary>The island's biome name; one of <see cref="IslandAPI.GetIslandTypes"/>.</summary>
        public string Type { get; private set; }
        /// <summary>The key holding the island, or empty for a player's boss island or a released island.</summary>
        public string OwnerKey { get; private set; }
        /// <summary>The name shown on the island's monument.</summary>
        public string OwnerName { get; private set; }
        /// <summary>The player who owns a boss island, or 0.</summary>
        public long OwnerPlayerId { get; private set; }
        /// <summary>The monument's position at the island's centre.</summary>
        public Vector3 Center { get; private set; }
        /// <summary>Where travel to the island lands, a few meters from the monument.</summary>
        public Vector3 ArrivalPoint { get; private set; }
        /// <summary>Radius in meters of the island's land, before its coast's irregularity.</summary>
        public float LandRadius { get; private set; }

        internal static EHIsland From(Dictionary<string, object> values)
        {
            if (values == null) return null;
            return new EHIsland
            {
                Id = values.TryGetValue("id", out object id) && id is int i ? i : 0,
                Type = values.TryGetValue("type", out object type) ? type as string ?? "" : "",
                OwnerKey = values.TryGetValue("ownerKey", out object key) ? key as string ?? "" : "",
                OwnerName = values.TryGetValue("ownerName", out object name) ? name as string ?? "" : "",
                OwnerPlayerId = values.TryGetValue("ownerPlayerId", out object player) && player is long p ? p : 0L,
                Center = values.TryGetValue("center", out object center) && center is Vector3 c ? c : Vector3.zero,
                ArrivalPoint = values.TryGetValue("arrivalPoint", out object arrival) && arrival is Vector3 a ? a : Vector3.zero,
                LandRadius = values.TryGetValue("landRadius", out object radius) && radius is float r ? r : 0f,
            };
        }
    }

    /// <summary>
    /// The locations (dungeons, camps, ruins) a new island gets besides its monument, for
    /// <see cref="IslandAPI.RequestIsland(string, string, string, EHIslandLocations)"/>.
    /// </summary>
    public sealed class EHIslandLocations
    {
        /// <summary>How many to place; 0 places none. Each needs a free zone on the island's land, so a
        /// default-size island fits only a handful. A location that does not fit is left out.</summary>
        public int Count { get; set; }
        /// <summary>Location prefab names to pick from, such as <c>"Runestone_Boars"</c>, used whatever their biome.
        /// Empty picks from every eligible location of the island's biome. A name can be placed more than once when
        /// <see cref="Count"/> is larger than the list. The server's Location Deny List still applies, and boss
        /// stones, Hildir's locations and unique locations are never placed.</summary>
        public List<string> Names { get; set; } = new List<string>();
    }

    /// <summary>The answer to <see cref="IslandAPI.RequestIsland(string, string, string)"/>.</summary>
    public sealed class EHIslandRequestResult
    {
        public bool Success { get; private set; }
        /// <summary>True when this request created the island; false when the key already had it.</summary>
        public bool Created { get; private set; }
        /// <summary>Empty on success, otherwise one of <see cref="EHIslandErrors"/>.</summary>
        public string Error { get; private set; }
        /// <summary>The key's island. Null on failure, except for <see cref="EHIslandErrors.OwnerHasOtherType"/>.</summary>
        public EHIsland Island { get; private set; }

        internal static EHIslandRequestResult Failed(string error)
        {
            return new EHIslandRequestResult { Success = false, Error = error };
        }

        internal static EHIslandRequestResult From(Dictionary<string, object> values)
        {
            return new EHIslandRequestResult
            {
                Success = values.TryGetValue("success", out object success) && success is bool s && s,
                Created = values.TryGetValue("created", out object created) && created is bool c && c,
                Error = values.TryGetValue("error", out object error) ? error as string ?? "" : "",
                Island = EHIsland.From(values.TryGetValue("island", out object island) ? island as Dictionary<string, object> : null),
            };
        }
    }

    /// <summary>The error codes <see cref="IslandAPI.RequestIsland(string, string, string)"/> can fail with.</summary>
    public static class EHIslandErrors
    {
        /// <summary>Eternal Hall is not installed.</summary>
        public const string NotInstalled = "not_installed";
        /// <summary>The installed Eternal Hall is too old for this call; see <see cref="IslandAPI.ApiVersion"/>.</summary>
        public const string Unsupported = "unsupported";
        /// <summary>Called on a client; requests only work on the server or host.</summary>
        public const string NotServer = "not_server";
        /// <summary>No world is loaded yet.</summary>
        public const string NotReady = "not_ready";
        /// <summary>The type is not one of <see cref="IslandAPI.GetIslandTypes"/>.</summary>
        public const string UnknownType = "unknown_type";
        /// <summary>The owner key is empty.</summary>
        public const string InvalidOwner = "invalid_owner";
        /// <summary>The key already holds an island of another type, returned in the result.</summary>
        public const string OwnerHasOtherType = "owner_has_other_type";
        /// <summary>Every island slot in the world is in use.</summary>
        public const string NoRoom = "no_room";
    }
}
