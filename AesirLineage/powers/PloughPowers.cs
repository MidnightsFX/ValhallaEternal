using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// The Plough of Gefjun: a two-handed weapon's secondary attack drives a furrow through the ground ahead,
    /// striking every enemy along it a moment after the swing starts.
    /// </summary>
    internal static class PloughPowers {
        private const float StrikeDelay = 0.6f;
        private const float FurrowLength = 8f;
        private const float FurrowHalfWidth = 1.5f;
        private const float PushForce = 40f;

        private static readonly List<(float at, Vector3 from, Vector3 dir, float damage)> furrows = new List<(float, Vector3, Vector3, float)>();

        internal static void Reset() { furrows.Clear(); }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class Swing {
            private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result) {
                if (!__result || !secondaryAttack || __instance != Player.m_localPlayer) { return; }
                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if (weapon == null || !weapon.IsTwoHanded() || !PowerSet.Has(Power.FurrowSlam, out float share)) { return; }
                // IsTwoHanded counts bows too; only a swing ploughs.
                if (__instance.m_currentAttack == null || __instance.m_currentAttack.m_attackType == Attack.AttackType.Projectile) { return; }
                Vector3 dir = __instance.transform.forward;
                dir.y = 0f;
                furrows.Add((Time.time + StrikeDelay, __instance.transform.position, dir.normalized, weapon.GetDamage().GetTotalDamage() * share / 100f));
            }
        }

        internal static void Tick() {
            for (int i = furrows.Count - 1; i >= 0; i--) {
                if (Time.time < furrows[i].at) { continue; }
                (float _, Vector3 from, Vector3 dir, float damage) = furrows[i];
                furrows.RemoveAt(i);
                Strike(from, dir, damage);
            }
        }

        private static void Strike(Vector3 from, Vector3 dir, float damage) {
            for (float step = 1.5f; step <= FurrowLength; step += 2f) {
                PowerFx.Effect(Power.FurrowSlam, VfxRole.Harvest, from + dir * step, 0.8f, shared: true);
            }
            Vector3 middle = from + dir * (FurrowLength / 2f);
            foreach (Character enemy in PowerFx.EnemiesNear(middle, FurrowLength / 2f + FurrowHalfWidth).ToArray()) {
                Vector3 offset = enemy.transform.position - from;
                offset.y = 0f;
                float along = Vector3.Dot(offset, dir);
                if (along < 0f || along > FurrowLength || (offset - dir * along).magnitude > FurrowHalfWidth) { continue; }
                PowerFx.Strike(enemy, new HitData.DamageTypes() { m_blunt = damage }, from, PushForce);
            }
        }
    }
}
