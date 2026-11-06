using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.powers;
using AesirLineage.starmap;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// Boons: star map powers a deity lends for a while, as its shrine's blessing or the inspiration its
    /// dream leaves. Their powers join the allocated stars' in <see cref="PowerSet"/>, so a power works the
    /// same from a boon as from a star.
    /// </summary>
    /// <remarks>
    /// Each keeps its own clock in the save, counting play time only, so logging out pauses it and death
    /// doesn't end it. The status effect on the HUD is only for show, and is put back whenever it goes
    /// missing, as it does on death.
    /// </remarks>
    internal static class Boons {
        private static float nextIconCheck;

        internal static List<ActiveBoon> Active => PlayerData.Save.Boons;

        internal static int Minutes(BoonKind kind) => kind == BoonKind.Blessing ? ValConfig.BlessingMinutes.Value : ValConfig.InspirationMinutes.Value;

        /// <summary>The deity's blessing, or one of its inspirations, by id; null when the data has no such boon.</summary>
        internal static Boon Find(Deity deity, BoonKind kind, string id) {
            if (!ShrineData.All.TryGetValue(deity, out DeityShrine shrine) || shrine == null) { return null; }
            if (kind == BoonKind.Blessing) {
                return shrine.Blessing != null && shrine.Blessing.Id == id ? shrine.Blessing : null;
            }
            return shrine.Inspirations.Find(boon => boon.Id == id);
        }

        internal static string DisplayName(Boon boon) {
            if (string.IsNullOrEmpty(boon.Name)) { return Localization.instance.Localize($"$al_boon_{boon.Id}"); }
            return boon.Name.StartsWith("$") ? Localization.instance.Localize(boon.Name) : boon.Name;
        }

        /// <summary>What the boon lends, one power to a line.</summary>
        internal static string Describe(Boon boon) => string.Join("\n", boon.Grants.Select(grant => Compendium.DescribePower(grant.Key, grant.Value)));

        /// <summary>Seconds left on the deity's boon of this kind, or 0 when none is on.</summary>
        internal static float SecondsLeft(Deity deity, BoonKind kind) {
            ActiveBoon active = Active.Find(b => b.Deity == deity && b.Kind == kind);
            return active != null ? active.SecondsLeft : 0f;
        }

        /// <summary>Every active boon's powers, for <see cref="PowerSet"/>.</summary>
        internal static IEnumerable<KeyValuePair<Power, float>> Grants() {
            foreach (ActiveBoon active in Active) {
                Boon boon = Find(active.Deity, active.Kind, active.Id);
                if (boon == null) { continue; }
                foreach (KeyValuePair<Power, float> grant in boon.Grants) { yield return grant; }
            }
        }

        /// <summary>The names of the active boons lending a power, for the compendium.</summary>
        internal static IEnumerable<string> NamesGranting(Power power) {
            foreach (ActiveBoon active in Active) {
                Boon boon = Find(active.Deity, active.Kind, active.Id);
                if (boon != null && boon.Grants.Any(grant => grant.Key == power)) { yield return DisplayName(boon); }
            }
        }

        /// <summary>
        /// Starts a boon, or starts it over when it is already on. A deity has one blessing on at a time,
        /// and only one inspiration can be on at all, so a new one replaces the last.
        /// </summary>
        internal static void Start(Deity deity, BoonKind kind, Boon boon) {
            Player player = Player.m_localPlayer;
            if (player == null || boon == null) { return; }
            foreach (ActiveBoon replaced in Active.Where(b => b.Kind == kind && (kind == BoonKind.Inspiration || b.Deity == deity)).ToList()) {
                HideIcon(player, replaced);
                Active.Remove(replaced);
            }
            ActiveBoon started = new ActiveBoon() { Kind = kind, Deity = deity, Id = boon.Id, SecondsLeft = Minutes(kind) * 60f };
            Active.Add(started);
            PlayerData.SavePlayerConfiguration();
            PlayerData.Recompute();
            ShowIcon(player, started, boon);
            DeityVfx.Share(deity, VfxRole.LevelUp, player.transform.position, 1f);
            string message = kind == BoonKind.Blessing ? "$al_boon_blessed" : "$al_boon_inspired";
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(message, Compendium.DeityName(deity), DisplayName(boon)));
        }

        /// <summary>Ends every boon at once.</summary>
        internal static void EndAll() {
            Player player = Player.m_localPlayer;
            foreach (ActiveBoon active in Active) { HideIcon(player, active); }
            Active.Clear();
            PlayerData.SavePlayerConfiguration();
            PlayerData.Recompute();
        }

        /// <summary>Counts the clocks down, ends the boons whose time is up, and keeps their icons on the HUD.</summary>
        internal static void Tick(Player player) {
            if (Active.Count == 0) { return; }
            bool ended = false;
            for (int i = Active.Count - 1; i >= 0; i--) {
                ActiveBoon active = Active[i];
                active.SecondsLeft -= Time.deltaTime;
                if (active.SecondsLeft > 0f) { continue; }
                Active.RemoveAt(i);
                HideIcon(player, active);
                Boon boon = Find(active.Deity, active.Kind, active.Id);
                if (boon != null) { player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$al_boon_faded", DisplayName(boon))); }
                ended = true;
            }
            if (ended) {
                PlayerData.SavePlayerConfiguration();
                PlayerData.Recompute();
            }

            if (Time.time < nextIconCheck) { return; }
            nextIconCheck = Time.time + 1f;
            foreach (ActiveBoon active in Active) {
                // A boon the data no longer has, as on another server, lends nothing and shows nothing.
                Boon boon = Find(active.Deity, active.Kind, active.Id);
                if (boon != null) { ShowIcon(player, active, boon); }
            }
        }

        // ---- Icons ----

        private static string EffectName(BoonKind kind, Deity deity) => $"AL_{kind}_{deity}";

        /// <summary>One display effect per deity and kind, with the deity's rune as its icon.</summary>
        internal static void RegisterStatusEffects() {
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                string rune = StarMaps.TryGet(deity, out StarMap map) ? map.Definition.Rune : null;
                foreach (BoonKind kind in Enum.GetValues(typeof(BoonKind))) {
                    PowerFx.Register(EffectName(kind, deity), rune, Minutes(kind) * 60f);
                }
            }
        }

        // Puts the boon's icon on the HUD, or brings the one there in line with the boon's own clock. The
        // name and tooltip are the boon's, so one effect serves whichever boon of that deity is on.
        private static void ShowIcon(Player player, ActiveBoon active, Boon boon) {
            int hash = PowerFx.Hash(EffectName(active.Kind, active.Deity));
            SEMan seman = player.GetSEMan();
            if (hash == 0 || seman == null) { return; }
            StatusEffect shown = seman.GetStatusEffect(hash) ?? seman.AddStatusEffect(hash);
            if (shown == null) { return; }
            shown.m_ttl = shown.GetDuration() + active.SecondsLeft;
            shown.m_name = DisplayName(boon);
            shown.m_tooltip = Describe(boon);
        }

        private static void HideIcon(Player player, ActiveBoon active) {
            if (player == null) { return; }
            player.GetSEMan()?.RemoveStatusEffect(PowerFx.Hash(EffectName(active.Kind, active.Deity)), quiet: true);
        }

        // The game saves the character now and then, and on leaving. The clocks go into the save with it
        // rather than every frame.
        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        private static class SaveClocks {
            private static void Prefix(Player __instance) {
                if (__instance == Player.m_localPlayer && Active.Count > 0) { PlayerData.SavePlayerConfiguration(); }
            }
        }
    }
}
