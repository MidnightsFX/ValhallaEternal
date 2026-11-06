using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Víðarr's Wolf-slayer and Vengeance: great foes and the long fight against them, slipping free of
    /// what holds the player, the blood-debt paid out in a heavy blow, and the mark on whatever kills an
    /// ally or a tame.
    /// </summary>
    /// <remarks>
    /// A player or tame dies on the machine that owns it, so that machine tells every player who and
    /// where by a routed RPC, and each one near enough with Kin-vengeance marks the killer.
    /// </remarks>
    internal static class WolfPowers {
        private const float FightResetSeconds = 10f;
        private const float TearCooldown = 20f;
        private const float BossTearShare = 0.4f;
        private const float DebtFadeSeconds = 10f;
        private const float DebtCapShare = 0.5f;
        private const float AvengerRadius = 5f;
        private const float WrathSeconds = 5f;
        private const float KinRange = 20f;
        private const float KinSeconds = 60f;
        private const string FallenRpc = "AL_Fallen";

        private static float debt;
        private static float lastHurt = -100f;
        private static readonly Dictionary<ZDOID, (int stacks, float last)> fights = new Dictionary<ZDOID, (int, float)>();
        private static readonly Dictionary<ZDOID, float> torn = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<ZDOID, float> marked = new Dictionary<ZDOID, float>();

        internal static void Reset() {
            debt = 0f;
            lastHurt = -100f;
            fights.Clear();
            torn.Clear();
            marked.Clear();
        }

        private static void Trim<T>(Dictionary<ZDOID, T> entries) {
            if (entries.Count > 512) { entries.Clear(); }
        }

        /// <summary>A boss, or a creature with at least twice the player's max health.</summary>
        internal static bool GreatFoe(Player player, Character target) => target.IsBoss() || target.GetMaxHealth() >= player.GetMaxHealth() * 2f;

        private static float DebtCap(Player player) => player.GetMaxHealth() * DebtCapShare;

        internal static float OutgoingFactor(Player player, Character target) {
            ZDOID id = target.GetZDOID();
            float factor = 1f;
            if (GreatFoe(player, target)) {
                factor *= (1f + PowerSet.Get(Power.GreatFoeDamage) / 100f) * (1f + PowerSet.Get(Power.WolfSworn) / 100f);
                if (PowerSet.Has(Power.LongFight, out float most) && fights.TryGetValue(id, out var fight) && Time.time - fight.last < FightResetSeconds) {
                    factor *= 1f + Mathf.Min(most, fight.stacks) / 100f;
                }
            } else {
                factor *= Mathf.Max(0.2f, 1f - PowerSet.Get(Power.LesserFoeDamagePenalty) / 100f);
            }
            if (Time.time - lastHurt < WrathSeconds) { factor *= 1f + PowerSet.Get(Power.WrathDamage) / 100f; }
            if (marked.TryGetValue(id, out float until) && Time.time < until) { factor *= 1f + PowerSet.Get(Power.KinVengeance) / 100f; }
            return factor;
        }

        /// <summary>
        /// After the damage factors, so these land as they are: the Long Fight's count, then on a heavy
        /// attack the blood-debt paid out and the wolf's jaws torn.
        /// </summary>
        internal static void FinishHit(Player player, Character target, HitData hit, bool heavy, bool staggered) {
            ZDOID id = target.GetZDOID();
            bool great = GreatFoe(player, target);
            if (great && PowerSet.Has(Power.LongFight)) {
                Trim(fights);
                int stacks = fights.TryGetValue(id, out var fight) && Time.time - fight.last < FightResetSeconds ? fight.stacks : 0;
                fights[id] = (stacks + 1, Time.time);
            }
            if (!heavy) { return; }
            if (debt > 0f) { PayDebt(player, target, hit); }
            if (great && staggered && PowerSet.Has(Power.TearTheJaws, out float tear) && (!torn.TryGetValue(id, out float ready) || Time.time >= ready)) {
                Trim(torn);
                torn[id] = Time.time + TearCooldown;
                hit.m_damage.m_damage += target.GetMaxHealth() * tear / 100f * (target.IsBoss() ? BossTearShare : 1f);
                PowerFx.Effect(Power.TearTheJaws, VfxRole.Burst, target.GetCenterPoint(), 0.8f, shared: true);
            }
        }

        // The debt goes into the blow. Full, with the Avenger, it is doubled and strikes all around.
        private static void PayDebt(Player player, Character target, HitData hit) {
            float paid = debt;
            bool full = debt >= DebtCap(player) - 0.5f;
            debt = 0f;
            PowerFx.Hide(player, PowerFx.Debt);
            // Blood for Blood heals from the debt itself, not the Avenger's doubling.
            if (PowerSet.Has(Power.DebtHeal, out float heal)) { player.Heal(paid * heal / 100f, true); }
            if (full && PowerSet.Has(Power.Avenger)) {
                paid *= 2f;
                Vector3 at = target.transform.position;
                PowerFx.Effect(Power.Avenger, VfxRole.Burst, at, AvengerRadius / 5f, shared: true);
                foreach (Character enemy in PowerFx.EnemiesNear(at, AvengerRadius, target).ToArray()) {
                    PowerFx.Strike(enemy, new HitData.DamageTypes() { m_damage = paid }, player.transform.position);
                    if (!enemy.IsBoss()) { enemy.Stagger((enemy.transform.position - at).normalized); }
                }
                if (!target.IsBoss()) { target.Stagger(hit.m_dir); }
            } else {
                PowerFx.Effect(Power.BloodDebt, VfxRole.Proc, target.GetCenterPoint(), 0.7f);
            }
            hit.m_damage.m_damage += paid;
        }

        /// <summary>After an enemy's hit hurt the local player: Unforgiving's moment, and the debt it leaves.</summary>
        internal static void AfterHurt(Player player, float taken) {
            lastHurt = Time.time;
            if (!PowerSet.Has(Power.BloodDebt, out float share)) { return; }
            share += PowerSet.Get(Power.Unatoned);
            debt = Mathf.Min(DebtCap(player), debt + taken * share / 100f);
            PowerFx.Show(player, PowerFx.Debt, PowerSet.Has(Power.Unatoned) ? 3600f : DebtFadeSeconds);
        }

        internal static void Tick(Player player) {
            // Unatoned: the debt never fades.
            if (debt > 0f && (!PowerSet.Has(Power.BloodDebt) || (!PowerSet.Has(Power.Unatoned) && Time.time - lastHurt > DebtFadeSeconds))) {
                debt = 0f;
                PowerFx.Hide(player, PowerFx.Debt);
            }
        }

        // ---- Kin-vengeance ----

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<Vector3, ZDOID>(FallenRpc, OnFallen);
            }
        }

        private static void TellOfFall(Character fallen) {
            HitData hit = fallen.m_lastHit;
            if (hit == null || hit.m_attacker.IsNone() || ZRoutedRpc.instance == null) { return; }
            if (hit.GetAttacker() is Player || hit.m_attacker == fallen.GetZDOID()) { return; }
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, FallenRpc, fallen.transform.position, hit.m_attacker);
        }

        // The local player falls, on their own machine.
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class PlayerFalls {
            private static void Prefix(Player __instance) {
                if (__instance == Player.m_localPlayer && __instance.m_nview != null && __instance.m_nview.IsOwner()) { TellOfFall(__instance); }
            }
        }

        // A tame falls, on whichever machine owns it.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class TameFalls {
            private static void Prefix(Character __instance) {
                if (__instance != null && !(__instance is Player) && __instance.IsTamed() && __instance.m_nview != null && __instance.m_nview.IsOwner()) { TellOfFall(__instance); }
            }
        }

        private static void OnFallen(long sender, Vector3 at, ZDOID killer) {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead() || !PowerSet.Has(Power.KinVengeance)) { return; }
            if (Vector3.Distance(player.transform.position, at) > KinRange) { return; }
            Trim(marked);
            marked[killer] = Time.time + KinSeconds;
            player.Message(MessageHud.MessageType.TopLeft, "$al_kin_marked");
            PowerFx.Effect(Power.KinVengeance, VfxRole.Proc, player.GetCenterPoint(), 0.6f);
        }
    }
}
