using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using AesirLineage.common;
using AesirLineage.modules;
using static AesirLineage.common.Deities;
using AesirLineage.visuals;

namespace AesirLineage.starmap
{
    /// <summary>A node resolved against its map: links are references and powers are parsed.</summary>
    internal sealed class StarNode
    {
        public readonly string Id;
        public readonly Deity Deity;
        public readonly StarNodeDefinition Definition;
        public readonly List<StarNode> Neighbours = new List<StarNode>();
        public readonly List<KeyValuePair<Power, float>> Grants = new List<KeyValuePair<Power, float>>();
        /// <summary>The cosmetic slots this node offers its deity's look for.</summary>
        public readonly List<CosmeticSlot> Looks = new List<CosmeticSlot>();

        public StarNode(string id, Deity deity, StarNodeDefinition definition)
        {
            Id = id;
            Deity = deity;
            Definition = definition;
        }

        public NodeTier Tier => Definition.Tier;
        public int Cost => Definition.Cost;

        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrEmpty(Definition.Name) ? Id : Definition.Name;
                return name.StartsWith("$") && Localization.instance != null ? Localization.instance.Localize(name) : name;
            }
        }
    }

    internal sealed class StarMap
    {
        public readonly Deity Deity;
        public readonly StarMapDefinition Definition;
        public readonly Dictionary<string, StarNode> Nodes = new Dictionary<string, StarNode>();
        public StarNode Root;

        public StarMap(Deity deity, StarMapDefinition definition)
        {
            Deity = deity;
            Definition = definition;
        }
    }

    /// <summary>
    /// The star maps in use. A server or solo game loads its own (built in, or StarMaps.yaml when
    /// UseStarMapsFile is on); a client uses whatever the server sends.
    /// </summary>
    internal static class StarMaps
    {
        private static Dictionary<Deity, StarMap> maps = new Dictionary<Deity, StarMap>();

        /// <summary>Goes up every time the maps are replaced.</summary>
        public static int Revision { get; private set; }

        /// <summary>The YAML of the maps in use, which the server sends to clients.</summary>
        public static string ActiveYaml { get; private set; } = "";

        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
            .Build();

        public static bool TryGet(Deity deity, out StarMap map) => maps.TryGetValue(deity, out map);

        public static IEnumerable<StarMap> All => maps.Values;

        internal static void Setup()
        {
            for (int i = 0; i < PowerInfo.Count; i++) {
                if (PowerInfo.All[i] == null) { throw new InvalidOperationException($"Power {(Power)i} has no PowerInfo entry."); }
            }
            LoadLocal();
        }

        /// <summary>Loads the maps this machine uses when nobody else decides: built in, or the file when enabled.</summary>
        internal static void LoadLocal()
        {
            if (ValConfig.UseStarMapsFile.Value) {
                try {
                    if (ApplyYaml(File.ReadAllText(ValConfig.starMapsCfgPath))) { return; }
                } catch (Exception e) {
                    Logger.LogWarning($"Could not read {ValConfig.starMapsCfgPath}, using the built-in star maps. {e.Message}");
                }
            }
            Apply(DefaultStarMaps.Build(), DefaultYaml());
        }

        internal static string DefaultYaml() => Serializer.Serialize(DefaultStarMaps.Build());

        /// <summary>Replaces the maps with parsed YAML. Leaves the current maps alone if it does not parse.</summary>
        internal static bool ApplyYaml(string yaml)
        {
            Dictionary<Deity, StarMapDefinition> definitions;
            try {
                definitions = DataObjects.yamldeserializer.Deserialize<Dictionary<Deity, StarMapDefinition>>(yaml);
            } catch (Exception ex) {
                Logger.LogError($"Failed to parse the star maps: {ex.Message}");
                return false;
            }
            if (definitions == null) {
                Logger.LogError("The star maps are empty.");
                return false;
            }
            Apply(definitions, yaml);
            return true;
        }

        private static void Apply(Dictionary<Deity, StarMapDefinition> definitions, string yaml)
        {
            Dictionary<Deity, StarMap> built = new Dictionary<Deity, StarMap>();
            foreach (KeyValuePair<Deity, StarMapDefinition> kvp in definitions) {
                StarMap map = Resolve(kvp.Key, kvp.Value);
                if (map != null) { built[kvp.Key] = map; }
            }
            maps = built;
            ActiveYaml = yaml;
            Revision++;
            Logger.LogDebug($"Loaded {built.Count} star maps (revision {Revision}).");
            PlayerData.OnDataChanged();
        }

        private static StarMap Resolve(Deity deity, StarMapDefinition definition)
        {
            if (definition?.Nodes == null || definition.Nodes.Count == 0) {
                Logger.LogWarning($"{deity}'s star map has no nodes and is skipped.");
                return null;
            }

            StarMap map = new StarMap(deity, definition);
            foreach (KeyValuePair<string, StarNodeDefinition> entry in definition.Nodes) {
                if (entry.Value == null || string.IsNullOrWhiteSpace(entry.Key)) { continue; }
                if (entry.Value.Cost < 0) {
                    Logger.LogWarning($"{deity} node '{entry.Key}' has a negative cost; using 0.");
                    entry.Value.Cost = 0;
                }
                StarNode node = new StarNode(entry.Key, deity, entry.Value);
                AddGrants(node, entry.Value.Powers);
                AddGrants(node, entry.Value.Oaths);
                AddLooks(node, entry.Value.Cosmetics);
                map.Nodes[entry.Key] = node;
                if (entry.Value.Tier == NodeTier.Root) {
                    if (map.Root != null) {
                        Logger.LogError($"{deity}'s star map has more than one root ('{map.Root.Id}', '{entry.Key}') and is skipped.");
                        return null;
                    }
                    map.Root = node;
                }
            }
            if (map.Root == null) {
                Logger.LogError($"{deity}'s star map has no root node and is skipped.");
                return null;
            }

            foreach (StarNode node in map.Nodes.Values) {
                if (node.Definition.Links == null) { continue; }
                foreach (string linkId in node.Definition.Links) {
                    if (!map.Nodes.TryGetValue(linkId, out StarNode other)) {
                        Logger.LogWarning($"{deity} node '{node.Id}' links to unknown node '{linkId}'.");
                        continue;
                    }
                    if (other == node || node.Neighbours.Contains(other)) { continue; }
                    node.Neighbours.Add(other);
                    other.Neighbours.Add(node);
                }
            }

            HashSet<StarNode> reached = new HashSet<StarNode>() { map.Root };
            Queue<StarNode> open = new Queue<StarNode>();
            open.Enqueue(map.Root);
            while (open.Count > 0) {
                foreach (StarNode next in open.Dequeue().Neighbours) {
                    if (reached.Add(next)) { open.Enqueue(next); }
                }
            }
            foreach (StarNode node in map.Nodes.Values) {
                if (!reached.Contains(node)) {
                    Logger.LogWarning($"{deity} node '{node.Id}' is not connected to the root and can never be taken.");
                }
            }
            return map;
        }

        private static void AddLooks(StarNode node, List<string> slots)
        {
            if (slots == null) { return; }
            foreach (string name in slots) {
                if (Enum.TryParse(name, ignoreCase: true, out CosmeticSlot slot) && Enum.IsDefined(typeof(CosmeticSlot), slot)) {
                    // A hidden look, like the rune crown in an older custom map, is left out quietly.
                    if (Array.IndexOf(Cosmetics.Slots, slot) >= 0 && !node.Looks.Contains(slot)) { node.Looks.Add(slot); }
                } else {
                    Logger.LogWarning($"{node.Deity} node '{node.Id}' offers unknown cosmetic '{name}'.");
                }
            }
        }

        private static void AddGrants(StarNode node, Dictionary<string, float> grants)
        {
            if (grants == null) { return; }
            foreach (KeyValuePair<string, float> grant in grants) {
                if (Enum.TryParse(grant.Key, ignoreCase: true, out Power power)) {
                    node.Grants.Add(new KeyValuePair<Power, float>(power, grant.Value));
                } else {
                    Logger.LogWarning($"{node.Deity} node '{node.Id}' grants unknown power '{grant.Key}'.");
                }
            }
        }
    }
}
