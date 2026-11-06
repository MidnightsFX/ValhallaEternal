using System.Collections.Generic;
using System.ComponentModel;

namespace AesirLineage.starmap
{
    public enum NodeTier
    {
        Root,
        Minor,
        Major,
        Oath,
        Apex,
    }

    /// <summary>One deity's constellation as written in StarMaps.yaml.</summary>
    public class StarMapDefinition
    {
        public string Rune { get; set; }
        public DeityPalette Palette { get; set; }
        public Dictionary<string, StarNodeDefinition> Nodes { get; set; } = new Dictionary<string, StarNodeDefinition>();
    }

    public class DeityPalette
    {
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Glow { get; set; }
    }

    /// <summary>
    /// One node. Position is polar around the deity at the centre: <see cref="Ring"/> counts outwards
    /// from 0, <see cref="Angle"/> is in degrees, 90 pointing up. Links are undirected, so each needs
    /// writing on one side only.
    /// </summary>
    public class StarNodeDefinition
    {
        /// <summary>Shown as written, or localized when it starts with '$'.</summary>
        public string Name { get; set; }
        // DefaultValue keeps the written file short: YamlDotNet leaves out values equal to it.
        [DefaultValue(NodeTier.Minor)]
        public NodeTier Tier { get; set; } = NodeTier.Minor;
        public float Ring { get; set; }
        public float Angle { get; set; }
        [DefaultValue(1)]
        public int Cost { get; set; } = 1;
        /// <summary>Ascension level with this deity needed before the node can be taken.</summary>
        public int Ascension { get; set; }
        public List<string> Links { get; set; } = new List<string>();
        /// <summary>Power name to value. Oaths may sit here or under <see cref="Oaths"/>; the split is for readers.</summary>
        public Dictionary<string, float> Powers { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> Oaths { get; set; } = new Dictionary<string, float>();
        /// <summary>
        /// Looks (cosmetic slot names: Aura, Wings, Footsteps, WeaponGlow, KillEffect) this deity offers
        /// the player while the node is allocated. Left out when there are none. Crown is hidden and ignored.
        /// </summary>
        public List<string> Cosmetics { get; set; }
    }
}
