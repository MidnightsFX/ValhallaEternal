using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Víðarr's Silence: patience built up between attacks and spent on the next one, stillness that
    /// hides the player, and the single blow after a long wait. An attack takes its patience when it
    /// starts and spends it on the hits of its first blow, the ones landing in the same frame; any hit
    /// counts as attacking again.
    /// </summary>
    internal static class SilencePowers {
        private const float PatiencePerSecond = 2f;
        private const float SingleBlowWait = 8f;
        private const float ImpatientWithin = 2f;
        private const float StillAfter = 2f;
        private const float StillSpeed = 0.1f;
        private const float StillStamina = 25f;

        private static float lastAttack = -100f;
        private static float stillSince = -1f;
        // What the attack under way carries, and the frame its first blow landed in (-1 before then).
        private static float patience;
        private static bool fullPatience;
        private static bool singleBlow;
        private static bool impatient;
        private static int spentFrame = -1;
        // Whether the hit being dealt is part of that first blow.
        private static bool firstBlow;

        internal static void Reset() {
            lastAttack = -100f;
            stillSince = -1f;
            patience = 0f;
            fullPatience = false;
            singleBlow = false;
            impatient = false;
            spentFrame = -1;
            firstBlow = false;
        }

        // Vow of Silence: twice as quick, and further.
        private static float MostPatience() => PowerSet.Get(Power.Patience) + PowerSet.Get(Power.SilenceVow);

        private static float PerSecond() => PatiencePerSecond * (PowerSet.Has(Power.SilenceVow) ? 2f : 1f);

        [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static class AttackStarts {
            private static void Postfix(Humanoid character, bool __result) {
                if (!__result || character != Player.m_localPlayer) { return; }
                float waited = Time.time - lastAttack;
                lastAttack = Time.time;
                float most = MostPatience();
                patience = PowerSet.Has(Power.Patience) ? Mathf.Min(most, waited * PerSecond()) : 0f;
                fullPatience = patience > 0f && patience >= most;
                singleBlow = PowerSet.Has(Power.SingleBlow) && waited >= SingleBlowWait;
                impatient = waited < ImpatientWithin;
                spentFrame = -1;
                if (singleBlow) { PowerFx.Effect(Power.SingleBlow, VfxRole.Proc, character.GetCenterPoint(), 0.6f); }
            }
        }

        /// <summary>The first of the three calls for each hit: settles whether it belongs to the first blow.</summary>
        internal static void PrepareHit(HitData hit) {
            if (spentFrame < 0) { spentFrame = Time.frameCount; }
            firstBlow = spentFrame == Time.frameCount;
            // A looping attack or a late projectile keeps hitting without a new start; each hit is attacking.
            lastAttack = Time.time;
            if (firstBlow && fullPatience && PowerSet.Has(Power.OneWord, out float word)) { hit.m_staggerMultiplier *= 1f + word / 100f; }
        }

        internal static float OutgoingFactor(Character target) {
            if (!firstBlow) { return 1f; }
            float factor = 1f + patience / 100f;
            if (singleBlow) { factor *= target.IsBoss() ? 2f : 3f; }
            if (impatient) { factor *= Mathf.Max(0.2f, 1f - PowerSet.Get(Power.ImpatiencePenalty) / 100f); }
            return factor;
        }

        internal static void AfterHit(Character target, HitData hit) {
            if (firstBlow && fullPatience && PowerSet.Has(Power.OneWord) && !target.IsBoss()) { target.Stagger(hit.m_dir); }
        }

        internal static void Tick(Player player) {
            bool standing = player.IsOnGround() && player.GetVelocity().magnitude < StillSpeed;
            if (!standing) {
                stillSince = -1f;
            } else if (stillSince < 0f) {
                stillSince = Time.time;
            }
        }

        private static bool Still => stillSince >= 0f && Time.time - stillSince >= StillAfter;

        /// <summary>Stillness: lower is harder to notice. Vanilla only asks while the player sneaks.</summary>
        internal static float StealthFactor() => Still ? 1f - PowerSet.Get(Power.Stillness) / 100f : 1f;

        /// <summary>Stillness's stamina regen, in percent.</summary>
        internal static float StaminaBonus() => Still && PowerSet.Has(Power.Stillness) ? StillStamina : 0f;
    }
}
