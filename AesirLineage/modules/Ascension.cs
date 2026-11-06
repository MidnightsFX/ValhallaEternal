using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AesirLineage.common;
using AesirLineage.starmap;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.modules {
    /// <summary>
    /// Ascension: starting over in one deity's name, up to <c>MaxAscension</c> times each. Every level
    /// opens that deity's apex star of the same level, gives the deity's points, keeps more of the
    /// deity's looks for good and shows them in their ascended form, and lets every boss's Soul of Power
    /// be claimed again. The price is the deity's items (<see cref="AscensionTribute"/>) and the server's
    /// costs: skills, recipes, a return to the start and the boss keys.
    /// </summary>
    internal static class Ascension {
        // Looks a level keeps for good, on top of the levels before it: one per level, wings last.
        private static readonly CosmeticSlot[][] LooksByLevel = {
            new[] { CosmeticSlot.Aura },
            new[] { CosmeticSlot.Footsteps },
            new[] { CosmeticSlot.WeaponGlow },
            new[] { CosmeticSlot.KillEffect },
            new[] { CosmeticSlot.Wings },
        };

        internal static int Max => ValConfig.MaxAscension.Value;

        internal static bool CanAscend(Deity deity) => PlayerData.AscensionLevel(deity) < Max;

        /// <summary>The looks reaching this level keeps; none past the last listed level.</summary>
        internal static IReadOnlyList<CosmeticSlot> LooksAt(int level) {
            return level >= 1 && level <= LooksByLevel.Length ? LooksByLevel[level - 1] : new CosmeticSlot[0];
        }

        /// <summary>The deity's stars that need exactly this Ascension level.</summary>
        internal static List<StarNode> StarsOpenedAt(Deity deity, int level) {
            if (!StarMaps.TryGet(deity, out StarMap map)) { return new List<StarNode>(); }
            return map.Nodes.Values.Where(node => node.Definition.Ascension == level).ToList();
        }

        /// <summary>
        /// Ascends now. The shrine has already checked the keys and taken the items, and asked the player
        /// to confirm.
        /// </summary>
        internal static void Ascend(Player player, Deity deity, AscensionTribute tribute) {
            int level = PlayerData.AscensionLevel(deity) + 1;
            Logger.LogInfo($"Ascending with {deity} to level {level}.");

            // The price.
            if (ValConfig.AscensionRemovesBossKeys.Value && tribute.RequiredKeys != null) {
                foreach (string key in tribute.RequiredKeys) { RemoveKey(player, key); }
            }
            if (ValConfig.AscensionForgetsRecipes.Value) {
                player.m_knownMaterial.Clear();
                player.m_knownRecipes.Clear();
            }
            if (ValConfig.AscensionSkillLoss.Value > 0) {
                player.m_skills.LowerAllSkills(ValConfig.AscensionSkillLoss.Value / 100f);
            }

            // What it gives.
            PlayerData.SetAscension(deity, level);
            if (tribute.Points != 0) { PlayerData.AddPoints(deity, tribute.Points); }
            foreach (CosmeticSlot slot in LooksAt(level)) { PlayerData.UnlockCosmetic(slot, deity); }
            PlayerData.StartNewRun();
            PrestigeDisplays.UpdateLocalPlayerLevelDisplay();

            Vector3 at = player.transform.position;
            DeityVfx.Share(deity, VfxRole.Burst, at, 1.4f);
            DeityVfx.Share(deity, VfxRole.LevelUp, at, 1.6f);
            player.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$al_ascended", Compendium.DeityName(deity), level.ToString(), Max.ToString()));
            AscensionRagnarok.Ascended(player, deity, level);

            if (ValConfig.AscensionReturnsToStart.Value) { ReturnToStart(player); }
        }

        // Vanilla keeps boss kills in the world's global keys; VentureValheim's private-key mode keeps
        // them per player, and its dedicated-server mode needs the private copy removed here too.
        private static void RemoveKey(Player player, string key) {
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key)) {
                ZoneSystem.instance.RemoveGlobalKey(key);
            }
            player.PlayerRemoveUniqueKey(key);
            ProgressionCompat.TryRemovePrivateKey(key);
        }

        private static void ReturnToStart(Player player) {
            GameObject startTemple = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(obj => obj.name == "StartTemple");
            if (startTemple != null) {
                player.TeleportTo(startTemple.transform.position, startTemple.transform.rotation, false);
                return;
            }
            Logger.LogWarning("Start temple not found, sending the player to the spawn point instead.");
            Game.instance.FindSpawnPoint(out Vector3 point, out _, 0f);
            player.TeleportTo(point, Quaternion.identity, true);
        }
    }
}
