using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.modules
{
    public static class PlayerData
    {
        public static CompositePlayerConfig localPlayerConfig = new CompositePlayerConfig();

        public static void SetPlayerConfig(PlayerLevelConfiguration plc)
        {
            Dictionary<DataObjects.Oaths, float> totalOathValues = new Dictionary<DataObjects.Oaths, float>();
            Dictionary<DataObjects.Boons, float> totalBoonValues = new Dictionary<Boons, float>();

            // Add difficulty level configuration
            foreach (KeyValuePair<DataObjects.Oaths, float> kvp in plc.DifficultyOaths) {
                if (totalOathValues.ContainsKey(kvp.Key)) {
                    totalOathValues[kvp.Key] += kvp.Value;
                } else {
                    totalOathValues.Add(kvp.Key, kvp.Value);
                }
            }

            foreach (KeyValuePair<DataObjects.Boons, float> kvp in plc.DifficultyBoons) {
                if (totalBoonValues.ContainsKey(kvp.Key)) {
                    totalBoonValues[kvp.Key] += kvp.Value;
                } else {
                    totalBoonValues.Add(kvp.Key, kvp.Value);
                }
            }

            localPlayerConfig = new CompositePlayerConfig()
            {
                TotalOaths = totalOathValues,
                TotalBoons = totalBoonValues,
            };
        }

        public static void LoadPlayerData(Player player)
        {
            player.m_customData.ContainsKey(CustomDataKey);
        }

        public static string PackPlayerDataToString(PlayerLevelData playerData)
        {
            return JsonConvert.SerializeObject(playerData, compactSerializationSettings);
        }

        public static PlayerLevelData UnpackPlayerData(string packedPlayerData)
        {
            return JsonConvert.DeserializeObject<PlayerLevelData>(packedPlayerData);
        }
    }
}
