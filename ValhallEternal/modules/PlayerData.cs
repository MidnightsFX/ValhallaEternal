using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.modules
{
    public static class PlayerData
    {
        public static int PlayerPrestigeLevel = 0;

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
