using HarmonyLib;
using UnityEngine;
using AesirLineage.starmap;
using AesirLineage.visuals;

namespace AesirLineage.powers {
    /// <summary>
    /// Freyja's Cauldron: meads that last longer and do more, come off cooldown sooner, are sometimes not
    /// used up, give more from the fermenter, and leave a burst of strength behind.
    /// </summary>
    internal static class MeadPowers {
        private const float ElixirSeconds = 10f;

        // The mead being drunk, while vanilla adds its effect.
        private static ItemDrop.ItemData drinking;
        private static float elixirUntil;
        private static readonly System.Collections.Generic.Dictionary<ZDOID, long> tapped = new System.Collections.Generic.Dictionary<ZDOID, long>();

        internal static void Reset() {
            elixirUntil = 0f;
        }

        /// <summary>Elixir of Seiðr: the damage bonus left by a recent mead, in percent.</summary>
        internal static float ElixirBonus() => Time.time < elixirUntil ? PowerSet.Get(Power.Elixir) : 0f;

        private static bool IsMead(ItemDrop.ItemData item) {
            return item != null && item.m_shared.m_food <= 0f && item.m_shared.m_consumeStatusEffect != null;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        private static class Drinking {
            private static void Prefix(Player __instance, ItemDrop.ItemData item) {
                drinking = __instance == Player.m_localPlayer && IsMead(item) ? item : null;
            }

            private static void Finalizer() { drinking = null; }

            private static void Postfix(Player __instance, Inventory inventory, ItemDrop.ItemData item, bool __result) {
                bool mead = drinking != null;
                drinking = null;
                if (!__result || !mead || __instance != Player.m_localPlayer) { return; }
                if (PowerSet.Has(Power.MeadSave, out float save) && Random.value * 100f < save && item.m_dropPrefab != null) {
                    inventory?.AddItem(item.m_dropPrefab, 1);
                    PowerFx.Effect(Power.MeadSave, VfxRole.Proc, __instance.transform.position + Vector3.up, 0.5f);
                }
                if (PowerSet.Has(Power.Elixir)) {
                    elixirUntil = Time.time + ElixirSeconds;
                    PowerFx.Show(__instance, PowerFx.Elixir, ElixirSeconds);
                    BlockPowers.CleanseOne(__instance);
                }
            }
        }

        // Völva's Brew and Quick Draught, on the mead's own copy of its effect as it starts.
        [HarmonyPatch(typeof(SE_Stats), nameof(SE_Stats.Setup))]
        private static class Brewing {
            private static void Prefix(SE_Stats __instance, Character character) {
                if (drinking == null || character != Player.m_localPlayer) { return; }
                // A healing mead's time is its cooldown; any other mead's time is how long it works.
                if (__instance.m_cooldownIcon) {
                    __instance.m_ttl *= 1f - PowerSet.Get(Power.MeadCooldown) / 100f;
                } else {
                    __instance.m_ttl *= 1f + PowerSet.Get(Power.MeadDuration) / 100f;
                }
                if (PowerSet.Has(Power.MeadStrength, out float stronger)) {
                    float factor = 1f + stronger / 100f;
                    __instance.m_healthUpFront *= factor;
                    __instance.m_healthOverTime *= factor;
                    __instance.m_staminaUpFront *= factor;
                    __instance.m_staminaOverTime *= factor;
                    __instance.m_eitrUpFront *= factor;
                    __instance.m_eitrOverTime *= factor;
                }
            }
        }

        // Twice-brewed: a fermenter the local player taps gives one more.
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Interact))]
        private static class Tapping {
            private static void Prefix(Fermenter __instance, Humanoid user, bool hold) {
                if (hold || user != Player.m_localPlayer || !PowerSet.Has(Power.TwiceBrewed)) { return; }
                if (__instance.GetStatus() != Fermenter.Status.Ready || !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false)) { return; }
                // A batch is known by when it started; pressing again before the owner empties it gives nothing more.
                ZDO zdo = __instance.m_nview.GetZDO();
                long batch = zdo.GetLong(ZDOVars.s_startTime, 0L);
                if (tapped.TryGetValue(zdo.m_uid, out long known) && known == batch) { return; }
                tapped[zdo.m_uid] = batch;
                Fermenter.ItemConversion conversion = __instance.GetItemConversion(__instance.GetContent());
                if (conversion?.m_to == null || __instance.m_outputPoint == null) { return; }
                Vector3 at = __instance.m_outputPoint.position + Vector3.up * 0.3f;
                Object.Instantiate(conversion.m_to, at, Quaternion.identity);
                PowerFx.Effect(Power.TwiceBrewed, VfxRole.Harvest, at);
            }
        }
    }
}
