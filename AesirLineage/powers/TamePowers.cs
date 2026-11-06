using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Powers for tamed creatures: Gefjun's herd (taming, breeding, feeding, carts) and Skaði's pack
    /// (companions that hit harder, take less and come back), plus the wild creatures Call of the Hunt
    /// raises and the raiders the Barred Hall turns. Most of it runs on whichever machine owns the
    /// creature, which reads what players nearby publish on their ZDOs (<see cref="AllyPowers"/>).
    /// </summary>
    internal static class TamePowers {
        private const float HerdRange = 30f;
        private const float GuardRange = 20f;
        private const float FourOxenRange = 20f;
        private const int FourOxenMax = 4;
        private const float FourOxenGuardPerTame = 4f;
        private const float PackTargetSeconds = 8f;
        private const float LickEvery = 1f;
        // Lick Wounds and the like wait until no enemy is this close.
        private const float CalmRange = 10f;
        private const float SparedSeconds = 10f;
        private const float SparedCooldown = 60f;
        private const string SparedUntilKey = "al_spared_until";
        private const float HarnessRange = 8f;
        // Each creature this machine owns is looked at this often, not every frame.
        private const float AiCheckEvery = 2f;
        private const string HuntedKey = "al_hunted";
        private const float HuntRange = 30f;
        private const int HuntMaxLevel = 10;
        private const float BarredRange = 30f;
        private const float VowRange = 10f;

        private static int tamesNear;
        private static float nextCount;
        private static float nextLick;
        private static readonly Dictionary<int, float> nextAiCheck = new Dictionary<int, float>();

        internal static void Reset() {
            tamesNear = 0;
            nextAiCheck.Clear();
        }

        /// <summary>The Four Oxen: carry weight for each tame close by, up to four.</summary>
        internal static float FourOxenCarry() => PowerSet.Has(Power.FourOxen, out float carry) ? carry * Mathf.Min(FourOxenMax, tamesNear) : 0f;

        /// <summary>The Four Oxen: less damage taken for each tame close by, in percent.</summary>
        internal static float FourOxenGuard() => PowerSet.Has(Power.FourOxen) ? FourOxenGuardPerTame * Mathf.Min(FourOxenMax, tamesNear) : 0f;

        internal static void Tick(Player player) {
            if (Time.time >= nextCount) {
                nextCount = Time.time + 1f;
                tamesNear = PowerSet.Has(Power.FourOxen) ? AllyPowers.TamesNear(player.transform.position, FourOxenRange).Count : 0;
            }
            if (PowerSet.Has(Power.LickWounds, out float lick) && Time.time >= nextLick) {
                nextLick = Time.time + LickEvery;
                if (PowerFx.EnemiesNear(player.transform.position, CalmRange).Count == 0) {
                    foreach (Character tame in AllyPowers.MyTamesNear(player.transform.position, HerdRange)) {
                        if (tame.GetHealth() < tame.GetMaxHealth()) { tame.Heal(tame.GetMaxHealth() * lick / 100f, false); }
                    }
                }
            }
        }

        /// <summary>Pack Tactics: the local player's companions go for what they hit.</summary>
        internal static void OnLocalHit(Player player, Character target) {
            if (!PowerSet.Has(Power.PackTactics) || ZNet.instance == null) { return; }
            long until = ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(PackTargetSeconds).Ticks;
            AllyPowers.PublishTarget(player, AllyPowers.PackTargetKey, AllyPowers.PackTargetTimeKey, target.GetZDOID(), until);
        }

        /// <summary>Pack Tactics: more damage to an enemy one of the local player's companions is close to.</summary>
        internal static float PackTacticsBonus(Character target) {
            if (!PowerSet.Has(Power.PackTactics, out float bonus)) { return 0f; }
            return AllyPowers.MyTamesNear(target.transform.position, 3f).Count > 0 ? bonus : 0f;
        }

        // ---- Taming and breeding, on the creature's owner ----

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.DecreaseRemainingTime))]
        private static class TamingSpeed {
            private static void Prefix(Tameable __instance, ref float time) {
                float bonus = AllyPowers.Strongest(__instance.transform.position, HerdRange, AllyPowers.TamingKey);
                if (bonus > 0f) { time *= 1f + bonus / 100f; }
            }
        }

        // Never Hungry: tames near a herder keep fed. Creatures still being tamed must eat as usual.
        [HarmonyPatch(typeof(Tameable), nameof(Tameable.IsHungry))]
        private static class Fed {
            private static void Postfix(Tameable __instance, ref bool __result) {
                if (__result && __instance.IsTamed() && AllyPowers.AnyNear(__instance.transform.position, HerdRange, AllyPowers.HerdFedKey)) {
                    __result = false;
                }
            }
        }

        private struct BreedingState {
            public bool Changed;
            public float Chance;
            public float Duration;
        }

        [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
        private static class Breeding {
            private static void Prefix(Procreation __instance, out BreedingState __state) {
                __state = default;
                float bonus = AllyPowers.Strongest(__instance.transform.position, HerdRange, AllyPowers.FruitfulKey);
                if (bonus <= 0f) { return; }
                __state = new BreedingState() { Changed = true, Chance = __instance.m_pregnancyChance, Duration = __instance.m_pregnancyDuration };
                // Despite its name, vanilla's m_pregnancyChance is the chance a try is skipped.
                __instance.m_pregnancyChance /= 1f + bonus / 100f;
                __instance.m_pregnancyDuration /= 1f + bonus / 100f;
            }

            private static void Finalizer(Procreation __instance, BreedingState __state) {
                if (!__state.Changed) { return; }
                __instance.m_pregnancyChance = __state.Chance;
                __instance.m_pregnancyDuration = __state.Duration;
            }
        }

        [HarmonyPatch(typeof(Growup), nameof(Growup.GrowUpdate))]
        private static class GrowingUp {
            private static void Prefix(Growup __instance, out float __state) {
                __state = -1f;
                float bonus = AllyPowers.Strongest(__instance.transform.position, HerdRange, AllyPowers.FruitfulKey);
                if (bonus <= 0f) { return; }
                __state = __instance.m_growTime;
                __instance.m_growTime /= 1f + bonus / 100f;
            }

            private static void Finalizer(Growup __instance, float __state) {
                if (__state >= 0f) { __instance.m_growTime = __state; }
            }
        }

        // ---- Companions in a fight, on the creature's owner ----

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class TameHits {
            private static void Prefix(Character __instance, HitData hit) {
                Character attacker = hit?.GetAttacker();
                if (PowerFx.DealingSecondary || attacker == null || attacker.IsPlayer() || !attacker.IsTamed()) { return; }
                if (attacker.m_nview == null || !attacker.m_nview.IsOwner() || __instance == attacker) { return; }
                float multiplier = 1f;
                Player followed = AllyPowers.FollowedPlayer(attacker);
                ZDO zdo = followed?.m_nview?.GetZDO();
                if (zdo != null) {
                    multiplier *= 1f + zdo.GetFloat(AllyPowers.PackDamageKey, 0f) / 100f;
                    if (SummonPowers.IsSummon(attacker)) { multiplier *= 1f + zdo.GetFloat(AllyPowers.SummonDamageKey, 0f) / 100f; }
                }
                multiplier *= 1f + AllyPowers.Strongest(attacker.transform.position, VowRange, AllyPowers.VowKey) / 100f;
                if (multiplier != 1f) { hit.m_damage.Modify(multiplier); }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class TameGuard {
            private static void Prefix(Character __instance, HitData hit) {
                if (hit == null || __instance.IsPlayer() || !__instance.IsTamed() || __instance.m_nview == null || !__instance.m_nview.IsOwner()) { return; }
                if (IsSpared(__instance)) {
                    hit.m_damage = new HitData.DamageTypes();
                    return;
                }
                Vector3 at = __instance.transform.position;
                float factor = 1f - AllyPowers.Strongest(at, GuardRange, AllyPowers.TameGuardKey) / 100f;
                Player followed = AllyPowers.FollowedPlayer(__instance);
                if (followed != null) { factor *= 1f - (followed.m_nview?.GetZDO()?.GetFloat(AllyPowers.PackGuardKey, 0f) ?? 0f) / 100f; }
                factor *= 1f - AllyPowers.Strongest(at, LightPowers.HavenRange, AllyPowers.HavenKey) / 100f;
                // Shared Burden: the bonded tame's hurt is partly the local player's to carry.
                if (followed == Player.m_localPlayer) { factor *= BondPowers.ShareTameBurden(__instance, hit.GetTotalDamage() * factor); }
                if (factor < 1f) { hit.m_damage.Modify(Mathf.Max(0f, factor)); }
            }
        }

        // Endless Pack: a following tame's killing blow leaves it standing, shielded while it recovers.
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class Spared {
            private static void Prefix(Character __instance, HitData hit) {
                if (hit == null || __instance.IsPlayer() || !__instance.IsTamed() || __instance.m_nview == null || !__instance.m_nview.IsOwner()) { return; }
                // After this, vanilla scales a creature's damage by difficulty and the world's damage rate.
                float scale = Game.instance.GetDifficultyDamageScaleEnemy(__instance.transform.position) * Game.m_playerDamageRate;
                float total = hit.GetTotalDamage() * scale;
                float health = __instance.GetHealth();
                if (total < health || total <= 0f) { return; }
                Player followed = AllyPowers.FollowedPlayer(__instance);
                if (followed?.m_nview?.GetZDO()?.GetBool(AllyPowers.EndlessKey, false) != true) { return; }
                ZDO zdo = __instance.m_nview.GetZDO();
                long now = ZNet.instance.GetTime().Ticks;
                long readyAt = zdo.GetLong(SparedUntilKey, 0L) + System.TimeSpan.FromSeconds(SparedCooldown - SparedSeconds).Ticks;
                if (now < readyAt) { return; }
                hit.m_damage.Modify(Mathf.Max(0f, health - 1f) / total);
                zdo.Set(SparedUntilKey, now + System.TimeSpan.FromSeconds(SparedSeconds).Ticks);
                DeityVfx.Share(PowerFx.DeityOf(Power.EndlessPack), VfxRole.Proc, __instance.transform.position);
            }
        }

        private static bool IsSpared(Character tame) {
            long until = tame.m_nview?.GetZDO()?.GetLong(SparedUntilKey, 0L) ?? 0L;
            return until > 0L && ZNet.instance != null && ZNet.instance.GetTime().Ticks < until;
        }

        // ---- What creatures this machine owns go after ----

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
        private static class Behaviour {
            private static void Postfix(MonsterAI __instance) {
                Character character = __instance.m_character;
                ZNetView nview = __instance.m_nview;
                if (character == null || nview == null || !nview.IsOwner() || character.IsDead() || ZNet.instance == null) { return; }
                int id = character.GetInstanceID();
                if (nextAiCheck.TryGetValue(id, out float next) && Time.time < next) { return; }
                if (nextAiCheck.Count > 4000) { nextAiCheck.Clear(); }
                nextAiCheck[id] = Time.time + AiCheckEvery;
                if (character.IsTamed()) {
                    GoForPackTarget(__instance, character);
                } else {
                    CallOfTheHunt(character);
                    RaidersComeForYou(__instance, character);
                }
            }
        }

        private static void GoForPackTarget(MonsterAI ai, Character tame) {
            ZDO zdo = AllyPowers.FollowedPlayer(tame)?.m_nview?.GetZDO();
            if (zdo == null || ZNet.instance.GetTime().Ticks > zdo.GetLong(AllyPowers.PackTargetTimeKey, 0L)) { return; }
            Character target = ZNetScene.instance?.FindInstance(zdo.GetZDOID(AllyPowers.PackTargetKey))?.GetComponent<Character>();
            if (target == null || target.IsDead() || target.IsTamed() || target == ai.m_targetCreature) { return; }
            ai.m_targetCreature = target;
            ai.m_targetStatic = null;
        }

        // Call of the Hunt: a wild creature that comes near a huntress gains a star, once.
        private static void CallOfTheHunt(Character creature) {
            if (creature.IsBoss() || creature.GetLevel() >= HuntMaxLevel) { return; }
            ZDO zdo = creature.m_nview.GetZDO();
            if (zdo.GetBool(HuntedKey, false) || !AllyPowers.AnyNear(creature.transform.position, HuntRange, AllyPowers.HuntKey)) { return; }
            zdo.Set(HuntedKey, true);
            creature.SetLevel(creature.GetLevel() + 1);
            DeityVfx.Share(PowerFx.DeityOf(Power.CallOfTheHunt), VfxRole.Proc, creature.transform.position, 0.8f);
        }

        // The Barred Hall: raiders turn from the buildings to the warden.
        private static void RaidersComeForYou(MonsterAI ai, Character raider) {
            if (!ai.IsEventCreature() || ai.m_targetStatic == null) { return; }
            List<Player> players = new List<Player>();
            Player.GetPlayersInRange(raider.transform.position, BarredRange, players);
            foreach (Player player in players) {
                if (player == null || player.IsDead() || player.m_nview?.GetZDO()?.GetBool(AllyPowers.BarredKey, false) != true) { continue; }
                ai.m_targetStatic = null;
                ai.m_targetCreature = player;
                return;
            }
        }

        // ---- Carts ----

        // Draught Ox: carts the local player or one of their tames pulls weigh less.
        [HarmonyPatch(typeof(Vagon), nameof(Vagon.SetMass))]
        private static class LighterCart {
            private static void Prefix(Vagon __instance, ref float mass) {
                Player local = Player.m_localPlayer;
                if (local == null || !PowerSet.Has(Power.DraughtOx, out float lighter) || __instance.m_attachedObject == null) { return; }
                Character puller = __instance.m_attachedObject.GetComponent<Character>();
                if (puller == local || (puller != null && AllyPowers.FollowedPlayer(puller) == local)) {
                    mass *= 1f - lighter / 100f;
                }
            }
        }

        // Draught Ox: crouching as you take a cart's handle harnesses a following tame to it instead.
        [HarmonyPatch(typeof(Vagon), nameof(Vagon.Interact))]
        private static class Harness {
            private static void Prefix(Vagon __instance, ref Humanoid character, bool hold) {
                if (hold || character != Player.m_localPlayer || !character.IsCrouching() || __instance.IsAttached() || !PowerSet.Has(Power.DraughtOx)) { return; }
                Humanoid beast = NearestFollowingBeast(__instance.m_attachPoint.position);
                if (beast == null) { return; }
                // Brought to the handle, where a player would stand to take it.
                Vector3 at = __instance.m_attachPoint.position - __instance.m_attachOffset;
                beast.transform.position = at;
                if (beast.m_body != null) { beast.m_body.position = at; }
                character = beast;
            }
        }

        private static Humanoid NearestFollowingBeast(Vector3 at) {
            Humanoid best = null;
            float bestDistance = HarnessRange;
            foreach (Character tame in AllyPowers.MyTamesNear(at, HarnessRange)) {
                if (!(tame is Humanoid beast) || tame.m_nview == null || !tame.m_nview.IsOwner() || SummonPowers.IsSummon(tame)) { continue; }
                float distance = Vector3.Distance(at, tame.transform.position);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = beast;
                }
            }
            return best;
        }
    }
}
