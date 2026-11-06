using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Skaði's Quarry (hides, meat, trophies and what trophies teach) and Spear (the thrown hunting spear).
    /// </summary>
    internal static class HuntPowers {
        private const float PierceLength = 6f;
        private const float PierceHalfWidth = 1f;
        private const float WinterSpearRadius = 4f;
        // What a kill can drop an extra one of: hides, pelts and meat, by prefab name.
        private static readonly string[] Spoils = { "Meat", "Hide", "Pelt", "Leather", "NeckTail", "Scale" };
        // Meat dishes, by prefab name. Fish are left out: Skaði never took to the sea.
        private static readonly string[] MeatDishes = { "Meat", "Sausage", "NeckTail", "Jerky", "Lox", "Hare", "Chicken", "Svin", "Skewer", "Platter", "Serpent" };

        private static readonly Dictionary<string, string> trophies = new Dictionary<string, string>();
        // Hits each creature this machine owns has taken, so a kill in one hit can be told apart.
        private static readonly Dictionary<ZDOID, int> hitsTaken = new Dictionary<ZDOID, int>();

        internal static void Reset() {
            hitsTaken.Clear();
        }

        /// <summary>The trophy a creature drops, by prefab name, or "" when it has none.</summary>
        internal static string TrophyOf(Character creature) {
            if (creature == null) { return ""; }
            string prefab = Utils.GetPrefabName(creature.gameObject);
            if (trophies.TryGetValue(prefab, out string trophy)) { return trophy; }
            trophy = "";
            CharacterDrop drops = creature.GetComponent<CharacterDrop>();
            if (drops != null) {
                foreach (CharacterDrop.Drop drop in drops.m_drops) {
                    if (drop.m_prefab != null && drop.m_prefab.name.StartsWith("Trophy")) {
                        trophy = drop.m_prefab.name;
                        break;
                    }
                }
            }
            trophies[prefab] = trophy;
            return trophy;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Hits {
            private static void Prefix(Character __instance, HitData hit) {
                if (hit == null || __instance.IsPlayer() || __instance.m_nview == null || !__instance.m_nview.IsOwner() || !(hit.GetAttacker() is Player)) { return; }
                if (hitsTaken.Count > 2000) { hitsTaken.Clear(); }
                ZDOID id = __instance.GetZDOID();
                hitsTaken[id] = hitsTaken.TryGetValue(id, out int hits) ? hits + 1 : 1;
            }
        }

        /// <summary>Þjazi's Eyes: more damage to a creature whose trophy the local player carries.</summary>
        internal static float TrophyLoreBonus(Player player, Character target) {
            if (!PowerSet.Has(Power.TrophyLore, out float bonus)) { return 0f; }
            string trophy = TrophyOf(target);
            if (trophy.Length == 0) { return 0f; }
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems()) {
                if (item.m_dropPrefab != null && item.m_dropPrefab.name == trophy) { return bonus; }
            }
            return 0f;
        }

        /// <summary>Hunter's Fare: the extra health and stamina meat dishes give.</summary>
        internal static void MeatBonus(Player player, ref float hp, ref float stamina) {
            if (!PowerSet.Has(Power.MeatFood, out float bonus)) { return; }
            foreach (Player.Food food in player.GetFoods()) {
                string name = food.m_item?.m_dropPrefab != null ? food.m_item.m_dropPrefab.name : "";
                if (!IsMeat(name)) { continue; }
                hp += food.m_health * bonus / 100f;
                stamina += food.m_stamina * bonus / 100f;
            }
        }

        private static bool IsMeat(string prefab) {
            if (prefab.Length == 0 || prefab.StartsWith("Fish")) { return false; }
            foreach (string part in MeatDishes) {
                if (prefab.Contains(part)) { return true; }
            }
            return false;
        }

        // ---- What a kill drops, on the victim's owner ----

        private struct DropState {
            public CharacterDrop.Drop Trophy;
            public float Chance;
        }

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        private static class KillDrops {
            private static void Prefix(CharacterDrop __instance, out DropState __state) {
                __state = default;
                float more = Killer(__instance)?.m_nview?.GetZDO()?.GetFloat(AllyPowers.TrophyKey, 0f) ?? 0f;
                if (more <= 0f) { return; }
                bool oneHit = hitsTaken.TryGetValue(__instance.m_character.GetZDOID(), out int hits) && hits == 1;
                foreach (CharacterDrop.Drop drop in __instance.m_drops) {
                    if (drop.m_prefab == null || !drop.m_prefab.name.StartsWith("Trophy")) { continue; }
                    __state = new DropState() { Trophy = drop, Chance = drop.m_chance };
                    drop.m_chance = oneHit ? 1f : Mathf.Min(1f, drop.m_chance * (1f + more / 100f));
                    break;
                }
            }

            private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result) {
                float chance = Killer(__instance)?.m_nview?.GetZDO()?.GetFloat(AllyPowers.SpoilsKey, 0f) ?? 0f;
                if (__result == null || chance <= 0f || Random.value * 100f >= chance) { return; }
                List<GameObject> spoils = new List<GameObject>();
                foreach (CharacterDrop.Drop drop in __instance.m_drops) {
                    if (drop.m_prefab != null && IsSpoil(drop.m_prefab.name)) { spoils.Add(drop.m_prefab); }
                }
                if (spoils.Count == 0) { return; }
                __result.Add(new KeyValuePair<GameObject, int>(spoils[Random.Range(0, spoils.Count)], 1));
                PowerFx.Effect(Power.HideYield, VfxRole.Harvest, __instance.transform.position);
            }

            private static void Finalizer(DropState __state) {
                if (__state.Trophy != null) { __state.Trophy.m_chance = __state.Chance; }
            }
        }

        // The player who killed the creature these drops belong to, read on the creature's owner.
        private static Player Killer(CharacterDrop drops) => drops.m_character?.m_lastHit?.GetAttacker() as Player;

        private static bool IsSpoil(string prefab) {
            if (prefab.StartsWith("Trophy")) { return false; }
            foreach (string part in Spoils) {
                if (prefab.Contains(part)) { return true; }
            }
            return false;
        }

        // ---- The thrown spear ----

        private struct ThrowState {
            public bool Changed;
            public float Velocity;
            public float VelocityMin;
        }

        // Huntress's Spear: thrown spears fly faster.
        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        private static class Throw {
            private static void Prefix(Attack __instance, out ThrowState __state) {
                __state = default;
                if (__instance.m_character != Player.m_localPlayer || __instance.m_weapon == null) { return; }
                if (__instance.m_weapon.m_shared.m_skillType != Skills.SkillType.Spears || !PowerSet.Has(Power.ThrowSpeed, out float faster)) { return; }
                __state = new ThrowState() { Changed = true, Velocity = __instance.m_projectileVel, VelocityMin = __instance.m_projectileVelMin };
                __instance.m_projectileVel *= 1f + faster / 100f;
                __instance.m_projectileVelMin *= 1f + faster / 100f;
            }

            private static void Finalizer(Attack __instance, ThrowState __state) {
                if (!__state.Changed) { return; }
                __instance.m_projectileVel = __state.Velocity;
                __instance.m_projectileVelMin = __state.VelocityMin;
            }
        }

        // Returning Spear: a thrown spear comes back to the hand instead of landing.
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
        private static class Returning {
            private static void Prefix(Projectile __instance) {
                Player local = Player.m_localPlayer;
                ItemDrop.ItemData spear = __instance.m_spawnItem;
                if (local == null || __instance.m_owner != local || spear == null || !PowerSet.Has(Power.ReturningSpear)) { return; }
                if (spear.m_shared.m_skillType != Skills.SkillType.Spears) { return; }
                Inventory inventory = local.GetInventory();
                // A thrown stack only lost one; a single spear left the bag when it was thrown.
                if (inventory.ContainsItem(spear)) {
                    if (spear.m_stack >= spear.m_shared.m_maxStackSize) { return; }
                    spear.m_stack++;
                } else {
                    if (!inventory.CanAddItem(spear)) { return; }
                    inventory.AddItem(spear);
                }
                __instance.m_spawnItem = null;
                if (local.GetCurrentWeapon() == null || local.GetCurrentWeapon() == local.m_unarmedWeapon?.m_itemData) { local.EquipItem(spear); }
                PowerFx.Effect(Power.ReturningSpear, VfxRole.Proc, local.transform.position, 0.5f);
            }
        }

        // Winter Spear: where a thrown spear lands, ice bursts.
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        private static class IceBurst {
            private static void Postfix(Projectile __instance, Vector3 hitPoint) {
                if (__instance.m_owner != Player.m_localPlayer || __instance.m_skill != Skills.SkillType.Spears || !__instance.m_didHit) { return; }
                if (!PowerSet.Has(Power.WinterSpear, out float share)) { return; }
                float damage = __instance.m_damage.GetTotalDamage() * share / 100f;
                if (damage <= 0f) { return; }
                PowerFx.Effect(Power.WinterSpear, VfxRole.Burst, hitPoint, WinterSpearRadius / 5f, shared: true);
                foreach (Character enemy in PowerFx.EnemiesNear(hitPoint, WinterSpearRadius).ToArray()) {
                    PowerFx.Strike(enemy, new HitData.DamageTypes() { m_frost = damage }, hitPoint);
                }
            }
        }

        /// <summary>What a thrown spear's hit sets off: a pinned target, and the enemies in line behind it.</summary>
        internal static void OnThrownSpearHit(Character target, HitData hit, float damage) {
            if (PowerSet.Has(Power.PinningThrow, out float pin) && Random.value * 100f < pin) {
                target.GetSEMan().AddStatusEffect(PowerFx.Hash(PowerFx.Rooted), resetTime: true);
            }
            if (!PowerSet.Has(Power.SpearPierce)) { return; }
            Vector3 dir = hit.m_dir.sqrMagnitude > 0.01f ? hit.m_dir.normalized : target.transform.forward;
            dir.y = 0f;
            dir.Normalize();
            Vector3 from = target.transform.position;
            foreach (Character other in PowerFx.EnemiesNear(from + dir * (PierceLength / 2f), PierceLength / 2f + PierceHalfWidth, target).ToArray()) {
                Vector3 offset = other.transform.position - from;
                offset.y = 0f;
                float along = Vector3.Dot(offset, dir);
                if (along <= 0f || along > PierceLength || (offset - dir * along).magnitude > PierceHalfWidth) { continue; }
                PowerFx.Strike(other, new HitData.DamageTypes() { m_pierce = damage }, from);
            }
        }

        // The oath against blocking: the block button does nothing.
        [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        private static class NoBlocking {
            private static void Prefix(Player __instance, ref bool block, ref bool blockHold) {
                if (__instance != Player.m_localPlayer || !PowerSet.Has(Power.NoBlock)) { return; }
                block = false;
                blockHold = false;
            }
        }
    }
}
