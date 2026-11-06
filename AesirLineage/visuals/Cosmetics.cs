using System;
using System.Collections.Generic;
using System.Linq;
using AesirLineage.modules;
using AesirLineage.starmap;
using static AesirLineage.common.Deities;

namespace AesirLineage.visuals {
    /// <summary>Where a look shows on the player. Each deity has one look per slot.</summary>
    public enum CosmeticSlot {
        Aura,
        Wings,
        // Hidden: kept in code, but left out of Cosmetics.Slots, so no star offers it and no one shows it.
        Crown,
        Footsteps,
        WeaponGlow,
        KillEffect,
    }

    /// <summary>A look as shown: whose it is, and how far its wearer has Ascended with that deity.</summary>
    internal readonly struct Look {
        // The ZDO text comes from other clients; levels beyond this are clamped.
        internal const int MaxAscension = 10;

        public readonly Deity Deity;
        public readonly int Ascension;

        public Look(Deity deity, int ascension) {
            Deity = deity;
            Ascension = Math.Max(0, Math.Min(MaxAscension, ascension));
        }

        public bool Same(Look other) => Deity == other.Deity && Ascension == other.Ascension;
    }

    /// <summary>
    /// Which deity's look the local player shows in each slot. A look is available while the player holds
    /// a star that grants it (the node's <c>cosmetics</c> list), or for good once a devotion or Ascension
    /// unlocks it. With no choice made for a slot, the slot shows the available deity the player has given
    /// the most stars, so taking an aura star shows the aura at once.
    /// </summary>
    internal static class Cosmetics {
        internal const string None = "None";
        /// <summary>The slots in use; the hidden rune crown is not one.</summary>
        internal static readonly CosmeticSlot[] Slots = ((CosmeticSlot[])Enum.GetValues(typeof(CosmeticSlot)))
            .Where(slot => slot != CosmeticSlot.Crown).ToArray();

        /// <summary>Deities whose look for this slot the local player can show now, in deity order.</summary>
        internal static List<Deity> Available(CosmeticSlot slot) {
            HashSet<Deity> found = new HashSet<Deity>();
            foreach (StarNode node in PlayerData.AllAllocated()) {
                if (node.Looks.Contains(slot)) { found.Add(node.Deity); }
            }
            foreach (string entry in PlayerData.Save.UnlockedCosmetics) {
                if (TryParseUnlock(entry, out CosmeticSlot unlockedSlot, out Deity deity) && unlockedSlot == slot) { found.Add(deity); }
            }
            return ((Deity[])Enum.GetValues(typeof(Deity))).Where(found.Contains).ToList();
        }

        /// <summary>What the local player shows in a slot, or null for nothing.</summary>
        internal static Deity? Effective(CosmeticSlot slot) {
            List<Deity> available = Available(slot);
            if (available.Count == 0) { return null; }
            string chosen = PlayerData.CosmeticChoice(slot);
            if (chosen == None) { return null; }
            if (chosen != null && Enum.TryParse(chosen, out Deity deity) && available.Contains(deity)) { return deity; }
            // No choice, or the chosen look was given back with its star.
            return available.OrderByDescending(d => PlayerData.Allocated(d).Count).First();
        }

        /// <summary>
        /// The local player's looks as "Aura=Gefjun:2|Wings=Hel", the number being their Ascension with that
        /// deity (left off at 0), which other clients read off their ZDO.
        /// </summary>
        internal static string Pack() {
            List<string> parts = new List<string>();
            foreach (CosmeticSlot slot in Slots) {
                Deity? deity = Effective(slot);
                if (deity == null) { continue; }
                int ascension = Math.Min(Look.MaxAscension, PlayerData.AscensionLevel(deity.Value));
                parts.Add(ascension > 0 ? $"{slot}={deity.Value}:{ascension}" : $"{slot}={deity.Value}");
            }
            return string.Join("|", parts);
        }

        /// <summary>Reads another player's looks. Unknown slots or deities are skipped: the text comes from another client.</summary>
        internal static Dictionary<CosmeticSlot, Look> Unpack(string packed) {
            Dictionary<CosmeticSlot, Look> looks = new Dictionary<CosmeticSlot, Look>();
            if (string.IsNullOrEmpty(packed)) { return looks; }
            foreach (string part in packed.Split('|')) {
                int equals = part.IndexOf('=');
                if (equals <= 0) { continue; }
                string value = part.Substring(equals + 1);
                int colon = value.IndexOf(':');
                int ascension = 0;
                if (colon >= 0) {
                    if (!int.TryParse(value.Substring(colon + 1), out ascension)) { continue; }
                    value = value.Substring(0, colon);
                }
                if (Enum.TryParse(part.Substring(0, equals), out CosmeticSlot slot) && Enum.IsDefined(typeof(CosmeticSlot), slot)
                    && Enum.TryParse(value, out Deity deity) && Enum.IsDefined(typeof(Deity), deity)) {
                    looks[slot] = new Look(deity, ascension);
                }
            }
            return looks;
        }

        internal static string UnlockKey(CosmeticSlot slot, Deity deity) => $"{slot}:{deity}";

        internal static bool TryParseUnlock(string entry, out CosmeticSlot slot, out Deity deity) {
            slot = default;
            deity = default;
            int colon = entry?.IndexOf(':') ?? -1;
            return colon > 0 && Enum.TryParse(entry.Substring(0, colon), out slot) && Enum.TryParse(entry.Substring(colon + 1), out deity);
        }
    }
}
