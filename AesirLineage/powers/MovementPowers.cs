using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    internal static class MovementPowers {
        // Falcon Glide falls no faster than this; Falcon Form hovers and costs this much stamina a second.
        private const float GlideFallSpeed = 2f;
        private const float HoverStaminaPerSecond = 6f;
        private const float HangFallSpeed = 0.3f;
        // How long after a glide or hover a landing still takes no fall damage.
        private const float SoftLandingWindow = 1f;
        private const float SilentAfterKill = 3f;
        private const float SilentSeconds = 5f;

        private static bool doubleJumpUsed;
        private static float softLandingUntil;
        private static float silentUntil;

        internal static bool Silent => Time.time < silentUntil;
        internal static bool SoftLanding => Time.time < softLandingUntil;

        internal static void Reset() {
            doubleJumpUsed = false;
            softLandingUntil = 0f;
            silentUntil = 0f;
        }

        /// <summary>Paths, roads and tilled ground: terrain painted as cleared, paved or cultivated.</summary>
        internal static bool OnPath(Player player) {
            if (!player.IsOnGround()) { return false; }
            Vector3 at = player.transform.position;
            Heightmap heightmap = Heightmap.FindHeightmap(at);
            return heightmap != null && heightmap.IsCleared(at);
        }

        internal static bool OnSnow(Player player) {
            Heightmap.Biome biome = player.m_currentBiome;
            return biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth;
        }

        // Player overrides GetRunSpeedFactor without calling the base, so Valhalla Eternal's patch on
        // Character's version never ran for players.
        [HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
        private static class ShadowSpeed {
            private static void Postfix(Player __instance, ref float __result) {
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.ShadowSpeed, out float value)) { return; }
                if (EnvMan.IsNight() || OnSnow(__instance)) {
                    __result *= 1f + value / 100f;
                }
            }
        }

        /// <summary>Jump height (Leap while sprinting) and the double jump.</summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        private static class Jumping {
            private static bool Prefix(Character __instance, out float __state) {
                __state = -1f;
                if (__instance != Player.m_localPlayer) { return true; }
                Player player = (Player)__instance;

                // Vanilla calls Jump on every press, in the air too: there it pulls a grappling hook, and
                // while swimming it does nothing.
                bool canAct = !player.IsDead() && !player.InAttack() && !player.InDodge() && !player.IsEncumbered()
                    && !player.IsStaggering() && !player.IsKnockedBack() && !player.InLiquidSwimDepth() && GrapplingPoint.m_localGrappler == null;
                if (!player.IsOnGround() && PowerSet.Has(Power.DoubleJump) && !doubleJumpUsed && canAct && player.HaveStamina(player.m_jumpStaminaUsage)) {
                    doubleJumpUsed = true;
                    Vector3 velocity = player.m_body.linearVelocity;
                    velocity.y = player.m_jumpForce * 0.9f * JumpFactor(player);
                    player.m_body.linearVelocity = velocity;
                    player.UseStamina(player.m_jumpStaminaUsage);
                    PowerFx.Effect(Power.DoubleJump, VfxRole.Proc, player.transform.position, 0.6f);
                    return false;
                }

                float factor = JumpFactor(player);
                if (factor != 1f) {
                    __state = player.m_jumpForce;
                    player.m_jumpForce *= factor;
                }
                return true;
            }

            private static void Postfix(Character __instance, float __state) {
                if (__state >= 0f) { __instance.m_jumpForce = __state; }
            }

            private static float JumpFactor(Player player) {
                float factor = 1f + PowerSet.Get(Power.JumpHeight) / 100f;
                if (player.IsRunning() && PowerSet.Has(Power.Leap, out float leap)) { factor *= 1f + leap / 100f; }
                return factor;
            }
        }

        /// <summary>Glide, hover and hanging in the air while drawing a bow; run from the plugin's FixedUpdate.</summary>
        internal static void FixedTick(Player player) {
            if (player.IsOnGround()) {
                doubleJumpUsed = false;
                return;
            }
            if (player.IsDead() || player.InWater()) { return; }
            Vector3 velocity = player.m_body.linearVelocity;
            // TakeInput is false while chat, the console or a menu has the keyboard.
            bool jumpHeld = player.TakeInput() && ZInput.GetButton("Jump");
            bool changed = false;

            if (PowerSet.Has(Power.FalconForm) && jumpHeld && player.HaveStamina(HoverStaminaPerSecond * Time.fixedDeltaTime)) {
                player.UseStamina(HoverStaminaPerSecond * Time.fixedDeltaTime);
                if (velocity.y < 0f) { velocity.y = 0f; changed = true; }
            } else if (PowerSet.Has(Power.FalconGlide) && jumpHeld && velocity.y < -GlideFallSpeed) {
                velocity.y = -GlideFallSpeed;
                changed = true;
            }
            if (PowerSet.Has(Power.HangOnTheWind) && player.m_attackDrawTime > 0f && velocity.y < -HangFallSpeed) {
                velocity.y = -HangFallSpeed;
                velocity.x *= 0.9f;
                velocity.z *= 0.9f;
                changed = true;
            }
            if (changed) {
                player.m_body.linearVelocity = velocity;
                // Vanilla measures a fall from the highest point in the air; reset it while the player is held up.
                player.m_maxAirAltitude = player.transform.position.y;
                softLandingUntil = Time.time + SoftLandingWindow;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.SetCrouch))]
        private static class NoSneaking {
            private static void Prefix(Player __instance, ref bool crouch) {
                if (crouch && __instance == Player.m_localPlayer && PowerSet.Has(Power.NoSneak)) { crouch = false; }
            }
        }

        // Silent Passing and Charged Grace both answer a dodge.
        [HarmonyPatch(typeof(Player), nameof(Player.Dodge))]
        private static class Dodging {
            private static void Postfix(Player __instance) {
                if (__instance != Player.m_localPlayer) { return; }
                if (PowerSet.Has(Power.SilentPassing) && Time.time - KillPowers.LastKillTime < SilentAfterKill) {
                    silentUntil = Time.time + SilentSeconds;
                    PowerFx.Show(__instance, PowerFx.Silent, SilentSeconds);
                }
                DamagePowers.ChargeNextAttack();
            }
        }
    }
}
