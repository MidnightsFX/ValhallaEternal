using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.ui;

namespace AesirLineage.ragnarok {
    // A ring that closes in on the main world as Ragnarok nears. Each cycle the server picks its last circle, and the
    // ring closes on it in Steps steps between RingStartDays and RingEndDays before Ragnarok, each circle a straight
    // step from the whole world towards the last one. Because each step moves the centre no further than it shrinks
    // the radius, every circle lies inside the one before.
    //
    // Every machine works the ring out from the cycle's snapshot and the synced config, as it does the countdown, so
    // only the last circle travels. Players are judged on their own machine: true damage there, and enemy hits in
    // Character.RPC_Damage, which for a player runs on that player's machine.
    internal static class RagnarokRing {
        internal const int Steps = 10;
        // The main world's land, which the last circle stays within, and where its ocean ends. Past the ocean is no
        // longer the main world (Eternal Hall's islands lie out there), and the ring never reaches it.
        private const float LandRadius = WorldGenerator.worldSize;
        internal const float EdgeRadius = WorldGenerator.waterEdge;
        private const float StackSeconds = 60f;
        private const float TickSeconds = 1f;
        private const int Candidates = 24;
        private const int LandSamples = 64;
        private const float GoldenAngle = 2.39996323f;
        private const string EffectName = "AL_RagnarokRing";

        internal struct Circle {
            internal Vector2 Center;
            internal float Radius;

            internal bool Contains(Vector3 point) => (new Vector2(point.x, point.z) - Center).sqrMagnitude <= Radius * Radius;
        }

        internal static bool Active {
            get {
                RagnarokSnapshot snapshot = Ragnarok.Current;
                return snapshot.Active && snapshot.RingRadius > 0f && ValConfig.RagnarokRing.Value;
            }
        }

        internal static Circle Final => new Circle() { Center = Ragnarok.Current.RingCenter, Radius = Ragnarok.Current.RingRadius };

        // Steps closed so far: 0 is the whole world, Steps the last circle.
        internal static int Stage => StageNow(out _);

        // In-game seconds until the next step closes, or -1 at the last circle.
        internal static double SecondsToNextStep {
            get {
                StageNow(out double seconds);
                return seconds;
            }
        }

        private static int StageNow(out double secondsToNext) {
            secondsToNext = -1;
            RagnarokSnapshot snapshot = Ragnarok.Current;
            if (snapshot.Phase == RagnarokPhase.Ragnarok) { return Steps; }
            double dayLength = Ragnarok.DayLengthSeconds;
            double left = Math.Max(0, snapshot.EndSeconds - Ragnarok.Now) / dayLength;
            double start = Math.Min(ValConfig.RingStartDays.Value, ValConfig.RagnarokCycleDays.Value);
            double end = ValConfig.RingEndDays.Value;
            if (start <= end || left <= end) { return Steps; }
            double step = (start - end) / Steps;
            int stage = Mathf.Clamp((int)Math.Floor((start - left) / step), 0, Steps - 1);
            secondsToNext = (left - (start - (stage + 1) * step)) * dayLength;
            return stage;
        }

        internal static Circle At(int stage) {
            Circle final = Final;
            float t = Mathf.Clamp01(stage / (float)Steps);
            return new Circle() { Center = Vector2.Lerp(Vector2.zero, final.Center, t), Radius = Mathf.Lerp(EdgeRadius, final.Radius, t) };
        }

        // Dungeons count by where they lie, since they share the world's x and z.
        internal static bool InMainWorld(Vector3 position) {
            return Utils.DistanceXZ(Vector3.zero, position) <= EdgeRadius && !RagnarokPlacement.OnIsland(position);
        }

        internal static bool IsOutside(Vector3 position) {
            if (!Active) { return false; }
            int stage = Stage;
            return stage > 0 && InMainWorld(position) && !At(stage).Contains(position);
        }

        // ---------------------------------------------------------------------------------------
        // The last circle (server)
        // ---------------------------------------------------------------------------------------

        // Of a few random circles wholly on the main world's land, the one with the most dry land in it.
        internal static void Choose(RagnarokState state) {
            WorldGenerator generator = WorldGenerator.instance;
            if (generator == null) { return; }
            float radius = LandRadius * Mathf.Sqrt(ValConfig.RingFinalAreaPercent.Value / 100f);
            float reach = Mathf.Max(0f, LandRadius - radius);
            Vector2 best = Vector2.zero;
            float bestLand = -1f;
            for (int i = 0; i < Candidates; i++) {
                Vector2 center = UnityEngine.Random.insideUnitCircle * reach;
                float land = LandShare(generator, center, radius);
                if (land > bestLand) {
                    best = center;
                    bestLand = land;
                }
            }
            state.RingX = best.x;
            state.RingZ = best.y;
            state.RingRadius = radius;
            Logger.LogInfo($"Ragnarok cycle {state.Cycle}: the ring closes on {radius:0}m around {best.x:0}, {best.y:0} ({bestLand * 100f:0}% land).");
        }

