using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;
using static ValhallEternal.common.DataObjects;

namespace ValhallEternal.modules {
    public static class PlayerData {
        public static CompositePlayerConfig localPlayerConfig = new CompositePlayerConfig();

        [HarmonyPatch(typeof(Player))]
        public static class LoadPlayerBoonOaths {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(Player.Load))]
            static void Postfix(Player __instance) {
                LoadPlayerConfiguration(__instance);
            }
        }

        public static void SetPlayerConfig(PlayerLevelConfiguration plc, PlayerLevelData pld) {
            Dictionary<DataObjects.Oaths, float> totalOathValues = new Dictionary<DataObjects.Oaths, float>();
            Dictionary<DataObjects.Boons, float> totalBoonValues = new Dictionary<Boons, float>();

            // Add difficulty level configuration
            if (plc.DifficultyOaths != null) {
                foreach (KeyValuePair<DataObjects.Oaths, float> kvp in plc.DifficultyOaths) {
                    if (totalOathValues.ContainsKey(kvp.Key)) {
                        totalOathValues[kvp.Key] += kvp.Value;
                    } else {
                        totalOathValues.Add(kvp.Key, kvp.Value);
                    }
                }
            }


            if (plc.DifficultyBoons != null) {
                foreach (KeyValuePair<DataObjects.Boons, float> kvp in plc.DifficultyBoons) {
                    if (totalBoonValues.ContainsKey(kvp.Key)) {
                        totalBoonValues[kvp.Key] += kvp.Value;
                    } else {
                        totalBoonValues.Add(kvp.Key, kvp.Value);
                    }
                }
            }


            // Add player saved configuration
            if (pld.PlayerOaths != null) {
                foreach (KeyValuePair<DataObjects.Oaths, float> kvp in pld.PlayerOaths) {
                    if (totalOathValues.ContainsKey(kvp.Key)) {
                        totalOathValues[kvp.Key] += kvp.Value;
                    } else {
                        totalOathValues.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            if (pld.PlayerBoons != null) {
                foreach (KeyValuePair<DataObjects.Boons, float> kvp in pld.PlayerBoons) {
                    if (totalBoonValues.ContainsKey(kvp.Key)) {
                        totalBoonValues[kvp.Key] += kvp.Value;
                    } else {
                        totalBoonValues.Add(kvp.Key, kvp.Value);
                    }
                }
            }


            // Check if the player has a deal reduced damage oath
            bool DealReducedDamageActive = false;
            foreach (DataObjects.Oaths oath in DamageReductionOaths) {
                if (totalOathValues.ContainsKey(oath)) {
                    DealReducedDamageActive = true;
                    break;
                }
            }

            // Check if the player should have lower skill gains
            bool ReduceSkillGainsActive = false;
            foreach (DataObjects.Oaths oath in ReducedSkillGainOaths) {
                if (totalOathValues.ContainsKey(oath)) {
                    ReduceSkillGainsActive = true;
                    break;
                }
            }

            localPlayerConfig = new CompositePlayerConfig() {
                ReduceSkillGainsActive = ReduceSkillGainsActive,
                DealReductedDamageActive = DealReducedDamageActive,
                TotalOaths = totalOathValues,
                TotalBoons = totalBoonValues,
            };
            if (pld != null) {
                Logger.LogDebug($"Player Presitge Level: {pld.PlayerLevel}");
            }
            Logger.LogDebug($"Set player config: Oaths-{localPlayerConfig.TotalOaths.Count}, Boons-{localPlayerConfig.TotalBoons.Count}");
            foreach (KeyValuePair<DataObjects.Oaths, float> kvp in localPlayerConfig.TotalOaths) {
                Logger.LogDebug($" - Oath: {kvp.Key} Value: {kvp.Value}");
            }
            foreach (KeyValuePair<DataObjects.Boons, float> kvp in localPlayerConfig.TotalBoons) {
                Logger.LogDebug($" - Boon: {kvp.Key} Value: {kvp.Value}");
            }
        }

        public static bool HasBoon(Boons boon) {
            if (localPlayerConfig.TotalBoons == null) { return false; }
            if (localPlayerConfig.TotalBoons.Keys.Contains(boon)) { return true; }
            return false;
        }

        public static bool HasBoonWithValue(Boons boon, out float value) {
            value = 0f;
            if (localPlayerConfig.TotalBoons == null) { return false; }
            if (localPlayerConfig.TotalBoons.Keys.Contains(boon)) {
                value = localPlayerConfig.TotalBoons[boon];
                return true;
            }
            return false;
        }

        public static void AddOathToPlayerConfig(DataObjects.Oaths oath, float value) {
            if (localPlayerConfig.TotalOaths.ContainsKey(oath)) {
                localPlayerConfig.TotalOaths[oath] += value;
            } else {
                localPlayerConfig.TotalOaths.Add(oath, value);
            }
        }

        public static void AddBoonToPlayerConfig(DataObjects.Boons boon, float value) {
            if (localPlayerConfig.TotalBoons.ContainsKey(boon)) {
                localPlayerConfig.TotalBoons[boon] += value;
            } else {
                localPlayerConfig.TotalBoons.Add(boon, value);
            }
        }

        public static void SavePlayerConfiguration() {
            if (Player.m_localPlayer == null) {
                Logger.LogWarning("Cannot save player configuration, local player is null.");
                return;
            }
            PlayerLevelData playerData = new PlayerLevelData() {
                PlayerLevel = localPlayerConfig.PlayerLevel,
                PlayerOaths = localPlayerConfig.TotalOaths,
                PlayerBoons = localPlayerConfig.TotalBoons,
            };
            string packedData = PackPlayerDataToString(playerData);
            if (Player.m_localPlayer.m_customData.ContainsKey(CustomDataKey)) {
                Player.m_localPlayer.m_customData[CustomDataKey] = packedData;
            } else {
                Player.m_localPlayer.m_customData.Add(CustomDataKey, packedData);
            }
        }


        public static void LoadPlayerConfiguration(Player player) {
            if (player.m_customData.ContainsKey(CustomDataKey)) {
                Logger.LogDebug("Saved player data found, loading.");
                PlayerLevelData pld = UnpackPlayerData(player.m_customData[CustomDataKey]);
                PlayerLevelConfiguration plc = PrestigeLevelConfigData.GetPlayerLevelConfiguration(pld.PlayerLevel);
                SetPlayerConfig(plc, pld);
                PrestigeDisplays.UpdateLocalPlayerLevelDisplay();
            } else {
                Logger.LogDebug("No saved player saved data found.");
            }
        }

        public static string PackPlayerDataToString(PlayerLevelData playerData) {
            return JsonConvert.SerializeObject(playerData, compactSerializationSettings);
        }

        public static PlayerLevelData UnpackPlayerData(string packedPlayerData) {
            return JsonConvert.DeserializeObject<PlayerLevelData>(packedPlayerData);
        }
    }
}
