using HarmonyLib;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    /// <summary>Oaths that forbid a kind of weapon: it can't be equipped, and is put away when the oath is taken.</summary>
    internal static class WeaponOaths {
        private static bool Forbidden(ItemDrop.ItemData item) {
            if (item == null) { return false; }
            Skills.SkillType skill = item.m_shared.m_skillType;
            return (skill == Skills.SkillType.Bows && PowerSet.Has(Power.NoBows))
                || (skill == Skills.SkillType.Crossbows && PowerSet.Has(Power.NoCrossbows));
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class RefuseEquip {
            private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result) {
                if (__instance != Player.m_localPlayer || !Forbidden(item)) { return true; }
                __instance.Message(MessageHud.MessageType.Center, "$al_oath_forbids_weapon");
                __result = false;
                return false;
            }
        }

        internal static void EnforceOnLocalPlayer() {
            Player player = Player.m_localPlayer;
            if (player == null) { return; }
            if (Forbidden(player.m_leftItem)) { player.UnequipItem(player.m_leftItem); }
            if (Forbidden(player.m_rightItem)) { player.UnequipItem(player.m_rightItem); }
        }
    }
}
