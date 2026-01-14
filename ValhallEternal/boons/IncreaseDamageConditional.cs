using HarmonyLib;
using ValhallEternal.common;
using ValhallEternal.modules;
using static InventoryGrid;

namespace ValhallEternal.boons {
    internal static class IncreaseDamageConditional {

        [HarmonyPatch(typeof(Character))]
        public static class ModifyAttackDamages {

            static float perfectFormCharge = 0f;

            [HarmonyPrefix]
            [HarmonyPatch(nameof(Character.RPC_Damage))]
            public static void ModifyAttackDamage(Character __instance, HitData hit) {
                Character attacker = hit.GetAttacker();
                // For effects where the player is damaging something else
                if (Player.m_localPlayer != null && attacker as Player == Player.m_localPlayer) {
                    if (__instance.m_faction == Character.Faction.Undead && PlayerData.HasBoonWithValue(DataObjects.Boons.DamageAgainstUndead, out float damageAgainstUndead)) {
                        float modifier = 1f + ((100 + damageAgainstUndead) / 100);
                        hit.m_damage.Modify(modifier);
                        Logger.LogDebug($"[IncreaseDamageAgainstUndead] modifying hit by x{modifier}");
                    }
                    if (EnvMan.IsNight() && PlayerData.HasBoonWithValue(DataObjects.Boons.StormboundRage, out float stormRageValue)) {
                        float addedLightning = (hit.m_damage.GetTotalDamageNoHarvestValues() * ((100f + stormRageValue) / 100f));
                        hit.m_damage.m_lightning += addedLightning;
                        Logger.LogDebug($"[StormboundRage] adding {addedLightning} lightning damage.");
                    }
                    if (PlayerData.HasBoonWithValue(DataObjects.Boons.DedicationToTheBlade, out float meleeDedication)) {
                        if (hit.m_ranged == false) {
                            float modifier = 1f + ((100 + meleeDedication) / 100);
                            hit.m_damage.Modify(modifier);
                            Logger.LogDebug($"[DedicationToTheBlade] modifying hit by x{modifier}.");
                        } else {
                            float modifier = ((100 - meleeDedication*2) / 100);
                            if (modifier <0) { modifier = 0; }
                            hit.m_damage.Modify(modifier);
                            Logger.LogDebug($"[DedicationToTheBlade] modifying hit by x{modifier}.");
                        }
                    }
                    if (PlayerData.HasBoonWithValue(DataObjects.Boons.PerfectForm, out float perfectForm)) {
                        float modifier = 1f + ((100 + perfectForm) / 100);
                        perfectFormCharge += hit.GetTotalDamage() * modifier;
                        // TODO use a config value for the charge required for this
                        if (perfectFormCharge > 1000) {
                            float critmod = 2 + ((12 + perfectForm) / 12);
                            hit.m_damage.Modify(critmod);
                            perfectFormCharge = 0;
                            Logger.LogDebug($"[DedicationToTheBlade] modifying hit by x{modifier}.");
                        }
                    }
                    
                } else if (__instance == Player.m_localPlayer) {
                    // For effects where the player is taking damage
                    if (__instance.IsStaggering() && PlayerData.HasBoonWithValue(DataObjects.Boons.Everwatchful, out float everwatch)) {
                        float modifier = ((100 - everwatch * 2) / 100);
                        if (modifier < 0) { modifier = 0; }
                        hit.m_damage.Modify(modifier);
                        Logger.LogDebug($"[Everwatchful] modifying hit by x{modifier}.");
                    }
                    if (PlayerData.HasBoonWithValue(DataObjects.Boons.BalanceOfTheJotunn, out float bofJotunn)) {
                        Player.m_localPlayer.AddStamina(bofJotunn);
                        Logger.LogDebug($"[BalanceOfTheJotunn] providing stamina {bofJotunn}.");
                    }
                    if (PlayerData.HasBoonWithValue(DataObjects.Boons.BalanceOfTheAesir, out float bofAesir)) {
                        Player.m_localPlayer.AddEitr(bofAesir);
                        Logger.LogDebug($"[BalanceOfTheAesir] providing eitr {bofJotunn}.");
                    }
                }
                
            }
        }
    }
}
