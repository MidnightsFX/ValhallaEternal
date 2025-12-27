using HarmonyLib;
using ValhallEternal.modules;

namespace ValhallEternal.oaths
{
    internal static class ReduceCarryWeight
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        public static class ReduceCarryWeightOath
        {
            public static void Postfix(SEMan __instance, ref float limit)
            {
                if (__instance.m_character.IsPlayer() && PlayerData.localPlayerConfig.HasOath(common.DataObjects.Oaths.ReducePlayerCarryWeight, out float value))
                {
                    limit -= value;
                }
            }
        }
    }
}
