using HarmonyLib;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.oaths
{
    internal static class ReducePlayerHealthPercent
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        public static class ReducePlayerHealthStaminaEitrPercent
        {
            public static void Postfix(ref float stamina, ref float hp, ref float eitr)
            {
                if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.ReducePlayerHealthPercent, out float hpmod)) {
                    hp *= (1f - hpmod);
                }
                if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.ReducePlayerStaminaPercent, out float stammod))
                {
                    stamina *= (1f - stammod);
                }
                if (PlayerData.localPlayerConfig.HasOath(DataObjects.Oaths.ReducePlayerEitrPercent, out float eitrmod)) {
                    eitr *= (1f - eitrmod);
                }
            }
        }
    }
}
