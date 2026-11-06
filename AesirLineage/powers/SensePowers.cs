using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AesirLineage.starmap;

namespace AesirLineage.powers {
    /// <summary>
    /// Vör's senses: temporary minimap pins for ore, chests, dungeon entrances and rare creatures nearby,
    /// a pin on the last creature hit, and wider or periodic map exploring. Pins are local and never saved.
    /// </summary>
    internal static class SensePowers {
        private const float ScanEvery = 3f;
        private const float TrackEvery = 1f;
        private const float TrackFor = 60f;
        private const float RavenEvery = 60f;
        // Two stars or more counts as rare.
        private const int RareLevel = 3;

        private static float nextScan;
        private static float nextTrack;
        private static float nextRaven;
        private static readonly Dictionary<Object, Minimap.PinData> orePins = new Dictionary<Object, Minimap.PinData>();
        private static readonly Dictionary<Object, Minimap.PinData> seeressPins = new Dictionary<Object, Minimap.PinData>();
        private static Minimap.PinData trackPin;
        private static readonly Collider[] hits = new Collider[512];

        internal static void Reset() {
            ClearPins(orePins);
            ClearPins(seeressPins);
            RemovePin(ref trackPin);
        }

        internal static void Tick(Player player) {
            if (Minimap.instance == null) { return; }
            if (Time.time >= nextScan) {
                nextScan = Time.time + ScanEvery;
                ScanOre(player);
                ScanSeeress(player);
            }
            if (Time.time >= nextTrack) {
                nextTrack = Time.time + TrackEvery;
                UpdateTrack();
            }
            if (PowerSet.Has(Power.RavensEye, out float ravenRadius) && Time.time >= nextRaven) {
                nextRaven = Time.time + RavenEvery;
                Minimap.instance.Explore(player.transform.position, ravenRadius);
            }
        }

        // ---- Ore Sense ----

        private static void ScanOre(Player player) {
            if (!PowerSet.Has(Power.OreSense, out float radius)) {
                ClearPins(orePins);
                return;
            }
            HashSet<Object> found = new HashSet<Object>();
            int count = Physics.OverlapSphereNonAlloc(player.transform.position, radius, hits, LayerMask.GetMask("static_solid", "Default", "Default_small", "piece"));
            for (int i = 0; i < count; i++) {
                Collider collider = hits[i];
                if (collider == null) { continue; }
                Component deposit = OreDeposit(collider);
                if (deposit == null || !found.Add(deposit)) { continue; }
                if (!orePins.ContainsKey(deposit)) {
                    orePins[deposit] = Minimap.instance.AddPin(deposit.transform.position, Minimap.PinType.Icon2,
                        Localization.instance.Localize("$al_pin_ore"), save: false, isChecked: false);
                }
            }
            Prune(orePins, found);
        }

        private static Component OreDeposit(Collider collider) {
            MineRock5 rock5 = collider.GetComponentInParent<MineRock5>();
            if (rock5 != null) { return HasOre(rock5.m_dropItems) ? rock5 : null; }
            MineRock rock = collider.GetComponentInParent<MineRock>();
            if (rock != null) { return HasOre(rock.m_dropItems) ? rock : null; }
            DropOnDestroyed drop = collider.GetComponentInParent<DropOnDestroyed>();
            if (drop != null && drop.GetComponent<Destructible>() != null) { return HasOre(drop.m_dropWhenDestroyed) ? drop : null; }
            return null;
        }

        private static bool HasOre(DropTable table) {
            if (table?.m_drops == null) { return false; }
            foreach (DropTable.DropData drop in table.m_drops) {
                string name = drop.m_item != null ? drop.m_item.name : "";
                if (name.EndsWith("Ore") || name.Contains("OreNew") || name.Contains("Scrap") || name == "Obsidian") { return true; }
            }
            return false;
        }

        // ---- The Seeress's Map ----

