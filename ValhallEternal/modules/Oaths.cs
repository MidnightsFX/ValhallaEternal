using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;

namespace ValhallEternal.modules
{
    public static class Oaths
    {
        public static Dictionary<DataObjects.Oaths, float> ActiveOaths = new Dictionary<DataObjects.Oaths, float>() { };

        public static class MultiplayerDamageMod
        {
            [HarmonyPatch(typeof(Game), nameof(Game.GetDifficultyDamageScalePlayer))]
            public static class EnemyDamageScalingIncrease
            {
                public static void Postfix(ref float __result)
                {
                    if (Player.m_localPlayer != null && ActiveOaths.ContainsKey(DataObjects.Oaths.DamageTakenPerLevel))
                    {
                        float extra_damagetaken_percent = ActiveOaths[DataObjects.Oaths.DamageTakenPerLevel] * PlayerData.PlayerPrestigeLevel;
                        Logger.LogDebug($"Oath of Damage Taken: oath:{ActiveOaths[DataObjects.Oaths.DamageTakenPerLevel]} * level {PlayerData.PlayerPrestigeLevel} = x{extra_damagetaken_percent}");
                        __result += extra_damagetaken_percent;
                    }
                }
            }
        }
    }
}
