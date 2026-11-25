using System;
using System.Collections;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    // The server's side of the Ragnarok cycle, ticked once a second while a world is loaded.
    //
    //   Countdown  counts the cycle down; heralds appear in its last part
    //   Ragnarok   the world boss rises near a player in the main world (inside the ring's last circle, with the
    //              ring on), as soon as one is there, and Ragnarok lasts until it is killed. Heralds keep coming.
    //              Then the next cycle starts.
    internal static class RagnarokServer {
        private const float TickSeconds = 1f;
        // Real seconds before the end at which everyone is told it is at hand.
        private const double ImminentSeconds = 60;

        private static int dayWarnedCycle;
        private static int imminentWarnedCycle;
        private static int waitLoggedCycle;

        private static RagnarokState State => RagnarokSave.State;

        internal static IEnumerator Run() {
            WaitForSeconds wait = new WaitForSeconds(TickSeconds);
            while (ZNet.instance != null && State != null) {
                yield return wait;
                try {
                    Tick();
                } catch (Exception e) {
                    Logger.LogWarning($"Ragnarok tick failed: {e}");
                }
            }
        }

        internal static void Reset() {
            dayWarnedCycle = 0;
            imminentWarnedCycle = 0;
            waitLoggedCycle = 0;
            RagnarokHeralds.ResetTimer();
        }

        private static void Tick() {
            RagnarokState state = State;
            // Still loading: the world's time is not known yet.
            if (state == null || ZoneSystem.instance == null || !ZoneSystem.instance.LocationsGenerated) { return; }
            if (ValConfig.RagnarokEnabled.Value && !state.CycleStarted) {
                StartCycle(state, advance: false);
            }
            // A cycle saved by a build without the ring.
            if (state.CycleStarted && state.RingRadius <= 0f) {
                RagnarokRing.Choose(state);
                RagnarokSave.Save();
            }
            PublishIfChanged(state);
            if (!Ragnarok.Current.Active) { return; }

            if (state.Phase == RagnarokPhase.Countdown) {
                double left = Ragnarok.SecondsLeft;
                if (left <= Ragnarok.DayLengthSeconds && dayWarnedCycle != state.Cycle) {
                    dayWarnedCycle = state.Cycle;
                    MessageHud.instance?.MessageAll(MessageHud.MessageType.Center, "$al_ragnarok_day");
                }
                if (left <= ImminentSeconds && imminentWarnedCycle != state.Cycle) {
                    imminentWarnedCycle = state.Cycle;
                    MessageHud.instance?.MessageAll(MessageHud.MessageType.Center, "$al_ragnarok_imminent");
                }
                if (left <= 0) { BeginRagnarok(state); }
            } else if (TickRagnarok(state)) {
                return;
            }
            RagnarokHeralds.Tick(state);
        }

        internal static double CycleEnd(RagnarokState state) {
            return state.CycleStartSeconds + ValConfig.RagnarokCycleDays.Value * Ragnarok.DayLengthSeconds;
        }

        private static void StartCycle(RagnarokState state, bool advance) {
            if (advance) { state.Cycle++; }
            state.CycleStarted = true;
            state.CycleStartSeconds = Ragnarok.Now;
            state.Phase = RagnarokPhase.Countdown;
            state.WorldBossPrefab = "";
            state.WorldBoss = "";
            state.Heralds.Clear();
            state.RingRadius = 0f;
            RagnarokRing.Choose(state);
            RagnarokHeralds.ResetTimer();
            RagnarokSave.Save();
            Logger.LogInfo($"Ragnarok cycle {state.Cycle} begins: {ValConfig.RagnarokCycleDays.Value} in-game days until Ragnarok.");
        }

        // Settings can move the end of the cycle, so the snapshot is rebuilt every tick and sent only when it changed.
        private static void PublishIfChanged(RagnarokState state) {
            RagnarokSnapshot next = new RagnarokSnapshot(ValConfig.RagnarokEnabled.Value && state.CycleStarted, state.Phase, state.Cycle,
                state.CycleStartSeconds, CycleEnd(state), state.WorldBossPrefab, new Vector2(state.RingX, state.RingZ), state.RingRadius);
            if (next.SameAs(Ragnarok.Current)) { return; }
            RagnarokNetwork.BroadcastState(next);
        }

        private static void BeginRagnarok(RagnarokState state) {
            state.Phase = RagnarokPhase.Ragnarok;
            state.WorldBossPrefab = "";
            state.WorldBoss = "";
            RagnarokSave.Save();
            PublishIfChanged(state);
            Logger.LogWarning($"Ragnarok: cycle {state.Cycle} has run out. Ragnarok lasts until its world boss is killed.");
            // The boss's own message says Ragnarok has come, and would replace this one at once.
            if (ValConfig.RagnarokWorldBoss.Value && !TryRaiseWorldBoss(state)) {
                MessageHud.instance?.MessageAll(MessageHud.MessageType.Center, RagnarokRing.Active ? "$al_ragnarok_begins_ring" : "$al_ragnarok_begins");
            }
        }

        // Raises the world boss once a player is in the main world, and ends Ragnarok once it is dead. True when it
        // ended. With World Boss off there is nothing to kill, and Ragnarok passes at once.
        private static bool TickRagnarok(RagnarokState state) {
            if (string.IsNullOrEmpty(state.WorldBoss)) {
                if (!ValConfig.RagnarokWorldBoss.Value) {
                    MessageHud.instance?.MessageAll(MessageHud.MessageType.Center, "$al_ragnarok_passes");
                    Logger.LogWarning($"Ragnarok: cycle {state.Cycle} passes with no world boss.");
                    EndRagnarok(state);
                    return true;
                }
                TryRaiseWorldBoss(state);
                return false;
            }
            // Its owner destroys it on death, and the server hears of it wherever that happened.
            if (RagnarokPlacement.ParseId(state.WorldBoss) is ZDOID id && ZDOMan.instance.GetZDO(id) != null) { return false; }
            RagnarokWorldBosses.WorldBoss slain = RagnarokWorldBosses.Find(state.WorldBossPrefab);
            if (slain != null) { RagnarokNetwork.BroadcastWorldBoss(slain, Heightmap.Biome.None, slain: true); }
            Logger.LogWarning($"Ragnarok: {state.WorldBossPrefab} is dead; cycle {state.Cycle} is over.");
            EndRagnarok(state);
            return true;
        }

        private static bool TryRaiseWorldBoss(RagnarokState state) {
            if (!RagnarokWorldBosses.TryRaiseNearPlayer(out RagnarokWorldBosses.WorldBoss boss, out ZDO zdo)) {
                if (waitLoggedCycle != state.Cycle) {
                    waitLoggedCycle = state.Cycle;
                    Logger.LogInfo(RagnarokRing.Active
                        ? "Ragnarok: the world boss rises once a player is inside the ring's last circle."
                        : "Ragnarok: the world boss rises once a player is in the main world.");
                }
                return false;
            }
            state.WorldBossPrefab = boss.Prefab;
            state.WorldBoss = RagnarokPlacement.KeyOf(zdo.m_uid);
            RagnarokSave.Save();
            PublishIfChanged(state);
            Vector3 position = zdo.GetPosition();
            RagnarokNetwork.BroadcastWorldBoss(boss, WorldGenerator.instance.GetBiome(position), slain: false);
            Logger.LogWarning($"Ragnarok: {boss.Prefab} rises at {position.x:0}, {position.z:0}.");
            return true;
        }

        private static void EndRagnarok(RagnarokState state) {
            StartCycle(state, advance: true);
            PublishIfChanged(state);
        }

        // ---------------------------------------------------------------------------------------
        // Admin control and Ascension
        // ---------------------------------------------------------------------------------------

        private static bool TryGetRunningState(out RagnarokState state, out string message) {
            state = State;
            message = null;
            if (state == null) { message = "No world is loaded on this server."; return false; }
            if (!ValConfig.RagnarokEnabled.Value || !state.CycleStarted) { message = "The Ragnarok cycle is not running."; return false; }
            if (state.Phase != RagnarokPhase.Countdown) { message = "Ragnarok is already under way; it ends when its world boss is killed."; return false; }
            return true;
        }

        internal static bool TriggerNow(out string message) {
            if (!TryGetRunningState(out RagnarokState state, out message)) { return false; }
            if (ZoneSystem.instance == null || !ZoneSystem.instance.LocationsGenerated) { message = "The world is still loading."; return false; }
            BeginRagnarok(state);
            message = "Ragnarok has begun. Its world boss rises near a player in the main world, and it lasts until the boss is killed.";
            return true;
        }

        internal static bool SetDaysLeft(float days, out string message) {
            if (!TryGetRunningState(out RagnarokState state, out message)) { return false; }
            double length = ValConfig.RagnarokCycleDays.Value * Ragnarok.DayLengthSeconds;
            double left = Math.Max(0, Math.Min(days * Ragnarok.DayLengthSeconds, length));
            state.CycleStartSeconds = Ragnarok.Now - (length - left);
            RagnarokSave.Save();
            PublishIfChanged(state);
            message = $"Ragnarok in {left / Ragnarok.DayLengthSeconds:0.##} in-game days.";
            return true;
        }

        // Brings Ragnarok closer by some in-game days, on a player's Ascension (AscensionRagnarok). It never brings
        // it closer than minDaysLeft from now: a jump that would pass the end stops there instead, so players still
        // get warning. A countdown already closer than that is left where it is. `moved` is how many days it came.
        internal static bool Advance(float days, float minDaysLeft, string reason, out float moved) {
            moved = 0f;
            if (!TryGetRunningState(out RagnarokState state, out string message)) {
                Logger.LogDebug($"{reason} did not move Ragnarok: {message}");
                return false;
            }
            if (days <= 0f) { return false; }
            double dayLength = Ragnarok.DayLengthSeconds;
            double left = CycleEnd(state) - Ragnarok.Now;
            double floor = Math.Min(left, Math.Max(0f, minDaysLeft) * dayLength);
            double after = Math.Max(left - days * dayLength, floor);
            if (after >= left) {
                Logger.LogDebug($"{reason} did not move Ragnarok: it is already {left / dayLength:0.##} in-game days away.");
                return false;
            }
            state.CycleStartSeconds -= left - after;
            RagnarokSave.Save();
            PublishIfChanged(state);
            moved = (float)((left - after) / dayLength);
            Logger.LogInfo($"{reason} brought Ragnarok {moved:0.##} in-game days closer: it is now {after / dayLength:0.##} days away.");
            return true;
        }

        // Starts the countdown over, ending Ragnarok if it is under way. A world boss already risen stays in the
        // world, no longer holding Ragnarok.
        internal static bool Restart(out string message) {
            RagnarokState state = State;
            if (state == null) { message = "No world is loaded on this server."; return false; }
            StartCycle(state, advance: false);
            PublishIfChanged(state);
            message = $"The countdown starts over: {ValConfig.RagnarokCycleDays.Value} in-game days until Ragnarok.";
            return true;
        }
    }
}
