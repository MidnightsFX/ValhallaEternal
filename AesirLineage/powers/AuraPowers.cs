using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.ui;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>Powers that act around the player over time, pulsed once a second.</summary>
    internal static class AuraPowers {
        private const float PulseEvery = 1f;
        private const float RetortSeconds = 5f;
        private const float RetortRadius = 3f;
        private const float DawnRadius = 6f;
        private const float MedicRadius = 5f;
        // Pyre of Helheim burns this share of max health a second, but never below 1 health.
        private const float PyrePerSecond = 0.01f;
        // Stone Skin: standing this still for this long.
        private const float StillSpeed = 0.1f;
        private const float StillSeconds = 1f;

        private static float nextPulse;
        private static float retortUntil;
        private static float stillSince;
        private static bool stoneSkinApplied;
        private static Light dawnLight;

        internal static void Reset() {
            retortUntil = 0f;
            stillSince = 0f;
            stoneSkinApplied = false;
            if (dawnLight != null) { Object.Destroy(dawnLight.gameObject); }
            dawnLight = null;
        }

        internal static void OnFireTaken() {
            if (!PowerSet.Has(Power.Retort)) { return; }
            if (Time.time >= retortUntil) { PowerFx.Effect(Power.Retort, VfxRole.Proc, Player.m_localPlayer.transform.position); }
            retortUntil = Time.time + RetortSeconds;
            PowerFx.Show(Player.m_localPlayer, PowerFx.Retort, RetortSeconds);
        }

        internal static void Tick(Player player) {
            UpdateStoneSkin(player);
            UpdateDawnLight(player);
            if (Time.time < nextPulse) { return; }
            nextPulse = Time.time + PulseEvery;
            Vector3 at = player.transform.position;

            if (Time.time < retortUntil && PowerSet.Has(Power.Retort, out float retort)) {
                foreach (Character enemy in PowerFx.EnemiesNear(at, RetortRadius).ToArray()) {
                    PowerFx.Strike(enemy, new HitData.DamageTypes() { m_fire = retort }, at);
                }
            }
            if (PowerSet.Has(Power.Dawnbringer, out float dawn)) {
                foreach (Character enemy in PowerFx.EnemiesNear(at, DawnRadius).ToArray()) {
                    if (enemy.m_faction == Character.Faction.Undead) {
                        PowerFx.Strike(enemy, new HitData.DamageTypes() { m_spirit = dawn }, at);
                    }
                }
            }
            if (PowerSet.Has(Power.PyreOfHelheim) && player.GetHealth() > 1f) {
                player.SetHealth(Mathf.Max(1f, player.GetHealth() - player.GetMaxHealth() * PyrePerSecond));
            }
            if (PowerSet.Has(Power.BattleMedic, out float medic) && player.IsBlocking()) {
                foreach (Player ally in PowerFx.AlliesNear(at, MedicRadius)) { ally.Heal(medic); }
            }
        }

        private static void UpdateStoneSkin(Player player) {
            if (!PowerSet.Has(Power.StoneSkin)) { return; }
            bool still = player.GetVelocity().magnitude < StillSpeed && player.IsOnGround();
            if (!still) {
                stillSince = Time.time;
                stoneSkinApplied = false;
                return;
            }
            if (!stoneSkinApplied && Time.time - stillSince >= StillSeconds) {
                stoneSkinApplied = true;
                SurvivalPowers.Fortify(5);
                PowerFx.Effect(Power.StoneSkin, VfxRole.Proc, player.transform.position);
            }
        }

        // Dawnbringer lights the dark around the player.
        private static void UpdateDawnLight(Player player) {
            bool want = PowerSet.Has(Power.Dawnbringer);
            if (want && dawnLight == null) {
                dawnLight = new GameObject("AL_Dawnbringer").AddComponent<Light>();
                dawnLight.transform.SetParent(player.transform, false);
                dawnLight.transform.localPosition = Vector3.up * 2f;
                dawnLight.type = LightType.Point;
                dawnLight.color = StarMapArt.ParseColor("#FFF4D6", Color.white);
                dawnLight.range = 10f;
                dawnLight.intensity = 1.2f;
                dawnLight.shadows = LightShadows.None;
            } else if (!want && dawnLight != null) {
                Object.Destroy(dawnLight.gameObject);
                dawnLight = null;
            }
        }
    }
}
