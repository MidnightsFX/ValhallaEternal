using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ValhallEternal.common
{
    public static class DataObjects {
        public static readonly string CustomLevelZKey = "VELevel";
        public static readonly string CustomDataKey = "VEOath";
        internal static JsonSerializer serializer = new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore};
        internal static JsonSerializer compactSerializer = new JsonSerializer() {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
            };
        internal static JsonSerializerSettings compactSerializationSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        public enum DisplayStyle
        {
            Numeric,
            Roman,
            Nordic
        }

        public enum Oaths
        {
            DamageTaken
        }

        public enum Boons
        {
        
        }

        public class LevelTextGradiant
        {
            public string TopLeft { get; set; }
            public string BottomLeft { get; set; }
            public string TopRight { get; set; }
            public string BottomRight { get; set; }
        }

        public class PlayerLevelConfiguration {
            public LevelTextGradiant TextColors { get; set; }
            public DisplayStyle DisplayStyle { get; set; }
            public int Level { get; set; }
            public Dictionary<Oaths, float> DifficultyOaths { get; set; }
            public Dictionary<Boons, float> DifficultyBoons { get; set; }
        }

        public class CompositePlayerConfig
        {
            public Dictionary<Oaths, float> TotalOaths { get; set; } = new Dictionary<Oaths, float>();
            public Dictionary<Boons, float> TotalBoons { get; set; } = new Dictionary<Boons, float>();
        }

        [Serializable]
        public class PlayerLevelData
        {
            [DataMember]
            public Dictionary<Oaths, float> PlayerOaths { get; set; }
            [DataMember]
            public Dictionary<Boons, float> PlayerBoons { get; set; }
        }
    }
}
