using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// The home: Gefjun's Homestead (comfort, rest, busy stations, the hermit's hives and the swarm) and
    /// Syn's Warden (buildings that hold, raids turned on the warden, walls mended, a banned kind).
    /// Stations and buildings are run by whoever owns them, which reads what players nearby publish.
    /// </summary>
    internal static class HomePowers {
        private const float StationRange = 15f;
        private const float HermitRange = 30f;
        private const int HermitComfort = 2;
        private const float HermitYield = 0.25f;
        private const float SwarmRadius = 4f;
        private const float HiveRange = 30f;
        private const float MendRange = 10f;
        private const float MassRepairRange = 8f;
        private const string BannedDataKey = "al_banned";

        private const float HiveRest = 5f;
        private static readonly Dictionary<ZDOID, float> hiveRested = new Dictionary<ZDOID, float>();
        private static float nextSwarm;
        private static float nextHiveCheck;
        private static bool nearHive;
        private static readonly Dictionary<long, float> wardCache = new Dictionary<long, float>();
        private static readonly Dictionary<long, string> bannedCache = new Dictionary<long, string>();
        private static float wardCacheTime;

        internal static void Reset() {
            nearHive = false;
            wardCache.Clear();
            bannedCache.Clear();
        }

        /// <summary>Hearth Strength: the bonus to damage and stamina regen while rested, in percent.</summary>
        internal static float HearthBonus(Player player) {
            if (!PowerSet.Has(Power.HearthStrength, out float perComfort) || !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectRested)) { return 0f; }
            return player.GetComfortLevel() * perComfort;
        }

        /// <summary>Defender of the Hall: the bonus while a raid is on the local player.</summary>
        internal static float RaidBonus() => RandEventSystem.InEvent() ? PowerSet.Get(Power.RaidDefender) : 0f;

        internal static void Tick(Player player) {
            if (!PowerSet.Has(Power.Swarm, out float sting) || Time.time < nextSwarm) { return; }
            nextSwarm = Time.time + 1f;
            if (Time.time >= nextHiveCheck) {
                nextHiveCheck = Time.time + 5f;
                nearHive = HiveNear(player.transform.position);
            }
            if (nearHive) { sting *= 2f; }
            Vector3 at = player.transform.position;
            foreach (Character enemy in PowerFx.EnemiesNear(at, SwarmRadius).ToArray()) {
                PowerFx.Strike(enemy, new HitData.DamageTypes() { m_poison = sting }, at);
                PowerFx.Effect(Power.Swarm, VfxRole.Proc, enemy.GetCenterPoint(), 0.4f);
            }
        }

        private static bool HiveNear(Vector3 at) {
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(at, HiveRange, pieces);
            foreach (Piece piece in pieces) {
                if (piece != null && piece.GetComponent<Beehive>() != null) { return true; }
            }
            return false;
        }

        private static bool NearBed(Vector3 at) {
            PlayerProfile profile = Game.instance?.GetPlayerProfile();
            return profile != null && profile.HaveCustomSpawnPoint() && Vector3.Distance(profile.GetCustomSpawnPoint(), at) < HermitRange;
        }

        // ---- Comfort and rest ----

        [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.CalculateComfortLevel), typeof(Player))]
        private static class Comfort {
            private static void Postfix(Player player, ref int __result) {
                if (player != Player.m_localPlayer) { return; }
                __result += Mathf.RoundToInt(PowerSet.Get(Power.Comfort));
                if (PowerSet.Has(Power.Hermit)) { __result += HermitComfort; }
            }
        }

        private struct RestedState {
            public bool Changed;
            public float Base;
            public float PerComfort;
        }

        [HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.UpdateTTL))]
        private static class LongerRest {
            private static void Prefix(SE_Rested __instance, out RestedState __state) {
                __state = default;
                if (__instance.m_character != Player.m_localPlayer || !PowerSet.Has(Power.RestedDuration, out float longer)) { return; }
                __state = new RestedState() { Changed = true, Base = __instance.m_baseTTL, PerComfort = __instance.m_TTLPerComfortLevel };
                __instance.m_baseTTL *= 1f + longer / 100f;
                __instance.m_TTLPerComfortLevel *= 1f + longer / 100f;
            }

            private static void Finalizer(SE_Rested __instance, RestedState __state) {
                if (!__state.Changed) { return; }
                __instance.m_baseTTL = __state.Base;
                __instance.m_TTLPerComfortLevel = __state.PerComfort;
            }
        }

        // ---- Busy Household: stations near a homesteader, on the station's owner ----

        private static float StationFactor(Component station) => 1f + AllyPowers.Strongest(station.transform.position, StationRange, AllyPowers.StationKey) / 100f;

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.GetDeltaTime))]
        private static class Smelting {
            private static void Postfix(Smelter __instance, ref double __result) {
                if (__result > 0.0) { __result *= StationFactor(__instance); }
            }
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetDeltaTime))]
        private static class Cooking {
            private static void Postfix(CookingStation __instance, ref float __result) {
                if (__result > 0f) { __result *= StationFactor(__instance); }
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetFermentationTime))]
        private static class Fermenting {
            private static void Postfix(Fermenter __instance, ref double __result) {
                if (__result > 0.0) { __result *= StationFactor(__instance); }
            }
        }

        // ---- Hermit: more from hives and crops near the bed ----

        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
        private static class HermitHoney {
            private static void Prefix(Beehive __instance, Humanoid character, bool repeat) {
                if (repeat || character != Player.m_localPlayer || !PowerSet.Has(Power.Hermit) || !NearBed(__instance.transform.position)) { return; }
                int honey = __instance.GetHoneyLevel();
                if (honey <= 0 || !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false)) { return; }
                // Each press asks the owner to empty the hive; only the first of a quick run counts.
                ZDOID id = __instance.m_nview.GetZDO().m_uid;
                if (hiveRested.TryGetValue(id, out float until) && Time.time < until) { return; }
                hiveRested[id] = Time.time + HiveRest;
                int extra = Mathf.FloorToInt(honey * HermitYield + Random.value);
                if (extra <= 0) { return; }
                Vector3 at = __instance.m_spawnPoint != null ? __instance.m_spawnPoint.position : __instance.transform.position + Vector3.up;
                Extensions.DropItemsImmediate(new Dictionary<GameObject, int>() { { __instance.m_honeyItem.gameObject, extra } }, at, 0.5f);
                PowerFx.Effect(Power.Hermit, VfxRole.Harvest, at);
            }
        }

        /// <summary>Hermit: a picked crop near the bed sometimes gives a second harvest.</summary>
        internal static bool HermitHarvest(Vector3 at) => PowerSet.Has(Power.Hermit) && NearBed(at) && Random.value < HermitYield;

        // ---- Buildings: Hall-ward and Ban the Kind, on the building's owner ----

        // Each building's builder, when they are loaded: what they publish, read once a second.
        private static void RefreshWards() {
            if (Time.time - wardCacheTime < 1f) { return; }
            wardCacheTime = Time.time;
            wardCache.Clear();
            bannedCache.Clear();
            foreach (Player player in Player.GetAllPlayers()) {
                ZDO zdo = player?.m_nview?.GetZDO();
                if (zdo == null) { continue; }
                long id = player.GetPlayerID();
                wardCache[id] = zdo.GetFloat(AllyPowers.HallWardKey, 0f);
                bannedCache[id] = zdo.GetString(AllyPowers.BannedKey, "");
            }
        }

        private static float WardOf(WearNTear wear, out string banned) {
            banned = "";
            long creator = wear.m_piece != null ? wear.m_piece.GetCreator() : 0L;
            if (creator == 0L) { return 0f; }
            RefreshWards();
            bannedCache.TryGetValue(creator, out banned);
            return wardCache.TryGetValue(creator, out float ward) ? ward : 0f;
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
        private static class Walls {
            private static void Prefix(WearNTear __instance, HitData hit) {
                if (hit == null || __instance.m_nview == null || !__instance.m_nview.IsOwner()) { return; }
                float ward = WardOf(__instance, out string banned);
                Character attacker = hit.GetAttacker();
                if (!string.IsNullOrEmpty(banned) && attacker != null && !attacker.IsPlayer() && HuntPowers.TrophyOf(attacker) == banned) {
                    hit.m_damage = new HitData.DamageTypes();
                    return;
                }
                if (ward > 0f) { hit.m_damage.Modify(1f - ward / 100f); }
            }
        }

        // Hall-ward: rain doesn't rot a warden's buildings.
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.IsWet))]
        private static class NoRot {
            private static void Postfix(WearNTear __instance, ref bool __result) {
                if (__result && WardOf(__instance, out _) > 0f) { __result = false; }
            }
        }

        // ---- Mend the Walls ----

        /// <summary>A kill near the local player's damaged buildings mends the worst of them.</summary>
        internal static void OnLocalKill(Player player, Vector3 at) {
            if (!PowerSet.Has(Power.MendWalls)) { return; }
            WearNTear worst = null;
            float worstHealth = 1f;
            foreach (WearNTear wear in OwnBuildingsNear(player, at, MendRange)) {
                float health = wear.GetHealthPercentage();
                if (health < worstHealth) {
                    worstHealth = health;
                    worst = wear;
                }
            }
            if (worst != null && worst.Repair()) { PowerFx.Effect(Power.MendWalls, VfxRole.Proc, worst.transform.position); }
        }

        // Vanilla repairs the piece under the cursor; repairPiece is only the hammer's repair tool.
        [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
        private static class MassRepair {
            private static void Prefix(Player __instance, out Piece __state) {
                __state = null;
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.MendWalls)) { return; }
                Piece hovered = __instance.GetHoveringPiece();
                WearNTear wear = hovered != null ? hovered.GetComponent<WearNTear>() : null;
                if (wear != null && wear.GetHealthPercentage() < 1f && PrivateArea.CheckAccess(hovered.transform.position, 0f, flash: false)) { __state = hovered; }
            }

            private static void Postfix(Player __instance, Piece __state) {
                if (__state == null) { return; }
                foreach (WearNTear wear in OwnBuildingsNear(__instance, __state.transform.position, MassRepairRange)) {
                    if (wear.GetHealthPercentage() < 1f) { wear.Repair(); }
                }
            }
        }

        private static List<WearNTear> OwnBuildingsNear(Player player, Vector3 at, float range) {
            List<WearNTear> found = new List<WearNTear>();
            List<Piece> pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(at, range, pieces);
            long id = player.GetPlayerID();
            foreach (Piece piece in pieces) {
                if (piece == null || piece.GetCreator() != id) { continue; }
                WearNTear wear = piece.GetComponent<WearNTear>();
                if (wear != null) { found.Add(wear); }
            }
            return found;
        }

        // ---- Ban the Kind ----

        /// <summary>The trophy the local player last hung on an item stand of their own.</summary>
        internal static string BannedTrophy(Player player) {
            return player.m_customData.TryGetValue(BannedDataKey, out string trophy) ? trophy : "";
        }

        [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.UseItem))]
        private static class HangTrophy {
            private static void Postfix(ItemStand __instance, Humanoid user, ItemDrop.ItemData item) {
                if (!(user is Player player) || player != Player.m_localPlayer || item?.m_dropPrefab == null) { return; }
                if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy) { return; }
                Piece piece = __instance.GetComponent<Piece>();
                if (piece == null || piece.GetCreator() != player.GetPlayerID()) { return; }
                player.m_customData[BannedDataKey] = item.m_dropPrefab.name;
                if (PowerSet.Has(Power.BanTheKind)) {
                    player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_banned_kind", Localization.instance.Localize(item.m_shared.m_name)));
                }
            }
        }

        /// <summary>Ban the Kind: more damage to the banned kind, less to every other while a kind is banned.</summary>
        internal static float BanFactor(Player player, Character target) {
            if (!PowerSet.Has(Power.BanTheKind, out float bonus)) { return 1f; }
            string banned = BannedTrophy(player);
            if (string.IsNullOrEmpty(banned)) { return 1f; }
            if (HuntPowers.TrophyOf(target) == banned) { return 1f + bonus / 100f; }
            return Mathf.Max(0.2f, 1f - PowerSet.Get(Power.UnbannedDamagePenalty) / 100f);
        }
    }
}