        private static void ScanSeeress(Player player) {
            if (!PowerSet.Has(Power.SeeressMap, out float radius)) {
                ClearPins(seeressPins);
                return;
            }
            Vector3 at = player.transform.position;
            HashSet<Object> found = new HashSet<Object>();

            int count = Physics.OverlapSphereNonAlloc(at, radius, hits, LayerMask.GetMask("piece", "Default", "static_solid", "Default_small"));
            for (int i = 0; i < count; i++) {
                Collider collider = hits[i];
                if (collider == null) { continue; }
                Container chest = collider.GetComponentInParent<Container>();
                if (chest != null && IsWorldChest(chest) && found.Add(chest)) {
                    Pin(seeressPins, chest, chest.transform.position, Minimap.PinType.Icon1, "$al_pin_chest");
                }
                Teleport entrance = collider.GetComponentInParent<Teleport>();
                if (entrance != null && found.Add(entrance)) {
                    Pin(seeressPins, entrance, entrance.transform.position, Minimap.PinType.Icon3, "$al_pin_dungeon");
                }
            }
            foreach (Character creature in Character.GetAllCharacters()) {
                if (creature == null || creature.IsPlayer() || creature.IsTamed() || creature.IsDead() || creature.GetLevel() < RareLevel) { continue; }
                if (Vector3.Distance(at, creature.transform.position) > radius || !found.Add(creature)) { continue; }
                Pin(seeressPins, creature, creature.transform.position, Minimap.PinType.Icon4, creature.m_name);
                seeressPins[creature].m_pos = creature.transform.position;
            }
            Prune(seeressPins, found);
        }

        // Chests that came with the world, not ones players built.
        private static bool IsWorldChest(Container chest) {
            Piece piece = chest.GetComponent<Piece>();
            return (piece == null || piece.GetCreator() == 0L) && chest.GetInventory() != null && chest.GetInventory().NrOfItems() > 0;
        }

        // ---- Tracks ----

        private static void UpdateTrack() {
            Character target = DamagePowers.LastTarget;
            bool keep = PowerSet.Has(Power.Tracks) && target != null && !target.IsDead() && Time.time - DamagePowers.LastTargetTime < TrackFor;
            if (!keep) {
                RemovePin(ref trackPin);
                return;
            }
            if (trackPin == null) {
                trackPin = Minimap.instance.AddPin(target.transform.position, Minimap.PinType.Icon4,
                    Localization.instance.Localize(target.m_name), save: false, isChecked: false);
            }
            trackPin.m_pos = target.transform.position;
        }

        // ---- Exploring ----

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(Vector3), typeof(float))]
        private static class WiderExplore {
            // Only the walking reveal uses m_exploreRadius; Raven's Eye and shared maps pass their own.
            private static void Prefix(Minimap __instance, ref float radius) {
                if (radius == __instance.m_exploreRadius && PowerSet.Has(Power.ExploreRadius, out float wider)) {
                    radius *= 1f + wider / 100f;
                }
            }
        }

        // ---- Pins ----

        private static void Pin(Dictionary<Object, Minimap.PinData> pins, Object key, Vector3 at, Minimap.PinType type, string name) {
            if (pins.ContainsKey(key)) { return; }
            pins[key] = Minimap.instance.AddPin(at, type, Localization.instance.Localize(name), save: false, isChecked: false);
        }

        private static void Prune(Dictionary<Object, Minimap.PinData> pins, HashSet<Object> keep) {
            List<Object> gone = new List<Object>();
            foreach (KeyValuePair<Object, Minimap.PinData> entry in pins) {
                if (entry.Key == null || !keep.Contains(entry.Key)) { gone.Add(entry.Key); }
            }
            foreach (Object key in gone) {
                Minimap.instance?.RemovePin(pins[key]);
                pins.Remove(key);
            }
        }

        private static void ClearPins(Dictionary<Object, Minimap.PinData> pins) {
            foreach (Minimap.PinData pin in pins.Values) { Minimap.instance?.RemovePin(pin); }
            pins.Clear();
        }

        private static void RemovePin(ref Minimap.PinData pin) {
            if (pin != null) { Minimap.instance?.RemovePin(pin); }
            pin = null;
        }
    }
}
