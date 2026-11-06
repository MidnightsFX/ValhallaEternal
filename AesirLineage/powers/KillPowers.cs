using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>Powers that fire when the local player lands a killing blow.</summary>
    /// <remarks>
    /// Kills are seen in <c>Character.OnDeath</c> on the creature's owner, where <c>m_lastHit</c> names the
    /// killer. When another player owns the creature, its owner tells the killer by a routed RPC, which runs
    /// the powers that need no more than where it fell and its stars; the bursts, loot and summons that need
    /// the body only fire for creatures the local player owns, usually what they are fighting.
    /// </remarks>
    internal static class KillPowers {
        private const float SpeedOnKillSeconds = 5f;
        private const float FallenSeconds = 20f;
        private const float BurstRadius = 4f;
        private const float WinterBurstRadius = 6f;
        // Bursts are a share of the victim's max health; a boss's huge pool is counted as this much.
        private const float BurstHealthCap = 1000f;
        private const int WolfKills = 3;
        private const float WolfKillWindow = 20f;
        private const float WolfSeconds = 60f;
        private const float EinherjarSeconds = 20f;
        private const float ThrallSeconds = 20f;
        private const int MaxSummons = 3;
        private const float FieldDressingSeconds = 15f;
        private const float FieldDressingRadius = 3f;

        /// <summary>When the local player last killed something, for Silent Passing.</summary>
        internal static float LastKillTime = -100f;
        private static float fallenUntil;
        private static float fallenBonus;
        private static readonly List<float> bowKills = new List<float>();
        private static readonly List<Character> summons = new List<Character>();
        private static readonly List<(Vector3 at, float since, float until)> dressings = new List<(Vector3, float, float)>();

        internal static void Reset() {
            LastKillTime = -100f;
            fallenUntil = 0f;
            fallenBonus = 0f;
            bowKills.Clear();
            summons.Clear();
            dressings.Clear();
        }

        /// <summary>Learned from the Fallen: the damage bonus left by a recent starred kill.</summary>
        internal static float FallenBonus() => Time.time < fallenUntil ? fallenBonus : 0f;

        private struct Valuable {
            public string Prefab;
            public float Weight;
            public int Min;
            public int Max;
        }

        private static readonly Valuable[] Valuables = new Valuable[] {
            new Valuable() { Prefab = "Coins", Weight = 60, Min = 3, Max = 15 },
            new Valuable() { Prefab = "Amber", Weight = 15, Min = 1, Max = 1 },
            new Valuable() { Prefab = "AmberPearl", Weight = 10, Min = 1, Max = 1 },
            new Valuable() { Prefab = "Ruby", Weight = 10, Min = 1, Max = 1 },
            new Valuable() { Prefab = "SilverNecklace", Weight = 5, Min = 1, Max = 1 },
        };

        private static int speedEffectHash;
        private static float nextDressing;

        internal static void RegisterStatusEffects() {
            SE_Stats hasten = ScriptableObject.CreateInstance<SE_Stats>();
            hasten.name = "AL_HastenTheInevitable";
            hasten.m_name = "$al_hasten_death_effect";
            hasten.m_icon = DataObjects.hastenDeath;
            hasten.m_ttl = SpeedOnKillSeconds;
            // Set from the power on each kill; SE_Stats adds base speed times this.
            hasten.m_speedModifier = 0f;
            CustomStatusEffect effect = new CustomStatusEffect(hasten, fixReference: false);
            ItemManager.Instance.AddStatusEffect(effect);
            speedEffectHash = effect.StatusEffect.NameHash();
        }

        // Runs where the creature is owned, which is the only place its death is seen. A kill by another
        // player is told to them, so their kill powers (Death's Bargain above all) still hear of it.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class LocalKill {
            private static void Prefix(Character __instance) {
                if (__instance == null || __instance is Player || __instance.IsTamed() || __instance.m_lastHit == null) { return; }
                Player local = Player.m_localPlayer;
                Character killer = __instance.m_lastHit.GetAttacker();
                if (killer == null) { return; }
                if (local != null && killer == local) {
                    OnKill(local, __instance);
                } else if (killer is Player other) {
                    ZDO zdo = other.m_nview?.GetZDO();
                    if (zdo != null) { ZRoutedRpc.instance?.InvokeRoutedRPC(zdo.GetOwner(), KillRpc, __instance.transform.position, __instance.GetLevel()); }
                } else if (local != null && PowerSet.Has(Power.AscendantThralls) && SummonPowers.IsSummon(killer) && AllyPowers.FollowedPlayer(killer) == local) {
                    SummonPowers.OnSummonKill(killer);
                }
            }
        }

        // Vanilla's burning and poison ticks name no attacker, so a creature that died of them was killed by no
        // one and no kill power heard of it. On the creature's owner a tick takes the attacker of the hit before
        // it, when that was a player, so fire or poison a player started finishes the kill in their name. Any
        // other hit in between, a creature's or the world's, breaks the chain.
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class CreditDamageOverTime {
            private static void Prefix(Character __instance, HitData hit) {
                if (hit == null || __instance is Player || !hit.m_attacker.IsNone()) { return; }
                if (hit.m_hitType != HitData.HitType.Burning && hit.m_hitType != HitData.HitType.Poisoned) { return; }
                HitData last = __instance.m_lastHit;
                if (last != null && last.GetAttacker() is Player) { hit.m_attacker = last.m_attacker; }
            }
        }

        private const string KillRpc = "AL_Kill";

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc {
            private static void Postfix() {
                ZRoutedRpc.instance?.Register<Vector3, int>(KillRpc, OnKillElsewhere);
            }
        }

        // On the killer's machine: a kill whose creature another machine owned. Only what needs no more
        // than where it fell and how many stars it had.
        private static void OnKillElsewhere(long sender, Vector3 at, int level) {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead()) { return; }
            OnAnyKill(player, at, level);
        }

        // What any kill by the local player sets off, wherever the creature was owned.
        private static void OnAnyKill(Player player, Vector3 at, int level) {
            LastKillTime = Time.time;
            SurvivalPowers.OnLocalKill();
            if (PowerSet.Has(Power.SpeedOnKill, out float speed)) { Hasten(player, speed); }
            if (PowerSet.Has(Power.HealOnKill, out float heal)) { player.Heal(player.GetMaxHealth() * heal / 100f, true); }
            if (PowerSet.Has(Power.LearnedFromFallen, out float learned) && level > 1) {
                float bonus = learned * (level - 1);
                if (Time.time >= fallenUntil || bonus > fallenBonus) { fallenBonus = bonus; }
                fallenUntil = Time.time + FallenSeconds;
                PowerFx.Show(player, PowerFx.Fallen, FallenSeconds);
            }
            StrikePowers.OnAnyKill(player);
            HomePowers.OnLocalKill(player, at);
            CraftPowers.OnLocalKill(player);
        }

        private static void OnKill(Player player, Character victim) {
            OnAnyKill(player, victim.transform.position, victim.GetLevel());
            PlayerVisuals.OnLocalKill(victim);
            HitData hit = victim.m_lastHit;
            bool bow = hit.m_skill == Skills.SkillType.Bows || hit.m_skill == Skills.SkillType.Crossbows;

            if (PowerSet.Has(Power.ArrowScavenge, out float scavenge) && Random.value * 100f < scavenge) { ReturnAmmo(player); }
            if (PowerSet.Has(Power.LootWealth, out float wealth) && Random.value * 100f < wealth) { DropValuable(victim); }

            // Bursts from the body, each a share of its health.
            float health = Mathf.Min(victim.GetMaxHealth(), BurstHealthCap);
            if (hit.m_damage.m_frost > 0f && PowerSet.Has(Power.Shatter, out float shatter)) {
                Burst(Power.Shatter, victim, BurstRadius, new HitData.DamageTypes() { m_frost = health * shatter / 100f });
            }
            if (bow && PowerSet.Has(Power.WintersShatter, out float winter)) {
                Burst(Power.WintersShatter, victim, WinterBurstRadius, new HitData.DamageTypes() { m_frost = health * winter / 100f });
            }
            if (victim.m_faction == Character.Faction.Undead && PowerSet.Has(Power.CleansingFlame, out float cleansing)) {
                float share = health * cleansing / 200f;
                Burst(Power.CleansingFlame, victim, BurstRadius, new HitData.DamageTypes() { m_fire = share, m_spirit = share });
            }
            bool burning = hit.m_damage.m_fire > 0f || victim.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectBurning);
            if (burning && PowerSet.Has(Power.Wildfire, out float wildfire)) {
                Burst(Power.Wildfire, victim, BurstRadius, new HitData.DamageTypes() { m_fire = health * wildfire / 100f });
            }
            if (DamagePowers.WasWounded(victim) && PowerSet.Has(Power.OpenWound, out float wound)) {
                Burst(Power.OpenWound, victim, BurstRadius, new HitData.DamageTypes() { m_poison = health * wound / 100f });
            }

            // Help called up by the kill.
            if (bow && PowerSet.Has(Power.WolfSister)) {
                bowKills.Add(Time.time);
                bowKills.RemoveAll(t => Time.time - t > WolfKillWindow);
                if (bowKills.Count >= WolfKills) {
                    bowKills.Clear();
                    // Endless Pack: the wolves join the pack and stay.
                    CallUp(Power.WolfSister, SummonPowers.Wolf, player, PowerSet.Has(Power.EndlessPack) ? 0f : WolfSeconds, explodes: false);
                }
            }
            if (PowerSet.Has(Power.Folkvangr, out float folkvangr) && Random.value * 100f < folkvangr) {
                CallUp(Power.Folkvangr, SummonPowers.Einherjar, player, EinherjarSeconds, explodes: false);
            }
            if (PowerSet.Has(Power.Naglfar, out float naglfar) && Random.value * 100f < naglfar) {
                CallUp(Power.Naglfar, SummonPowers.Einherjar, victim, ThrallSeconds, explodes: true);
            }

            if (bow && PowerSet.Has(Power.FieldDressing)) {
                dressings.Add((victim.transform.position, Time.time, Time.time + FieldDressingSeconds));
            }

            StrikePowers.OnLocalKill(player, victim);
        }

        private static void Burst(Power power, Character victim, float radius, HitData.DamageTypes damage) {
            Vector3 at = victim.GetCenterPoint();
            PowerFx.Effect(power, VfxRole.Burst, at, radius / 5f, shared: true);
            foreach (Character enemy in PowerFx.EnemiesNear(at, radius, victim).ToArray()) {
                PowerFx.Strike(enemy, damage, at);
            }
        }

        private static void CallUp(Power power, string prefab, Component near, float seconds, bool explodes) {
            summons.RemoveAll(c => c == null || c.IsDead());
            // Legion: more at once. Wolves that stay for good count wherever they are.
            int allowed = MaxSummons + Mathf.RoundToInt(PowerSet.Get(Power.Legion));
            if (summons.Count >= allowed || SummonPowers.CountMine(prefab) >= allowed) { return; }
            Character summoned = SummonPowers.Summon(prefab, near.transform.position, seconds, PowerFx.DeityOf(power), explodes);
            if (summoned != null) { summons.Add(summoned); }
        }

        /// <summary>Field Dressing: what a bow kill drops comes to the player for a little while.</summary>
        internal static void Tick(Player player) {
            if (dressings.Count == 0 || Time.time < nextDressing) { return; }
            nextDressing = Time.time + 0.25f;
            dressings.RemoveAll(d => Time.time > d.until);
            foreach ((Vector3 at, float since, float until) in dressings) {
                foreach (ItemDrop drop in ItemDrop.s_instances.ToArray()) {
                    // Only what fell after the kill, so nothing the players dropped there is taken.
                    if (drop == null || drop.m_spawnTime < since || Vector3.Distance(drop.transform.position, at) > FieldDressingRadius) { continue; }
                    // A full bag would make Pickup complain every frame; leave those on the ground.
                    if (!player.GetInventory().CanAddItem(drop.m_itemData)) { continue; }
                    if (!drop.CanPickup(autoPickupDelay: false)) {
                        drop.RequestOwn();
                        continue;
                    }
                    player.Pickup(drop.gameObject, autoequip: false, autoPickupDelay: false);
                }
            }
        }

        // One of the arrows or bolts in use, so the reward follows the player's progression.
        private static void ReturnAmmo(Player player) {
            Skills.SkillType weapon = player.GetCurrentWeapon()?.m_shared.m_skillType ?? Skills.SkillType.None;
            if (weapon != Skills.SkillType.Bows && weapon != Skills.SkillType.Crossbows) { return; }
            GameObject prefab = player.GetAmmoItem()?.m_dropPrefab;
            if (prefab == null) { return; }
            if (!player.GetInventory().AddItem(prefab, 1)) { return; }
            PowerFx.Effect(Power.ArrowScavenge, VfxRole.Proc, player.transform.position, 0.6f);
            string name = Localization.instance.Localize(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            DamageText.instance.ShowText(DamageText.TextType.Bonus, player.transform.position + Vector3.up * 0.2f, $"+1 {name}", player: true);
        }

        /// <summary>A valuable from the weighted table, as an item prefab and an amount.</summary>
        internal static (GameObject prefab, int amount) RollValuable() {
            float total = 0f;
            foreach (Valuable valuable in Valuables) { total += valuable.Weight; }
            float roll = Random.value * total;
            Valuable picked = Valuables[0];
            foreach (Valuable valuable in Valuables) {
                roll -= valuable.Weight;
                if (roll <= 0f) { picked = valuable; break; }
            }
            return (ObjectDB.instance?.GetItemPrefab(picked.Prefab), Random.Range(picked.Min, picked.Max + 1));
        }

        private static void DropValuable(Character victim) {
            (GameObject prefab, int amount) = RollValuable();
            if (prefab == null) { return; }
            Vector3 position = victim.transform.position + Vector3.up * 0.5f;
            Extensions.DropItemsImmediate(new Dictionary<GameObject, int>() { { prefab, amount } }, position, 0.5f);
            PowerFx.Effect(Power.LootWealth, VfxRole.Proc, position);
        }

        private static void Hasten(Player player, float percent) {
            if (speedEffectHash == 0) { return; }
            // SEMan clones the ObjectDB copy, so setting it here sets this application's strength.
            if (ObjectDB.instance?.GetStatusEffect(speedEffectHash) is SE_Stats template) {
                template.m_speedModifier = percent / 100f;
            }
            player.GetSEMan().AddStatusEffect(speedEffectHash, resetTime: true);
        }
    }
}
