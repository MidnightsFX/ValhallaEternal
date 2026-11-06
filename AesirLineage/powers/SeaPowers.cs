using HarmonyLib;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Baldur's Hringhorni: ships that sail faster with the wind always behind them and shrug off the sea,
    /// stronger swimming, and a ship that comes when called. A ship's physics run on the machine of
    /// whoever steers it, which takes ownership of it, so the sailing powers read the local player.
    /// </summary>
    internal static class SeaPowers {
        private const string CallShipRpc = "AL_CallShip";
        private const string LastShipKey = "al_lastship";
        private const float CallCooldown = 600f;
        private const float ShoreSearch = 30f;
        private const float MinDepth = 1.5f;

        private static bool sailing;
        private static float callReadyAt;

        internal static void Reset() {
            sailing = false;
        }

        private static bool Steering(Ship ship) => Player.m_localPlayer != null && Player.m_localPlayer.GetControlledShip() == ship;

        private static bool LocalShipPiece(Component piece, out Ship ship) {
            ship = piece.GetComponent<Ship>();
            return ship != null && Steering(ship);
        }

        // ---- Sailing ----

        [HarmonyPatch(typeof(Ship), nameof(Ship.GetSailForce))]
        private static class Sail {
            private static void Prefix(Ship __instance) {
                sailing = PowerSet.Has(Power.FairWind) && Steering(__instance);
            }

            private static void Postfix(Ship __instance, ref Vector3 __result) {
                if (PowerSet.Has(Power.SailSpeed, out float faster) && Steering(__instance)) { __result *= 1f + faster / 100f; }
            }

            private static void Finalizer() { sailing = false; }
        }

        // Vanilla gives a wind from behind 0.7 of the force a wind from the side gets; Fair Wind sails as well as either.
        [HarmonyPatch(typeof(Ship), nameof(Ship.GetWindAngleFactor))]
        private static class WindAngle {
            private static void Postfix(ref float __result) {
                if (sailing) { __result = Mathf.Max(__result, 1f); }
            }
        }

        // Fair Wind Always: while a steered ship works out its sail, the wind blows the way it faces.
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetWindDir))]
        private static class Wind {
            private static void Postfix(ref Vector3 __result) {
                Ship ship = sailing ? Player.m_localPlayer?.GetControlledShip() : null;
                if (ship == null) { return; }
                Vector3 forward = ship.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.01f) { __result = forward.normalized; }
            }
        }

        // Steady Keel: waves, rocks and capsizing hurt a steered ship less. Those hits have no attacker.
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Damage))]
        private static class Keel {
            private static void Prefix(WearNTear __instance, HitData hit) {
                if (hit == null || hit.GetAttacker() != null || !PowerSet.Has(Power.SteadyKeel, out float keel) || !LocalShipPiece(__instance, out _)) { return; }
                hit.m_damage.Modify(1f - keel / 100f);
            }
        }

        // ---- Swimming ----

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateSwimming))]
        private static class Swimming {
            private static void Prefix(Character __instance, out float __state) {
                __state = -1f;
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.SwimSpeed, out float faster)) { return; }
                __state = __instance.m_swimSpeed;
                __instance.m_swimSpeed *= 1f + faster / 100f;
            }

            private static void Finalizer(Character __instance, float __state) {
                if (__state >= 0f) { __instance.m_swimSpeed = __state; }
            }
        }

        // ---- Ship-Caller ----

        /// <summary>Remembers the ship being steered, and calls it on the hotkey.</summary>
        internal static void Tick(Player player) {
            Ship steered = player.GetControlledShip();
            ZDO zdo = steered?.m_nview?.GetZDO();
            if (zdo != null) {
                string id = $"{zdo.m_uid.UserID}:{zdo.m_uid.ID}";
                if (!player.m_customData.TryGetValue(LastShipKey, out string known) || known != id) { player.m_customData[LastShipKey] = id; }
            }
            if (!PowerSet.Has(Power.ShipCaller) || ValConfig.CallShipKey == null || !ValConfig.CallShipKey.Value.IsDown() || !player.TakeInput()) { return; }
            CallShip(player);
        }

        private static void CallShip(Player player) {
            if (Time.time < callReadyAt) {
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_ship_resting", Mathf.CeilToInt((callReadyAt - Time.time) / 60f).ToString()));
                return;
            }
            if (!TryLastShip(player, out ZDOID ship)) {
                player.Message(MessageHud.MessageType.Center, "$al_ship_none");
                return;
            }
            if (!FindWater(player, out Vector3 at)) {
                player.Message(MessageHud.MessageType.Center, "$al_ship_no_water");
                return;
            }
            Vector3 facing = player.transform.forward;
            facing.y = 0f;
            Quaternion rotation = Quaternion.LookRotation(facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward);
            callReadyAt = Time.time + CallCooldown;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), CallShipRpc, ship, at, rotation);
            // A copy this machine already has would otherwise put the ship back where it was.
            GameObject loaded = ZNetScene.instance?.FindInstance(ship);
            if (loaded != null) {
                loaded.transform.SetPositionAndRotation(at, rotation);
                Rigidbody body = loaded.GetComponent<Rigidbody>();
                if (body != null) {
                    body.position = at;
                    body.linearVelocity = Vector3.zero;
                }
            }
            PowerFx.Effect(Power.ShipCaller, VfxRole.Burst, at, 1f, shared: true);
            player.Message(MessageHud.MessageType.Center, "$al_ship_called");
        }

        private static bool TryLastShip(Player player, out ZDOID ship) {
            ship = ZDOID.None;
            if (!player.m_customData.TryGetValue(LastShipKey, out string id)) { return false; }
            string[] parts = id.Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], out long user) || !uint.TryParse(parts[1], out uint number)) { return false; }
            ship = new ZDOID(user, number);
            return true;
        }

        // The nearest water deep enough to float a ship, within reach of the shore the player stands on.
        private static bool FindWater(Player player, out Vector3 found) {
            found = Vector3.zero;
            float water = ZoneSystem.instance.m_waterLevel;
            float best = float.MaxValue;
            Vector3 from = player.transform.position;
            for (float radius = 6f; radius <= ShoreSearch; radius += 4f) {
                for (int step = 0; step < 16; step++) {
                    float angle = step * Mathf.PI / 8f;
                    Vector3 point = from + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    float ground = ZoneSystem.instance.GetGroundHeight(point);
                    if (water - ground < MinDepth) { continue; }
                    float distance = Vector3.Distance(from, point);
                    if (distance < best) {
                        best = distance;
                        found = new Vector3(point.x, water, point.z);
                    }
                }
                if (best < float.MaxValue) { return true; }
            }
            return false;
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<ZDOID, Vector3, Quaternion>(CallShipRpc, OnCallShip);
            }
        }

        // Runs on the server, which knows every ship wherever it lies: the ship is moved and handed to the caller.
        private static void OnCallShip(long sender, ZDOID ship, Vector3 at, Quaternion rotation) {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) { return; }
            ZDO zdo = ZDOMan.instance.GetZDO(ship);
            if (zdo == null) {
                Logger.LogDebug($"Ship {ship} asked for by {sender} is gone.");
                return;
            }
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.SetPosition(at);
            zdo.SetRotation(rotation);
            zdo.SetOwner(sender);
        }
    }
}
