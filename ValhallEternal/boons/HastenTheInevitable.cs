using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValhallEternal.common;
using ValhallEternal.modules;

namespace ValhallEternal.boons {
    internal static class HastenTheInevitable {

        // TODO: Add icon, description, text etc for the hasten deathbringer effect
        static int hastenDeathBringerEffectID = 0;

        internal static void AddHastenDeathStatus() {
            SE_Stats hastenDeathBringerEffect = new SE_Stats() { m_speedModifier = 1.3f };
            hastenDeathBringerEffect.name = "HastenDeathEffect";
            hastenDeathBringerEffect.m_name = "$ve_hasten_death_effect";
            hastenDeathBringerEffect.m_icon = DataObjects.hastenDeath;
            hastenDeathBringerEffect.m_ttl = 5f;
            CustomStatusEffect hastenDeathEffect = new CustomStatusEffect(hastenDeathBringerEffect, fixReference: false);  // We dont need to fix refs here, because no mocks were used
            ItemManager.Instance.AddStatusEffect(hastenDeathEffect);
            hastenDeathBringerEffectID = hastenDeathEffect.StatusEffect.NameHash();
        }


        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class HastenOnKill {
            private static void Prefix(Character __instance) {
                bool wasLastHit = __instance.m_lastHit != null && Player.m_localPlayer != null && __instance.m_lastHit.GetAttacker() == Player.m_localPlayer;
                if (wasLastHit && PlayerData.HasBoonWithValue(common.DataObjects.Boons.HastenTheInevitable, out float value)) {
                    //float ttl = 2 + ((10 + value) / 10f);
                    //hastenDeathBringerEffect.m_ttl = ttl;
                    //Player.m_localPlayer.status (hastenDeathBringerEffect);
                    Player.m_localPlayer.GetSEMan().AddStatusEffect(hastenDeathBringerEffectID, true);

                }
            }
        }
    }
}
