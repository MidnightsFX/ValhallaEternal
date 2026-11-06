using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>Powers that react to blocking and parrying, and to being staggered.</summary>
    internal static class BlockPowers {
        // Vanilla parries when the block started less than this long ago.
        private const float VanillaParryWindow = 0.25f;
        private const int BrutalDefianceBlocks = 3;
        private const float BrutalDefianceWindow = 6f;
        private const float BrutalDefianceRadius = 3f;
        private const float BrightRetortRadius = 4f;

        private static readonly List<float> recentBlocks = new List<float>();

        // Ailments a Clean Parry removes, first found first.
        private static readonly int[] Ailments = new int[] {
            SEMan.s_statusEffectBurning,
            SEMan.s_statusEffectPoison,
            SEMan.s_statusEffectFrost,
            SEMan.s_statusEffectLightning,
            SEMan.s_statusEffectSpirit,
            SEMan.s_statusEffectWet,
            SEMan.s_statusEffectSmoked,
            SEMan.s_statusEffectTared,
        };

        internal static void Reset() {
            recentBlocks.Clear();
        }

        /// <summary>Tower shields are the shields that can't parry: vanilla parries only when this is above 1.</summary>
        internal static bool IsTowerShield(ItemDrop.ItemData item) {
            return item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield && item.m_shared.m_timedBlockBonus <= 1f;
        }

        internal static bool HoldingTowerShield(Humanoid humanoid) => IsTowerShield(humanoid.GetCurrentBlocker());

        private struct BlockState {
            public bool Parry;
            public float Blockable;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
        private static class Blocking {
            private static void Prefix(Humanoid __instance, HitData hit, out BlockState __state) {
                __state = default;
                if (__instance != Player.m_localPlayer || hit == null) { return; }
                ItemDrop.ItemData blocker = __instance.GetCurrentBlocker();
                if (blocker == null) { return; }
                // A wider parry window: a block started a little too early still counts, by nudging the
                // timer vanilla reads back inside its window.
                float window = VanillaParryWindow * (1f + PowerSet.Get(Power.ParryWindow) / 100f);
                if (__instance.m_blockTimer >= VanillaParryWindow && __instance.m_blockTimer < window) {
                    __instance.m_blockTimer = VanillaParryWindow * 0.96f;
                }
                __state.Parry = blocker.m_shared.m_timedBlockBonus > 1f && __instance.m_blockTimer != -1f && __instance.m_blockTimer < VanillaParryWindow;
                __state.Blockable = hit.GetTotalBlockableDamage();
            }

            private static void Postfix(Humanoid __instance, HitData hit, Character attacker, BlockState __state) {
                if (__instance != Player.m_localPlayer || hit == null) { return; }
                float blocked = __state.Blockable - hit.GetTotalBlockableDamage();
                if (blocked <= 0f) { return; }
                Player player = (Player)__instance;
                Vector3 from = player.transform.position;

                if (PowerSet.Has(Power.MothersWard)) { SurvivalPowers.Fortify(1); }
                if (attacker != null && PowerSet.Has(Power.ThornsOnBlock, out float thorns)) {
                    PowerFx.Strike(attacker, new HitData.DamageTypes() { m_blunt = blocked * thorns / 100f }, from);
                }
                if (PowerSet.Has(Power.BrutalDefiance, out float defiance)) { BrutalDefiance(player, defiance); }

                if (!__state.Parry) { return; }
                if (PowerSet.Has(Power.CleanParry)) { CleanseOne(player); }
                if (hit.m_ranged && attacker != null && PowerSet.Has(Power.ReturnedLight, out float returned)) {
                    PowerFx.Effect(Power.ReturnedLight, VfxRole.Proc, attacker.GetCenterPoint());
                    PowerFx.Strike(attacker, new HitData.DamageTypes() { m_spirit = blocked * returned / 100f }, from);
                }
                if (PowerSet.Has(Power.BrightRetort)) {
                    PowerFx.Effect(Power.BrightRetort, VfxRole.Burst, from, 0.7f, shared: true);
                    foreach (Character enemy in PowerFx.EnemiesNear(from, BrightRetortRadius).ToArray()) {
                        enemy.Stagger((enemy.transform.position - from).normalized);
                    }
                }
                DamagePowers.ChargeNextAttack();
            }
        }

        // Every third block within a few seconds releases a burst around the player.
        private static void BrutalDefiance(Player player, float percentOfBlockPower) {
            float now = Time.time;
            recentBlocks.RemoveAll(t => now - t > BrutalDefianceWindow);
            recentBlocks.Add(now);
            if (recentBlocks.Count < BrutalDefianceBlocks) { return; }
            recentBlocks.Clear();
            ItemDrop.ItemData blocker = player.GetCurrentBlocker();
            float blockPower = blocker != null ? blocker.GetBlockPower(player.GetSkillFactor(Skills.SkillType.Blocking)) : 20f;
            float damage = blockPower * percentOfBlockPower / 100f;
            Vector3 from = player.transform.position;
            PowerFx.Effect(Power.BrutalDefiance, VfxRole.Burst, from, 0.6f, shared: true);
            foreach (Character enemy in PowerFx.EnemiesNear(from, BrutalDefianceRadius).ToArray()) {
                PowerFx.Strike(enemy, new HitData.DamageTypes() { m_blunt = damage }, from, pushForce: 40f);
            }
        }

        internal static void CleanseOne(Player player) {
            SEMan seman = player.GetSEMan();
            foreach (int ailment in Ailments) {
                if (seman.HaveStatusEffect(ailment)) {
                    seman.RemoveStatusEffect(ailment);
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
        private static class Staggering {
            private static bool Prefix(Character __instance, ref float damage, ref bool __result) {
                if (__instance != Player.m_localPlayer) { return true; }
                if (PowerSet.Has(Power.Unstaggerable)) {
                    __result = false;
                    return false;
                }
                // Footing: while blocking, stagger is paid in stamina when there is enough of it.
                if (PowerSet.Has(Power.Footing) && __instance.IsBlocking()) {
                    float cost = damage * 0.5f;
                    if (__instance.HaveStamina(cost)) {
                        __instance.UseStamina(cost);
                        damage = 0f;
                    }
                }
                return true;
            }
        }
    }
}