        // From points spread evenly over the circle.
        private static float LandShare(WorldGenerator generator, Vector2 center, float radius) {
            int land = 0;
            for (int i = 0; i < LandSamples; i++) {
                float distance = radius * Mathf.Sqrt((i + 0.5f) / LandSamples);
                float angle = i * GoldenAngle;
                if (generator.GetHeight(center.x + Mathf.Cos(angle) * distance, center.y + Mathf.Sin(angle) * distance) >= RagnarokPlacement.DryLand) { land++; }
            }
            return land / (float)LandSamples;
        }

        // ---------------------------------------------------------------------------------------
        // The local player
        // ---------------------------------------------------------------------------------------

        internal static int Stacks { get; private set; }

        private static int effectHash;
        private static Player tracked;
        private static float nextTick;
        private static float nextStackChange;
        private static int lastStage = -1;
        private static bool wasOutside;

        internal static bool LocalOutside => wasOutside;

        internal static void RegisterStatusEffect() {
            SE_RagnarokRing effect = ScriptableObject.CreateInstance<SE_RagnarokRing>();
            effect.name = EffectName;
            effect.m_name = "$al_se_ring";
            Texture2D icon = RuneArt.Texture("hagalaz");
            if (icon != null) {
                effect.m_icon = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
            }
            CustomStatusEffect custom = new CustomStatusEffect(effect, fixReference: false);
            ItemManager.Instance.AddStatusEffect(custom);
            effectHash = custom.StatusEffect.NameHash();
        }

        internal static void Update() {
            if (Time.time < nextTick) { return; }
            nextTick = Time.time + TickSeconds;
            Player player = Player.m_localPlayer;
            if (player != tracked) {
                tracked = player;
                Stacks = 0;
                wasOutside = false;
                lastStage = -1;
            }
            if (player == null) { return; }
            if (!Active) {
                Stacks = 0;
                wasOutside = false;
                lastStage = -1;
                ShowEffect(player, false);
                return;
            }

            int stage = Stage;
            if (lastStage >= 0 && stage > lastStage) {
                player.Message(MessageHud.MessageType.Center, stage >= Steps ? "$al_ring_final" : "$al_ring_closes");
            }
            lastStage = stage;
            if (player.IsDead()) { return; }

            bool outside = IsOutside(player.transform.position);
            if (outside != wasOutside) {
                wasOutside = outside;
                nextStackChange = Time.time + StackSeconds;
                if (outside && Stacks == 0) { Stacks = 1; }
                player.Message(outside ? MessageHud.MessageType.Center : MessageHud.MessageType.TopLeft, outside ? "$al_ring_outside" : "$al_ring_inside");
            } else if ((outside || Stacks > 0) && Time.time >= nextStackChange) {
                nextStackChange = Time.time + StackSeconds;
                Stacks += outside ? 1 : -1;
            }
            Stacks = Mathf.Clamp(Stacks, 0, stage);

            bool burning = ValConfig.RingTrueDamage.Value;
            if (outside && burning && Stacks > 0) { Burn(player, Stacks * ValConfig.RingDamagePerStack.Value * TickSeconds); }
            ShowEffect(player, outside || (burning && Stacks > 0));
        }

        // Straight to ApplyDamage, past the armor, resistances and blocking that RPC_Damage applies.
        private static void Burn(Player player, float amount) {
            HitData hit = new HitData();
            hit.m_damage.m_damage = amount;
            hit.m_point = player.GetCenterPoint();
            player.ApplyDamage(hit, showDamageText: true, triggerEffects: false);
        }

        private static void ShowEffect(Player player, bool show) {
            SEMan seman = player.GetSEMan();
            if (seman == null || effectHash == 0) { return; }
            bool shown = seman.HaveStatusEffect(effectHash);
            if (show && !shown) {
                seman.AddStatusEffect(effectHash);
            } else if (!show && shown) {
                seman.RemoveStatusEffect(effectHash, quiet: true);
            }
        }

        internal static string Tooltip() {
            Localization loc = Localization.instance;
            string text = loc.Localize(wasOutside ? "$al_se_ring_outside" : "$al_se_ring_inside");
            if (wasOutside && ValConfig.RingEnemyDamage.Value) {
                text += "\n" + loc.Localize("$al_se_ring_enemies", ValConfig.RingEnemyDamageMultiplier.Value.ToString("0.#"));
            }
            if (ValConfig.RingTrueDamage.Value) {
                text += "\n" + loc.Localize("$al_se_ring_burn", Stacks.ToString(), (Stacks * ValConfig.RingDamagePerStack.Value).ToString("0.#"));
            }
            return text;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Character_RPC_Damage_Patch {
            private static void Prefix(Character __instance, HitData hit) {
                if (hit == null || __instance != Player.m_localPlayer || !ValConfig.RingEnemyDamage.Value) { return; }
                if (!IsOutside(__instance.transform.position)) { return; }
                Character attacker = hit.GetAttacker();
                if (attacker == null || attacker.IsPlayer()) { return; }
                hit.m_damage.Modify(ValConfig.RingEnemyDamageMultiplier.Value);
            }
        }
    }

    // Shows the stacks on the effect's icon.
    public class SE_RagnarokRing : StatusEffect {
        public override string GetIconText() {
            return ValConfig.RingTrueDamage.Value && RagnarokRing.Stacks > 0 ? "x" + RagnarokRing.Stacks : "";
        }

        public override string GetTooltipString() {
            return RagnarokRing.Tooltip();
        }
    }
}
