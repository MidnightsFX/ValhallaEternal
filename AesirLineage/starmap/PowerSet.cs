using System;
using System.Collections.Generic;

namespace AesirLineage.starmap
{
    /// <summary>
    /// The local player's powers: every allocated node's grants and every active boon's, summed and
    /// capped. Rebuilt only when the allocation, the maps or the boons change, so the game hooks read a
    /// plain array.
    /// </summary>
    internal static class PowerSet
    {
        private static readonly float[] totals = new float[PowerInfo.Count];
        private static readonly float[] dormant = new float[PowerInfo.Count];
        private static readonly List<StarNode>[] sources = NewSources();
        // The active boons' grants, kept so previews of other allocations include them too.
        private static readonly List<KeyValuePair<Power, float>> boonGrants = new List<KeyValuePair<Power, float>>();

        /// <summary>Goes up on every rebuild.</summary>
        public static int Version { get; private set; }

        public static float Get(Power power) => totals[(int)power];

        public static bool Has(Power power) => totals[(int)power] > 0f;

        public static bool Has(Power power, out float value)
        {
            value = totals[(int)power];
            return value > 0f;
        }

        /// <summary>What minors have fed a mechanic that no major, oath-bound or apex node has switched on yet.</summary>
        public static float Dormant(Power power) => dormant[(int)power];

        /// <summary>The allocated nodes granting a power, in allocation order.</summary>
        public static IReadOnlyList<StarNode> SourcesOf(Power power) => sources[(int)power];

        /// <param name="boons">The active boons' grants. Left out, the player has none.</param>
        internal static void Rebuild(IEnumerable<StarNode> allocated, IEnumerable<KeyValuePair<Power, float>> boons = null)
        {
            foreach (List<StarNode> list in sources) { list.Clear(); }
            boonGrants.Clear();
            if (boons != null) { boonGrants.AddRange(boons); }
            Compute(allocated, totals, dormant, sources);
            Version++;
        }

        /// <summary>
        /// What the powers would be with these nodes and today's boons, without changing the live ones.
        /// Used by the star map's tooltips to show a node's effect before it is taken or given back.
        /// </summary>
        internal static void Preview(IEnumerable<StarNode> allocated, float[] previewTotals, float[] previewDormant)
        {
            Compute(allocated, previewTotals, previewDormant, null);
        }

        private static void Compute(IEnumerable<StarNode> allocated, float[] outTotals, float[] outDormant, List<StarNode>[] outSources)
        {
            float[] raw = new float[PowerInfo.Count];
            bool[] switchedOn = new bool[PowerInfo.Count];

            foreach (StarNode node in allocated) {
                foreach (KeyValuePair<Power, float> grant in node.Grants) {
                    int i = (int)grant.Key;
                    raw[i] += grant.Value;
                    if (outSources != null && !outSources[i].Contains(node)) { outSources[i].Add(node); }
                    if (node.Tier != NodeTier.Minor && node.Tier != NodeTier.Root) { switchedOn[i] = true; }
                }
            }
            // A boon lends its powers whole, as a major star does, so its mechanics work from it alone.
            foreach (KeyValuePair<Power, float> grant in boonGrants) {
                raw[(int)grant.Key] += grant.Value;
                switchedOn[(int)grant.Key] = true;
            }

            // Bound Word: the oaths carried are lighter. Worked out first, as it changes the others.
            int ease = (int)Power.OathEase;
            PowerInfo easeInfo = PowerInfo.All[ease];
            float oathKept = switchedOn[ease] ? 1f - Math.Max(0f, Math.Min(raw[ease], easeInfo.Cap)) / 100f : 1f;

            for (int i = 0; i < PowerInfo.Count; i++) {
                PowerInfo info = PowerInfo.All[i];
                float value = raw[i];
                outDormant[i] = 0f;
                if (info.Kind == PowerKind.Mechanic && !switchedOn[i]) {
                    outDormant[i] = Math.Max(0f, value);
                    value = 0f;
                }
                if (info.Kind == PowerKind.Flag) { value = value > 0f ? 1f : 0f; }
                if (info.IsOath && info.Kind != PowerKind.Flag) { value *= oathKept; }
                outTotals[i] = Math.Max(0f, Math.Min(value, info.Cap));
            }
        }

        private static List<StarNode>[] NewSources()
        {
            List<StarNode>[] lists = new List<StarNode>[PowerInfo.Count];
            for (int i = 0; i < lists.Length; i++) { lists[i] = new List<StarNode>(); }
            return lists;
        }
    }
}
