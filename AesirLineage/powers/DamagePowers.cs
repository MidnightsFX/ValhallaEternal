using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Every power that changes a hit, in three hooks rather than one patch per power.
    /// </summary>
    /// <remarks>
    /// Outgoing hits are changed in <c>Character.Damage</c>, which runs on the attacker's machine before the
    /// hit is sent to whoever owns the target. Valhalla Eternal changed them in <c>RPC_Damage</c>, which runs
    /// on the target's owner, so in multiplayer a player's damage boons only worked on creatures that player
    /// happened to own. Incoming hits are changed in <c>RPC_Damage</c>, which for a player always runs on
    /// that player's own machine, and the final, after-armour amount in <c>ApplyDamage</c>, where lethal
    /// blows can be caught.
    /// </remarks>
    internal static class DamagePowers {
        // Perfect Form charges with damage dealt and releases on the hit that fills it.
        private const float PerfectFormCharge = 500f;
        private const int ArrowCatcherEvery = 5;
        // Below this share of its health a target takes Execute damage.
        private const float ExecuteBelowHealth = 0.3f;
        // Damage-dealt oaths never take a hit below this share.
        private const float MinimumDamageKept = 0.2f;
        private const float BaseCritMultiplier = 1.5f;
        private const float CertainBlowMultiplier = 1.25f;
        private const int MaxFury = 20;
        private const float FuryLasts = 4f;
        private const int MaxMarks = 5;
        private const float MarkLasts = 6f;
        private const float WoundLasts = 5f;
        private const float ForesightWindow = 20f;
        private const float AllThingsEvery = 20f;
        private const float ShelterBelowHealth = 0.35f;
        private const float DomeRange = 4f;
        private const float LastDoorRange = 10f;

        private static float perfectFormCharge;
        private static int projectilesTaken;
        private static int furyStacks;
        private static float furyLastHit;
        private static bool charged;
        private static float allThingsReadyAt;
        private static readonly Dictionary<ZDOID, (int stacks, float until)> marks = new Dictionary<ZDOID, (int, float)>();
        private static readonly Dictionary<ZDOID, float> wounded = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<ZDOID, float> foresightSeen = new Dictionary<ZDOID, float>();

        /// <summary>The creature the local player last hit, for Tracks.</summary>
        internal static Character LastTarget;
        internal static float LastTargetTime;

        internal static void Reset() {
            perfectFormCharge = 0f;
            projectilesTaken = 0;
            furyStacks = 0;
            charged = false;
            allThingsReadyAt = 0f;
            marks.Clear();
            wounded.Clear();
            foresightSeen.Clear();
            LastTarget = null;
        }

        internal static void ChargeNextAttack() {
            if (!PowerSet.Has(Power.ChargedGrace)) { return; }
            charged = true;
            PowerFx.Show(Player.m_localPlayer, PowerFx.Charged, 10f);
        }

        /// <summary>
        /// Open Wound's bleeding: hit with it in the last few seconds, or still carrying the poison it left,
        /// so a creature the bleed itself finishes off counts too.
        /// </summary>
        internal static bool WasWounded(Character victim) {
            if (victim == null || !wounded.TryGetValue(victim.GetZDOID(), out float until)) { return false; }
            return until > Time.time || victim.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectPoison);
        }

        internal static void Tick() {
            if (furyStacks > 0 && Time.time - furyLastHit > FuryLasts) { furyStacks = 0; }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Outgoing {
            private static void Prefix(Character __instance, HitData hit) {
                Player local = Player.m_localPlayer;
                if (PowerFx.DealingSecondary || local == null || hit == null || __instance == local || hit.m_hitType != HitData.HitType.PlayerHit) { return; }
                if (hit.GetAttacker() != local) { return; }

                float raw = hit.GetTotalDamage();
                bool melee = !hit.m_ranged;
                Skills.SkillType skill = hit.m_skill;
                bool magic = skill == Skills.SkillType.ElementalMagic || skill == Skills.SkillType.BloodMagic;
                bool bow = skill == Skills.SkillType.Bows;
                bool undead = __instance.m_faction == Character.Faction.Undead;
                ZDOID targetId = __instance.GetZDOID();
                bool forceCrit = false;
                StrikePowers.PrepareHit(local, __instance, hit, ref forceCrit);

                float multiplier = 1f + PowerSet.Get(Power.AllDamage) / 100f;
                multiplier *= StrikePowers.OutgoingFactor(local, __instance, hit);
                multiplier *= 1f + PowerSet.Get(melee ? Power.MeleeDamage : Power.RangedDamage) / 100f;
                if (melee) { multiplier *= Kept(Power.MeleeDamagePenalty); }
                if (bow) { multiplier *= (1f + PowerSet.Get(Power.BowDamage) / 100f) * Kept(Power.BowDamagePenalty); }
                if (magic) { multiplier *= 1f + PowerSet.Get(Power.MagicDamage) / 100f; }
                if (undead) { multiplier *= 1f + PowerSet.Get(Power.UndeadDamage) / 100f; }
                if (PowerSet.Has(Power.ExecuteDamage, out float execute) && __instance.GetHealthPercentage() < ExecuteBelowHealth) {
                    multiplier *= 1f + execute / 100f;
                }
                if (PowerSet.Has(Power.PerfectForm, out float perfectForm)) {
                    perfectFormCharge += raw;
                    if (perfectFormCharge >= PerfectFormCharge) {
                        perfectFormCharge = 0f;
                        multiplier *= 1f + perfectForm / 100f;
                        PowerFx.Effect(Power.PerfectForm, VfxRole.Proc, __instance.GetCenterPoint());
                    }
                }
                if (PowerSet.Has(Power.BattleFury, out float fury)) { multiplier *= 1f + furyStacks * fury / 100f; }
                multiplier *= 1f + SkillPowers.VoluspaBonus() / 100f;
                multiplier *= 1f + KillPowers.FallenBonus() / 100f;
                if (PowerSet.Has(Power.Unveiling, out float unveiling) && marks.TryGetValue(targetId, out var mark) && mark.until > Time.time) {
                    multiplier *= 1f + mark.stacks * unveiling / 100f;
                }
                bool wasCharged = charged && PowerSet.Has(Power.ChargedGrace);
                if (wasCharged) {
                    multiplier *= 1f + PowerSet.Get(Power.ChargedGrace) / 100f;
                    charged = false;
                    PowerFx.Hide(local, PowerFx.Charged);
                }
                multiplier *= Kept(Power.LessDamageDealt);
                if (EnvMan.IsNight()) { multiplier *= Kept(Power.NightDamagePenalty); }

                bool certain = PowerSet.Has(Power.CertainBlow);
                bool crit = certain || forceCrit || (PowerSet.Has(Power.CritChance, out float critChance) && UnityEngine.Random.value * 100f < critChance);
                if (crit) {
                    multiplier *= certain ? CertainBlowMultiplier : BaseCritMultiplier + PowerSet.Get(Power.CritDamage) / 100f;
                }
                if (multiplier != 1f) { hit.m_damage.Modify(multiplier); }

                hit.m_damage.m_blunt *= Kept(Power.LessBluntDamage);
                hit.m_damage.m_pierce *= Kept(Power.LessPierceDamage);
                hit.m_damage.m_slash *= Kept(Power.LessSlashDamage);
                hit.m_damage.m_spirit += PowerSet.Get(Power.SpiritDamage);
                hit.m_damage.m_frost += PowerSet.Get(Power.FrostDamage);
                if (PowerSet.Has(Power.PyreOfHelheim)) { hit.m_damage.m_fire *= 1.5f; }
                if (PowerSet.Has(Power.OpenWound, out float wound)) {
                    // Bleeding: poison damage, which the target's owner turns into damage over time.
                    hit.m_damage.m_poison += raw * wound / 100f;
                    wounded[targetId] = Time.time + WoundLasts;
                }
                if (PowerSet.Has(Power.NoPoisonDealt)) { hit.m_damage.m_poison = 0f; }
                if (PowerSet.Has(Power.StormRage, out float stormRage)) { AddStormDamage(hit, stormRage); }
                if (PowerSet.Has(Power.QueensDecree, out float decree) && !__instance.IsBoss() && __instance.GetHealthPercentage() * 100f < decree) {
                    hit.m_damage.m_damage += __instance.GetHealth() * 2f;
                }

                // What the hit set off.
                if (PowerSet.Has(Power.LifeLeech, out float leech)) { local.Heal(hit.GetTotalDamage() * leech / 100f, false); }
                if (PowerSet.Has(Power.BattleFury)) {
                    furyStacks = Mathf.Min(MaxFury, furyStacks + 1);
                    furyLastHit = Time.time;
                }
                if (PowerSet.Has(Power.Unveiling)) {
                    int stacks = marks.TryGetValue(targetId, out var current) && current.until > Time.time ? current.stacks : 0;
                    marks[targetId] = (Mathf.Min(MaxMarks, stacks + 1), Time.time + MarkLasts);
                }
                LastTarget = __instance;
                LastTargetTime = Time.time;

                Vector3 from = local.transform.position;
                if (crit && undead && PowerSet.Has(Power.Sunstrike, out float sunstrike)) {
                    PowerFx.Effect(Power.Sunstrike, VfxRole.Proc, __instance.GetCenterPoint());
                    PowerFx.Strike(__instance, new HitData.DamageTypes() { m_spirit = raw * sunstrike / 100f }, from);
                }
                if (bow && PowerSet.Has(Power.PiercingShot, out float pierce)) { PierceBehind(__instance, hit, raw * pierce / 100f); }
                if (hit.m_damage.m_lightning > 0f && PowerSet.Has(Power.StormWeave, out float weave)) { WeaveStorm(__instance, hit.m_damage.m_lightning * weave / 100f, from); }
                if (wasCharged) {
                    PowerFx.Effect(Power.ChargedGrace, VfxRole.Proc, __instance.GetCenterPoint(), 1.2f);
                    foreach (Character near in PowerFx.EnemiesNear(__instance.transform.position, 2.5f, __instance).ToArray()) {
                        PowerFx.Strike(near, new HitData.DamageTypes() { m_blunt = raw * 0.5f }, from);
                    }
                }
                StrikePowers.AfterHit(local, __instance, hit, raw);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Incoming {
            // __state is the health before the hit, so "when hit" powers only count hits that hurt:
            // RPC_Damage also runs for dodged and fully blocked hits.
            private static void Prefix(Character __instance, HitData hit, out float __state) {
                __state = -1f;
                Player local = Player.m_localPlayer;
                if (local == null || __instance != local || hit == null) { return; }
                __state = local.GetHealth();
                Character attacker = hit.GetAttacker();

                // Hits that never land.
                if (attacker != null && attacker != local) {
                    if (PowerSet.Has(Power.Foresight)) {
                        ZDOID attackerId = attacker.GetZDOID();
                        bool seen = foresightSeen.TryGetValue(attackerId, out float last) && Time.time - last < ForesightWindow;
                        foresightSeen[attackerId] = Time.time;
                        if (!seen) {
                            Negate(hit, local);
                            PowerFx.Effect(Power.Foresight, VfxRole.Proc, local.GetCenterPoint());
                            return;
                        }
                    }
                    if (PowerSet.Has(Power.AllThings) && Time.time >= allThingsReadyAt) {
                        allThingsReadyAt = Time.time + AllThingsEvery;
                        Negate(hit, local);
                        PowerFx.Effect(Power.AllThings, VfxRole.Proc, local.GetCenterPoint());
                        return;
                    }
                }

                float multiplier = 1f + PowerSet.Get(Power.DamageTaken) / 100f;
                if (__instance.IsStaggering()) {
                    multiplier *= 1f - PowerSet.Get(Power.StaggeredDamageReduction) / 100f;
                }
                // A dodged hit never lands, so it doesn't use up the count.
                if (hit.m_ranged && !(hit.m_dodgeable && local.IsDodgeInvincible()) && PowerSet.Has(Power.ArrowCatcher, out float arrowCatcher)) {
                    projectilesTaken++;
                    if (projectilesTaken >= ArrowCatcherEvery) {
                        projectilesTaken = 0;
                        multiplier *= 1f - arrowCatcher / 100f;
                    }
                }
                multiplier *= 1f - DomeProtection(local) / 100f;
                multiplier *= StrikePowers.IncomingFactor(local, attacker, hit);
                if (multiplier != 1f) { hit.m_damage.Modify(Mathf.Max(0f, multiplier)); }

                hit.m_damage.m_frost *= 1f - PowerSet.Get(Power.FrostResist) / 100f;
                hit.m_damage.m_fire *= (1f - PowerSet.Get(Power.FireResist) / 100f) * (1f + PowerSet.Get(Power.FireDamageTaken) / 100f);
                hit.m_damage.m_pierce *= 1f + PowerSet.Get(Power.PierceDamageTaken) / 100f;
                if (PowerSet.Has(Power.PoisonImmunity)) { hit.m_damage.m_poison = 0f; }
                if (PowerSet.Has(Power.SpiritImmunity)) { hit.m_damage.m_spirit = 0f; }
                if (PowerSet.Has(Power.SealedWall, out float sealedWall) && local.IsBlocking() && BlockPowers.HoldingTowerShield(local)) {
                    float kept = 1f - sealedWall / 100f;
                    hit.m_damage.m_fire *= kept;
                    hit.m_damage.m_frost *= kept;
                    hit.m_damage.m_lightning *= kept;
                    hit.m_damage.m_poison *= kept;
                    hit.m_damage.m_spirit *= kept;
                }
            }

            private static void Postfix(Character __instance, HitData hit, float __state) {
                Player local = Player.m_localPlayer;
                if (__state < 0f || local == null || __instance != local || local.GetHealth() >= __state) { return; }
                if (hit.GetAttacker() == local) { return; }
                if (PowerSet.Has(Power.StaminaWhenHit, out float stamina)) { local.AddStamina(stamina); }
                if (PowerSet.Has(Power.EitrWhenHit, out float eitr)) { local.AddEitr(eitr); }
                StrikePowers.AfterHurt(local, hit.GetAttacker(), __state - local.GetHealth());
            }
        }

        /// <summary>The damage that actually lands, after armour: where lethal blows are caught.</summary>
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class Landing {
            private static void Prefix(Character __instance, HitData hit) {
                Player local = Player.m_localPlayer;
                if (local == null || __instance != local || hit == null) { return; }
                SurvivalPowers.OnDamageLanding(local, hit);
                // Retort: vanilla turns a hit's fire into burning, so it is the burning that lands here. A
                // dodged hit sets nothing alight, and a fully blocked one has no fire left to burn with.
                if (hit.m_damage.m_fire > 0f) { AuraPowers.OnFireTaken(); }
            }
        }

        private static void Negate(HitData hit, Player local) {
            hit.m_damage = new HitData.DamageTypes();
            hit.m_pushForce = 0f;
            hit.m_staggerMultiplier = 0f;
            hit.m_statusEffectHash = 0;
            DamageText.instance.ShowText(DamageText.TextType.Immune, local.GetCenterPoint() + Vector3.up * 0.3f, 0f);
        }

        // The strongest rune dome over the local player, from any player (themselves included) blocking within range.
        private static float DomeProtection(Player local) {
            float best = 0f;
            List<Player> players = new List<Player>();
            Player.GetPlayersInRange(local.transform.position, DomeRange, players);
            foreach (Player player in players) {
                float dome = player.m_nview?.GetZDO()?.GetFloat(AllyPowers.DomeKey, 0f) ?? 0f;
                best = Mathf.Max(best, dome);
            }
            return best;
        }

        private static float Kept(Power oath) {
            return Mathf.Max(MinimumDamageKept, 1f - PowerSet.Get(oath) / 100f);
        }

        // Storms add part of the hit again: frost in snowstorms, lightning in any other storm.
        private static void AddStormDamage(HitData hit, float percent) {
            string environment = EnvMan.instance?.GetCurrentEnvironment()?.m_name;
            if (string.IsNullOrEmpty(environment) || environment.IndexOf("storm", StringComparison.OrdinalIgnoreCase) < 0) { return; }
            float extra = hit.m_damage.GetTotalDamageNoHarvestValues() * percent / 100f;
            if (environment.IndexOf("snow", StringComparison.OrdinalIgnoreCase) >= 0) {
                hit.m_damage.m_frost += extra;
            } else {
                hit.m_damage.m_lightning += extra;
            }
        }

        // Piercing Shot: the nearest enemy just beyond the target, along the arrow's flight, takes part of the hit.
        private static void PierceBehind(Character target, HitData hit, float damage) {
            Vector3 dir = hit.m_dir.sqrMagnitude > 0.01f ? hit.m_dir.normalized : target.transform.forward;
            Character best = null;
            float bestDistance = float.MaxValue;
            foreach (Character other in PowerFx.EnemiesNear(target.transform.position, 4f, target)) {
                Vector3 offset = other.transform.position - target.transform.position;
                if (Vector3.Dot(offset, dir) <= 0f) { continue; }
                if (offset.magnitude < bestDistance) {
                    bestDistance = offset.magnitude;
                    best = other;
                }
            }
            if (best != null) { PowerFx.Strike(best, new HitData.DamageTypes() { m_pierce = damage }, target.transform.position); }
        }

        // Storm-weave: in rain, or into enemies standing in water, lightning arcs on to others nearby.
        private static void WeaveStorm(Character target, float damage, Vector3 from) {
            bool wet = EnvMan.IsWet() || target.InWater();
            if (!wet) { return; }
            foreach (Character other in PowerFx.EnemiesNear(target.transform.position, 6f, target).ToArray()) {
                if (!EnvMan.IsWet() && !other.InWater()) { continue; }
                PowerFx.Strike(other, new HitData.DamageTypes() { m_lightning = damage }, target.transform.position);
            }
        }
    }
}
