using HarmonyLib;
using System;
using UnityEngine;
using Object = UnityEngine.Object;
using AesirLineage.starmap;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.powers {
    /// <summary>
    /// Creatures the powers call up for a while: Skaði's wolf, Freyja's einherjar, Hel's thralls. They are
    /// ordinary tamed creatures that follow the player; their ZDO carries when they leave, and whoever owns
    /// them at the time sends them away, so they leave even if the summoner has gone.
    /// </summary>
    internal static class SummonPowers {
        private const string UntilKey = "al_summon_until";
        private const string ExplodeKey = "al_summon_explode";
        // Whose summon it is, for the effects when it arrives, leaves or bursts.
        private const string DeityKey = "al_summon_deity";
        // The player who called it up. A summon that stays for good leaves once its summoner is gone.
        private const string SummonerKey = "al_summoner";
        private const float LeashRange = 100f;
        private const float LeashGrace = 10f;
        private static readonly System.Collections.Generic.Dictionary<int, float> strayedSince = new System.Collections.Generic.Dictionary<int, float>();
        private const float ExplodeBelowHealth = 0.25f;
        private const float ExplodeRadius = 3f;

        internal const string Wolf = "Wolf";
        // The Staff of the Dead's skeleton: already tame, already a summon.
        internal const string Einherjar = "Skeleton_Friendly";

        // Ascendant Thralls: a summon gains a star after this many kills, up to this level.
        private const string KillsKey = "al_summon_kills";
        private const int KillsPerStar = 3;
        private const int AscendMaxLevel = 5;

        /// <summary>Calls up a creature for a while; with no time given it stays for good.</summary>
        internal static Character Summon(string prefabName, Vector3 near, float seconds, Deity deity, bool explodes = false) {
            Player player = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (player == null || prefab == null || ZNet.instance == null) {
                Logger.LogWarning($"Could not summon {prefabName}.");
                return null;
            }
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.5f;
            GameObject summoned = Object.Instantiate(prefab, near + new Vector3(offset.x, 0.2f, offset.y), Quaternion.identity);
            Character character = summoned.GetComponent<Character>();
            if (character != null && !character.IsTamed()) { character.SetTamed(true); }
            summoned.GetComponent<Tameable>()?.Command(player, message: false);
            ZDO zdo = summoned.GetComponent<ZNetView>()?.GetZDO();
            if (zdo != null) {
                if (seconds > 0f) {
                    seconds *= 1f + PowerSet.Get(Power.SummonDuration) / 100f;
                    zdo.Set(UntilKey, ZNet.instance.GetTime().Ticks + TimeSpan.FromSeconds(seconds).Ticks);
                }
                if (explodes) { zdo.Set(ExplodeKey, true); }
                zdo.Set(DeityKey, (int)deity);
                zdo.Set(SummonerKey, player.GetPlayerID());
            }
            DeityVfx.Share(deity, VfxRole.Proc, summoned.transform.position, 1.5f);
            return character;
        }

        /// <summary>A creature called up for a while: by a staff, or by one of these powers.</summary>
        internal static bool IsSummon(Character character) {
            if (character == null || !character.IsTamed()) { return false; }
            if (character.GetComponent<CharacterTimedDestruction>() != null) { return true; }
            return character.m_nview?.GetZDO()?.GetInt(DeityKey, -1) >= 0;
        }

        /// <summary>
        /// What these powers have called up for the local player, of one kind, following or not. Summons
        /// that stay for good never stray further than this, so all of them are counted. A staff's own
        /// summons don't count.
        /// </summary>
        internal static int CountMine(string prefabName) {
            int count = 0;
            Player local = Player.m_localPlayer;
            if (local == null) { return 0; }
            long id = local.GetPlayerID();
            foreach (Character tame in AllyPowers.TamesNear(local.transform.position, LeashRange)) {
                if (Utils.GetPrefabName(tame.gameObject) == prefabName && tame.m_nview?.GetZDO()?.GetLong(SummonerKey, 0L) == id) { count++; }
            }
            return count;
        }

        // A summon that stays for good leaves once its summoner has been out of reach for a little while.
        private static bool Strayed(Character summon, long summoner) {
            int id = summon.GetInstanceID();
            Player owner = Player.GetPlayer(summoner);
            if (owner != null && Vector3.Distance(owner.transform.position, summon.transform.position) < LeashRange) {
                strayedSince.Remove(id);
                return false;
            }
            if (!strayedSince.TryGetValue(id, out float since)) {
                if (strayedSince.Count > 256) { strayedSince.Clear(); }
                strayedSince[id] = Time.time;
                return false;
            }
            return Time.time - since > LeashGrace;
        }

        /// <summary>Ascendant Thralls: counts a kill for a summon this machine owns, and raises it a star every few.</summary>
        internal static void OnSummonKill(Character summon) {
            ZDO zdo = summon.m_nview?.GetZDO();
            if (zdo == null || !summon.m_nview.IsOwner()) { return; }
            int kills = zdo.GetInt(KillsKey, 0) + 1;
            zdo.Set(KillsKey, kills);
            if (kills % KillsPerStar == 0 && summon.GetLevel() < AscendMaxLevel) {
                summon.SetLevel(summon.GetLevel() + 1);
                DeityVfx.Share(PowerFx.DeityOf(Power.AscendantThralls), VfxRole.LevelUp, summon.transform.position, 0.8f);
            }
        }

        // Lord of the Host: a staff's summons last longer for the summoner who has it.
        [HarmonyPatch(typeof(Tameable), nameof(Tameable.Command))]
        private static class StaffSummons {
            private static void Postfix(Tameable __instance, Humanoid user) {
                if (user != Player.m_localPlayer || !PowerSet.Has(Power.SummonDuration, out float longer)) { return; }
                CharacterTimedDestruction timer = __instance.GetComponent<CharacterTimedDestruction>();
                BaseAI ai = __instance.GetComponent<BaseAI>();
                // Only just after it was called up, not each time it is told to follow.
                if (timer == null || ai == null || ai.GetTimeSinceSpawned().TotalSeconds > 2.0) { return; }
                timer.CancelInvoke("DestroyNow");
                timer.Trigger(UnityEngine.Random.Range(timer.m_timeoutMin, timer.m_timeoutMax) * (1f + longer / 100f));
            }
        }

        // Legion: a staff calls up more at once for the local player.
        [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.Setup))]
        private static class Legion {
            private static void Postfix(SpawnAbility __instance, Character owner) {
                if (owner != Player.m_localPlayer || __instance.m_maxSpawned <= 0 || !PowerSet.Has(Power.Legion, out float more)) { return; }
                __instance.m_maxSpawned += Mathf.RoundToInt(more);
            }
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
        private static class Expiry {
            private static void Postfix(MonsterAI __instance) {
                Character character = __instance.m_character;
                ZNetView nview = __instance.m_nview;
                // Cheap checks first: this runs for every creature this machine owns.
                if (character == null || nview == null || !nview.IsOwner() || !character.IsTamed()) { return; }
                ZDO zdo = nview.GetZDO();
                if (zdo == null || ZNet.instance == null) { return; }
                long until = zdo.GetLong(UntilKey, 0L);
                long summoner = zdo.GetLong(SummonerKey, 0L);
                if (until == 0L && summoner == 0L) { return; }
                Deity deity = (Deity)zdo.GetInt(DeityKey, (int)Deity.Hel);
                if (zdo.GetBool(ExplodeKey, false) && character.GetHealthPercentage() < ExplodeBelowHealth) {
                    Explode(character, deity);
                    ZNetScene.instance.Destroy(character.gameObject);
                    return;
                }
                bool timeUp = until > 0L && ZNet.instance.GetTime().Ticks >= until;
                if (timeUp || (until == 0L && Strayed(character, summoner))) {
                    DeityVfx.Share(deity, VfxRole.Proc, character.transform.position);
                    ZNetScene.instance.Destroy(character.gameObject);
                }
            }
        }

        // Náglfar's thralls burst into fire for a third of their max health.
        private static void Explode(Character thrall, Deity deity) {
            Vector3 at = thrall.transform.position;
            DeityVfx.Share(deity, VfxRole.Burst, at, 0.6f);
            float damage = thrall.GetMaxHealth() / 3f;
            foreach (Character enemy in PowerFx.EnemiesNear(at, ExplodeRadius, thrall).ToArray()) {
                HitData hit = new HitData() {
                    m_point = enemy.GetCenterPoint(),
                    m_dir = (enemy.transform.position - at).normalized,
                    m_hitType = HitData.HitType.EnemyHit,
                };
                hit.m_damage.m_fire = damage;
                hit.SetAttacker(thrall);
                enemy.Damage(hit);
            }
        }
    }
}
