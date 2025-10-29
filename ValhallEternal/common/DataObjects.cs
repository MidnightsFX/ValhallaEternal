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

        public enum TextStyle
        {
            Bronze,
            Silver,
            Gold,
            Diamond,
            Mythic
        }

        public enum DisplayStyle
        {
            Numeric,
            Roman,
            Nordic
        }

        public enum Oaths
        {
            DamageTakenPerLevel
        }

        public enum Boons
        {
        
        }

        public class PlayerLevelConfiguration {
            public TextStyle TextColor;
            public DisplayStyle DisplayStyle;
            public int levelStart = 1;
            public int levelEnd = 10;
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
