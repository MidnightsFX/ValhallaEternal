using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Baldur's Breiðablik (a hall of healing around the player, for players and tames alike) and Dawn
    /// (strength in sunlight, a torch that warms and burns, mist burned away, the rainbow after rain and
    /// judgment called down from the sky).
    /// </summary>
    internal static class LightPowers {
        internal const float SanctuaryRange = 8f;
        internal const float HavenRange = 6f;
        private const float MercyRange = 10f;
        private const float MercyShare = 0.3f;
        private const float SelflessPerSecond = 0.01f;
        private const float AilmentsLess = 0.25f;
        private const float WeptRange = 15f;
        private const float WeptBelowHealth = 0.2f;
        private const float WeptCooldown = 180f;
        private const float BifrostRange = 30f;
        private const float BifrostSeconds = 300f;
        private const float JudgmentRange = 20f;
        private const float JudgmentCooldown = 120f;
        private const float JudgmentDelay = 0.5f;
        private const float TorchWarmth = 2f;

        private static float nextPulse;
        private static float nextWept;
        private static float nextBifrostCheck;
        private static bool wasWet;
        private static float judgmentReadyAt;
        private static readonly List<(float at, float damage)> judgments = new List<(float, float)>();
        private static readonly Dictionary<ZDOID, float> weptReadyAt = new Dictionary<ZDOID, float>();
        private static GameObject torchHeat;
        private static GameObject mistBall;

        internal static void Reset() {
            weptReadyAt.Clear();
            judgments.Clear();
            judgmentReadyAt = 0f;
            if (torchHeat != null) { Object.Destroy(torchHeat); }
            if (mistBall != null) { Object.Destroy(mistBall); }
            torchHeat = null;
            mistBall = null;
        }

        /// <summary>Direct sunlight: day, no rain, under the open sky.</summary>
        internal static bool InSunlight(Player player) => EnvMan.IsDay() && !EnvMan.IsWet() && !player.InShelter() && !player.InInterior();

        /// <summary>Breiðablik: the regen bonus from the strongest hall nearby, the local player's own included.</summary>
        internal static float SanctuaryBonus(Player player) => AllyPowers.Strongest(player.transform.position, SanctuaryRange, AllyPowers.SanctuaryKey);

        /// <summary>Safe Haven: less damage from another player's haven nearby. A player's own haven is for others.</summary>
        internal static float HavenFor(Player player) {
            float best = 0f;
            foreach (Player ally in PowerFx.AlliesNear(player.transform.position, HavenRange)) {
                best = Mathf.Max(best, ally.m_nview?.GetZDO()?.GetFloat(AllyPowers.HavenKey, 0f) ?? 0f);
            }
            return best;
        }

        internal static bool HoldingTorch(Player player) {
            return (player.m_rightItem != null && player.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
                || (player.m_leftItem != null && player.m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch);
        }

        internal static void Tick(Player player) {
            UpdateTorchHeat(player);
            UpdateMistBall(player);
            UpdateBifrost(player);
            UpdateJudgments();
            Vector3 at = player.transform.position;
            if (Time.time >= nextPulse) {
                nextPulse = Time.time + 1f;
                if (PowerSet.Has(Power.Sanctuary, out float sanctuary)) {
                    foreach (Character tame in AllyPowers.TamesNear(at, SanctuaryRange)) { Mend(tame, sanctuary / 1000f); }
                }
                if (PowerSet.Has(Power.Selfless)) {
                    foreach (Player ally in PowerFx.AlliesNear(at, SanctuaryRange).ToArray()) { Mend(ally, SelflessPerSecond); }
                    foreach (Character tame in AllyPowers.TamesNear(at, SanctuaryRange)) { Mend(tame, SelflessPerSecond); }
                }
            }
            if (PowerSet.Has(Power.AllThingsWept) && Time.time >= nextWept) {
                nextWept = Time.time + 0.5f;
                foreach (Player ally in PowerFx.AlliesNear(at, WeptRange).ToArray()) { Weep(ally); }
                foreach (Character tame in AllyPowers.TamesNear(at, WeptRange)) { Weep(tame); }
            }
        }

        private static void Mend(Character character, float shareOfMax) {
            if (character.GetHealth() < character.GetMaxHealth()) { character.Heal(character.GetMaxHealth() * shareOfMax, false); }
        }

        // All Things Wept: someone falling low is lifted back to half.
        private static void Weep(Character character) {
            float max = character.GetMaxHealth();
            if (max <= 0f || character.GetHealth() / max >= WeptBelowHealth) { return; }
            ZDOID id = character.GetZDOID();
            if (weptReadyAt.TryGetValue(id, out float ready) && Time.time < ready) { return; }
            weptReadyAt[id] = Time.time + WeptCooldown;
            character.Heal(max * 0.5f - character.GetHealth(), true);
            PowerFx.Effect(Power.AllThingsWept, VfxRole.Burst, character.transform.position, 0.6f, shared: true);
        }

        /// <summary>Mercy: some hits heal whoever nearby is worst hurt, the local player included.</summary>
        internal static void OnLocalHit(Player player, float damage) {
            if (damage <= 0f || !PowerSet.Has(Power.Mercy, out float chance) || Random.value * 100f >= chance) { return; }
            Character worst = player;
            float worstShare = player.GetHealthPercentage();
            foreach (Player ally in PowerFx.AlliesNear(player.transform.position, MercyRange)) {
                if (ally.GetHealthPercentage() < worstShare) { worst = ally; worstShare = ally.GetHealthPercentage(); }
            }
            foreach (Character tame in AllyPowers.TamesNear(player.transform.position, MercyRange)) {
                if (tame.GetHealthPercentage() < worstShare) { worst = tame; worstShare = tame.GetHealthPercentage(); }
            }
            if (worstShare >= 1f) { return; }
            worst.Heal(damage * MercyShare, true);
            PowerFx.Effect(Power.Mercy, VfxRole.Proc, worst.GetCenterPoint(), 0.6f);
        }

        // ---- Healing received, and shorter ailments in the hall ----

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Heal))]
        private static class Healing {
            private static void Prefix(Character __instance, ref float hp) {
                if (__instance == Player.m_localPlayer && PowerSet.Has(Power.HealingReceived, out float more)) { hp *= 1f + more / 100f; }
            }
        }

        /// <summary>Breiðablik: burning and poison hurt less inside it, as a factor on their damage.</summary>
        internal static float AilmentFactor(Player player) => SanctuaryBonus(player) > 0f ? 1f - AilmentsLess : 1f;

        // ---- Dawn ----

        // Torchbearer: a held torch warms like a campfire, so the player can rest and keep warm by it.
        private static void UpdateTorchHeat(Player player) {
            bool want = PowerSet.Has(Power.Torchbearer) && HoldingTorch(player);
            if (want && torchHeat == null) {
                torchHeat = new GameObject("AL_TorchHeat");
                torchHeat.layer = LayerMask.NameToLayer("character_trigger");
                torchHeat.transform.SetParent(player.transform, false);
                torchHeat.transform.localPosition = Vector3.up;
                SphereCollider sphere = torchHeat.AddComponent<SphereCollider>();
                sphere.radius = TorchWarmth;
                sphere.isTrigger = true;
                EffectArea area = torchHeat.AddComponent<EffectArea>();
                area.m_type = EffectArea.Type.Heat;
            } else if (!want && torchHeat != null) {
                Object.Destroy(torchHeat);
                torchHeat = null;
            }
        }

        // Mist-burner: the wisp's mist-clearing ball goes with the player, its reach set by the power.
        private static void UpdateMistBall(Player player) {
            bool want = PowerSet.Has(Power.MistBurner, out float radius);
            if (!want) {
                if (mistBall != null) {
                    Object.Destroy(mistBall);
                    mistBall = null;
                }
                return;
            }
            if (mistBall == null) {
                GameObject prefab = DemisterBall();
                if (prefab == null) { return; }
                // The wisp's ball is a networked prefab; this copy stays on this machine, as a build ghost does.
                ZNetView.m_forceDisableInit = true;
                try {
                    mistBall = Object.Instantiate(prefab, player.transform.position + Vector3.up * 2f, Quaternion.identity);
                } finally {
                    ZNetView.m_forceDisableInit = false;
                }
                mistBall.transform.SetParent(player.transform, true);
            }
            foreach (ParticleSystemForceField field in mistBall.GetComponentsInChildren<ParticleSystemForceField>()) {
                if (field.endRange != radius) { field.endRange = radius; }
            }
        }

        private static GameObject DemisterBall() {
            if (ObjectDB.instance == null) { return null; }
            foreach (StatusEffect effect in ObjectDB.instance.m_StatusEffects) {
                if (effect is SE_Demister demister && demister.m_ballPrefab != null) { return demister.m_ballPrefab; }
            }
            return null;
        }

        // Bifröst: rain ending near dawn or dusk lays a rainbow over the player and everyone near.
        private static void UpdateBifrost(Player player) {
            bool wet = EnvMan.IsWet();
            if (wasWet && !wet && PowerSet.Has(Power.Bifrost) && EnvMan.instance != null && ZNet.instance != null) {
                float day = EnvMan.instance.GetDayFraction();
                bool twilight = (day > 0.18f && day < 0.32f) || (day > 0.68f && day < 0.82f);
                if (twilight) {
                    long until = ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(BifrostSeconds).Ticks;
                    player.m_nview?.GetZDO()?.Set(AllyPowers.BifrostKey, until);
                    player.Message(MessageHud.MessageType.Center, "$al_bifrost");
                    PowerFx.Effect(Power.Bifrost, VfxRole.Burst, player.transform.position, 2f, shared: true);
                }
            }
            wasWet = wet;
            if (Time.time < nextBifrostCheck || ZNet.instance == null) { return; }
            nextBifrostCheck = Time.time + 2f;
            long now = ZNet.instance.GetTime().Ticks;
            long best = 0L;
            List<Player> players = new List<Player>();
            Player.GetPlayersInRange(player.transform.position, BifrostRange, players);
            foreach (Player near in players) {
                best = System.Math.Max(best, near?.m_nview?.GetZDO()?.GetLong(AllyPowers.BifrostKey, 0L) ?? 0L);
            }
            if (best <= now || player.GetSEMan().HaveStatusEffect(PowerFx.Hash(PowerFx.Bifrost))) { return; }
            PowerFx.Show(player, PowerFx.Bifrost, (float)System.TimeSpan.FromTicks(best - now).TotalSeconds);
        }

        // Radiant Judgment: a heavy attack calls down light, a moment after the swing begins.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class Judgment {
            private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result) {
                if (!__result || !secondaryAttack || __instance != Player.m_localPlayer || Time.time < judgmentReadyAt) { return; }
                if (!PowerSet.Has(Power.RadiantJudgment, out float share)) { return; }
                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if (weapon == null) { return; }
                judgmentReadyAt = Time.time + JudgmentCooldown;
                judgments.Add((Time.time + JudgmentDelay, weapon.GetDamage().GetTotalDamage() * share / 100f));
            }
        }

        private static void UpdateJudgments() {
            Player player = Player.m_localPlayer;
            for (int i = judgments.Count - 1; i >= 0; i--) {
                if (Time.time < judgments[i].at) { continue; }
                float damage = judgments[i].damage;
                judgments.RemoveAt(i);
                if (player == null) { continue; }
                Vector3 from = player.transform.position;
                PowerFx.Effect(Power.RadiantJudgment, VfxRole.Burst, from, 1.5f, shared: true);
                foreach (Character enemy in PowerFx.EnemiesNear(from, JudgmentRange).ToArray()) {
                    PowerFx.Effect(Power.RadiantJudgment, VfxRole.Proc, enemy.GetCenterPoint(), 0.8f);
                    PowerFx.Strike(enemy, new HitData.DamageTypes() { m_spirit = damage }, from);
                }
            }
        }
    }
}
