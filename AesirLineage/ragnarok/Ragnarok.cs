using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;

namespace AesirLineage.ragnarok {
    // Ragnarok: a repeating cycle in the main world.
    //
    // A cycle counts down a number of in-game days. As it runs, creatures spawn more often, and in its last
    // stretch heralds of Ragnarok appear near players in the main world. When it ends Ragnarok itself begins: a
    // world boss rises near a player in the main world (RagnarokWorldBosses), and Ragnarok lasts until it is
    // killed. Then the next cycle starts. See RagnarokServer for the server's side.
    //
    // Every machine works the countdown out the same way, from the cycle's start and end in ZNet time (which
    // the server keeps in step on every client) and the synced config, so the HUD and the spawn scaling need
    // no traffic beyond the state packet sent when the cycle changes.

    public enum RagnarokPhase {
        Countdown = 0,
        // The countdown has run out. Lasts until the world boss is killed.
        Ragnarok = 1,
    }

    // What the server saves about the cycle, in a file beside the world (RagnarokSave).
    public class RagnarokState {
        public int Cycle = 1;
        public bool CycleStarted;
        // ZNet time, in seconds, at which the countdown under way began. Can be below 0: setting the days left
        // in a world younger than the cycle moves the start to before the world began.
        public double CycleStartSeconds;
        public RagnarokPhase Phase = RagnarokPhase.Countdown;
        // During Ragnarok, the world boss's prefab and ZDO id ("user:id"); both empty until it has risen.
        public string WorldBossPrefab = "";
        public string WorldBoss = "";
        // ZDO ids ("user:id") of the heralds that have appeared this cycle and may still be alive.
        public List<string> Heralds = new List<string>();
        // The ring's smallest circle for this cycle (RagnarokRing). A radius of 0 means none is chosen yet.
        public float RingX;
        public float RingZ;
        public float RingRadius;
    }

    // The cycle as every machine sees it. Swapped as a whole.
    internal sealed class RagnarokSnapshot {
        internal readonly bool Active;
        internal readonly RagnarokPhase Phase;
        internal readonly int Cycle;
        internal readonly double StartSeconds;
        internal readonly double EndSeconds;
        // During Ragnarok, the world boss that has risen; empty until then.
        internal readonly string WorldBossPrefab;
        internal readonly Vector2 RingCenter;
        internal readonly float RingRadius;

        internal static readonly RagnarokSnapshot None = new RagnarokSnapshot(false, RagnarokPhase.Countdown, 0, 0, 0, "", Vector2.zero, 0f);

        internal RagnarokSnapshot(bool active, RagnarokPhase phase, int cycle, double start, double end, string worldBossPrefab, Vector2 ringCenter, float ringRadius) {
            Active = active;
            Phase = phase;
            Cycle = cycle;
            StartSeconds = start;
            EndSeconds = end;
            WorldBossPrefab = worldBossPrefab ?? "";
            RingCenter = ringCenter;
            RingRadius = ringRadius;
        }

        internal bool SameAs(RagnarokSnapshot other) {
            return other != null && Active == other.Active && Phase == other.Phase && Cycle == other.Cycle
                && StartSeconds == other.StartSeconds && EndSeconds == other.EndSeconds && WorldBossPrefab == other.WorldBossPrefab
                && RingCenter == other.RingCenter && RingRadius == other.RingRadius;
        }
    }

    internal static class Ragnarok {
        internal static RagnarokSnapshot Current = RagnarokSnapshot.None;

        internal static double Now => ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : 0;

        internal static bool CountingDown => Current.Active && Current.Phase == RagnarokPhase.Countdown;

        internal static bool InRagnarok => Current.Active && Current.Phase == RagnarokPhase.Ragnarok;

        // How far through the cycle the world is, 0 at its start and 1 at Ragnarok.
        internal static float Progress {
            get {
                RagnarokSnapshot snapshot = Current;
                if (!snapshot.Active) { return 0f; }
                if (snapshot.Phase == RagnarokPhase.Ragnarok) { return 1f; }
                double length = snapshot.EndSeconds - snapshot.StartSeconds;
                if (length <= 0) { return 1f; }
                return Mathf.Clamp01((float)((Now - snapshot.StartSeconds) / length));
            }
        }

        internal static double SecondsLeft => CountingDown ? System.Math.Max(0, Current.EndSeconds - Now) : 0;

        // In-game days until Ragnarok, or -1 while it is not counting down.
        internal static double DaysLeft => CountingDown ? SecondsLeft / DayLengthSeconds : -1;

        // Heralds keep coming through Ragnarok itself, as well as in the last part of the countdown.
        internal static bool InHeraldPhase {
            get {
                float phase = ValConfig.HeraldPhase.Value;
                return Current.Active && phase > 0f && Progress >= 1f - phase;
            }
        }

        // The spawn rate for the time of day, rising evenly from the start value to the end value over the
        // cycle, and staying at the end value through Ragnarok. 1 when no cycle is running.
        internal static float SpawnRate(bool night) {
            if (!Current.Active) { return 1f; }
            float start = night ? ValConfig.NightSpawnRateStart.Value : ValConfig.SpawnRateStart.Value;
            float end = night ? ValConfig.NightSpawnRateEnd.Value : ValConfig.SpawnRateEnd.Value;
            return Mathf.Lerp(start, end, Progress);
        }

        internal static double DayLengthSeconds => EnvMan.instance != null && EnvMan.instance.m_dayLengthSec > 0 ? EnvMan.instance.m_dayLengthSec : 1800;

        internal static string BiomeName(Heightmap.Biome biome) {
            return Localization.instance.Localize("$biome_" + biome.ToString().ToLowerInvariant());
        }

        internal static void Publish(RagnarokSnapshot snapshot) {
            Current = snapshot ?? RagnarokSnapshot.None;
        }

        internal static void Reset() {
            Current = RagnarokSnapshot.None;
        }
    }
}
