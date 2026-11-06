using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.powers;
using AesirLineage.souls;
using AesirLineage.starmap;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// Shrine quests: each deity's shrine asks the player to slay its biome's boss, the one
    /// <see cref="SoulBosses"/> maps to the deity. Each one done gives the deity points, and the first in a
    /// run opens the deity's blessing. A deity's next quest waits some in-game days after the last was taken.
    /// </summary>
    /// <remarks>
    /// A quest has no time limit and needs the boss killed after it was taken, by the player or near them
    /// (<see cref="Deeds"/> tells them). Each quest under way shows an icon with no timer, put back
    /// whenever it goes missing, as it does on death.
    /// </remarks>
    internal static class Quests {
        private static float nextIconCheck;
        private static readonly Dictionary<string, string> bossNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static RunState Run => PlayerData.Save.Run;

        /// <summary>The boss the deity's quests ask for, or null when the deity has none, and so no quest.</summary>
        internal static string BossFor(Deity deity) => SoulBosses.BossFor(deity);

        /// <summary>The boss the deity's quest under way asks for, or null when none is under way.</summary>
        internal static string UnderWay(Deity deity) => Run.QuestsUnderWay.TryGetValue(deity, out string boss) ? boss : null;

        /// <summary>In-game days until the deity's next quest can be taken; 0 when it can be now.</summary>
        internal static int DaysUntilNext(Deity deity) {
            if (!Run.QuestTaken.TryGetValue(deity, out DayStamp taken) || taken == null) { return 0; }
            DayStamp today = PlayerData.Today();
            // Another world counts its own days, so a quest taken there doesn't hold this one back. A day
            // before the one it was taken on means the world's time was turned back.
            if (today == null || today.World != taken.World || today.Day < taken.Day) { return 0; }
            return Math.Max(0, ValConfig.QuestCooldownDays.Value - (today.Day - taken.Day));
        }

        /// <summary>
        /// Whether the deity's blessing can be had this run: once one of its quests is done, or from the
        /// start when it has no quest.
        /// </summary>
        internal static bool BlessingOpen(Deity deity) => PlayerData.QuestsDone(deity) > 0 || BossFor(deity) == null;

        /// <summary>The boss's name as the game shows it, found from its defeat key or prefab name.</summary>
        internal static string BossName(string bossKey) {
            if (bossNames.TryGetValue(bossKey, out string name)) { return name; }
            if (ZNetScene.instance == null) { return bossKey; }
            Character boss = null;
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs) {
                Character character = prefab != null ? prefab.GetComponent<Character>() : null;
                if (character != null && (string.Equals(character.m_defeatSetGlobalKey, bossKey, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefab.name, bossKey, StringComparison.OrdinalIgnoreCase))) {
                    boss = character;
                    break;
                }
            }
            // A key no creature sets, as from a mod that isn't installed, is shown as written.
            name = boss != null ? Localization.instance.Localize(boss.m_name) : bossKey;
            bossNames[bossKey] = name;
            return name;
        }

        /// <summary>Takes the deity's quest, starting the wait for its next one.</summary>
        internal static void Take(Deity deity) {
            Player player = Player.m_localPlayer;
            string boss = BossFor(deity);
            if (player == null || boss == null) { return; }
            Run.QuestsUnderWay[deity] = boss;
            Run.QuestTaken[deity] = PlayerData.Today();
            PlayerData.SavePlayerConfiguration();
            ShowIcon(player, deity, boss);
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_quest_taken", Compendium.DeityName(deity), BossName(boss)));
        }

        /// <summary>Completes the deity's quest: its points, and its blessing opened if this is the run's first.</summary>
        internal static void Complete(Player player, Deity deity) {
            Run.QuestsUnderWay.Remove(deity);
            HideIcon(player, deity);
            PlayerData.CompleteQuest(deity);
            int points = ValConfig.QuestPoints.Value;
            if (points != 0) { PlayerData.AddPoints(deity, points); }
            DeityVfx.Share(deity, VfxRole.Burst, player.transform.position, 1f);
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$al_quest_done", Compendium.DeityName(deity), points.ToString()));
        }

        /// <summary>A boss the player took part in killing: completes every quest under way that asked for it.</summary>
        internal static void OnBossKilled(Player player, string bossKey) {
            List<Deity> done = Run.QuestsUnderWay.Where(quest => string.Equals(quest.Value, bossKey, StringComparison.OrdinalIgnoreCase)).Select(quest => quest.Key).ToList();
            foreach (Deity deity in done) { Complete(player, deity); }
        }

        /// <summary>Keeps an icon on the HUD for each quest under way, and none for any other, as after Ascension.</summary>
        internal static void Tick(Player player) {
            if (Time.time < nextIconCheck) { return; }
            nextIconCheck = Time.time + 1f;
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                string boss = UnderWay(deity);
                if (boss != null) {
                    ShowIcon(player, deity, boss);
                } else {
                    HideIcon(player, deity);
                }
            }
        }

        // ---- Icons ----

        private static string EffectName(Deity deity) => $"AL_Quest_{deity}";

        /// <summary>One display effect per deity, with its rune as the icon and no timer.</summary>
        internal static void RegisterStatusEffects() {
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                string rune = StarMaps.TryGet(deity, out StarMap map) ? map.Definition.Rune : null;
                PowerFx.Register(EffectName(deity), rune, 0f);
            }
        }

        private static void ShowIcon(Player player, Deity deity, string boss) {
            SEMan seman = player.GetSEMan();
            int hash = PowerFx.Hash(EffectName(deity));
            if (hash == 0 || seman == null) { return; }
            StatusEffect shown = seman.GetStatusEffect(hash) ?? seman.AddStatusEffect(hash);
            if (shown == null) { return; }
            shown.m_name = Localization.instance.Localize("$al_quest_icon", Compendium.DeityName(deity));
            shown.m_tooltip = Localization.instance.Localize("$al_quest_tooltip", BossName(boss));
        }

        private static void HideIcon(Player player, Deity deity) {
            SEMan seman = player?.GetSEMan();
            int hash = PowerFx.Hash(EffectName(deity));
            if (seman != null && seman.HaveStatusEffect(hash)) { seman.RemoveStatusEffect(hash, quiet: true); }
        }
    }
}
