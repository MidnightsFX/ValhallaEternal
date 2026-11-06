using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// The second three arms' share of every hit, called from <see cref="DamagePowers"/>: what changes a
    /// hit before it is sent (sneak attacks, weaknesses, poison, polearm reach), the factors on damage
    /// dealt and taken, and what a hit, a hurt or a kill sets off. Vör's Weak Points and Vows, Syn's
    /// Polearm and Denial, Hel's Rot, Host and Dread, and the load, coin and bond factors live here.
    /// Víðarr's arms keep their own classes (<see cref="ShoePowers"/>, <see cref="WinterPowers"/>,
    /// <see cref="WolfPowers"/>, <see cref="SilencePowers"/>, <see cref="AfterPowers"/>) and are called from here.
    /// </summary>
    internal static class StrikePowers {
        private const int MaxOaths = 6;
        private const float GrudgeSeconds = 20f;
        private const float RefuseSeconds = 5f;
        private const int MaxBreaks = 5;
        private const float BreakLasts = 10f;
        private const int VerdictEvery = 5;
        private const float VerdictSeconds = 10f;
        private const float QuietSeconds = 1f;
        private const float DreadSeconds = 3f;
        private const float GraspRootSeconds = 3f;
        private const float GraspBurstShare = 0.15f;
        private const float BurstRadius = 4f;
        private const float BurstHealthCap = 1000f;
        private const float RotRadius = 5f;
        private const float RotGuard = 0.1f;
        private const float GazeRadius = 8f;
        private const float WitnessRange = 10f;
        private const float BurdenRange = 10f;
        private const float RaiderGuard = 0.25f;
        private const float ReflectShare = 0.5f;
        private const int MaxTerror = 10;
        private const float TerrorSeconds = 10f;
        private const int MaxSouls = 20;
        private const float SoulSeconds = 10f;
        private const float SoulRange = 15f;
        private const float SoulHeal = 0.01f;
        private const float ChargeRange = 3f;
        private const float ChargeCooldown = 10f;
        private const float StillSpeed = 0.1f;
        private const float LoadPerStep = 25f;
        private const float MaxLoadBonus = 20f;
        // Ox-strength: stamina a second for each kilo over the limit.
        private const float OverCarryStamina = 0.1f;

        private static int oathCount;
        private static int oathVersion = -1;
        private static float quietUntil;
        private static float nextCharge;
        private static float chargeReadyAt;
        private static float nextRot;
        private static float soulsUntil;
        private static int souls;
        private static readonly List<float> terror = new List<float>();
        private static readonly Dictionary<ZDOID, float> grudges = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<ZDOID, float> refused = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<ZDOID, (int stacks, float until)> breaks = new Dictionary<ZDOID, (int, float)>();
        private static readonly Dictionary<ZDOID, int> verdictHits = new Dictionary<ZDOID, int>();
        private static readonly Dictionary<ZDOID, float> judged = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<ZDOID, float> grasped = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<int, float> lastDistance = new Dictionary<int, float>();

        internal static void Reset() {
            oathVersion = -1;
            quietUntil = 0f;
            chargeReadyAt = 0f;
            soulsUntil = 0f;
            souls = 0;
            terror.Clear();
            grudges.Clear();
            refused.Clear();
            breaks.Clear();
            verdictHits.Clear();
            judged.Clear();
            grasped.Clear();
            lastDistance.Clear();
        }

        // ---- Conditions ----

        /// <summary>Night, the Mistlands' gloom, or underground: where Hel's half-dark is at home.</summary>
        internal static bool InDark(Player player) => EnvMan.IsNight() || player.m_currentBiome == Heightmap.Biome.Mistlands || player.InInterior();

        private static bool Unaware(Character target) {
            BaseAI ai = target.GetBaseAI();
            return ai != null && !ai.IsAlerted();
        }

        private static bool Standing(Player player) => player.GetVelocity().magnitude < StillSpeed && player.IsOnGround();

        private static bool Expired(Dictionary<ZDOID, float> marks, ZDOID id) => !marks.TryGetValue(id, out float until) || Time.time > until;

        private static void Trim<T>(Dictionary<ZDOID, T> marks) {
            if (marks.Count > 512) { marks.Clear(); }
        }

        // ---- Vows ----

        /// <summary>How many of the local player's stars carry an oath.</summary>
        internal static int OathCount() {
            if (oathVersion == PowerSet.Version) { return oathCount; }
            HashSet<StarNode> sworn = new HashSet<StarNode>();
            foreach (PowerInfo info in PowerInfo.All) {
                if (!info.IsOath) { continue; }
                foreach (StarNode node in PowerSet.SourcesOf(info.Id)) { sworn.Add(node); }
            }
            oathCount = sworn.Count;
            oathVersion = PowerSet.Version;
            return oathCount;
        }

        /// <summary>Keeper of Vows: the damage and experience bonus from the oaths carried, in percent.</summary>
        internal static float VowBonus() => PowerSet.Has(Power.VowKeeper, out float perOath) ? Mathf.Min(MaxOaths, OathCount()) * perOath : 0f;

        /// <summary>Vár's Witness: the bonus another player's oaths lend the local player.</summary>
        internal static float WitnessBonus(Player player) {
            float best = 0f;
            foreach (Player ally in PowerFx.AlliesNear(player.transform.position, WitnessRange)) {
                best = Mathf.Max(best, ally.m_nview?.GetZDO()?.GetFloat(AllyPowers.VowKey, 0f) ?? 0f);
            }
            return best;
        }

        // ---- Terror and Soul Eater stacks ----

        internal static int TerrorStacks {
            get {
                terror.RemoveAll(until => Time.time > until);
                return terror.Count;
            }
        }

        private static int SoulStacks => Time.time < soulsUntil ? souls : 0;

        // Soul Eater: a body falling near the local player, whoever felled it.
        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Awake))]
        private static class Souls {
            private static void Postfix(Ragdoll __instance) {
                Player local = Player.m_localPlayer;
                if (local == null || local.IsDead() || !PowerSet.Has(Power.SoulEater) || __instance.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0) { return; }
                if (Vector3.Distance(__instance.transform.position, local.transform.position) > SoulRange) { return; }
                souls = Mathf.Min(MaxSouls, SoulStacks + 1);
                soulsUntil = Time.time + SoulSeconds;
                local.Heal(local.GetMaxHealth() * SoulHeal, false);
                PowerFx.Show(local, PowerFx.SoulEater, SoulSeconds);
            }
        }

        // ---- Changes to the local player's hit before it goes ----

        /// <summary>The hit being prepared comes from a heavy attack (see <see cref="HeavyAttack"/>).</summary>
        private static bool heavy;
        /// <summary>The hit's target was already staggered when it landed.</summary>
        private static bool wasStaggered;

        /// <summary>
        /// A secondary attack, or the last blow of a combo: what Víðarr's stars call a heavy attack. Vanilla
        /// leaves the last attack's state behind when it ends, so only a hit during an attack's animation
        /// counts; a shield's charged burst, for one, has none.
        /// </summary>
        private static bool HeavyAttack(Player player) {
            Attack attack = player.m_currentAttack;
            if (attack == null || !player.InAttack()) { return false; }
            return player.m_currentAttackIsSecondary || (attack.m_attackChainLevels > 1 && attack.m_currentAttackCainLevel == attack.m_attackChainLevels - 1);
        }

        internal static void PrepareHit(Player player, Character target, HitData hit, ref bool forceCrit) {
            heavy = !hit.m_ranged && HeavyAttack(player);
            wasStaggered = target.IsStaggering();
            ShoePowers.PrepareHit(player, hit);
            SilencePowers.PrepareHit(hit);
            bool unaware = Unaware(target);
            if (hit.m_backstabBonus > 1f) {
                hit.m_backstabBonus *= 1f + PowerSet.Get(Power.SneakAttack) / 100f;
                if (unaware && PowerSet.Has(Power.FirstCut)) { forceCrit = true; }
                // Read the Fight: a staggered enemy takes the sneak attack bonus, unless vanilla will apply it anyway.
                if (!unaware && PowerSet.Has(Power.ReadTheFight) && target.IsStaggering()) {
                    hit.m_damage.Modify(hit.m_backstabBonus);
                    hit.m_backstabBonus = 1f;
                }
            }
            if (unaware && PowerSet.Has(Power.QuietKill)) { quietUntil = Time.time + QuietSeconds; }
            if (PowerSet.Has(Power.Weakness, out float weakness)) { ExploitWeakness(target, hit, 1f + weakness / 100f); }
            if (PowerSet.Has(Power.Torchbearer, out float torch) && LightPowers.HoldingTorch(player) && hit.m_skill == Skills.SkillType.Clubs) {
                hit.m_damage.m_fire *= 1f + torch / 100f;
            }
            if (PowerSet.Has(Power.Interrupt, out float interrupt) && target.InAttack()) { hit.m_staggerMultiplier *= 1f + interrupt / 100f; }
            if (hit.m_skill == Skills.SkillType.Polearms) {
                hit.m_pushForce *= 1f + PowerSet.Get(Power.PolearmDamage) / 60f;
                if (PowerSet.Has(Power.HoldTheGate) && Standing(player)) { hit.m_pushForce *= 2f; }
            }
            hit.m_damage.m_poison = (hit.m_damage.m_poison + PowerSet.Get(Power.PoisonOnHit)) * (1f + PowerSet.Get(Power.PoisonDamage) / 100f);
        }

        // Know Thy Foe: each damage type the creature is weak to does more.
        private static void ExploitWeakness(Character target, HitData hit, float factor) {
            HitData.DamageModifiers mods = target.GetDamageModifiers();
            ref HitData.DamageTypes d = ref hit.m_damage;
            if (Weak(mods.m_blunt)) { d.m_blunt *= factor; }
            if (Weak(mods.m_slash)) { d.m_slash *= factor; }
            if (Weak(mods.m_pierce)) { d.m_pierce *= factor; }
            if (Weak(mods.m_fire)) { d.m_fire *= factor; }
            if (Weak(mods.m_frost)) { d.m_frost *= factor; }
            if (Weak(mods.m_lightning)) { d.m_lightning *= factor; }
            if (Weak(mods.m_poison)) { d.m_poison *= factor; }
            if (Weak(mods.m_spirit)) { d.m_spirit *= factor; }
        }

        private static bool Weak(HitData.DamageModifier mod) {
            return mod == HitData.DamageModifier.Weak || mod == HitData.DamageModifier.VeryWeak || mod == HitData.DamageModifier.SlightlyWeak;
        }

        // ---- Factors on damage dealt and taken ----

        internal static float OutgoingFactor(Player player, Character target, HitData hit) {
            ZDOID id = target.GetZDOID();
            float factor = 1f;
            if (!Unaware(target)) { factor *= Mathf.Max(0.2f, 1f - PowerSet.Get(Power.AlertedDamagePenalty) / 100f); }
            factor *= 1f + (VowBonus() + WitnessBonus(player)) / 100f;
            if (!Expired(grudges, id)) { factor *= 1f + PowerSet.Get(Power.Grudge) / 100f; }
            factor *= 1f + HomePowers.HearthBonus(player) / 100f;
            factor *= 1f + HomePowers.RaidBonus() / 100f;
            factor *= HomePowers.BanFactor(player, target);
            if (LightPowers.InSunlight(player)) { factor *= 1f + PowerSet.Get(Power.DaylightDamage) / 100f; }
            if (InDark(player)) { factor *= 1f + PowerSet.Get(Power.NightDamage) / 100f; }
            if (EnvMan.IsNight()) {
                factor *= 1f + PowerSet.Get(Power.ChildOfNight) / 100f;
            } else {
                factor *= Mathf.Max(0.2f, 1f - PowerSet.Get(Power.DayDamagePenalty) / 100f);
            }
            factor *= 1f + TerrorStacks * PowerSet.Get(Power.Terror) / 100f;
            factor *= 1f + SoulStacks * PowerSet.Get(Power.SoulEater) / 100f;
            if (PowerSet.Has(Power.LoadStrength, out float load)) {
                factor *= 1f + Mathf.Min(MaxLoadBonus, Mathf.Floor(player.GetInventory().GetTotalWeight() / LoadPerStep) * load) / 100f;
            }
            factor *= 1f + TradePowers.GoldHungerBonus(player) / 100f;
            factor *= 1f + MeadPowers.ElixirBonus() / 100f;
            factor *= BondPowers.DamageFactor(player);
            factor *= 1f + HuntPowers.TrophyLoreBonus(player, target) / 100f;
            factor *= 1f + TamePowers.PackTacticsBonus(target) / 100f;
            factor *= CraftPowers.WeaponFactor(player);
            if (hit.m_skill == Skills.SkillType.Polearms) {
                factor *= (1f + PowerSet.Get(Power.PolearmDamage) / 100f) * (1f + PowerSet.Get(Power.WallOfSpears) / 100f);
            }
            if (hit.m_skill == Skills.SkillType.Spears) { factor *= 1f + PowerSet.Get(Power.SpearDamage) / 100f; }
            if (PowerSet.Has(Power.ArmourBreak, out float perBreak) && breaks.TryGetValue(id, out var broken) && Time.time < broken.until) {
                factor *= 1f + broken.stacks * perBreak / 100f;
            }
            if (!Expired(judged, id)) { factor *= 1f + PowerSet.Get(Power.Verdict) / 100f; }
            factor *= ShoePowers.OutgoingFactor(player, hit);
            factor *= WinterPowers.OutgoingFactor(player);
            factor *= WolfPowers.OutgoingFactor(player, target);
            factor *= SilencePowers.OutgoingFactor(target);
            return factor;
        }

        internal static float IncomingFactor(Player player, Character attacker, HitData hit) {
            float factor = 1f - TamePowers.FourOxenGuard() / 100f;
            factor *= 1f - HomePowers.RaidBonus() / 100f;
            factor *= 1f - LightPowers.HavenFor(player) / 100f;
            factor *= BondPowers.GuardFactor(player);
            if (EnvMan.IsNight()) { factor *= 1f + PowerSet.Get(Power.NightDamageTaken) / 100f; }
            factor *= 1f + TerrorStacks * PowerSet.Get(Power.Terror) / 200f;
            factor *= WinterPowers.IncomingFactor() * AfterPowers.IncomingFactor(player);
            if (attacker == null) { return factor; }
            float distance = Vector3.Distance(attacker.transform.position, player.transform.position);
            if (!Expired(refused, attacker.GetZDOID())) { factor *= 1f - PowerSet.Get(Power.Refuse) / 100f; }
            if (EnvMan.IsNight() && distance < GazeRadius) { factor *= 1f - PowerSet.Get(Power.HelsGaze) / 100f; }
            if (PowerSet.Has(Power.RottingGround) && distance < RotRadius) { factor *= 1f - RotGuard; }
            if (PowerSet.Has(Power.BarredHall) && attacker.GetBaseAI() is MonsterAI ai && ai.IsEventCreature()) { factor *= 1f - RaiderGuard; }
            return factor;
        }

        /// <summary>Thrall's Burden and Shared Burden: parts of a hit moved onto a summon, or onto a bonded player.</summary>
        internal static float MoveBurdens(Player player, HitData hit) {
            if (hit.m_hitType == HitData.HitType.Self) { return 1f; }
            float total = hit.GetTotalDamage();
            if (total <= 0f) { return 1f; }
            float factor = 1f;
            if (PowerSet.Has(Power.ThrallsBurden, out float share)) {
                Character nearest = null;
                float best = BurdenRange;
                foreach (Character tame in AllyPowers.MyTamesNear(player.transform.position, BurdenRange)) {
                    float distance = Vector3.Distance(tame.transform.position, player.transform.position);
                    if (distance < best) { best = distance; nearest = tame; }
                }
                if (nearest != null) {
                    HitData moved = new HitData() { m_hitType = HitData.HitType.Self, m_point = nearest.GetCenterPoint() };
                    moved.m_damage.m_damage = total * share / 100f;
                    nearest.Damage(moved);
                    factor *= 1f - share / 100f;
                }
            }
            factor *= BondPowers.ShareBurden(player, total * factor);
            return factor;
        }

        // ---- What a hit, a hurt or a kill sets off ----

        internal static void AfterHit(Player player, Character target, HitData hit, float raw) {
            ZDOID id = target.GetZDOID();
            TamePowers.OnLocalHit(player, target);
            LightPowers.OnLocalHit(player, hit.GetTotalDamage());
            float leech = BondPowers.LongingLeech();
            if (leech > 0f) { player.Heal(hit.GetTotalDamage() * leech / 100f, false); }
            if (PowerSet.Has(Power.PoisonDrain, out float drain) && hit.m_damage.m_poison > 0f) { player.Heal(hit.m_damage.m_poison * drain / 100f, false); }
            if (PowerSet.Has(Power.Refuse)) {
                Trim(refused);
                refused[id] = Time.time + RefuseSeconds;
            }
            if (PowerSet.Has(Power.ArmourBreak) && hit.m_damage.m_blunt > 0f) { Break(target, id, hit); }
            if (PowerSet.Has(Power.Verdict)) {
                Trim(verdictHits);
                int hits = (verdictHits.TryGetValue(id, out int known) ? known : 0) + 1;
                verdictHits[id] = hits;
                if (hits % VerdictEvery == 0) {
                    Trim(judged);
                    judged[id] = Time.time + VerdictSeconds;
                    PowerFx.Effect(Power.Verdict, VfxRole.Proc, target.GetCenterPoint());
                }
            }
            if (PowerSet.Has(Power.GraspOfTheDead, out float grasp) && Random.value * 100f < grasp) {
                Trim(grasped);
                grasped[id] = Time.time + GraspRootSeconds;
                target.GetSEMan().AddStatusEffect(PowerFx.Hash(PowerFx.Rooted), resetTime: true);
            }
            if (PowerSet.Has(Power.Dread, out float dread) && !target.IsBoss() && Random.value * 100f < dread && ZNet.instance != null) {
                long until = ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(DreadSeconds).Ticks;
                AllyPowers.PublishTarget(player, AllyPowers.DreadKey, AllyPowers.DreadUntilKey, id, until);
                PowerFx.Effect(Power.Dread, VfxRole.Proc, target.GetCenterPoint(), 0.6f);
            }
            if (hit.m_skill == Skills.SkillType.Polearms) {
                if (PowerSet.Has(Power.SweepFortify)) { SurvivalPowers.Fortify(1); }
                if (PowerSet.Has(Power.HoldTheGate) && Standing(player)) {
                    target.GetSEMan().AddStatusEffect(PowerFx.Hash(PowerFx.Slowed), resetTime: true);
                }
            }
            if (hit.m_skill == Skills.SkillType.Spears && hit.m_ranged) { HuntPowers.OnThrownSpearHit(target, hit, raw); }
            // Last, so the leeches above don't count them, but still before the hit is sent: the debt
            // and the torn jaws go in after the damage factors.
            WolfPowers.FinishHit(player, target, hit, heavy, wasStaggered);
            ShoePowers.AfterHit(target, hit, heavy, wasStaggered);
            SilencePowers.AfterHit(target, hit);
        }

        // Armour Break: blunt hits pile up; a full break staggers and starts again.
        private static void Break(Character target, ZDOID id, HitData hit) {
            Trim(breaks);
            int stacks = breaks.TryGetValue(id, out var known) && Time.time < known.until ? known.stacks : 0;
            stacks++;
            if (stacks >= MaxBreaks) {
                target.Stagger(hit.m_dir);
                PowerFx.Effect(Power.ArmourBreak, VfxRole.Proc, target.GetCenterPoint(), 0.8f);
                stacks = 0;
            }
            breaks[id] = (stacks, Time.time + BreakLasts);
        }

        /// <summary>After a hit landed on the local player and hurt them.</summary>
        internal static void AfterHurt(Player player, Character attacker, float taken) {
            if (attacker == null || attacker == player || taken <= 0f) { return; }
            WolfPowers.AfterHurt(player, taken);
            if (PowerSet.Has(Power.Grudge)) {
                Trim(grudges);
                grudges[attacker.GetZDOID()] = Time.time + GrudgeSeconds;
            }
            if (PowerSet.Has(Power.Reflect, out float chance) && Random.value * 100f < chance) {
                PowerFx.Effect(Power.Reflect, VfxRole.Proc, attacker.GetCenterPoint(), 0.6f);
                PowerFx.Strike(attacker, new HitData.DamageTypes() { m_damage = taken * ReflectShare }, player.transform.position);
            }
        }

        /// <summary>A kill by the local player, wherever the creature was owned: Terror of Niflhel.</summary>
        internal static void OnAnyKill(Player player) {
            if (!PowerSet.Has(Power.Terror) || !EnvMan.IsNight()) { return; }
            terror.RemoveAll(until => Time.time > until);
            if (terror.Count >= MaxTerror) { terror.RemoveAt(0); }
            terror.Add(Time.time + TerrorSeconds);
            PowerFx.Show(player, PowerFx.Terror, TerrorSeconds);
        }

        /// <summary>A kill of a creature this machine owns: the bursts that need its body.</summary>
        internal static void OnLocalKill(Player player, Character victim) {
            float health = Mathf.Min(victim.GetMaxHealth(), BurstHealthCap);
            ZDOID id = victim.GetZDOID();
            if (PowerSet.Has(Power.Contagion, out float contagion) && victim.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectPoison)) {
                Burst(Power.Contagion, victim, new HitData.DamageTypes() { m_poison = health * contagion / 100f });
            }
            if (PowerSet.Has(Power.GraspOfTheDead) && !Expired(grasped, id)) {
                Burst(Power.GraspOfTheDead, victim, new HitData.DamageTypes() { m_poison = health * GraspBurstShare });
            }
        }

        private static void Burst(Power power, Character victim, HitData.DamageTypes damage) {
            Vector3 at = victim.GetCenterPoint();
            PowerFx.Effect(power, VfxRole.Burst, at, BurstRadius / 5f, shared: true);
            foreach (Character enemy in PowerFx.EnemiesNear(at, BurstRadius, victim).ToArray()) {
                PowerFx.Strike(enemy, damage, at);
            }
        }

        // ---- On a clock ----

        internal static void Tick(Player player) {
            SetAgainstTheCharge(player);
            OverCarry(player);
            if (PowerSet.Has(Power.RottingGround, out float rot) && Time.time >= nextRot) {
                nextRot = Time.time + 1f;
                Vector3 at = player.transform.position;
                foreach (Character enemy in PowerFx.EnemiesNear(at, RotRadius).ToArray()) {
                    PowerFx.Strike(enemy, new HitData.DamageTypes() { m_poison = rot }, at);
                }
            }
        }

        // Set Against the Charge: an enemy closing in on a levelled polearm is stopped short.
        private static void SetAgainstTheCharge(Player player) {
            if (!PowerSet.Has(Power.SetAgainstCharge) || Time.time < nextCharge) { return; }
            nextCharge = Time.time + 0.25f;
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (weapon == null || weapon.m_shared.m_skillType != Skills.SkillType.Polearms) {
                lastDistance.Clear();
                return;
            }
            Vector3 at = player.transform.position;
            Dictionary<int, float> seen = new Dictionary<int, float>();
            foreach (Character enemy in PowerFx.EnemiesNear(at, ChargeRange * 4f).ToArray()) {
                int id = enemy.GetInstanceID();
                float distance = Vector3.Distance(enemy.transform.position, at);
                seen[id] = distance;
                bool closedIn = distance < ChargeRange && lastDistance.TryGetValue(id, out float before) && before >= ChargeRange;
                if (closedIn && Time.time >= chargeReadyAt) {
                    chargeReadyAt = Time.time + ChargeCooldown;
                    enemy.Stagger((enemy.transform.position - at).normalized);
                    PowerFx.Effect(Power.SetAgainstCharge, VfxRole.Proc, enemy.GetCenterPoint(), 0.8f);
                }
            }
            lastDistance.Clear();
            foreach (KeyValuePair<int, float> entry in seen) { lastDistance[entry.Key] = entry.Value; }
        }

        // Ox-strength: over the carry limit, stamina pays for the extra weight.
        private static void OverCarry(Player player) {
            if (!PowerSet.Has(Power.OverCarry)) { return; }
            float over = player.GetInventory().GetTotalWeight() - player.GetMaxCarryWeight();
            if (over > 0f) { player.UseStamina(over * OverCarryStamina * Time.deltaTime); }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.IsEncumbered))]
        private static class Encumbered {
            private static void Postfix(Player __instance, ref bool __result) {
                if (__result && __instance == Player.m_localPlayer && PowerSet.Has(Power.OverCarry) && __instance.GetStamina() > 1f) { __result = false; }
            }
        }

        // Wall of Spears: polearm attacks reach wider. Each attack is vanilla's own copy, so this lasts one swing.
        [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static class Reach {
            private static void Prefix(Attack __instance, Humanoid character, ItemDrop.ItemData weapon) {
                if (character != Player.m_localPlayer || weapon == null || weapon.m_shared.m_skillType != Skills.SkillType.Polearms || !PowerSet.Has(Power.WallOfSpears)) { return; }
                __instance.m_attackRange *= 1.25f;
                __instance.m_attackAngle *= 1.25f;
            }
        }

        // Keep the Secret: the noise of a sneak attack never carries.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_AddNoise))]
        private static class Quiet {
            private static bool Prefix(Character __instance) => __instance != Player.m_localPlayer || Time.time > quietUntil;
        }

        // ...nor does an arrow, bolt or spell landing from one. Vanilla alerts creatures near the impact
        // after the hit is dealt, so the hit has already started the quiet.
        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.DoProjectileHitNoise))]
        private static class QuietImpact {
            private static bool Prefix(Character attacker) => attacker == null || attacker != Player.m_localPlayer || Time.time > quietUntil;
        }

        // Dread: a creature its owner sees marked by a player's Dread runs, using vanilla's own flight.
        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.PheromoneFleeCheck))]
        private static class Flee {
            private static void Postfix(MonsterAI __instance, Character target, ref bool __result) {
                if (__result || !(target is Player player) || ZNet.instance == null) { return; }
                ZDO zdo = player.m_nview?.GetZDO();
                if (zdo == null || ZNet.instance.GetTime().Ticks > zdo.GetLong(AllyPowers.DreadUntilKey, 0L)) { return; }
                if (zdo.GetZDOID(AllyPowers.DreadKey) == __instance.m_character.GetZDOID()) { __result = true; }
            }
        }
    }
}
