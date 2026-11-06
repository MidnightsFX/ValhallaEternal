using System.Collections.Generic;
using AesirLineage.visuals;
using static AesirLineage.common.Deities;

namespace AesirLineage.starmap
{
    /// <summary>
    /// Everything Aesir Lineage keeps on a character, stored as JSON in
    /// <c>Player.m_customData["AL_DATA"]</c> so it travels with the character between worlds.
    /// </summary>
    public class LineageSave
    {
        public int Version { get; set; } = 1;
        /// <summary>Total Ascensions across deities; shown next to the player's name.</summary>
        public int PrestigeLevel { get; set; }
        public Dictionary<Deity, DeityProgress> Deities { get; set; } = new Dictionary<Deity, DeityProgress>();
        /// <summary>The look chosen per slot: a deity's name, or "None". A slot left out shows the default.</summary>
        public Dictionary<CosmeticSlot, string> Cosmetics { get; set; } = new Dictionary<CosmeticSlot, string>();
        /// <summary>Looks unlocked for good, as "Slot:Deity": devotions now, Ascension later.</summary>
        public List<string> UnlockedCosmetics { get; set; } = new List<string>();
        /// <summary>Blessings and the dream's inspiration active now, with the time each has left.</summary>
        public List<ActiveBoon> Boons { get; set; } = new List<ActiveBoon>();
        /// <summary>What the character has done since they last Ascended. Ascension starts it over.</summary>
        public RunState Run { get; set; } = new RunState();
    }

    /// <summary>Everything that starts over when the character Ascends.</summary>
    public class RunState
    {
        /// <summary>Shrine quests done per deity. The first opens the deity's blessing.</summary>
        public Dictionary<Deity, int> QuestsDone { get; set; } = new Dictionary<Deity, int>();
        /// <summary>The boss each deity's quest under way asks for. A quest leaves here when it is done.</summary>
        public Dictionary<Deity, string> QuestsUnderWay { get; set; } = new Dictionary<Deity, string>();
        /// <summary>The day each deity's last quest was taken; the next one waits on it.</summary>
        public Dictionary<Deity, DayStamp> QuestTaken { get; set; } = new Dictionary<Deity, DayStamp>();
        /// <summary>The day each deity's blessing was last given.</summary>
        public Dictionary<Deity, DayStamp> LastBlessing { get; set; } = new Dictionary<Deity, DayStamp>();
        /// <summary>Star points each deity has given by chance, for starred kills, crafts and harvests.</summary>
        public Dictionary<Deity, int> LuckyPoints { get; set; } = new Dictionary<Deity, int>();
        /// <summary>The minibosses killed, by their entry in the Minibosses setting.</summary>
        public List<string> MinibossesKilled { get; set; } = new List<string>();
    }

    /// <summary>An in-game day. Each world counts its own days, so the world is kept with it.</summary>
    public class DayStamp
    {
        public long World { get; set; }
        public int Day { get; set; }
    }

    public enum BoonKind
    {
        /// <summary>From a shrine, one per deity at a time.</summary>
        Blessing,
        /// <summary>Left by a deity's dream, one at a time across every deity.</summary>
        Inspiration,
    }

    /// <summary>A boon the character has now. Its powers are looked up by id when they are needed.</summary>
    public class ActiveBoon
    {
        public BoonKind Kind { get; set; }
        public Deity Deity { get; set; }
        public string Id { get; set; }
        /// <summary>Seconds of play left; it doesn't count down while the character is out of the world.</summary>
        public float SecondsLeft { get; set; }
    }

    public class DeityProgress
    {
        /// <summary>Points earned with this deity. Spent points are worked out from the allocated nodes.</summary>
        public int Points { get; set; }
        public int Ascension { get; set; }
        public List<string> Allocated { get; set; } = new List<string>();
    }
}
