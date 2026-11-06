using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.modules;
using static AesirLineage.common.Deities;

namespace AesirLineage.visuals {
    /// <summary>
    /// Shows a player's looks: aura, wings and weapon glow as lasting parts, footprints and kill effects
    /// as they happen. On every client, for every player: the local player publishes their looks to
    /// their ZDO as a short string (<see cref="Cosmetics.Pack"/>), everyone else reads it there. A look
    /// worn by a player who has Ascended with its deity takes an ascended form, growing with each level:
    /// larger wings, more sparks and bigger kill effects. The rune crown is hidden but still handled here.
    /// </summary>
    internal sealed class PlayerVisuals : MonoBehaviour {
        private const float CheckEvery = 0.5f;
        // Valheim players face +Z; Piloto's wings are made for a character facing -Z, so they turn round.
        private static readonly Vector3 WingsOffset = new Vector3(0f, 1.32f, -0.22f);
        private static readonly Quaternion WingsTurn = Quaternion.Euler(0f, 180f, 0f);

        private Player player;
        private ZNetView nview;
        private string shownPacked;
        private int seenRevision = -1;
        private bool seenEnabled = true;
        private float nextCheck;
        private readonly Dictionary<CosmeticSlot, Look> shown = new Dictionary<CosmeticSlot, Look>();
        private readonly Dictionary<CosmeticSlot, GameObject> parts = new Dictionary<CosmeticSlot, GameObject>();
        private WeaponGlow weaponGlow;

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class AddToPlayers {
            private static void Postfix(Player __instance) {
                if (__instance.GetComponent<PlayerVisuals>() == null) { __instance.gameObject.AddComponent<PlayerVisuals>(); }
            }
        }

        internal bool TryGetLook(CosmeticSlot slot, out Look look) => shown.TryGetValue(slot, out look);

        /// <summary>The local player's kill effect, for everyone nearby to see.</summary>
        internal static void OnLocalKill(Character victim) {
            Player local = Player.m_localPlayer;
            if (local == null || victim == null || !local.TryGetComponent(out PlayerVisuals visuals)) { return; }
            if (visuals.TryGetLook(CosmeticSlot.KillEffect, out Look look)) {
                DeityVfx.Share(look.Deity, VfxRole.Kill, victim.GetCenterPoint(), 1f + 0.125f * look.Ascension);
            }
        }

        private void Awake() {
            player = GetComponent<Player>();
            nview = GetComponent<ZNetView>();
        }

        private void Update() {
            if (Time.time < nextCheck) { return; }
            nextCheck = Time.time + CheckEvery;
            ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            if (zdo == null) { return; }

            // With looks off (EnableLooks) nobody shows any. The server's setting reaches every client, so each
            // hides everyone's on its own; the local player also stops publishing theirs.
            bool enabled = ValConfig.EnableLooks.Value;
            string packed;
            if (player == Player.m_localPlayer) {
                // Looks change only with the lineage or the setting, so they are worked out again only then.
                if (PlayerData.Revision != seenRevision || enabled != seenEnabled) {
                    seenRevision = PlayerData.Revision;
                    seenEnabled = enabled;
                    packed = enabled ? Cosmetics.Pack() : "";
                    if (zdo.GetString(DataObjects.CosmeticsZKey, "") != packed) { zdo.Set(DataObjects.CosmeticsZKey, packed); }
                } else {
                    packed = shownPacked;
                }
            } else {
                packed = enabled ? zdo.GetString(DataObjects.CosmeticsZKey, "") : "";
            }
            if (packed != shownPacked) { Apply(packed); }
            weaponGlow?.Track();
        }

        private void Apply(string packed) {
            shownPacked = packed;
            Dictionary<CosmeticSlot, Look> wanted = Cosmetics.Unpack(packed);
            foreach (CosmeticSlot slot in Cosmetics.Slots) {
                bool had = shown.TryGetValue(slot, out Look before);
                bool has = wanted.TryGetValue(slot, out Look after);
                if (had && has && before.Same(after)) { continue; }
                Remove(slot);
                if (has) { Add(slot, after); }
            }
            shown.Clear();
            foreach (KeyValuePair<CosmeticSlot, Look> look in wanted) { shown[look.Key] = look.Value; }
        }

        private void Add(CosmeticSlot slot, Look look) {
            Deity deity = look.Deity;
            switch (slot) {
                case CosmeticSlot.Aura:
                    parts[slot] = DeityVfx.Attach(deity, VfxRole.Aura, transform, Vector3.zero, Quaternion.identity);
                    break;
                case CosmeticSlot.Wings: {
                    GameObject wings = DeityVfx.Attach(deity, VfxRole.Wings, transform, WingsOffset, WingsTurn);
                    if (wings != null) { wings.transform.localScale *= 1f + 0.04f * look.Ascension; }
                    parts[slot] = wings;
                    break;
                }
                case CosmeticSlot.Crown:
                    parts[slot] = RuneCrown.Create(deity, player, look.Ascension);
                    break;
                case CosmeticSlot.WeaponGlow:
                    weaponGlow = gameObject.AddComponent<WeaponGlow>();
                    weaponGlow.Deity = deity;
                    weaponGlow.Ascension = look.Ascension;
                    break;
                // Footsteps and kill effects happen as events.
            }
        }

        private void Remove(CosmeticSlot slot) {
            if (parts.TryGetValue(slot, out GameObject part)) {
                if (part != null) { Destroy(part); }
                parts.Remove(slot);
            }
            if (slot == CosmeticSlot.WeaponGlow && weaponGlow != null) {
                Destroy(weaponGlow);
                weaponGlow = null;
            }
        }

        private void OnDestroy() {
            foreach (GameObject part in parts.Values) {
                if (part != null) { Destroy(part); }
            }
        }

        // Every client gets each step through this RPC, so remote players leave footprints too.
        [HarmonyPatch(typeof(FootStep), nameof(FootStep.RPC_Step))]
        private static class Footprints {
            private static void Postfix(FootStep __instance, Vector3 point) {
                if (!__instance.TryGetComponent(out PlayerVisuals visuals) || !visuals.TryGetLook(CosmeticSlot.Footsteps, out Look look)) { return; }
                DeityVfx.Play(look.Deity, VfxRole.Footstep, point);
                Footprint.Leave(look.Deity, point, __instance.transform.forward);
            }
        }
    }
}
