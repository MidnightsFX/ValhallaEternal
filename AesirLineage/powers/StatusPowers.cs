using HarmonyLib;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    /// <summary>Immunities and shorter ailments, as the local player's status effects are added.</summary>
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short))]
    internal static class StatusPowers {
        // __state: whether the effect was already on. Rain re-adds Wet constantly to refresh it; only a
        // fresh Wet is shortened, or each refresh would shorten it again.
        private static bool Prefix(SEMan __instance, StatusEffect statusEffect, ref StatusEffect __result, out bool __state) {
            __state = true;
            if (statusEffect == null || __instance.m_character != Player.m_localPlayer) { return true; }
            int hash = statusEffect.NameHash();
            bool refused = (PowerSet.Has(Power.ColdImmunity) && (hash == SEMan.s_statusEffectCold || hash == SEMan.s_statusEffectFreezing))
                || (PowerSet.Has(Power.PoisonImmunity) && hash == SEMan.s_statusEffectPoison)
                || (hash == SEMan.s_statusEffectFreezing && WinterPowers.Sheltered(Player.m_localPlayer));
            if (refused) {
                __result = null;
                return false;
            }
            __state = __instance.HaveStatusEffect(hash);
            return true;
        }

        private static void Postfix(SEMan __instance, StatusEffect __result, bool __state) {
            if (__state || __result == null || __instance.m_character != Player.m_localPlayer) { return; }
            if (__result.NameHash() == SEMan.s_statusEffectWet && PowerSet.Has(Power.WetResist, out float wetResist)) {
                __result.m_ttl *= 1f - wetResist / 100f;
            }
            if (PowerSet.Has(Power.Unbound, out float unbound) && __result.m_ttl > 0f && Holds(__result)) {
                __result.m_ttl *= 1f - unbound / 100f;
            }
            WinterPowers.Endure(__result);
        }

        // Slip the Fetter: what slows or holds the player. Frost and tar by name, and any other timed
        // effect that takes speed away, such as webs.
        private static bool Holds(StatusEffect effect) {
            int hash = effect.NameHash();
            return hash == SEMan.s_statusEffectFrost || hash == SEMan.s_statusEffectTared || (effect is SE_Stats stats && stats.m_speedModifier < 0f);
        }
    }
}
