using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
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

        public static void SetPlayerConfig(PlayerLevelData pld) {
            Dictionary<DataObjects.Oaths, float> totalOathValues = new Dictionary<DataObjects.Oaths, float>();
            Dictionary<DataObjects.Boons, float> totalBoonValues = new Dictionary<Boons, float>();

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
                ActiveEffectsForPlayer = pld.ActiveEffectsForPlayer,
                AvailableEffectsForPlayer = pld.AvailableEffectsForPlayer,
            };

            // Ensure that "None" option is always available for visual effects, so that players can opt-out of visual changes
            if (localPlayerConfig.AvailableEffectsForPlayer != null) {
                foreach (KeyValuePair<PrestigeEffect, List<string>> kvp in localPlayerConfig.AvailableEffectsForPlayer) {
                    if (kvp.Value.Contains(DataObjects.None) == false) {
                        kvp.Value.Insert(0, DataObjects.None);
                    }
                }
            }

            if (pld != null) {
                Logger.LogDebug($"Player Presitge Level: {pld.PlayerLevel}");
                localPlayerConfig.PlayerLevel = pld.PlayerLevel;
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

        public static bool HasOathWithValue(Oaths oath, out float value) {
            value = 0f;
            if (localPlayerConfig.TotalOaths == null) { return false; }
            if (localPlayerConfig.TotalOaths.Keys.Contains(oath)) {
                value = localPlayerConfig.TotalOaths[oath];
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

        public static void AddVisualPrestigeEffectOptionToPlayerConfig(PrestigeEffect effectType, string effectKey) {
            if (localPlayerConfig.AvailableEffectsForPlayer != null) {
                if (localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(effectType) && localPlayerConfig.AvailableEffectsForPlayer[effectType].Contains(effectKey) == false) {
                    localPlayerConfig.AvailableEffectsForPlayer[effectType].Add(effectKey);
                } else if (!localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(effectType)) {
                    localPlayerConfig.AvailableEffectsForPlayer[effectType] = new List<string>() { effectKey };
                }
            } else {
                localPlayerConfig.AvailableEffectsForPlayer = new Dictionary<PrestigeEffect, List<string>>() {
                    { effectType, new List<string>() { DataObjects.None, effectKey } },
                };
            }

        }

        public static void SetActivePrestigeEffectForPlayer(PrestigeEffect effectType, string effectKey) {
            if (localPlayerConfig.ActiveEffectsForPlayer != null) {
                if (localPlayerConfig.ActiveEffectsForPlayer.ContainsKey(effectType)) {
                    localPlayerConfig.ActiveEffectsForPlayer[effectType] = effectKey;
                } else {
                    localPlayerConfig.ActiveEffectsForPlayer.Add(effectType, effectKey);
                }
            } else {
                localPlayerConfig.ActiveEffectsForPlayer = new Dictionary<PrestigeEffect, string>() {
                    { effectType, effectKey }
                };
            }

        }

        public static bool PlayerHasAnyPrestigeEffect() {
            if (localPlayerConfig.AvailableEffectsForPlayer != null) {
                foreach(var entry in localPlayerConfig.AvailableEffectsForPlayer) {
                    if (entry.Value.Count > 1) { return true; }
                }
            }
            return false;
        }

        public static bool PlayerHasPrestigeEffect(PrestigeEffect type, string name) {
            if (localPlayerConfig.AvailableEffectsForPlayer != null && localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(type) && localPlayerConfig.AvailableEffectsForPlayer[type].Contains(name) == false) {
                return true;
            }
            return false;
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
                ActiveEffectsForPlayer = localPlayerConfig.ActiveEffectsForPlayer,
                AvailableEffectsForPlayer = localPlayerConfig.AvailableEffectsForPlayer,
            };
            string packedData = PackPlayerDataToString(playerData);
            if (Player.m_localPlayer.m_customData.ContainsKey(CustomDataKey)) {
                Player.m_localPlayer.m_customData[CustomDataKey] = packedData;
            } else {
                Player.m_localPlayer.m_customData.Add(CustomDataKey, packedData);
            }
            Player.m_localPlayer.m_nview.GetZDO().Set(CustomLevelZKey, playerData.PlayerLevel);
            WritePrestigeEffectsToZDO();
        }

        public static string PackActiveEffectsForZDO(Dictionary<PrestigeEffect, string> active) {
            if (active == null || active.Count == 0) { return ""; }
            List<string> parts = new List<string>(active.Count);
            foreach (KeyValuePair<PrestigeEffect, string> kvp in active) {
                if (string.IsNullOrEmpty(kvp.Value) || kvp.Value == DataObjects.None) { continue; }
                parts.Add($"{kvp.Key}={kvp.Value}");
            }
            return string.Join("|", parts);
        }

        public static Dictionary<PrestigeEffect, string> UnpackActiveEffectsFromZDO(string packed) {
            Dictionary<PrestigeEffect, string> result = new Dictionary<PrestigeEffect, string>();
            if (string.IsNullOrEmpty(packed)) { return result; }
            foreach (string token in packed.Split('|')) {
                int eq = token.IndexOf('=');
                if (eq <= 0) { continue; }
                if (System.Enum.TryParse(token.Substring(0, eq), out PrestigeEffect fx)) {
                    result[fx] = token.Substring(eq + 1);
                }
            }
            return result;
        }

        public static void WritePrestigeEffectsToZDO() {
            if (Player.m_localPlayer == null || Player.m_localPlayer.m_nview == null) { return; }
            ZDO zdo = Player.m_localPlayer.m_nview.GetZDO();
            if (zdo == null) { return; }
            PrestigeEffectsDictionaryZNetProperty storedEffects = new PrestigeEffectsDictionaryZNetProperty(CustomPrestigeFxZKey, Player.m_localPlayer.m_nview, null);
            storedEffects.ForceSet(localPlayerConfig.ActiveEffectsForPlayer);
        }

        public static List<Dropdown.OptionData> ListPlayerAvailablePrestigeEffect(PrestigeEffect effect = PrestigeEffect.Wings) {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

            if (localPlayerConfig.AvailableEffectsForPlayer != null && localPlayerConfig.AvailableEffectsForPlayer.ContainsKey(effect)) {
                foreach (string effectKey in localPlayerConfig.AvailableEffectsForPlayer[effect]) {
                    options.Add(new Dropdown.OptionData(effectKey));
                }
            }

            return options;
        }

        public static string GetActivePrestigeEffectForType(PrestigeEffect type = PrestigeEffect.Wings) {
            if (localPlayerConfig.ActiveEffectsForPlayer != null && localPlayerConfig.ActiveEffectsForPlayer.ContainsKey(type)) {
                return localPlayerConfig.ActiveEffectsForPlayer[type];
            }
            return null;
        }


        public static void LoadPlayerConfiguration(Player player) {
            if (player.m_customData.ContainsKey(CustomDataKey)) {
                Logger.LogDebug("Saved player data found, loading.");
                PlayerLevelData pld = UnpackPlayerData(player.m_customData[CustomDataKey]);
                //PlayerLevelConfiguration plc = PrestigeLevelConfigData.GetPlayerLevelConfiguration(pld.PlayerLevel);
                SetPlayerConfig(pld);
                PrestigeDisplays.UpdateLocalPlayerLevelDisplay(pld.PlayerLevel);
                // Set ZValue when loading from customdata
                if (pld.PlayerLevel != 0 && player.m_nview.GetZDO().GetInt(DataObjects.CustomLevelZKey, 0) == 0) {
                    player.m_nview.GetZDO().Set(DataObjects.CustomLevelZKey, pld.PlayerLevel);
                }

                if (pld.ActiveEffectsForPlayer != null) {
                    foreach (KeyValuePair<PrestigeEffect, string> kvp in pld.ActiveEffectsForPlayer) {
                        Logger.LogDebug($"Setting up player {kvp.Key} display: {kvp.Value}");
                        switch (kvp.Key) {
                            case PrestigeEffect.Wings:
                                PrestigeDisplays.SetupPlayerWingsDisplay(kvp.Value);
                                break;
                            case PrestigeEffect.Aura:
                                PrestigeDisplays.SetupPlayerAuraDisplay(kvp.Value);
                                break;
                        }
                    }
                }

                WritePrestigeEffectsToZDO();

                // Update player boon summary
                Compendium.UpdateDietyPrestigeExplanations();

            } else {
                Logger.LogDebug("No saved player saved data found.");
            }
        }

        public static string PackPlayerDataToString(PlayerLevelData playerData) {
            return DataObjects.yamlserializerJsonCompat.Serialize(playerData);
        }

        public static PlayerLevelData UnpackPlayerData(string packedPlayerData) {
            return DataObjects.yamldeserializer.Deserialize<PlayerLevelData>(packedPlayerData);
        }
    }
}
